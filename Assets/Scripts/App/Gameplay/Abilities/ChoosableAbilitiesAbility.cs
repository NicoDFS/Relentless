using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using System.Collections.Generic;

namespace Loom.ZombieBattleground
{
    public class ChoosableAbilitiesAbility : AbilityBase
    {
        public ChoosableAbilitiesAbility(Enumerators.CardKind cardKind, AbilityData ability)
            : base(cardKind, ability)
        {
        }

        public override void Activate()
        {
            base.Activate();

           // TODO IMPROVE IT FOR PVP - NEED TO DISCUSSS
        }
    }
}
