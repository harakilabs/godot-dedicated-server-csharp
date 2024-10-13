// Main.cs
using Godot;
using System;

namespace NewGameProject.Client
{
    public partial class Main : Node
    {
        private Button createServerButton;
        private Button connectServerButton;
        private LineEdit ipAddressInput;

        public override void _Ready()
        {
            // Referenciar elementos da UI
            createServerButton = GetNode<Button>("Control/VBoxContainer/CreateServerButton");
            connectServerButton = GetNode<Button>("Control/VBoxContainer/ConnectServerButton");
            ipAddressInput = GetNode<LineEdit>("Control/VBoxContainer/IPAddressInput");

            // Conectar sinais dos botões
            createServerButton.Pressed += OnCreateServerPressed;
            connectServerButton.Pressed += OnConnectServerPressed;
        }

        private void OnCreateServerPressed()
        {
            // Define que este jogador será o servidor
            GameSettings.Instance.IsServer = true;

            // Navega para a cena DedicatedServer
            GetTree().ChangeSceneToFile("res://Scenes/DedicatedServer.tscn");
        }

        private void OnConnectServerPressed()
        {
            // Define que este jogador será um cliente
            GameSettings.Instance.IsServer = false;

            // Obtém o IP inserido pelo usuário
            string ipAddress = ipAddressInput.Text;
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = GameSettings.DEFAULT_SERVER_IP;
            }

            GameSettings.Instance.ServerIP = ipAddress;

            // Navega para a cena ClientLobby
            GetTree().ChangeSceneToFile("res://Scenes/Lobby.tscn");
        }
    }
}
