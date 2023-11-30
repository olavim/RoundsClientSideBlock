using BepInEx;
using HarmonyLib;
using Landfall.Network;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Linq;
using UnboundLib;
using UnityEngine;
using UnityEngine.UI;

namespace ClientSideBlock
{

#if DEBUG

	[BepInPlugin(ModId, ModName, ModVersion)]
	public sealed class ClientSideBlockDebug : BaseUnityPlugin
	{
		public const string ModId = ClientSideBlock.ModId + ".debug";
		public const string ModName = ClientSideBlock.ModName + "Debug";
		public const string ModVersion = "1.0.0";

		private void Start()
		{
			bool autoHost = Environment.GetCommandLineArgs().Any(arg => arg == "-autoHost");
			int connectArgIdx = Array.FindIndex(Environment.GetCommandLineArgs(), arg => arg == "-autoConnect");

			this.ExecuteAfterSeconds(1f, () =>
			{
				if (autoHost)
				{
					var onlineGo = GameObject.Find("/Game/UI/UI_MainMenu/Canvas/ListSelector/Online/Group/Invite friend");
					UnityEngine.Debug.Log(onlineGo);
					onlineGo.GetComponent<Button>().onClick.Invoke();
				}
				else if (connectArgIdx != -1)
				{
					var code = Environment.GetCommandLineArgs()[connectArgIdx + 1].Split(':');
					NetworkConnectionHandler.instance.ForceRegionJoin(code[0], code[1]);
				}
			});
		}
	}

	[HarmonyPatch(typeof(NetworkConnectionHandler))]
	internal static class Debug_NetworkConnectionHandlerPatch
	{
		[HarmonyPatch("CreateRoom")]
		[HarmonyPrefix]
		private static bool CreateRoomPrefix(RoomOptions roomOptions, ClientSteamLobby ___m_SteamLobby, ref bool ___m_ForceRegion)
		{
			if (___m_SteamLobby == null)
			{
				string roomName = "1234";
				int hostArgIdx = Array.FindIndex(Environment.GetCommandLineArgs(), arg => arg == "-autoHost");

				if (hostArgIdx != -1)
				{
					var code = Environment.GetCommandLineArgs()[hostArgIdx + 1].Split(':');
					RegionSelector.region = code[0];
					___m_ForceRegion = true;
					roomName = code[1];
				}

				PhotonNetwork.CreateRoom(roomName, roomOptions, null, null);
				return false;
			}

			return true;
		}
	}

#endif

}
