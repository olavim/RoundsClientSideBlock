using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Photon.Pun;
using System.Collections;
using TMPro;
using UnboundLib;
using UnboundLib.Networking;
using UnboundLib.Utils.UI;
using UnityEngine;

namespace ClientSideBlock
{
	[BepInPlugin(ModId, ModName, ModVersion)]
	public sealed class ClientSideBlock : BaseUnityPlugin
	{
		public const string ModId = "io.olavim.rounds.clientsideblock";
		public const string ModName = "ClientSideBlock";
		public const string ModNameUI = "Client Side Block";
		public const string ModVersion = ThisAssembly.Project.Version;

		internal const int OptimisticSyncAdditionalDelay = 10;

		private static ConfigEntry<bool> s_optimisticSyncEnabledConfig { get; set; }

		private static readonly string[] OptimisticSyncDescription = {
			"Enable to make damaging and blocking feel more responsive for the shooter.",
			"Disable if players have highly fluctuating ping and you experience issues with blocking."
		};

		internal static bool OptimisticSyncEnabled { get; set; }

		internal static ClientSideBlock Instance { get; private set; }

		private void Awake()
		{
			Instance = this;

			var harmony = new Harmony(ModId);
			harmony.PatchAll();

			s_optimisticSyncEnabledConfig = this.Config.Bind(ModName, "optimisticSyncing", true, string.Join(" ", OptimisticSyncDescription));

			On.MainMenuHandler.Awake += (orig, self) =>
			{
				orig(self);
				OptimisticSyncEnabled = s_optimisticSyncEnabledConfig.Value;
			};
		}

		private void Start()
		{
			Unbound.RegisterMenu(ModNameUI, () => { }, this.BuildSettingsGUI, null, false);
			Unbound.RegisterHandshake(ModId, this.OnHandShakeCompleted);
		}

		private void BuildSettingsGUI(GameObject menu)
		{
			MenuHandler.CreateText(ModNameUI, menu, out TextMeshProUGUI _, 60);
			MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 24);

			MenuHandler.CreateText("Optimistic Syncing", menu, out TextMeshProUGUI _, 40, false);
			MenuHandler.CreateText(OptimisticSyncDescription[0], menu, out TextMeshProUGUI _, 24, false);
			MenuHandler.CreateText(OptimisticSyncDescription[1], menu, out TextMeshProUGUI _, 24, false);
			MenuHandler.CreateToggle(s_optimisticSyncEnabledConfig.Value, "Enable", menu, value => { s_optimisticSyncEnabledConfig.Value = value; OptimisticSyncEnabled = value; }, 30, false);
		}

		private void OnHandShakeCompleted()
		{
			if (PhotonNetwork.IsMasterClient)
			{
				NetworkingManager.RPC_Others(typeof(ClientSideBlock), nameof(SyncSettings), new object[] { OptimisticSyncEnabled });
			}
		}

		[UnboundRPC]
		private static void SyncSettings(bool optimisticSyncEnabled)
		{
			OptimisticSyncEnabled = optimisticSyncEnabled;
		}

		internal static IEnumerator GetTargetBlocked(ProjectileHit hit, int targetViewId)
		{
			return ClientSideBlock.OptimisticSyncEnabled
				? GetTargetBlockedOptimistic(hit, targetViewId)
				: GetTargetBlockedPessimistic(hit, targetViewId);
		}

		// Assume that if the target blocked, the information will arrive within about half the shooter's and target's ping
		private static IEnumerator GetTargetBlockedOptimistic(ProjectileHit hit, int targetViewId)
		{
			var targetView = PhotonNetwork.GetPhotonView(targetViewId);

			if (targetView.IsMine)
			{
				hit.GetExtraData().IsBlockingAnswered[targetViewId] = true;
				hit.GetExtraData().IsBlocking[targetViewId] = targetView.GetComponent<Block>().IsBlocking();
				yield break;
			}

			hit.GetExtraData().IsBlockingAnswered[targetViewId] = false;

			int myPing = (int) PhotonNetwork.LocalPlayer.CustomProperties["Ping"];
			int targetPing = (int) targetView.Owner.CustomProperties["Ping"];
			float delayToTarget = (myPing + targetPing) / 2f;

			yield return new WaitForSeconds((delayToTarget + ClientSideBlock.OptimisticSyncAdditionalDelay) / 1000f);

			hit.GetExtraData().IsBlockingAnswered[targetViewId] = true;
			hit.GetExtraData().IsBlocking[targetViewId] = targetView.GetComponent<Block>().IsBlocking();
		}

		// Ask the target if it's blocking and wait for the answer
		private static IEnumerator GetTargetBlockedPessimistic(ProjectileHit hit, int targetViewId)
		{
			hit.GetExtraData().IsBlockingAnswered[targetViewId] = false;

			NetworkingManager.RPC(typeof(ClientSideBlock), nameof(RPC_AskIsBlocking), hit.view.ViewID, targetViewId);
			PhotonNetwork.SendAllOutgoingCommands();

			while (!hit.GetExtraData().IsBlockingAnswered[targetViewId])
			{
				yield return null;
			}
		}

		[UnboundRPC]
		private static void RPC_AskIsBlocking(int projectileViewId, int targetViewId)
		{
			var view = PhotonNetwork.GetPhotonView(targetViewId);
			if (view.IsMine)
			{
				bool isBlocking = view.GetComponent<Block>().IsBlocking();
				NetworkingManager.RPC(typeof(ClientSideBlock), nameof(RPC_AnswerIsBlocking), projectileViewId, targetViewId, isBlocking);
				PhotonNetwork.SendAllOutgoingCommands();
			}
		}

		[UnboundRPC]
		private static void RPC_AnswerIsBlocking(int projectileViewId, int targetViewId, bool isBlocking)
		{
			var view = PhotonNetwork.GetPhotonView(projectileViewId);
			if (view.IsMine)
			{
				var hit = view.GetComponent<ProjectileHit>();
				hit.GetExtraData().IsBlockingAnswered[targetViewId] = true;
				hit.GetExtraData().IsBlocking[targetViewId] = isBlocking;
			}
		}
	}
}
