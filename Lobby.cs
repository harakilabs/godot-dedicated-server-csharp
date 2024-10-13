using Godot;
using System;
using static Godot.MultiplayerApi;
using GDictionary = Godot.Collections.Dictionary;

namespace NewGameProject
{
    public partial class Lobby : Node
    {
        public static Lobby Instance { get; private set; }

        // Declaração dos sinais como eventos
        [Signal]
        public delegate void PlayerConnectedEventHandler(int peerId, GDictionary playerInfo);

        [Signal]
        public delegate void PlayerDisconnectedEventHandler(int peerId);

        [Signal]
        public delegate void ServerDisconnectedEventHandler();

        // Constantes
        public const int PORT = 7000;
        public const int MAX_CONNECTIONS = 20;

        // Variáveis
        private GDictionary players = new GDictionary();
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
            if (Instance == null)
            {
                Instance = this;
                // Não remover este nó ao mudar de cena
                AddToGroup("persist");
            }

            // Referenciar elementos da UI
            statusLabel = GetNode<Label>("CanvasLayer/UI/VBoxContainer/StatusLabel");
            startGameButton = GetNode<Button>("CanvasLayer/UI/VBoxContainer/StartGameButton");

            if (GameSettings.Instance.IsServer)
            {
                // O jogador é o servidor
                statusLabel.Text = "Servidor criado. Aguardando jogadores...";
                startGameButton.Visible = false; // inicialmente oculto, até ter pelo menos um client
                // Iniciar o servidor
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
                // O jogador é um cliente
                statusLabel.Text = $"Conectando ao servidor {GameSettings.Instance.ServerIP}:{PORT}...";
                // Tentar conectar ao servidor
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

            // Conectar eventos de multiplayer
            Multiplayer.PeerConnected += OnPeerConnected;
            Multiplayer.PeerDisconnected += OnPeerDisconnected;
            Multiplayer.ConnectedToServer += OnConnectedToServer;
            Multiplayer.ConnectionFailed += OnConnectionFailed;
            Multiplayer.ServerDisconnected += OnServerDisconnected;

            // Conectar sinal de StartGameButton
            if (GameSettings.Instance.IsServer)
            {
                startGameButton.Pressed += OnStartGamePressed;
            }
        }

        // Método para criar o servidor
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
            players[1] = playerInfo; // ID do servidor é sempre 1
            EmitSignal(nameof(PlayerConnected), 1, playerInfo);
            return Error.Ok;
        }

        // Método para o cliente se conectar ao servidor
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

        // RPC para carregar a cena do jogo
        [Rpc(RpcMode.AnyPeer)]
        public void LoadGame(string gameScenePath)
        {
            GD.Print($"Carregando cena do jogo: {gameScenePath}");
            GetTree().ChangeSceneToFile(gameScenePath);
        }

        // RPC para informar que o jogador carregou a cena do jogo
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

        // RPC para registrar um novo jogador
        [Rpc(RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        public void RegisterPlayer(GDictionary newPlayerInfo)
        {
            int newPlayerId = (int)Multiplayer.GetRemoteSenderId();
            players[newPlayerId] = newPlayerInfo;
            GD.Print($"[Cliente {Multiplayer.GetUniqueId()}] Registrando novo jogador com ID: {newPlayerId}");
            EmitSignal(nameof(PlayerConnected), newPlayerId, newPlayerInfo);

            // Se houver pelo menos um cliente, habilitar o botão de iniciar o jogo
            if (GameSettings.Instance.IsServer && players.Count >= 2)
            {
                startGameButton.Visible = true;
                statusLabel.Text = "Pelo menos um jogador conectado. Pode iniciar o jogo.";
            }
        }

        [Rpc(RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        public void RegisterPlayer(GDictionary newPlayerInfo)
        {
            // Este código será executado apenas no servidor
            int newPlayerId = (int)Multiplayer.GetRemoteSenderId();
            players[newPlayerId] = newPlayerInfo;
            GD.Print($"[Servidor] Registrando novo jogador com ID: {newPlayerId}");

            // Emitir sinal no servidor, se necessário
            EmitSignal(nameof(PlayerConnected), newPlayerId, newPlayerInfo);

            // Se houver pelo menos um cliente, habilitar o botão de iniciar o jogo
            if (players.Count >= 2)
            {
                startGameButton.Visible = true;
                statusLabel.Text = "Pelo menos um jogador conectado. Pode iniciar o jogo.";
            }

            // Notificar os clientes sobre o novo jogador
            Rpc(nameof(OnPlayerRegistered), newPlayerId, newPlayerInfo);
        }


        // RPC para receber mensagem de chat
        [Rpc(RpcMode.Authority)]
        public void ReceiveChatMessage(string message)
        {
            if (Multiplayer.IsServer())
            {
                // Loga no servidor
                GD.Print($"Mensagem recebida do cliente {Multiplayer.GetRemoteSenderId()}: {message}");

                // Envia para todos os Game nodes
                foreach (Node gameNode in GetTree().GetNodesInGroup("Game"))
                {
                    gameNode.Rpc(nameof(Game.ReceiveBroadcastMessage), message);
                }
            }
        }

        // Método para iniciar o jogo
        public void StartGame()
        {
            string gameScenePath = "res://Scenes/Game.tscn"; // Ajuste o caminho conforme necessário
            Rpc(nameof(LoadGame), gameScenePath);
        }

        // Métodos de eventos de multiplayer
        private void OnPeerConnected(long id)
        {
            if (Multiplayer.IsServer())
            {
                GD.Print($"[Servidor] Peer conectado com ID: {id}");
                RpcId(id, nameof(RegisterPlayer), playerInfo);
            }
        }

        private void OnPeerDisconnected(long id)
        {
            players.Remove(id);
            EmitSignal(nameof(PlayerDisconnected), (int)id);
            GD.Print($"[Servidor] Peer desconectado com ID: {id}");

            // Se for servidor, verificar se ainda há pelo menos um client
            if (GameSettings.Instance.IsServer)
            {
                if (players.Count < 2)
                {
                    // Menos de um client
                    startGameButton.Visible = false;
                    statusLabel.Text = "Aguardando jogadores...";
                }
            }
        }

        private void OnConnectedToServer()
        {
            int peerId = (int)Multiplayer.GetUniqueId();
            players[peerId] = playerInfo;
            GD.Print($"[Cliente {peerId}] Conectado ao servidor. players.Count: {players.Count}");
            EmitSignal(nameof(PlayerConnected), peerId, playerInfo);
        }

        private void OnConnectionFailed()
        {
            Multiplayer.MultiplayerPeer = null;
            GD.Print("Falha na conexão ao servidor.");
            statusLabel.Text = "Falha na conexão ao servidor.";
        }

        private void OnServerDisconnected()
        {
            Multiplayer.MultiplayerPeer = null;
            players.Clear();
            EmitSignal(nameof(ServerDisconnected));
            GD.Print("Servidor desconectado.");
            statusLabel.Text = "Servidor desconectado.";
        }

        private void OnStartGamePressed()
        {
            GD.Print("Iniciando o jogo...");
            StartGame();
        }
    }
}
