using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using System.Collections.Generic;

namespace Loom.ZombieBattleground
{
    public class AllyUnitsOfTypeInPlayGetStatsAbility : AbilityBase
    {
        public int Health { get; }

        public int Damage { get; }

        public Enumerators.SetType SetType { get; }

        public AllyUnitsOfTypeInPlayGetStatsAbility(Enumerators.CardKind cardKind, AbilityData ability)
            : base(cardKind, ability)
        {
            Health = ability.Health;
            Damage = ability.Damage;
            SetType = ability.AbilitySetType;
        }

        public override void Activate()
        {
            base.Activate();

            AbilitiesController.ThrowUseAbilityEvent(MainWorkingCard, new List<BoardObject>(), AbilityData.AbilityType, Protobuf.AffectObjectType.Character);

            if (AbilityCallType != Enumerators.AbilityCallType.ENTRY)
                return;

            Action();
        }

        public override void Action(object info = null)
        {
            base.Action(info);

            foreach (BoardUnitView unit in PlayerCallerOfAbility.BoardCards)
            {
                if (unit.Model.Card.LibraryCard.CardSetType.Equals(SetType) && unit.Model != AbilityUnitOwner)
                {
                    unit.Model.BuffedDamage += Damage;
                    unit.Model.CurrentDamage += Damage;

                    unit.Model.BuffedHp += Health;
                    unit.Model.CurrentHp += Health;
                }
            }
        }
    }
}
