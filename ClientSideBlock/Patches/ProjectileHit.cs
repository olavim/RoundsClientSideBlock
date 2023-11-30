using HarmonyLib;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ClientSideBlock
{
	[HarmonyPatch(typeof(ProjectileHit))]
	internal static class ProjectileHitPatch
	{
		[HarmonyPatch("Hit")]
		[HarmonyPrefix]
		private static bool HitPrefix(ProjectileHit __instance, MoveTransform ___move, List<HealthHandler> ___playersHit, HitInfo hit, bool forceCall)
		{
			if (!__instance.GetExtraData().HitPending)
			{
				__instance.GetExtraData().HitPending = true;
				ClientSideBlock.Instance.StartCoroutine(HitCoroutine(__instance, ___move, ___playersHit, hit, forceCall));
			}

			return false;
		}

		private static IEnumerator HitCoroutine(ProjectileHit hit, MoveTransform move, List<HealthHandler> playersHit, HitInfo hitInfo, bool forceCall)
		{
			var healthHandler = hitInfo.transform?.GetComponent<HealthHandler>();
			if (healthHandler && playersHit.Contains(healthHandler))
			{
				hit.GetExtraData().HitPending = false;
				yield break;
			}

			bool wasBlocked = false;
			var origVelocity = (Vector2) move.velocity;

			if (hit.view.IsMine)
			{
				move.velocity = Vector2.zero;
			}

			int targetViewId = hitInfo.transform?.root.GetComponent<PhotonView>()?.ViewID ?? -1;
			int targetColliderIdx = -1;

			if (targetViewId == -1)
			{
				var colliders = MapManager.instance.currentMap.Map.GetComponentsInChildren<Collider2D>();
				targetColliderIdx = Array.FindIndex(colliders, c => c == hitInfo.collider);
			}

			if (healthHandler)
			{
				if (hit.view.IsMine)
				{
					hit.gameObject.SetActive(false);

					yield return ClientSideBlock.GetTargetBlocked(hit, targetViewId);
					wasBlocked = hit.GetExtraData().IsBlocking[targetViewId];

					hit.gameObject.SetActive(true);
				}

				hit.AddPlayerToHeld(healthHandler);
			}

			if (!hit.view.IsMine && !forceCall)
			{
				hit.GetExtraData().HitPending = false;
				yield break;
			}

			if (hit.sendCollisions)
			{
				hit.view.RPC("RPCA_DoHit", RpcTarget.All, new object[]
				{
						hitInfo.point,
						hitInfo.normal,
						origVelocity,
						targetViewId,
						targetColliderIdx,
						wasBlocked
				});
				PhotonNetwork.SendAllOutgoingCommands();
				yield break;
			}

			hit.RPCA_DoHit(hitInfo.point, hitInfo.normal, origVelocity, targetViewId, targetColliderIdx, wasBlocked);
		}

		[HarmonyPatch("RPCA_DoHit")]
		[HarmonyPostfix]
		private static void DoHitPostfix(ProjectileHit __instance)
		{
			__instance.GetExtraData().HitPending = false;
		}
	}
}
