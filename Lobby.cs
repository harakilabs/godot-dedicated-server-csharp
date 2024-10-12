using Godot;
using NewGameProject;
using System;
using static Godot.MultiplayerApi;
using GDictionary = Godot.Collections.Dictionary;

public partial class Lobby : Node
{
    // Declaração dos sinais como eventos
    [Signal]
    public delegate void PlayerConnectedEventHandler(int peerId, GDictionary playerInfo);

    [Signal]
    public delegate void PlayerDisconnectedEventHandler(int peerId);

    [Signal]
    public delegate void ServerDisconnectedEventHandler();

    // Constantes
    public const int PORT = 7000;
    public const string DEFAULT_SERVER_IP = "127.0.0.1";
    public const int MAX_CONNECTIONS = 20;

    // Propriedade estática da instância
    public static Lobby Instance { get; private set; }

    // Variáveis
    private GDictionary players = new GDictionary();
    private GDictionary playerInfo = new GDictionary
    {
        { "Name", "PlayerName" }
    };
    private int playersLoaded = 0;

    public override void _Ready()
    {
        Instance = this;

        // Conectar os eventos de multiplayer aos métodos correspondentes
        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;
        Multiplayer.ConnectedToServer += OnConnectedToServer;
        Multiplayer.ConnectionFailed += OnConnectionFailed;
        Multiplayer.ServerDisconnected += OnServerDisconnected;
    }

    // Método para o cliente se conectar ao servidor
    public Error JoinGame(string address = "")
    {
        if (string.IsNullOrEmpty(address))
        {
            address = DEFAULT_SERVER_IP;
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

    // Método para criar um servidor
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
        players[1] = playerInfo;
        EmitSignal(nameof(PlayerConnected), 1, playerInfo);
        return Error.Ok;
    }

    // Método para remover o peer de multiplayer
    public void RemoveMultiplayerPeer()
    {
        Multiplayer.MultiplayerPeer = null;
    }

    // RPC para carregar a cena do jogo
    [Rpc(RpcMode.AnyPeer)]
    public void LoadGame(string gameScenePath)
    {
        GetTree().ChangeSceneToFile(gameScenePath);
    }

    // RPC para informar que o jogador carregou a cena do jogo
    [Rpc(RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void PlayerLoaded()
    {
        if (Multiplayer.IsServer())
        {
            playersLoaded += 1;
            if (playersLoaded == players.Count)
            {
                GetNode<Game>("/root/Game").StartGame();
                playersLoaded = 0;
            }
        }
    }

    // RPC para registrar um novo jogador
    [Rpc(RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void RegisterPlayer(GDictionary newPlayerInfo)
    {
        int newPlayerId = (int)Multiplayer.GetRemoteSenderId();
        players[newPlayerId] = newPlayerInfo;
        EmitSignal(nameof(PlayerConnected), newPlayerId, newPlayerInfo);
    }

    // Métodos de eventos de multiplayer

    private void OnPeerConnected(long id)
    {
        if (Multiplayer.IsServer())
        {
            RpcId(id, nameof(RegisterPlayer), playerInfo);
        }
    }

    private void OnPeerDisconnected(long id)
    {
        players.Remove(id);
        EmitSignal(nameof(PlayerDisconnected), (int)id);
    }

    private void OnConnectedToServer()
    {
        int peerId = (int)Multiplayer.GetUniqueId();
        players[peerId] = playerInfo;
        EmitSignal(nameof(PlayerConnected), peerId, playerInfo);
    }

    private void OnConnectionFailed()
    {
        Multiplayer.MultiplayerPeer = null;
        GD.Print("Falha na conexão ao servidor.");
    }

    private void OnServerDisconnected()
    {
        Multiplayer.MultiplayerPeer = null;
        players.Clear();
        EmitSignal(nameof(ServerDisconnected));
    }
}
