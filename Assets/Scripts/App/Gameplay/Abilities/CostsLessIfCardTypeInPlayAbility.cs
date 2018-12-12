using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using System.Collections.Generic;
using UnityEngine;

namespace Loom.ZombieBattleground
{
    public class CostsLessIfCardTypeInPlayAbility : AbilityBase
    {
        public Enumerators.SetType SetType;

        public int Value;

        public CostsLessIfCardTypeInPlayAbility(Enumerators.CardKind cardKind, AbilityData ability)
            : base(cardKind, ability)
        {
            SetType = ability.AbilitySetType;
            Value = ability.Value;
        }

        public override void Activate()
        {
            base.Activate();

            AbilitiesController.ThrowUseAbilityEvent(MainWorkingCard, new List<BoardObject>(), AbilityData.AbilityType, Protobuf.AffectObjectType.Types.Enum.Card);

            if (AbilityCallType != Enumerators.AbilityCallType.IN_HAND)
                return;

            PlayerCallerOfAbility.BoardChanged += BoardChangedHandler;
            PlayerCallerOfAbility.CardPlayed += CardPlayedHandler;

            Action();
        }

        public override void Action(object info = null)
        {
            base.Action(info);
            if (!PlayerCallerOfAbility.CardsInHand.Contains(MainWorkingCard))
                return;

            int gooCost = 0;

            if (AbilityData.AbilitySubTrigger == Enumerators.AbilitySubTrigger.IfHasUnitsWithFactionInPlay)
            {
                if (PlayerCallerOfAbility.BoardCards.FindAll(x => x.Model.Card.LibraryCard.CardSetType == SetType).Count > 0)
                {
                    gooCost = -Mathf.Abs(Value);
                }
            }
            else
            {
                gooCost = PlayerCallerOfAbility.BoardCards.FindAll(x => x.Model.Card.LibraryCard.CardSetType == SetType).Count * Value;
            }

            CardsController.SetGooCostOfCardInHand(PlayerCallerOfAbility, MainWorkingCard,
                MainWorkingCard.InstanceCard.Cost + gooCost, BoardCard);
        }

        private void CardPlayedHandler(WorkingCard card, int position)
        {
            if (!card.Equals(MainWorkingCard))
                return;

            PlayerCallerOfAbility.BoardChanged -= BoardChangedHandler;
            PlayerCallerOfAbility.CardPlayed -= CardPlayedHandler;
        }

        private void BoardChangedHandler(int obj)
        {
            Action();
        }
    }
}
