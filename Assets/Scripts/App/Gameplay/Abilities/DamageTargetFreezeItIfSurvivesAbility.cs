using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using System;
using System.Collections.Generic;

namespace Loom.ZombieBattleground
{
    public class DamageTargetFreezeItIfSurvivesAbility : AbilityBase
    {
        public DamageTargetFreezeItIfSurvivesAbility(Enumerators.CardKind cardKind, AbilityData ability)
            : base(cardKind, ability)
        {
        }

        public override void Activate()
        {
            base.Activate();

            AbilitiesController.ThrowUseAbilityEvent(MainWorkingCard, new List<BoardObject>(), AbilityData.AbilityType, Protobuf.AffectObjectType.Character);

            if (AbilityCallType != Enumerators.AbilityCallType.ENTRY)
                return;
        }

        protected override void InputEndedHandler()
        {
            base.InputEndedHandler();

            if (IsAbilityResolved)
            {
                DamageTarget(AffectObjectType == Enumerators.AffectObjectType.Player ? (BoardObject)TargetPlayer : TargetUnit);
            }
        }

        private void DamageTarget(BoardObject boardObject)
        {
            object caller = AbilityUnitOwner != null ? AbilityUnitOwner : (object)BoardSpell;

            object target = null;

            Enumerators.ActionType actionType = Enumerators.ActionType.None;

            bool isFreezed = false;
            switch (AffectObjectType)
            {
                case Enumerators.AffectObjectType.Player:
                    BattleController.AttackPlayerByAbility(caller, AbilityData, TargetPlayer);
                    AbilitiesController.ThrowUseAbilityEvent(MainWorkingCard, new List<BoardObject>()
                    {
                        TargetPlayer
                    }, AbilityData.AbilityType, Protobuf.AffectObjectType.Player);

                    target = TargetPlayer;
                    actionType = Enumerators.ActionType.CardAffectingOverlord;

                    if (TargetPlayer.Defense > 0)
                    {
                        TargetPlayer.Stun(Enumerators.StunType.FREEZE, 1);
                        isFreezed = true;
                    }
                    break;
                case Enumerators.AffectObjectType.Character:
                    BattleController.AttackUnitByAbility(caller, AbilityData, TargetUnit);
                    AbilitiesController.ThrowUseAbilityEvent(MainWorkingCard, new List<BoardObject>()
                    {
                        TargetUnit
                    }, AbilityData.AbilityType, Protobuf.AffectObjectType.Character);

                    target = TargetUnit;
                    actionType = Enumerators.ActionType.CardAffectingCard;

                    if (TargetUnit.CurrentHp > 0)
                    {
                        TargetUnit.Stun(Enumerators.StunType.FREEZE, 1);
                        isFreezed = true;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(AffectObjectType), AffectObjectType, null);
            }

            List<PastActionsPopup.TargetEffectParam> TargetEffects = new List<PastActionsPopup.TargetEffectParam>();

            TargetEffects.Add(new PastActionsPopup.TargetEffectParam()
            {
                ActionEffectType = Enumerators.ActionEffectType.ShieldDebuff,
                Target = target,
                HasValue = true,
                Value = -AbilityData.Value
            });

            TargetEffects.Add(new PastActionsPopup.TargetEffectParam()
            {
                ActionEffectType = Enumerators.ActionEffectType.Freeze,
                Target = target,
            });

            ActionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = actionType,
                Caller = GetCaller(),
                TargetEffects = TargetEffects
            });
        }
    }
}
