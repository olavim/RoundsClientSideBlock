using BepInEx;
using HarmonyLib;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnboundLib;
using UnboundLib.Networking;
using UnityEngine;

namespace ClientSideBlock
{
	[BepInPlugin(ModId, ModName, ModVersion)]
	public sealed class ClientSideBlock : BaseUnityPlugin
	{
		public const string ModId = "io.olavim.rounds.clientsideblock";
		public const string ModName = "ClientSideBlock";
		public const string ModVersion = ThisAssembly.Project.Version;

		internal static ClientSideBlock Instance { get; private set; }

		private void Awake()
		{
			Instance = this;

			var harmony = new Harmony(ModId);
			harmony.PatchAll();
		}
	}

	[HarmonyPatch(typeof(ProjectileHit))]
	internal static class ProjectileHitPatch
	{
		private class ProjectileHitExtraData
		{
			public bool HitPending { get; set; } = false;
		}
		private static readonly ConditionalWeakTable<ProjectileHit, ProjectileHitExtraData> s_extraData = new();
		private static readonly Dictionary<int, bool> s_isBlockingAnswered = new();
		private static readonly Dictionary<int, bool> s_isBlocking = new();

		[HarmonyPatch("Hit")]
		[HarmonyPrefix]
		private static bool HitPrefix(ProjectileHit __instance, MoveTransform ___move, List<HealthHandler> ___playersHit, HitInfo hit, bool forceCall)
		{
			var hitData = s_extraData.GetOrCreateValue(__instance);
			if (!hitData.HitPending)
			{
				hitData.HitPending = true;
				ClientSideBlock.Instance.StartCoroutine(HitCoroutine(__instance, ___move, ___playersHit, hit, forceCall));
			}

			return false;
		}

		private static IEnumerator HitCoroutine(ProjectileHit hit, MoveTransform move, List<HealthHandler> playersHit, HitInfo hitInfo, bool forceCall)
		{
			yield return DoHit(hit, move, playersHit, hitInfo, forceCall);
			s_extraData.GetOrCreateValue(hit).HitPending = false;
		}

		private static IEnumerator DoHit(ProjectileHit hit, MoveTransform move, List<HealthHandler> playersHit, HitInfo hitInfo, bool forceCall)
		{
			var healthHandler = hitInfo.transform?.GetComponent<HealthHandler>();
			if (healthHandler && playersHit.Contains(healthHandler))
			{
				yield break;
			}

			bool wasBlocked = false;
			var hitVelocity = (Vector2) move.velocity;

			int targetViewID = hitInfo.transform?.root.GetComponent<PhotonView>()?.ViewID ?? -1;
			int targetColliderIdx = -1;

			if (targetViewID == -1)
			{
				var colliders = MapManager.instance.currentMap.Map.GetComponentsInChildren<Collider2D>();
				targetColliderIdx = Array.FindIndex(colliders, c => c == hitInfo.collider);
			}

			if (healthHandler)
			{
				if (hit.view.IsMine)
				{
					yield return GetTargetBlocked(hit, targetViewID);
					wasBlocked = s_isBlocking[targetViewID];
				}

				hit.AddPlayerToHeld(healthHandler);
			}

			if (!hit.view.IsMine && !forceCall)
			{
				yield break;
			}

			if (hit.sendCollisions)
			{
				hit.view.RPC("RPCA_DoHit", RpcTarget.All, new object[]
				{
						hitInfo.point,
						hitInfo.normal,
						hitVelocity,
						targetViewID,
						targetColliderIdx,
						wasBlocked
				});
				PhotonNetwork.SendAllOutgoingCommands();
				yield break;
			}

			hit.RPCA_DoHit(hitInfo.point, hitInfo.normal, hitVelocity, targetViewID, targetColliderIdx, wasBlocked);
		}

		private static IEnumerator GetTargetBlocked(ProjectileHit hit, int targetViewID)
		{
			hit.gameObject.SetActive(false);
			s_isBlockingAnswered[targetViewID] = false;

			NetworkingManager.RPC(typeof(ProjectileHitPatch), nameof(RPC_AskIsBlocking), hit.view.ViewID, targetViewID);
			PhotonNetwork.SendAllOutgoingCommands();

			while (!s_isBlockingAnswered[targetViewID])
			{
				yield return null;
			}

			hit.gameObject.SetActive(true);
		}

		[UnboundRPC]
		private static void RPC_AskIsBlocking(int askerID, int viewID)
		{
			var view = PhotonNetwork.GetPhotonView(viewID);
			if (view.IsMine)
			{
				bool isBlocking = view.GetComponent<Block>().IsBlocking();
				NetworkingManager.RPC(typeof(ProjectileHitPatch), nameof(RPC_AnswerIsBlocking), askerID, viewID, isBlocking);
				PhotonNetwork.SendAllOutgoingCommands();
			}
		}

		[UnboundRPC]
		private static void RPC_AnswerIsBlocking(int askerID, int viewID, bool isBlocking)
		{
			if (PhotonNetwork.GetPhotonView(askerID).IsMine)
			{
				s_isBlockingAnswered[viewID] = true;
				s_isBlocking[viewID] = isBlocking;
			}
		}
	}
}