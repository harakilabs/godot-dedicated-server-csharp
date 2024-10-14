// ClientLobby.cs
using Godot;
using NewGameProject.Server;
using System;
using System.Collections.Generic;
using static Godot.MultiplayerApi;
using static Godot.MultiplayerPeer;
using GDictionary = Godot.Collections.Dictionary;

namespace NewGameProject.Client
{
	public partial class ClientLobby : Node
	{
		[Signal]
		public delegate void PlayerConnectedEventHandler(int peerId, GDictionary playerInfo);

		[Signal]
		public delegate void PlayerDisconnectedEventHandler(int peerId);

		[Signal]
		public delegate void ServerDisconnectedEventHandler();

		public const int PORT = 7000;
		public const int MAX_CONNECTIONS = 20;

		private Dictionary<int, GDictionary> players = new Dictionary<int, GDictionary>();
		private List<int> playerOrder = new List<int>(); // To track the order of players
		private GDictionary playerInfo = new GDictionary
		{
			{ "Name", "PlayerName" }
		};
		private int playersLoaded = 0;

		// UI Elements
		private Label statusLabel;
		private Button startGameButton;

		public override void _Ready()
		{
			// Initialize the singleton instance if needed
			if (Instance == null)
			{
				Instance = this;
				AddToGroup("persist");
			}

			// Reference UI elements
			var ui = GetNode<Control>("CanvasLayer/UI");
			statusLabel = ui.GetNode<Label>("VBoxContainer/StatusLabel");
			startGameButton = ui.GetNode<Button>("VBoxContainer/StartGameButton");

			// Hide the "Start Game" button initially
			startGameButton.Visible = false;

			// Connect button signals
			startGameButton.Pressed += OnStartGamePressed;

			// Update status label
			statusLabel.Text = "Connecting to server...";

			// Connect multiplayer events
			Multiplayer.PeerConnected += OnPeerConnected;
			Multiplayer.PeerDisconnected += OnPeerDisconnected;
			Multiplayer.ConnectedToServer += OnConnectedToServer;
			Multiplayer.ConnectionFailed += OnConnectionFailed;
			Multiplayer.ServerDisconnected += OnServerDisconnected;

			// Attempt to join the game
			JoinGame(GameSettings.Instance.ServerIP);
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
				GD.Print("[client] Error connecting to server: ", error);
				statusLabel.Text = "Error connecting to server.";
				return error;
			}

			Multiplayer.MultiplayerPeer = peer;
			statusLabel.Text = $"Connected to server {address}:{PORT}. Awaiting other players...";
			return Error.Ok;
		}

		[Rpc(RpcMode.AnyPeer, TransferMode = TransferModeEnum.Reliable)]
		public void RegisterPlayer(GDictionary newPlayerInfo)
		{
			int newPlayerId = (int)Multiplayer.GetRemoteSenderId();
			players[newPlayerId] = newPlayerInfo;
			playerOrder.Add(newPlayerId);

			GD.Print($"[client] Registering new player with ID: {newPlayerId}");

			bool isFirstPlayer = playerOrder.Count == 1; // Check if this is the first client

			// Notify the player if they are the first
			RpcId(newPlayerId, nameof(SetFirstPlayerStatus), isFirstPlayer);

			// Notify all clients about the new player
			Rpc(nameof(OnPlayerRegistered), newPlayerId, newPlayerInfo);
		}

		[Rpc(RpcMode.AnyPeer, TransferMode = TransferModeEnum.Reliable)]
		public void SetFirstPlayerStatus(bool isFirstPlayer)
		{
			if (isFirstPlayer)
			{
				GD.Print("[client] I am the first player. Enabling the start game button.");
				startGameButton.Visible = true;
			}
			else
			{
				GD.Print("[client] I am not the first player.");
				startGameButton.Visible = false;
			}
		}

		[Rpc(RpcMode.AnyPeer, TransferMode = TransferModeEnum.Reliable)]
		public void OnPlayerRegistered(int newPlayerId, GDictionary newPlayerInfo)
		{
			if (!Multiplayer.IsServer())
			{
				players[newPlayerId] = newPlayerInfo;
				GD.Print($"[client] New player registered with ID: {newPlayerId}");
				EmitSignal(nameof(PlayerConnected), newPlayerId, newPlayerInfo);
			}
		}

		[Rpc(RpcMode.AnyPeer, TransferMode = TransferModeEnum.Reliable)]
		public void LoadGame(string gameScenePath)
		{
			GD.Print("[client] Loading game scene: " + gameScenePath);
			GetTree().ChangeSceneToFile(gameScenePath);
		}

		[Rpc(RpcMode.AnyPeer, TransferMode = TransferModeEnum.Reliable)]
		public void PlayerLoaded()
		{
			if (Multiplayer.IsServer())
			{
				playersLoaded += 1;
				GD.Print($"[client] PlayerLoaded called. playersLoaded: {playersLoaded}, players.Count: {players.Count}");
				if (playersLoaded == players.Count)
				{
					GD.Print("[client] All players have loaded the scene. Starting the game.");
					StartGame();
					playersLoaded = 0;
				}
			}
			else
			{
				GD.Print($"[client] PlayerLoaded called on client (should not happen).");
			}
		}

		[Rpc(RpcMode.Authority, TransferMode = TransferModeEnum.Reliable)]
		public void ReceiveChatMessage(string message)
		{
			if (Multiplayer.IsServer())
			{
				GD.Print($"[client] Message received from server: {message}");
				// Implement chat message handling on the client side if needed
			}
		}

		// Multiplayer event handlers
		private void OnPeerConnected(long peerId)
		{
			GD.Print($"[client] Peer connected with ID: {peerId}");
		}

		private void OnPeerDisconnected(long peerId)
		{
			GD.Print($"[client] Peer disconnected with ID: {peerId}");
			EmitSignal(nameof(PlayerDisconnected), (int)peerId);
		}

		private void OnConnectedToServer()
		{
			GD.Print($"[client] Connected to server.");
			// Send registration to the server
			RpcId(1, nameof(RegisterPlayer), playerInfo);
		}

		private void OnConnectionFailed()
		{
			GD.Print("[client] Failed to connect to server.");
			statusLabel.Text = "Failed to connect to server.";
		}

		private void OnServerDisconnected()
		{
			GD.Print("[client] Server disconnected.");
			statusLabel.Text = "Server disconnected.";
			EmitSignal(nameof(ServerDisconnectedEventHandler));
		}

		private void OnStartGamePressed()
		{
			GD.Print("[client] Start game button pressed.");
			if (Multiplayer.IsServer())
			{
				StartGame();
			}
			else
			{
				// Send request to the server to start the game
				RpcId(1, nameof(ServerLobby.StartGame));
			}
		}

		[Rpc(RpcMode.Authority, TransferMode = TransferModeEnum.Reliable)]
		public void StartGame()
		{
			string gameScenePath = "res://Scenes/Game.tscn"; // Adjust the path as necessary
			Rpc(nameof(LoadGame), gameScenePath);
			GD.Print("[client] StartGame RPC called.");
		}

		// Singleton Instance
		public static ClientLobby Instance { get; private set; }
	}
}
