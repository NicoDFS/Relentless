using System;
using System.Collections.Generic;
using System.Linq;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using Loom.ZombieBattleground.Helpers;
using UnityEngine;

namespace Loom.ZombieBattleground
{
    public class RanksController : IController
    {
        public event Action<WorkingCard, List<BoardUnitView>> RanksUpdated;

        private ITutorialManager _tutorialManager;
        private IGameplayManager _gameplayManager;

        private Action _ranksUpgradeCompleteAction;

        public void Dispose()
        {
        }

        public void Init()
        {
            _tutorialManager = GameClient.Get<ITutorialManager>();
            _gameplayManager = GameClient.Get<IGameplayManager>();
        }

        public void Update()
        {
        }

        public void ResetAll()
        {
        }

        public void UpdateRanksByElements(List<BoardUnitView> units, WorkingCard card, GameplayQueueAction<object> actionInQueue)
        {
            if (GameClient.Get<IMatchManager>().MatchType == Enumerators.MatchType.PVP)
            {
                if (!card.Owner.IsLocalPlayer)
                    return;
            }

            actionInQueue.Action = (parameter, completeCallback) =>
                   {
                       _ranksUpgradeCompleteAction = completeCallback;

                       List<BoardUnitView> filter = units.Where(unit =>
                                    unit.Model.Card.LibraryCard.CardSetType == card.LibraryCard.CardSetType &&
                                    (int)unit.Model.Card.LibraryCard.CardRank < (int)card.LibraryCard.CardRank).ToList();

                       if ((filter.Count > 0 && !_tutorialManager.IsTutorial) ||
                           (_tutorialManager.IsTutorial &&
                           _tutorialManager.CurrentTutorial.TutorialContent.ToGameplayContent().SpecificBattlegroundInfo.RankSystemHasEnabled))
                       {
                           DoRankUpgrades(filter, card);

                           GameClient.Get<IOverlordExperienceManager>().ReportExperienceAction(filter[0].Model.OwnerPlayer.SelfHero,
                            Common.Enumerators.ExperienceActionType.ActivateRankAbility);

                           _tutorialManager.ReportActivityAction(Enumerators.TutorialActivityAction.RanksUpdated);
                       }
                       else
                       {
                           _ranksUpgradeCompleteAction?.Invoke();
                           _ranksUpgradeCompleteAction = null;
                       }
                   };
        }

        public void DoRankUpgrades(List<BoardUnitView> units, WorkingCard card, bool randomly = true)
        {
            switch (card.LibraryCard.CardSetType)
            {
                case Enumerators.SetType.AIR:
                    AirRankBuff(units, card.LibraryCard.CardRank, card, randomly);
                    break;
                case Enumerators.SetType.EARTH:
                    EarthRankBuff(units, card.LibraryCard.CardRank, card, randomly);
                    break;
                case Enumerators.SetType.WATER:
                    WaterRankBuff(units, card.LibraryCard.CardRank, card, randomly);
                    break;
                case Enumerators.SetType.FIRE:
                    FireRankBuff(units, card.LibraryCard.CardRank, card, randomly);
                    break;
                case Enumerators.SetType.TOXIC:
                    ToxicRankBuff(units, card.LibraryCard.CardRank, card, randomly);
                    break;
                case Enumerators.SetType.LIFE:
                    LifeRankBuff(units, card.LibraryCard.CardRank, card, randomly);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(card.LibraryCard.CardSetType), card.LibraryCard.CardSetType, null);
            }
        }

        private void AirRankBuff(List<BoardUnitView> units, Enumerators.CardRank rank, WorkingCard card, bool randomly = true)
        {
            List<Enumerators.BuffType> buffs = new List<Enumerators.BuffType>();
            int count = 1;
            switch (rank)
            {
                case Enumerators.CardRank.OFFICER:
                    buffs.Add(Enumerators.BuffType.GUARD);
                    break;
                case Enumerators.CardRank.COMMANDER:
                    buffs.Add(Enumerators.BuffType.GUARD);
                    buffs.Add(Enumerators.BuffType.ATTACK);
                    count = 2;
                    break;
                case Enumerators.CardRank.GENERAL:
                    buffs.Add(Enumerators.BuffType.GUARD);
                    buffs.Add(Enumerators.BuffType.ATTACK);
                    count = 3;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rank), rank, null);
            }

            BuffRandomAlly(units, count, buffs, card, randomly);
        }

        private void EarthRankBuff(List<BoardUnitView> units, Enumerators.CardRank rank, WorkingCard card, bool randomly = true)
        {
            List<Enumerators.BuffType> buffs = new List<Enumerators.BuffType>();
            int count = 1;
            switch (rank)
            {
                case Enumerators.CardRank.OFFICER:
                    buffs.Add(Enumerators.BuffType.HEAVY);
                    break;
                case Enumerators.CardRank.COMMANDER:
                    buffs.Add(Enumerators.BuffType.HEAVY);
                    buffs.Add(Enumerators.BuffType.ATTACK);
                    count = 2;
                    break;
                case Enumerators.CardRank.GENERAL:
                    buffs.Add(Enumerators.BuffType.HEAVY);
                    buffs.Add(Enumerators.BuffType.ATTACK);
                    count = 3;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rank), rank, null);
            }

            BuffRandomAlly(units, count, buffs, card, randomly);
        }

        private void FireRankBuff(List<BoardUnitView> units, Enumerators.CardRank rank, WorkingCard card, bool randomly = true)
        {
            List<Enumerators.BuffType> buffs = new List<Enumerators.BuffType>();
            int count = 1;
            switch (rank)
            {
                case Enumerators.CardRank.OFFICER:
                    buffs.Add(Enumerators.BuffType.BLITZ);
                    break;
                case Enumerators.CardRank.COMMANDER:
                    buffs.Add(Enumerators.BuffType.BLITZ);
                    buffs.Add(Enumerators.BuffType.ATTACK);
                    count = 2;
                    break;
                case Enumerators.CardRank.GENERAL:
                    buffs.Add(Enumerators.BuffType.BLITZ);
                    buffs.Add(Enumerators.BuffType.ATTACK);
                    count = 3;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rank), rank, null);
            }

            BuffRandomAlly(units, count, buffs, card, randomly);
        }

        private void LifeRankBuff(List<BoardUnitView> units, Enumerators.CardRank rank, WorkingCard card, bool randomly = true)
        {
            List<Enumerators.BuffType> buffs = new List<Enumerators.BuffType>();
            int count = 1;
            switch (rank)
            {
                case Enumerators.CardRank.OFFICER:
                    buffs.Add(Enumerators.BuffType.REANIMATE);
                    break;
                case Enumerators.CardRank.COMMANDER:
                    buffs.Add(Enumerators.BuffType.REANIMATE);
                    buffs.Add(Enumerators.BuffType.ATTACK);
                    count = 2;
                    break;
                case Enumerators.CardRank.GENERAL:
                    buffs.Add(Enumerators.BuffType.REANIMATE);
                    buffs.Add(Enumerators.BuffType.ATTACK);
                    count = 3;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rank), rank, null);
            }

            BuffRandomAlly(units, count, buffs, card, randomly);
        }

        private void ToxicRankBuff(List<BoardUnitView> units, Enumerators.CardRank rank, WorkingCard card, bool randomly = true)
        {
            List<Enumerators.BuffType> buffs = new List<Enumerators.BuffType>();
            int count = 1;
            switch (rank)
            {
                case Enumerators.CardRank.OFFICER:
                    buffs.Add(Enumerators.BuffType.DESTROY);
                    break;
                case Enumerators.CardRank.COMMANDER:
                    buffs.Add(Enumerators.BuffType.DESTROY);
                    buffs.Add(Enumerators.BuffType.ATTACK);
                    count = 2;
                    break;
                case Enumerators.CardRank.GENERAL:
                    buffs.Add(Enumerators.BuffType.DESTROY);
                    buffs.Add(Enumerators.BuffType.ATTACK);
                    count = 3;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rank), rank, null);
            }

            BuffRandomAlly(units, count, buffs, card, randomly);
        }

        private void WaterRankBuff(List<BoardUnitView> units, Enumerators.CardRank rank, WorkingCard card, bool randomly = true)
        {
            List<Enumerators.BuffType> buffs = new List<Enumerators.BuffType>();
            int count = 1;
            switch (rank)
            {
                case Enumerators.CardRank.OFFICER:
                    buffs.Add(Enumerators.BuffType.FREEZE);
                    break;
                case Enumerators.CardRank.COMMANDER:
                    buffs.Add(Enumerators.BuffType.FREEZE);
                    buffs.Add(Enumerators.BuffType.ATTACK);
                    count = 2;
                    break;
                case Enumerators.CardRank.GENERAL:
                    buffs.Add(Enumerators.BuffType.FREEZE);
                    buffs.Add(Enumerators.BuffType.ATTACK);
                    count = 3;
                    break;
            }

            BuffRandomAlly(units, count, buffs, card, randomly);
        }

        private void BuffRandomAlly(List<BoardUnitView> units, int count,
                                    List<Enumerators.BuffType> buffTypes,
                                    WorkingCard card, bool randomly = true)
        {
            if (_tutorialManager.IsTutorial)
            {
                units = units.FindAll(x => x.Model.UnitCanBeUsable());
            }

            if (randomly)
            {
                units = InternalTools.GetRandomElementsFromList(units, count);
            }

            foreach (Enumerators.BuffType buff in buffTypes)
            {
                foreach (BoardUnitView unit in units)
                {
                    unit.Model.ApplyBuff(buff);
                }
            }


            if (GameClient.Get<IMatchManager>().MatchType == Enumerators.MatchType.PVP)
            {
                if (!card.Owner.IsLocalPlayer)
                {
                    _ranksUpgradeCompleteAction?.Invoke();
                    _ranksUpgradeCompleteAction = null;
                    return;
                }
            }

            RanksUpdated?.Invoke(card, units);

            _ranksUpgradeCompleteAction?.Invoke();
            _ranksUpgradeCompleteAction = null;
        }

        public void BuffAllyManually(List<BoardUnitView> units, WorkingCard card)
        {
            _gameplayManager.GetController<ActionsQueueController>().AddNewActionInToQueue(
                 (parameter, completeCallback) =>
                 {
                     _ranksUpgradeCompleteAction = completeCallback;

                     DoRankUpgrades(units, card, false);
                 }, Enumerators.QueueActionType.RankBuff);
        }
    }
}
