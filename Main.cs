namespace NewGameProject;
using Godot;
using Godot.Collections;
using System;

public partial class Main : Node
{
	private Lobby lobby;
	private Button createServerButton;
	private Button connectServerButton;
	private LineEdit ipAddressInput;

	public override void _Ready()
	{
		lobby = GetNode<Lobby>("Lobby");

		// Referenciar elementos da UI
		var ui = GetNode<Control>("Control");
		createServerButton = ui.GetNode<Button>("VBoxContainer/CreateServerButton");
		connectServerButton = ui.GetNode<Button>("VBoxContainer/ConnectServerButton");
		ipAddressInput = ui.GetNode<LineEdit>("VBoxContainer/IPAddressInput");

		// Conectar sinais do Lobby para atualizar a UI ou lógica do jogo
		lobby.PlayerConnected += OnPlayerConnected;
		lobby.PlayerDisconnected += OnPlayerDisconnected;
		lobby.ServerDisconnected += OnServerDisconnected;

		// Conectar sinais dos botões usando eventos
		createServerButton.Pressed += OnCreateServerPressed;
		connectServerButton.Pressed += OnConnectServerPressed;
	}

	// Método chamado quando o botão "Criar Servidor" é pressionado
	private void OnCreateServerPressed()
	{
		Error error = lobby.CreateGame();
		if (error == Error.Ok)
		{
			GD.Print("Servidor criado com sucesso.");
		}
		else
		{
			GD.Print("Erro ao criar servidor: ", error);
		}
	}

	// Método chamado quando o botão "Conectar ao Servidor" é pressionado
	private void OnConnectServerPressed()
	{
		string ipAddress = ipAddressInput.Text;
		if (string.IsNullOrEmpty(ipAddress))
		{
			ipAddress = Lobby.DEFAULT_SERVER_IP; // IP padrão se nenhum for inserido
		}

		Error error = lobby.JoinGame(ipAddress);
		if (error == Error.Ok)
		{
			GD.Print("Conectado ao servidor.");
		}
		else
		{
			GD.Print("Erro ao conectar: ", error);
		}
	}

	// Manipulador para sinal de jogador conectado
	private void OnPlayerConnected(int peerId, Dictionary playerInfo)
	{
		GD.Print($"Jogador conectado: {peerId} - {playerInfo["Name"]}");
		// Atualize a UI ou lógica do jogo conforme necessário
	}

	// Manipulador para sinal de jogador desconectado
	private void OnPlayerDisconnected(int peerId)
	{
		GD.Print($"Jogador desconectado: {peerId}");
		// Atualize a UI ou lógica do jogo conforme necessário
	}

	// Manipulador para sinal de servidor desconectado
	private void OnServerDisconnected()
	{
		GD.Print("Servidor desconectado.");
		// Atualize a UI ou lógica do jogo conforme necessário
	}
}
