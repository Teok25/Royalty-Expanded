using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;
namespace RoyaltyExpanded
{
    public class PsychicFire : AttachableThing, ISizeReporter
    {
		// Token: 0x17000E50 RID: 3664
		// (get) Token: 0x0600517D RID: 20861 RVA: 0x001B7967 File Offset: 0x001B5B67
		public int TicksSinceSpawn
		{
			get
			{
				return this.ticksSinceSpawn;
			}
		}

		// Token: 0x17000E51 RID: 3665
		// (get) Token: 0x0600517E RID: 20862 RVA: 0x001B796F File Offset: 0x001B5B6F
		public override string Label
		{
			get
			{
				if (this.parent != null)
				{
					return "FireOn".Translate(this.parent.LabelCap, this.parent);
				}
				return this.def.label;
			}
		}

		// Token: 0x17000E52 RID: 3666
		// (get) Token: 0x0600517F RID: 20863 RVA: 0x001B79B0 File Offset: 0x001B5BB0
		public override string InspectStringAddon
		{
			get
			{
				return "Burning".Translate() + " (" + "FireSizeLower".Translate((this.fireSize * 100f).ToString("F0")) + ")";
			}
		}

		// Token: 0x17000E53 RID: 3667
		// (get) Token: 0x06005180 RID: 20864 RVA: 0x001B7A10 File Offset: 0x001B5C10
		private float SpreadInterval
		{
			get
			{
				float num = 150f - (this.fireSize - 1f) * 40f;
				if (num < 75f)
				{
					num = 75f;
				}
				return num;
			}
		}

		// Token: 0x06005181 RID: 20865 RVA: 0x001B7A45 File Offset: 0x001B5C45
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look<int>(ref this.ticksSinceSpawn, "ticksSinceSpawn", 0, false);
			Scribe_Values.Look<float>(ref this.fireSize, "fireSize", 0f, false);
		}

		// Token: 0x06005182 RID: 20866 RVA: 0x001B7A75 File Offset: 0x001B5C75
		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			this.RecalcPathsOnAndAroundMe(map);
			LessonAutoActivator.TeachOpportunity(ConceptDefOf.HomeArea, this, OpportunityType.Important);
			this.ticksSinceSpread = (int)(this.SpreadInterval * Rand.Value);
		}

		// Token: 0x06005183 RID: 20867 RVA: 0x001B7AA5 File Offset: 0x001B5CA5
		public float CurrentSize()
		{
			return this.fireSize;
		}

		// Token: 0x06005184 RID: 20868 RVA: 0x001B7AB0 File Offset: 0x001B5CB0
		public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
		{
			if (this.sustainer != null)
			{
				if (this.sustainer.externalParams.sizeAggregator == null)
				{
					this.sustainer.externalParams.sizeAggregator = new SoundSizeAggregator();
				}
				this.sustainer.externalParams.sizeAggregator.RemoveReporter(this);
			}
			Map map = base.Map;
			base.DeSpawn(mode);
			this.RecalcPathsOnAndAroundMe(map);
		}

		// Token: 0x06005185 RID: 20869 RVA: 0x001B7B18 File Offset: 0x001B5D18
		private void RecalcPathsOnAndAroundMe(Map map)
		{
			IntVec3[] adjacentCellsAndInside = GenAdj.AdjacentCellsAndInside;
			for (int i = 0; i < adjacentCellsAndInside.Length; i++)
			{
				IntVec3 c = base.Position + adjacentCellsAndInside[i];
				if (c.InBounds(map))
				{
					map.pathGrid.RecalculatePerceivedPathCostAt(c);
				}
			}
		}

		// Token: 0x06005186 RID: 20870 RVA: 0x001B7B64 File Offset: 0x001B5D64
		public override void AttachTo(Thing parent)
		{
			base.AttachTo(parent);
			Pawn pawn = parent as Pawn;
			if (pawn != null)
			{
				TaleRecorder.RecordTale(TaleDefOf.WasOnFire, new object[]
				{
					pawn
				});
			}
		}

		// Token: 0x06005187 RID: 20871 RVA: 0x001B7B98 File Offset: 0x001B5D98
		public override void Tick()
		{
			this.ticksSinceSpawn++;
			if (PsychicFire.lastFireCountUpdateTick != Find.TickManager.TicksGame)
			{
				PsychicFire.fireCount = base.Map.listerThings.ThingsOfDef(this.def).Count;
				PsychicFire.lastFireCountUpdateTick = Find.TickManager.TicksGame;
			}
			if (this.sustainer != null)
			{
				this.sustainer.Maintain();
			}
			else if (!base.Position.Fogged(base.Map))
			{
				SoundInfo info = SoundInfo.InMap(new TargetInfo(base.Position, base.Map, false), MaintenanceType.PerTick);
				this.sustainer = SustainerAggregatorUtility.AggregateOrSpawnSustainerFor(this, SoundDefOf.FireBurning, info);
			}
			this.ticksUntilSmoke--;
			if (this.ticksUntilSmoke <= 0)
			{
				this.SpawnSmokeParticles();
			}
			if (PsychicFire.fireCount < 15 && this.fireSize > 0.7f && Rand.Value < this.fireSize * 0.01f)
			{
				MoteMaker.ThrowMicroSparks(this.DrawPos, base.Map);
			}
			if (this.fireSize > 1f)
			{
				this.ticksSinceSpread++;
				if ((float)this.ticksSinceSpread >= this.SpreadInterval)
				{
					this.TrySpread();
					this.ticksSinceSpread = 0;
				}
			}
			if (this.IsHashIntervalTick(150))
			{
				this.DoComplexCalcs();
			}
			if (this.ticksSinceSpawn >= 7500)
			{
				this.TryBurnFloor();
			}
		}

		// Token: 0x06005188 RID: 20872 RVA: 0x001B7CFC File Offset: 0x001B5EFC
		private void SpawnSmokeParticles()
		{
			if (PsychicFire.fireCount < 15)
			{
				MoteMaker.ThrowSmoke(this.DrawPos, base.Map, this.fireSize);
			}
			if (this.fireSize > 0.5f && this.parent == null)
			{
				MoteMaker.ThrowFireGlow(base.Position, base.Map, this.fireSize);
			}
			float num = this.fireSize / 2f;
			if (num > 1f)
			{
				num = 1f;
			}
			num = 1f - num;
			this.ticksUntilSmoke = PsychicFire.SmokeIntervalRange.Lerped(num) + (int)(10f * Rand.Value);
		}

		// Token: 0x06005189 RID: 20873 RVA: 0x001B7D9C File Offset: 0x001B5F9C
		private void DoComplexCalcs()
		{
			bool flag = false;
			PsychicFire.flammableList.Clear();
			this.flammabilityMax = 0f;
			if (!base.Position.GetTerrain(base.Map).extinguishesFire)
			{
				if (this.parent == null)
				{
					if (base.Position.TerrainFlammableNow(base.Map))
					{
						this.flammabilityMax = base.Position.GetTerrain(base.Map).GetStatValueAbstract(StatDefOf.Flammability, null);
					}
					List<Thing> list = base.Map.thingGrid.ThingsListAt(base.Position);
					for (int i = 0; i < list.Count; i++)
					{
						Thing thing = list[i];
						if (thing is Building_Door)
						{
							flag = true;
						}
						float statValue = thing.GetStatValue(StatDefOf.Flammability, true);
						if (statValue >= 0.01f)
						{
							PsychicFire.flammableList.Add(list[i]);
							if (statValue > this.flammabilityMax)
							{
								this.flammabilityMax = statValue;
							}
							if (this.parent == null && this.fireSize > 0.4f && list[i].def.category == ThingCategory.Pawn && Rand.Chance(FireUtility.ChanceToAttachFireCumulative(list[i], 150f)))
							{
								list[i].TryAttachFire(this.fireSize * 0.2f);
							}
						}
					}
				}
				else
				{
					PsychicFire.flammableList.Add(this.parent);
					this.flammabilityMax = this.parent.GetStatValue(StatDefOf.Flammability, true);
				}
			}
			if (this.flammabilityMax < 0.01f)
			{
				this.Destroy(DestroyMode.Vanish);
				return;
			}
			Thing thing2;
			if (this.parent != null)
			{
				thing2 = this.parent;
			}
			else if (PsychicFire.flammableList.Count > 0)
			{
				thing2 = PsychicFire.flammableList.RandomElement<Thing>();
			}
			else
			{
				thing2 = null;
			}
			if (thing2 != null && (this.fireSize >= 0.4f || thing2 == this.parent || thing2.def.category != ThingCategory.Pawn))
			{
				this.DoFireDamage(thing2);
			}
			if (base.Spawned)
			{
				float num = this.fireSize * 160f;
				if (flag)
				{
					num *= 0.15f;
				}
				GenTemperature.PushHeat(base.Position, base.Map, num);
				if (Rand.Value < 0.4f)
				{
					float radius = this.fireSize * 3f;
					SnowUtility.AddSnowRadial(base.Position, base.Map, radius, -(this.fireSize * 0.1f));
				}
				this.fireSize += 0.00055f * this.flammabilityMax * 150f;
				if (this.fireSize > 1.75f)
				{
					this.fireSize = 1.75f;
				}
				if (base.Map.weatherManager.RainRate > 0.01f && this.VulnerableToRain() && Rand.Value < 6f)
				{
					base.TakeDamage(new DamageInfo(DamageDefOf.Extinguish, 10f, 0f, -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null));
				}
			}
		}

		// Token: 0x0600518A RID: 20874 RVA: 0x001B8083 File Offset: 0x001B6283
		private void TryBurnFloor()
		{
			if (this.parent != null || !base.Spawned)
			{
				return;
			}
			if (base.Position.TerrainFlammableNow(base.Map))
			{
				base.Map.terrainGrid.Notify_TerrainBurned(base.Position);
			}
		}

		// Token: 0x0600518B RID: 20875 RVA: 0x001B80C0 File Offset: 0x001B62C0
		private bool VulnerableToRain()
		{
			if (!base.Spawned)
			{
				return false;
			}
			RoofDef roofDef = base.Map.roofGrid.RoofAt(base.Position);
			if (roofDef == null)
			{
				return true;
			}
			if (roofDef.isThickRoof)
			{
				return false;
			}
			Thing edifice = base.Position.GetEdifice(base.Map);
			return edifice != null && edifice.def.holdsRoof;
		}

		// Token: 0x0600518C RID: 20876 RVA: 0x001B8120 File Offset: 0x001B6320
		private void DoFireDamage(Thing targ)
		{
			int num = GenMath.RoundRandom(Mathf.Clamp(0.0125f + 0.0036f * this.fireSize, 0.0125f, 0.05f) * 150f);
			if (num < 1)
			{
				num = 1;
			}
			Pawn pawn = targ as Pawn;
			if (pawn != null)
			{
				BattleLogEntry_DamageTaken battleLogEntry_DamageTaken = new BattleLogEntry_DamageTaken(pawn, RulePackDefOf.DamageEvent_Fire, null);
				Find.BattleLog.Add(battleLogEntry_DamageTaken);
				DamageInfo dinfo = new DamageInfo(DamageDefOf.Flame, (float)num, 0f, -1f, this, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null);
				dinfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
				targ.TakeDamage(dinfo).AssociateWithLog(battleLogEntry_DamageTaken);
				Apparel apparel;
				if (pawn.apparel != null && pawn.apparel.WornApparel.TryRandomElement(out apparel))
				{
					apparel.TakeDamage(new DamageInfo(DamageDefOf.Flame, (float)num, 0f, -1f, this, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null));
					return;
				}
			}
			else
			{
				targ.TakeDamage(new DamageInfo(DamageDefOf.Flame, (float)num, 0f, -1f, this, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null));
			}
		}

		// Token: 0x0600518D RID: 20877 RVA: 0x001B821C File Offset: 0x001B641C
		protected void TrySpread()
		{
			IntVec3 intVec = base.Position;
			bool flag;
			if (Rand.Chance(0.8f))
			{
				intVec = base.Position + GenRadial.ManualRadialPattern[Rand.RangeInclusive(1, 8)];
				flag = true;
			}
			else
			{
				intVec = base.Position + GenRadial.ManualRadialPattern[Rand.RangeInclusive(10, 20)];
				flag = false;
			}
			if (!intVec.InBounds(base.Map))
			{
				return;
			}
			if (Rand.Chance(FireUtility.ChanceToStartFireIn(intVec, base.Map)))
			{
				if (!flag)
				{
					CellRect startRect = CellRect.SingleCell(base.Position);
					CellRect endRect = CellRect.SingleCell(intVec);
					if (!GenSight.LineOfSight(base.Position, intVec, base.Map, startRect, endRect, null))
					{
						return;
					}
					((Spark)GenSpawn.Spawn(ThingDefOf.Spark, base.Position, base.Map, WipeMode.Vanish)).Launch(this, intVec, intVec, ProjectileHitFlags.All, null);
					return;
				}
				else
				{
					FireUtility.TryStartFireIn(intVec, base.Map, 0.1f);
				}
			}
		}

		// Token: 0x04002DFF RID: 11775
		private int ticksSinceSpawn;

		// Token: 0x04002E00 RID: 11776
		public float fireSize = 0.1f;

		// Token: 0x04002E01 RID: 11777
		private int ticksSinceSpread;

		// Token: 0x04002E02 RID: 11778
		private float flammabilityMax = 0.5f;

		// Token: 0x04002E03 RID: 11779
		private int ticksUntilSmoke;

		// Token: 0x04002E04 RID: 11780
		private Sustainer sustainer;

		// Token: 0x04002E05 RID: 11781
		private static List<Thing> flammableList = new List<Thing>();

		// Token: 0x04002E06 RID: 11782
		private static int fireCount;

		// Token: 0x04002E07 RID: 11783
		private static int lastFireCountUpdateTick;

		// Token: 0x04002E08 RID: 11784
		public const float MinFireSize = 0.1f;

		// Token: 0x04002E09 RID: 11785
		private const float MinSizeForSpark = 1f;

		// Token: 0x04002E0A RID: 11786
		private const float TicksBetweenSparksBase = 150f;

		// Token: 0x04002E0B RID: 11787
		private const float TicksBetweenSparksReductionPerFireSize = 40f;

		// Token: 0x04002E0C RID: 11788
		private const float MinTicksBetweenSparks = 75f;

		// Token: 0x04002E0D RID: 11789
		private const float MinFireSizeToEmitSpark = 1f;

		// Token: 0x04002E0E RID: 11790
		public const float MaxFireSize = 1.75f;

		// Token: 0x04002E0F RID: 11791
		private const int TicksToBurnFloor = 7500;

		// Token: 0x04002E10 RID: 11792
		private const int ComplexCalcsInterval = 150;

		// Token: 0x04002E11 RID: 11793
		private const float CellIgniteChancePerTickPerSize = 0.01f;

		// Token: 0x04002E12 RID: 11794
		private const float MinSizeForIgniteMovables = 0.4f;

		// Token: 0x04002E13 RID: 11795
		private const float FireBaseGrowthPerTick = 0.00055f;

		// Token: 0x04002E14 RID: 11796
		private static readonly IntRange SmokeIntervalRange = new IntRange(130, 200);

		// Token: 0x04002E15 RID: 11797
		private const int SmokeIntervalRandomAddon = 10;

		// Token: 0x04002E16 RID: 11798
		private const float BaseSkyExtinguishChance = 0.04f;

		// Token: 0x04002E17 RID: 11799
		private const int BaseSkyExtinguishDamage = 10;

		// Token: 0x04002E18 RID: 11800
		private const float HeatPerFireSizePerInterval = 160f;

		// Token: 0x04002E19 RID: 11801
		private const float HeatFactorWhenDoorPresent = 0.15f;

		// Token: 0x04002E1A RID: 11802
		private const float SnowClearRadiusPerFireSize = 3f;

		// Token: 0x04002E1B RID: 11803
		private const float SnowClearDepthFactor = 0.1f;

		// Token: 0x04002E1C RID: 11804
		private const int FireCountParticlesOff = 15;
	
	}
}
