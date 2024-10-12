namespace NewGameProject;
using Godot;
using System;

public partial class Game : Node
{
    public override void _Ready()
    {
        if (!Multiplayer.IsServer())
        {
            // Informar ao servidor que este cliente carregou a cena do jogo
            Lobby.Instance.RpcId(1, nameof(Lobby.PlayerLoaded));
        }
    }

    // Chamado apenas no servidor para iniciar o jogo para todos os clientes
    public void StartGame()
    {
        GD.Print("Iniciando o jogo para todos os clientes.");
        // Adicione aqui a lógica para iniciar o jogo
        // Por exemplo, habilitar controles, iniciar física, etc.
    }
}
