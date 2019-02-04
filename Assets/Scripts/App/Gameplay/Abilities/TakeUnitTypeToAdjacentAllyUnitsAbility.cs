using System;
using System.Collections.Generic;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;

namespace Loom.ZombieBattleground
{
    public class TakeUnitTypeToAdjacentAllyUnitsAbility : AbilityBase
    {
        public Enumerators.CardType CardType;

        public TakeUnitTypeToAdjacentAllyUnitsAbility(Enumerators.CardKind cardKind, AbilityData ability)
            : base(cardKind, ability)
        {
            CardType = ability.TargetCardType;
        }

        public override void Activate()
        {
            base.Activate();

            if (AbilityCallType != Enumerators.AbilityCallType.ENTRY)
                return;

            Action();

            ThrowUseAbilityEvent();
        }

        public override void Action(object info = null)
        {
            base.Action(info);

            Enumerators.ActionEffectType effectType = Enumerators.ActionEffectType.None;

            if (CardType == Enumerators.CardType.FERAL)
            {
                effectType = Enumerators.ActionEffectType.Feral;
            }
            else if (CardType == Enumerators.CardType.HEAVY)
            {
                effectType = Enumerators.ActionEffectType.Heavy;
            }

            List<PastActionsPopup.TargetEffectParam> TargetEffects = new List<PastActionsPopup.TargetEffectParam>();

            Player opponent = GetOpponentOverlord();

            int targetIndex = -1;
            for (int i = 0; i < opponent.BoardCards.Count; i++)
            {
                if (opponent.BoardCards[i].Model == AbilityUnitOwner)
                {
                    targetIndex = i;
                    break;
                }
            }

            if (targetIndex > -1)
            {
                if (targetIndex - 1 > -1)
                {
                    TakeTypeToUnit(opponent.BoardCards[targetIndex - 1]);

                    TargetEffects.Add(new PastActionsPopup.TargetEffectParam()
                    {
                        ActionEffectType = effectType,
                        Target = opponent.BoardCards[targetIndex - 1]
                    });
                }

                if (targetIndex + 1 < opponent.BoardCards.Count)
                {
                    TakeTypeToUnit(opponent.BoardCards[targetIndex + 1]);

                    TargetEffects.Add(new PastActionsPopup.TargetEffectParam()
                    {
                        ActionEffectType = effectType,
                        Target = opponent.BoardCards[targetIndex + 1]
                    });
                }
            }

            if (TargetEffects.Count > 0)
            {
                Enumerators.ActionType actionType = Enumerators.ActionType.CardAffectingMultipleCards;

                if (TargetEffects.Count == 1)
                {
                    actionType = Enumerators.ActionType.CardAffectingCard;
                }

                ActionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam()
                {
                    ActionType = actionType,
                    Caller = GetCaller(),
                    TargetEffects = TargetEffects
                });
            }
        }

        private void TakeTypeToUnit(BoardUnitView unit)
        {
            if (unit == null)
                return;

            switch (CardType)
            {
                case Enumerators.CardType.HEAVY:
                    unit.Model.SetAsHeavyUnit();
                    break;
                case Enumerators.CardType.FERAL:
                    unit.Model.SetAsFeralUnit();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(CardType), CardType, null);
            }
        }
    }
}
