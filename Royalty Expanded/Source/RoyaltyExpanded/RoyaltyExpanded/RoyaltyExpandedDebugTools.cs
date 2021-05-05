using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace RoyaltyExpanded
{
    public static class RoyaltyExpandedDebugTools
    {
		public static void TryAttachPsychicFire(this Thing t, float fireSize)
		{
			if (!t.CanEverAttachFire())
			{
				return;
			}
			if (t.HasAttachment(ThingDef.Named("PsychicFire")))
			{
				return;
			}
			PsychicFire fire = (PsychicFire)ThingMaker.MakeThing(ThingDef.Named("PsychicFire"),null);
			fire.fireSize = fireSize;
			fire.AttachTo(t);
			GenSpawn.Spawn(fire, t.Position, t.Map, Rot4.North, WipeMode.Vanish, false);
			Pawn pawn = t as Pawn;
			if (pawn != null)
			{
				pawn.jobs.StopAll(false, true);
				pawn.records.Increment(RecordDefOf.TimesOnFire);
			}
		}

		
			// Token: 0x06001902 RID: 6402 RVA: 0x000905D8 File Offset: 0x0008E7D8
		[DebugAction("Royalty Expanded", null, actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void AttachPsychicFire()
		{
			foreach (Thing t in Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()).ToList<Thing>())
			{
				t.TryAttachPsychicFire(0.01f);
			}
		}
	}
}
