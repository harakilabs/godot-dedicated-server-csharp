// DedicatedServer.cs
using Godot;
using NewGameProject.Gameplay;
using System;
using System.Collections.Generic;
using static Godot.MultiplayerApi;
using GDictionary = Godot.Collections.Dictionary;

namespace NewGameProject.Server
{
	public partial class DedicatedServer : Node
	{
		[Signal]
		public delegate void PlayerConnectedEventHandler(int peerId, GDictionary playerInfo);

		[Signal]
		public delegate void PlayerDisconnectedEventHandler(int peerId);

		[Signal]
		public delegate void ServerStatusChangedEventHandler(string status);

		public const int PORT = 7000;
		public const int MAX_CONNECTIONS = 20;

		private Dictionary<int, GDictionary> players = new Dictionary<int, GDictionary>();
		private List<int> playerOrder = new List<int>();
		private Button startGameButton;

		public override void _Ready()
		{
			StartServer();
		}

		private void StartServer()
		{
			var peer = new ENetMultiplayerPeer();
			Error error = peer.CreateServer(PORT, MAX_CONNECTIONS);
			if (error != Error.Ok)
			{
				GD.PrintErr("[server] Error creating server: ", error);
				return;
			}

			Multiplayer.MultiplayerPeer = peer;

			// Connect multiplayer events
			Multiplayer.PeerConnected += OnPeerConnected;
			Multiplayer.PeerDisconnected += OnPeerDisconnected;
		}

		private void OnPeerConnected(long peerId)
		{
			GD.Print($"[server] Peer connected with ID: {peerId}");
			GDictionary playerInfo = new GDictionary
			{
				{ "Name", $"Player{peerId}" }
			};
			players.Add((int)peerId, playerInfo);
			playerOrder.Add((int)peerId);

			EmitSignal(nameof(PlayerConnected), (int)peerId, playerInfo);

			bool isFirstPlayer = playerOrder.Count == 1;
			RpcId((int)peerId, nameof(SetFirstPlayerStatus), isFirstPlayer);
		}

		private void OnPeerDisconnected(long peerId)
		{
			GD.Print($"[server] Peer disconnected with ID: {peerId}");
			players.Remove((int)peerId);
			playerOrder.Remove((int)peerId);

			EmitSignal(nameof(PlayerDisconnected), (int)peerId);

			if (playerOrder.Count == 0)
			{
				startGameButton.Visible = false;
			}
			else if (playerOrder.Count >= 1)
			{
				int firstPlayerId = playerOrder[0];
				RpcId(firstPlayerId, nameof(SetFirstPlayerStatus), true);
			}
		}

		[Rpc(RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
		public void SetFirstPlayerStatus(bool isFirstPlayer)
		{
			if (isFirstPlayer)
			{
				GD.Print($"[server] Peer {Multiplayer.GetUniqueId()} is the first player. Enabling the start game button.");
				startGameButton.Visible = true;
			}
			else
			{
				GD.Print($"[server] Peer {Multiplayer.GetUniqueId()} is not the first player.");
				startGameButton.Visible = false;
			}
		}

		[Rpc(RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
		public void OnPlayerRegistered(int newPlayerId, GDictionary newPlayerInfo)
		{
			GD.Print($"[server] New player registered with ID: {newPlayerId}");
			// Notify all clients about the new player if needed
		}

		[Rpc(RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
		public void LoadGame(string gameScenePath)
		{
			GD.Print($"[server] Loading game scene: {gameScenePath}");
			GetTree().ChangeSceneToFile(gameScenePath);
		}

		[Rpc(RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
		public void ReceiveChatMessage(string message)
		{
			if (Multiplayer.IsServer())
			{
				GD.Print($"[server] Message received from client {Multiplayer.GetRemoteSenderId()}: {message}");

				foreach (Node gameNode in GetTree().GetNodesInGroup("Game"))
				{
					gameNode.Rpc(nameof(Game.ReceiveBroadcastMessage), message);
				}
			}
		}
	}
}
