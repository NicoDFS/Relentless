using System;
using System.Collections.Generic;
using System.Linq;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using Loom.ZombieBattleground.Helpers;
using UnityEngine;

namespace Loom.ZombieBattleground
{
    public class TakeUnitTypeToTargetUnitAbility : AbilityBase
    {
        public Enumerators.CardType UnitType;

        public int Count { get; }

        public TakeUnitTypeToTargetUnitAbility(Enumerators.CardKind cardKind, AbilityData ability)
            : base(cardKind, ability)
        {
            UnitType = ability.TargetUnitType;
            Count = ability.Count;

            Count = Mathf.Clamp(Count, 1, Count);
        }

        public override void Activate()
        {
            base.Activate();

            if (AbilityCallType == Enumerators.AbilityCallType.ENTRY && AbilityActivityType == Enumerators.AbilityActivityType.PASSIVE)
            {
                HandleTargets();
            }
        }

        protected override void InputEndedHandler()
        {
            base.InputEndedHandler();

            if (IsAbilityResolved)
            {
                TakeTypeToUnits(new List<BoardUnitModel>() { TargetUnit });
                AbilitiesController.ThrowUseAbilityEvent(MainWorkingCard, new List<BoardObject>()
                {
                    TargetUnit
                }, AbilityData.AbilityType, Enumerators.AffectObjectType.Character);
            }
        }

        private void HandleTargets()
        {
            List<BoardUnitModel> units;

            if (PredefinedTargets != null)
            {
                units = PredefinedTargets.Select(x => x.BoardObject).Cast<BoardUnitModel>().ToList();
            }
            else
            {
                units = PlayerCallerOfAbility.BoardCards.Select(x => x.Model)
               .Where(unit => unit != AbilityUnitOwner && !unit.HasFeral && unit.NumTurnsOnBoard == 0)
               .ToList();

                if (AbilityData.AbilitySubTrigger != Enumerators.AbilitySubTrigger.AllOtherAllyUnitsInPlay)
                {
                    units = InternalTools.GetRandomElementsFromList(units, Count);
                }
            }

            if (units.Count > 0)
            {
                TakeTypeToUnits(units);

                AbilitiesController.ThrowUseAbilityEvent(MainWorkingCard, units.Cast<BoardObject>().ToList(), AbilityData.AbilityType, Enumerators.AffectObjectType.Character);
            }
        }

        private void TakeTypeToUnits(List<BoardUnitModel> units)
        {
            foreach (BoardUnitModel unit in units)
            {
                switch (UnitType)
                {
                    case Enumerators.CardType.HEAVY:
                        unit.SetAsHeavyUnit();
                        break;
                    case Enumerators.CardType.FERAL:
                        unit.SetAsFeralUnit();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(UnitType), UnitType, null);
                }
            }

            PostGameActionReport(units);
        }

        private void PostGameActionReport(List<BoardUnitModel> targets)
        {
            Enumerators.ActionEffectType effectType = Enumerators.ActionEffectType.None;

            if (UnitType == Enumerators.CardType.FERAL)
            {
                effectType = Enumerators.ActionEffectType.Feral;
            }
            else if (UnitType == Enumerators.CardType.HEAVY)
            {
                effectType = Enumerators.ActionEffectType.Heavy;
            }

            List<PastActionsPopup.TargetEffectParam> TargetEffects = new List<PastActionsPopup.TargetEffectParam>();

            foreach (BoardUnitModel target in targets)
            {
                TargetEffects.Add(new PastActionsPopup.TargetEffectParam()
                {
                    ActionEffectType = effectType,
                    Target = target
                });
            }

            ActionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.CardAffectingCard,
                Caller = GetCaller(),
                TargetEffects = TargetEffects
            });
        }
    }
}
