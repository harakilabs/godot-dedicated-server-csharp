namespace NewGameProject;

using Godot;
using System;

public partial class DedicatedServer : Node
{
	private Lobby lobby;

	public override void _Ready()
	{
		// Inicializa o lobby como servidor
		lobby = new Lobby();
		AddChild(lobby);
		Error error = lobby.CreateGame();
		if (error != Error.Ok)
		{
			GD.Print("Erro ao iniciar o servidor.");
		}
		else
		{
			GD.Print($"Servidor iniciado na porta {Lobby.PORT}");
		}
	}
}
