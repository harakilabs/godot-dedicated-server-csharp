using Godot;
using NewGameProject.Gameplay;
using System;
using System.Collections.Generic;
using static Godot.MultiplayerApi;
using GDictionary = Godot.Collections.Dictionary;

namespace NewGameProject.Client
{
	public partial class Lobby : Node
	{
		public static Lobby Instance { get; private set; }

		[Signal]
		public delegate void PlayerConnectedEventHandler(int peerId, GDictionary playerInfo);

		[Signal]
		public delegate void PlayerDisconnectedEventHandler(int peerId);

		[Signal]
		public delegate void ServerDisconnectedEventHandler();

		public const int PORT = 7000;
		public const int MAX_CONNECTIONS = 20;

		private Dictionary<int, GDictionary> players = new Dictionary<int, GDictionary>();
		private List<int> playerOrder = new List<int>(); // Para rastrear a ordem de entrada
		private GDictionary playerInfo = new GDictionary
		{
			{ "Name", "PlayerName" }
		};
		private int playersLoaded = 0;

		private Label statusLabel;
		private Button startGameButton;

		public override void _Ready()
		{
			if (Instance == null)
			{
				Instance = this;
				AddToGroup("persist");
			}

			// Referenciar elementos da UI
			var ui = GetNode<Control>("CanvasLayer/UI");
			statusLabel = ui.GetNode<Label>("VBoxContainer/StatusLabel");
			startGameButton = ui.GetNode<Button>("VBoxContainer/StartGameButton");

			// Ocultar o botão "Iniciar Jogo" inicialmente
			startGameButton.Visible = false;

			statusLabel.Text = "Conectando...";

			// Conectar eventos de multiplayer
			Multiplayer.PeerConnected += OnPeerConnected;
			Multiplayer.PeerDisconnected += OnPeerDisconnected;
			Multiplayer.ConnectedToServer += OnConnectedToServer;
			Multiplayer.ConnectionFailed += OnConnectionFailed;
			Multiplayer.ServerDisconnected += OnServerDisconnected;

			// Verificar se é servidor ou cliente
			if (GameSettings.Instance.IsServer)
			{
				statusLabel.Text = "Servidor criado. Aguardando jogadores...";
				Error error = CreateGame();
				if (error != Error.Ok)
				{
					statusLabel.Text = "Erro ao iniciar o servidor.";
				}
				else
				{
					statusLabel.Text = $"Servidor iniciado na porta {PORT}. Aguardando jogadores...";
				}
			}
			else
			{
				statusLabel.Text = $"Conectando ao servidor {GameSettings.Instance.ServerIP}:{PORT}...";
				Error error = JoinGame(GameSettings.Instance.ServerIP);
				if (error != Error.Ok)
				{
					statusLabel.Text = "Erro ao conectar ao servidor.";
				}
				else
				{
					statusLabel.Text = "Conectado ao servidor. Aguardando outros jogadores...";
				}
			}
		}

		public Error CreateGame()
		{
			var peer = new ENetMultiplayerPeer();
			Error error = peer.CreateServer(PORT, MAX_CONNECTIONS);
			if (error != Error.Ok)
			{
				GD.Print("Erro ao criar servidor: ", error);
				return error;
			}

			Multiplayer.MultiplayerPeer = peer;

			// O servidor não é considerado um jogador
			GD.Print($"[Servidor] Servidor iniciado com ID: {Multiplayer.GetUniqueId()}");

			return Error.Ok;
		}

		public Error JoinGame(string address = "")
		{
			if (string.IsNullOrEmpty(address))
			{
				address = GameSettings.DEFAULT_SERVER_IP;
			}

			var peer = new ENetMultiplayerPeer();
			Error error = peer.CreateClient(address, PORT);
			if (error != Error.Ok)
			{
				GD.Print("Erro ao conectar: ", error);
				return error;
			}

			Multiplayer.MultiplayerPeer = peer;
			return Error.Ok;
		}

		[Rpc(RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
		public void RegisterPlayer(GDictionary newPlayerInfo)
		{
			int newPlayerId = (int)Multiplayer.GetRemoteSenderId();
			players[newPlayerId] = newPlayerInfo;
			playerOrder.Add(newPlayerId);

			GD.Print($"[Servidor] Registrando novo jogador com ID: {newPlayerId}");

			bool isFirstPlayer = playerOrder.Count == 1; // Verifica se é o primeiro cliente

			// Notifica o jogador se ele é o primeiro
			RpcId(newPlayerId, nameof(SetFirstPlayerStatus), isFirstPlayer);

			// Notifica todos os clientes sobre o novo jogador
			Rpc(nameof(OnPlayerRegistered), newPlayerId, newPlayerInfo);
		}

		[Rpc(RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
		public void SetFirstPlayerStatus(bool isFirstPlayer)
		{
			if (isFirstPlayer)
			{
				GD.Print($"[Peer {Multiplayer.GetUniqueId()}] Sou o primeiro jogador. Habilitando o botão de iniciar jogo.");

				startGameButton.Visible = true;
				startGameButton.Pressed += OnStartGamePressed;
			}
			else
			{
				GD.Print($"[Peer {Multiplayer.GetUniqueId()}] Não sou o primeiro jogador.");

				startGameButton.Visible = false;
			}
		}

		[Rpc(RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
		public void OnPlayerRegistered(int newPlayerId, GDictionary newPlayerInfo)
		{
			if (!Multiplayer.IsServer())
			{
				players[newPlayerId] = newPlayerInfo;
				GD.Print($"[Cliente {Multiplayer.GetUniqueId()}] Novo jogador registrado com ID: {newPlayerId}");
				EmitSignal(nameof(PlayerConnected), newPlayerId, newPlayerInfo);
			}
		}

		[Rpc(RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
		public void LoadGame(string gameScenePath)
		{
			GD.Print($"Carregando cena do jogo: {gameScenePath}");
			GetTree().ChangeSceneToFile(gameScenePath);
		}

		[Rpc(RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
		public void PlayerLoaded()
		{
			if (Multiplayer.IsServer())
			{
				playersLoaded += 1;
				GD.Print($"[Servidor] PlayerLoaded chamado. playersLoaded: {playersLoaded}, players.Count: {players.Count}");
				if (playersLoaded == players.Count)
				{
					GD.Print("[Servidor] Todos os jogadores carregaram a cena. Iniciando o jogo.");
					StartGame();
					playersLoaded = 0;
				}
			}
			else
			{
				GD.Print($"[Cliente {Multiplayer.GetUniqueId()}] PlayerLoaded chamado no cliente (não deveria acontecer).");
			}
		}

		[Rpc(RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
		public void ReceiveChatMessage(string message)
		{
			if (Multiplayer.IsServer())
			{
				GD.Print($"Mensagem recebida do cliente {Multiplayer.GetRemoteSenderId()}: {message}");

				foreach (Node gameNode in GetTree().GetNodesInGroup("Game"))
				{
					gameNode.Rpc(nameof(Game.ReceiveBroadcastMessage), message);
				}
			}
		}

		// Métodos de eventos de multiplayer
		private void OnPeerConnected(long peerId)
		{
			GD.Print($"[Servidor] Peer conectado com ID: {peerId}");
		}

		private void OnPeerDisconnected(long peerId)
		{
			GD.Print($"[Servidor] Peer desconectado com ID: {peerId}");
		}

		private void OnConnectedToServer()
		{
			GD.Print($"[Cliente {Multiplayer.GetUniqueId()}] Conectado ao servidor.");
			// Enviar solicitação de registro ao servidor
			RpcId(1, nameof(RegisterPlayer), playerInfo);
		}

		private void OnConnectionFailed()
		{
			GD.Print("Falha na conexão ao servidor.");
			statusLabel.Text = "Falha na conexão ao servidor.";
		}

		private void OnServerDisconnected()
		{
			GD.Print("Servidor desconectado.");
			statusLabel.Text = "Servidor desconectado.";
		}

		private void OnStartGamePressed()
		{
			GD.Print("Iniciando o jogo...");
			if (Multiplayer.IsServer())
			{
				StartGame();
			}
			else
			{
				// Enviar solicitação ao servidor para iniciar o jogo
				RpcId(1, nameof(StartGame), "");
			}
		}

		[Rpc(RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
		public void StartGame()
		{
			string gameScenePath = "res://Scenes/Game.tscn"; // Ajuste o caminho conforme necessário
			Rpc(nameof(LoadGame), gameScenePath);
		}
	}
}
