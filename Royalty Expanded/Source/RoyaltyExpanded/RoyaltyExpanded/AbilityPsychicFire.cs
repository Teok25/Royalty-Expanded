using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace RoyaltyExpanded
{
    public class CompAbilityEffect_PsychicFire : CompAbilityEffect_WithDuration
    {
        public new CompProperties_AbilityPsychicFire Props
        {
            get 
            {
                return (CompProperties_AbilityPsychicFire)this.props;
            }
        }
    }

    public class CompProperties_AbilityPsychicFire : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityPsychicFire()
        {
            this.compClass = typeof(CompAbilityEffect_PsychicFire);
        }
    }
}
