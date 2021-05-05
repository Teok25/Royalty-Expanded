using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RoyaltyExpanded
{
    public class CompAbilityEffect_SolarRupture : CompAbilityEffect
    {
        public new CompProperties_AbilitySolarRupture Props
        {
            get 
            {
                return (CompProperties_AbilitySolarRupture)this.props;
            }
        }
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);            
            Map map = this.parent.pawn.Map;
            foreach (IntVec3 intVec in Find.CurrentMap.AllCells)
            {
                GenTemperature.PushHeat(target.Cell, map, 10f);
            }
            GenExplosion.DoExplosion(target.Cell, this.parent.pawn.MapHeld, this.Props.flameRadius, DamageDefOf.Flame, null, this.Props.flameDamage, -1, null, null, null, null, this.Props.filthType, 1, 1, false, null, 0, 0, 1f, false, null, null);
        }

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            GenDraw.DrawRadiusRing(target.Cell, this.Props.flameRadius);
        }
    }

    public class CompProperties_AbilitySolarRupture : CompProperties_AbilityEffect 
    {
        public CompProperties_AbilitySolarRupture()
        {
            this.compClass = typeof(CompAbilityEffect_SolarRupture);
        }

        public float flameRadius;

        public int flameDamage;

        public ThingDef filthType;
    }
}
