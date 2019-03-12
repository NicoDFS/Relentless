using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using System.Collections.Generic;
using UnityEngine;

namespace Loom.ZombieBattleground
{
    public class StunOrDamageAdjustmentsAbility : AbilityBase
    {
        public Enumerators.StatType StatType { get; }

        public int Value { get; } = 1;

        public StunOrDamageAdjustmentsAbility(Enumerators.CardKind cardKind, AbilityData ability)
            : base(cardKind, ability)
        {
            StatType = ability.AbilityStatType;
            Value = ability.Value;
        }

        protected override void VFXAnimationEndedHandler()
        {
            base.VFXAnimationEndedHandler();

            BoardUnitModel creature = (BoardUnitModel)TargetUnit;

            CreateVfx(BattlegroundController.GetBoardUnitViewByModel<BoardUnitView>(creature).Transform.position);

            BoardUnitModel leftAdjustment = null, rightAdjastment = null;

            int targetIndex = -1;
            for (int i = 0; i < creature.OwnerPlayer.CardsOnBoard.Count; i++)
            {
                if (creature.OwnerPlayer.CardsOnBoard[i] == creature)
                {
                    targetIndex = i;
                }
            }

            if (targetIndex > -1)
            {
                if (targetIndex - 1 > -1)
                {
                    leftAdjustment = creature.OwnerPlayer.CardsOnBoard[targetIndex - 1];
                }

                if (targetIndex + 1 < creature.OwnerPlayer.CardsOnBoard.Count)
                {
                    rightAdjastment = creature.OwnerPlayer.CardsOnBoard[targetIndex + 1];
                }
            }

            if (leftAdjustment != null)
            {
                if (leftAdjustment.IsStun)
                {
                    BattleController.AttackUnitByAbility(AbilityUnitOwner, AbilityData, leftAdjustment);
                }
                else
                {
                    leftAdjustment.Stun(Enumerators.StunType.FREEZE, 1);
                }
            }

            if (rightAdjastment != null)
            {
                if (rightAdjastment.IsStun)
                {
                    BattleController.AttackUnitByAbility(AbilityUnitOwner, AbilityData, rightAdjastment);
                }
                else
                {
                    rightAdjastment.Stun(Enumerators.StunType.FREEZE, 1);
                }
            }

            if (creature.IsStun)
            {
                BattleController.AttackUnitByAbility(AbilityUnitOwner, AbilityData, creature);
            }
            else
            {
                creature.Stun(Enumerators.StunType.FREEZE, 1);
            }

            InvokeUseAbilityEvent(
                new List<ParametrizedAbilityBoardObject>
                {
                    new ParametrizedAbilityBoardObject(TargetUnit)
                }
            );
        }

        protected override void InputEndedHandler()
        {
            base.InputEndedHandler();

            if (IsAbilityResolved)
            {
                InvokeActionTriggered();
            }
        }
    }
}
