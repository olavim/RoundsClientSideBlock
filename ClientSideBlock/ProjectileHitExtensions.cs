using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ClientSideBlock
{
	internal static class ProjectileHitExtensions
	{
		internal class ProjectileHitExtraData
		{
			public bool HitPending { get; set; } = false;
			public Dictionary<int, bool> IsBlockingAnswered { get; } = new();
			public Dictionary<int, bool> IsBlocking { get; } = new();
		}

		private static ConditionalWeakTable<ProjectileHit, ProjectileHitExtraData> s_extraData = new();

		public static ProjectileHitExtraData GetExtraData(this ProjectileHit projectileHit)
		{
			return s_extraData.GetOrCreateValue(projectileHit);
		}
	}
}
