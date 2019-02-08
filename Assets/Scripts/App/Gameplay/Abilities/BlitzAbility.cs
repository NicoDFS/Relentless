using System.Collections.Generic;
using System.Linq;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using Loom.ZombieBattleground.Helpers;

namespace Loom.ZombieBattleground
{
    public class BlitzAbility : AbilityBase
    {
        private int Count { get; }
        private Enumerators.SetType SetType { get; }

        public BlitzAbility(Enumerators.CardKind cardKind, AbilityData ability)
            : base(cardKind, ability)
        {
            Count = ability.Count;
            SetType = ability.AbilitySetType;
        }

        public override void Activate()
        {
            base.Activate();

            if (AbilityCallType != Enumerators.AbilityCallType.ENTRY)
                return;

            Action();
        }

        public override void Action(object info = null)
        {
            base.Action(info);

            if (AbilityData.AbilitySubTrigger == Enumerators.AbilitySubTrigger.RandomUnit)
            {
                List<BoardUnitView> units = new List<BoardUnitView>();
                foreach (Enumerators.AbilityTargetType targetType in AbilityTargetTypes)
                {
                    switch (targetType)
                    {
                        case Enumerators.AbilityTargetType.OPPONENT_CARD:
                            units.AddRange(GetOpponentOverlord().BoardCards.FindAll(x => x.Model.Card.LibraryCard.CardSetType == SetType));
                            break;
                        case Enumerators.AbilityTargetType.PLAYER_CARD:
                            units.AddRange(PlayerCallerOfAbility.BoardCards.FindAll(x => x.Model.Card.LibraryCard.CardSetType == SetType));
                            break;
                    }
                }

                units = InternalTools.GetRandomElementsFromList(units, Count);

                foreach (BoardUnitView unit in units)
                {
                    TakeBlitzToUnit(unit.Model);
                }

                ThrowUseAbilityEvent(
                    units
                        .Select(x => new ParametrizedAbilityBoardObject(x.Model))
                        .ToList()
                );
            }
            else
            {
                TakeBlitzToUnit(AbilityUnitOwner);
                ThrowUseAbilityEvent(
                    new List<ParametrizedAbilityBoardObject>
                    {
                        new ParametrizedAbilityBoardObject(AbilityUnitOwner)
                    }
                );
            }
        }

        private void TakeBlitzToUnit(BoardUnitModel unit)
        {
            unit.ApplyBuff(Enumerators.BuffType.BLITZ);
            unit.AddGameMechanicDescriptionOnUnit(Enumerators.GameMechanicDescriptionType.Blitz);
        }
    }
}
