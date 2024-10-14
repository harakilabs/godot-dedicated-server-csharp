using Godot;
using NewGameProject.Client;
using System;
using static Godot.MultiplayerApi;

namespace NewGameProject.Gameplay
{
    public partial class Game : Node
    {
        [Signal]
        public delegate void ChatMessageReceivedEventHandler(string message);

        private LineEdit chatInput;
        private TextEdit chatHistory;

        public override void _Ready()
        {
            GD.Print("Cena do jogo carregada.");
            if (!Multiplayer.IsServer())
            {
                GD.Print($"[Cliente {Multiplayer.GetUniqueId()}] Informando ao servidor que a cena do jogo foi carregada.");
                ClientLobby.Instance.RpcId(1, nameof(ClientLobby.PlayerLoaded));
            }

            // Conectando o sinal 'ChatMessageReceived' ao método 'OnChatMessageReceived'
            Connect(nameof(ChatMessageReceived), new Callable(this, nameof(OnChatMessageReceived)));

            // Referenciando os elementos da interface de chat
            chatInput = GetNode<LineEdit>("CanvasLayer/VBoxContainer/ChatInput");
            chatHistory = GetNode<TextEdit>("CanvasLayer/VBoxContainer/ChatHistory");

            // Conectando o sinal de submissão de texto no campo de entrada
            chatInput.TextSubmitted += OnChatInputSubmitted;

            // Registrar este node no grupo "Game" para facilitar broadcast
            AddToGroup("Game");
        }

        private void OnChatInputSubmitted(string new_text)
        {
            if (!string.IsNullOrEmpty(new_text))
            {
                GD.Print($"Enviando mensagem de chat: {new_text}");
                // Envia a mensagem para o servidor
                ClientLobby.Instance.RpcId(1, nameof(ClientLobby.ReceiveChatMessage), new_text);
                chatInput.Text = "";
            }
        }

        // Método RPC para receber mensagem de broadcast
        [Rpc(RpcMode.AnyPeer)]
        public void ReceiveBroadcastMessage(string message)
        {
            EmitSignal(nameof(ChatMessageReceived), message);
        }

        private void OnChatMessageReceived(string message)
        {
            // Atualiza o histórico de chat na interface
            chatHistory.Text += message + "\n";
        }

        // Este método será chamado pelo servidor para iniciar o jogo
        public void StartGame()
        {
            GD.Print("O jogo está começando!");
            // Aqui você pode adicionar lógica adicional para iniciar o jogo
        }
    }
}
