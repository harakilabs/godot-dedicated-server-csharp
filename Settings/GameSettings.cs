using Godot;
using System;

namespace NewGameProject
{
    public partial class GameSettings : Node
    {
        public static GameSettings Instance { get; private set; }

        public bool IsServer { get; set; } = false;
        public string ServerIP { get; set; } = "127.0.0.1";

        public const string DEFAULT_SERVER_IP = "127.0.0.1";

        public override void _Ready()
        {
            if (Instance == null)
            {
                Instance = this;
                // Não remover este nó ao mudar de cena
                AddToGroup("persist");
            }
        }
    }
}
