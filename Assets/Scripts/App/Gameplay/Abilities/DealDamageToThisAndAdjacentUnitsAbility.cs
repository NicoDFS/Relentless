using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Loom.ZombieBattleground
{
    public class DealDamageToThisAndAdjacentUnitsAbility : AbilityBase
    {
        private List<BoardUnitModel> _units;

        public DealDamageToThisAndAdjacentUnitsAbility(Enumerators.CardKind cardKind, AbilityData ability)
            : base(cardKind, ability)
        {
        }

        public override void Activate()
        {
            base.Activate();
        }

        public override void Action(object param = null)
        {
            AbilityProcessingAction = ActionsQueueController.AddNewActionInToQueue(null);

            base.Action(param);
            _units = new List<BoardUnitModel>();

            int targetIndex = -1;
            for (int i = 0; i < PlayerCallerOfAbility.BoardCards.Count; i++)
            {
                if (PlayerCallerOfAbility.BoardCards[i].Model == AbilityUnitOwner)
                {
                    targetIndex = i;
                    break;
                }
            }

            if (targetIndex > -1)
            {
                if (targetIndex - 1 > -1)
                {
                    _units.Add(PlayerCallerOfAbility.BoardCards[targetIndex - 1].Model);
                }

                if (targetIndex + 1 < PlayerCallerOfAbility.BoardCards.Count)
                {
                    _units.Add(PlayerCallerOfAbility.BoardCards[targetIndex + 1].Model);
                }
            }

            _units.Add(AbilityUnitOwner);

            InvokeActionTriggered(_units);
        }

        protected override void TurnEndedHandler()
        {
            base.TurnEndedHandler();

            if (AbilityCallType != Enumerators.AbilityCallType.END ||
        !GameplayManager.CurrentTurnPlayer.Equals(PlayerCallerOfAbility))
                return;

            Action();
        }

        protected override void VFXAnimationEndedHandler()
        {
            base.VFXAnimationEndedHandler();

            foreach (var unit in _units)
            {
                TakeDamageToUnit(unit);
            }
            _units.Clear();

            AbilitiesController.ThrowUseAbilityEvent(MainWorkingCard, _units.Cast<BoardObject>().ToList(), AbilityData.AbilityType, Protobuf.AffectObjectType.Types.Enum.Character);

            AbilityProcessingAction?.ForceActionDone();
        }

        private void TakeDamageToUnit(BoardUnitModel unit)
        {
            BattleController.AttackUnitByAbility(AbilityUnitOwner, AbilityData, unit);
        }
    }
}
