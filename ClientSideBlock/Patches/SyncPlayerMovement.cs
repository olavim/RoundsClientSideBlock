using HarmonyLib;
using Photon.Pun;
using System;

namespace ClientSideBlock
{
	[HarmonyPatch(typeof(SyncPlayerMovement))]
	internal static class SyncPlayerMovementPatch
	{
		[HarmonyPatch("SendBlock", new Type[] { typeof(BlockTrigger.BlockTriggerType), typeof(bool) })]
		[HarmonyPostfix]
		private static void SendBlock1Postfix()
		{
			PhotonNetwork.SendAllOutgoingCommands();
		}

		[HarmonyPatch("SendBlock", new Type[] { typeof(BlockTrigger.BlockTriggerType), typeof(bool), typeof(bool) })]
		[HarmonyPostfix]
		private static void SendBlock2Postfix()
		{
			PhotonNetwork.SendAllOutgoingCommands();
		}
	}
}
