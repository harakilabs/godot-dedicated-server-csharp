using Godot;
using System;

namespace NewGameProject
{
    public partial class Main : Node
    {
        private Button createServerButton;
        private Button connectServerButton;
        private LineEdit ipAddressInput;

        public override void _Ready()
        {
            // Referenciar elementos da UI
            var ui = GetNode<Control>("Control");
            createServerButton = ui.GetNode<Button>("VBoxContainer/CreateServerButton");
            connectServerButton = ui.GetNode<Button>("VBoxContainer/ConnectServerButton");
            ipAddressInput = ui.GetNode<LineEdit>("VBoxContainer/IPAddressInput");

            // Conectar sinais dos botões
            createServerButton.Pressed += OnCreateServerPressed;
            connectServerButton.Pressed += OnConnectServerPressed;
        }

        private void OnCreateServerPressed()
        {
            // Define que este jogador será o servidor
            GameSettings.Instance.IsServer = true;

            // Navega para a cena do Lobby
            GetTree().ChangeSceneToFile("res://Scenes/Lobby.tscn");
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

            // Navega para a cena do Lobby
            GetTree().ChangeSceneToFile("res://Scenes/Lobby.tscn");
        }
    }
}
