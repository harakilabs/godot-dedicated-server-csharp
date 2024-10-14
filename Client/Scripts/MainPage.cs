using Godot;
using System;

namespace GodotMultiplayer.Client;

public partial class MainPage : Control
{
    private Button _connectButton;

    public override void _Ready()
    {
        _connectButton = GetNode<Button>("VBoxContainer/ConnectServerButton");

        _connectButton.Pressed += OnConnectButtonPressed;
    }

    private void OnConnectButtonPressed()
    {
        GetTree().ChangeSceneToFile("res://Pages/ConsolePage.tscn");
    }
}
