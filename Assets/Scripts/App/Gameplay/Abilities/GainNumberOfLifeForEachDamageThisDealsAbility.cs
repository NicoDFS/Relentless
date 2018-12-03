using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using System.Collections.Generic;
using UnityEngine;

namespace Loom.ZombieBattleground
{
    public class GainNumberOfLifeForEachDamageThisDealsAbility : AbilityBase
    {
        public int Value { get; }

        private int _damage;

        private bool _isAttacker;

        public GainNumberOfLifeForEachDamageThisDealsAbility(Enumerators.CardKind cardKind, AbilityData ability)
            : base(cardKind, ability)
        {
            Value = ability.Value;
        }

        public override void Activate()
        {
            base.Activate();

            AbilitiesController.ThrowUseAbilityEvent(MainWorkingCard, new List<BoardObject>(), AbilityData.AbilityType, Protobuf.AffectObjectType.Types.Enum.Character);
        }

        public override void Action(object info = null)
        {
            base.Action(info);

            int damageDeal = (int) info;

            AbilityUnitOwner.CurrentHp += Mathf.Clamp(Value * damageDeal, 0, AbilityUnitOwner.MaxCurrentHp);
        }

        protected override void UnitAttackedHandler(BoardObject info, int damage, bool isAttacker)
        {
            base.UnitAttackedHandler(info, damage, isAttacker);

            _isAttacker = isAttacker;

            _damage = damage;
        }

        protected override void UnitAttackedEndedHandler()
        {
            base.UnitAttackedEndedHandler();

            if (AbilityCallType != Enumerators.AbilityCallType.ATTACK || !_isAttacker)
                return;

            AbilityProcessingAction = ActionsQueueController.AddNewActionInToQueue(null);

            InvokeActionTriggered();
        }

        protected override void VFXAnimationEndedHandler()
        {
            Action(_damage);

            AbilityProcessingAction?.ForceActionDone();
        }
    }
}
