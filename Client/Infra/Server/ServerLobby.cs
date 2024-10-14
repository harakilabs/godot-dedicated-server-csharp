// ServerLobby.cs
using Godot;
using NewGameProject.Gameplay;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using static Godot.MultiplayerApi;
using GDictionary = Godot.Collections.Dictionary;

namespace NewGameProject.Server
{
	public partial class ServerLobby : Node
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
				EmitSignal(nameof(ServerStatusChanged), "Error starting the server.");
				return;
			}

			Multiplayer.MultiplayerPeer = peer;

			// Retrieve and log the server's IP addresses
			string serverIPs = GetLocalIPAddresses();
			GD.Print($"[server] Server started on IP(s): {serverIPs} and Port: {PORT}");

			EmitSignal(nameof(ServerStatusChanged), $"Server started on IP(s): {serverIPs} and Port: {PORT}");

			// Connect multiplayer events
			Multiplayer.PeerConnected += OnPeerConnected;
			Multiplayer.PeerDisconnected += OnPeerDisconnected;
		}

		private string GetLocalIPAddresses()
		{
			string ips = "";
			try
			{
				var host = Dns.GetHostEntry(Dns.GetHostName());
				foreach (var ip in host.AddressList)
				{
					if (ip.AddressFamily == AddressFamily.InterNetwork)
					{
						if (!string.IsNullOrEmpty(ips))
						{
							ips += ", ";
						}
						ips += ip.ToString();
					}
				}
				if (string.IsNullOrEmpty(ips))
				{
					ips = "127.0.0.1";
				}
			}
			catch (Exception ex)
			{
				GD.PrintErr($"[server] Error retrieving IP addresses: {ex.Message}");
				ips = "Unknown";
			}
			return ips;
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
				GD.Print("[server] No players remaining.");
			}
			else if (playerOrder.Count >= 1)
			{
				int firstPlayerId = playerOrder[0];
				RpcId(firstPlayerId, nameof(SetFirstPlayerStatus), true);
			}
		}

		[Rpc(RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
		public void SetFirstPlayerStatus(bool isFirstPlayer)
		{
			if (isFirstPlayer)
			{
				GD.Print("[server] Peer is the first player. Enabling the start game functionality.");
			}
			else
			{
				GD.Print("[server] Peer is not the first player.");
			}
		}

		[Rpc(RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
		public void OnPlayerRegistered(int newPlayerId, GDictionary newPlayerInfo)
		{
			GD.Print($"[server] New player registered with ID: {newPlayerId}");
			// Notify all clients about the new player if needed
		}

		public void StartGame()
		{
			string gameScenePath = "res://Scenes/Game.tscn"; // Adjust the path as necessary
			Rpc(nameof(LoadGame), gameScenePath);
			GD.Print("[server] StartGame RPC called.");
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
