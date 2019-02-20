using System;
using System.Collections.Generic;
using System.Linq;
using Loom.ZombieBattleground.BackendCommunication;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using Loom.ZombieBattleground.Protobuf;
using UnityEngine;
using Card = Loom.ZombieBattleground.Data.Card;
using InstanceId = Loom.ZombieBattleground.Data.InstanceId;

namespace Loom.ZombieBattleground
{
    public class OpponentController : IController
    {
        private IGameplayManager _gameplayManager;
        private IPvPManager _pvpManager;
        private BackendFacade _backendFacade;
        private BackendDataControlMediator _backendDataControlMediator;
        private IMatchManager _matchManager;

        private CardsController _cardsController;
        private BattlegroundController _battlegroundController;
        private BoardController _boardController;
        private SkillsController _skillsController;
        private BattleController _battleController;
        private BoardArrowController _boardArrowController;
        private AbilitiesController _abilitiesController;
        private ActionsQueueController _actionsQueueController;
        private RanksController _ranksController;

        public void Dispose()
        {
        }

        public void Init()
        {
            _gameplayManager = GameClient.Get<IGameplayManager>();
            _backendFacade = GameClient.Get<BackendFacade>();
            _backendDataControlMediator = GameClient.Get<BackendDataControlMediator>();
            _pvpManager = GameClient.Get<IPvPManager>();
            _matchManager = GameClient.Get<IMatchManager>();

            _cardsController = _gameplayManager.GetController<CardsController>();
            _skillsController = _gameplayManager.GetController<SkillsController>();
            _battlegroundController = _gameplayManager.GetController<BattlegroundController>();
            _battleController = _gameplayManager.GetController<BattleController>();
            _boardArrowController = _gameplayManager.GetController<BoardArrowController>();
            _abilitiesController = _gameplayManager.GetController<AbilitiesController>();
            _actionsQueueController = _gameplayManager.GetController<ActionsQueueController>();
            _ranksController = _gameplayManager.GetController<RanksController>();
            _boardController = _gameplayManager.GetController<BoardController>();

            _gameplayManager.GameStarted += GameStartedHandler;
            _gameplayManager.GameEnded += GameEndedHandler;

        }

        public void ResetAll()
        {
        }

        public void Update()
        {
        }

        public void InitializePlayer(InstanceId instanceId)
        {
            Player player = new Player(instanceId, GameObject.Find("Opponent"), true);
            _gameplayManager.OpponentPlayer = player;

            if (!_gameplayManager.IsSpecificGameplayBattleground ||
                (_gameplayManager.IsTutorial &&
                GameClient.Get<ITutorialManager>().CurrentTutorial.TutorialContent.ToGameplayContent().
                SpecificBattlegroundInfo.DisabledInitialization))
            {
                List<WorkingCard> deck = new List<WorkingCard>();

                bool isMainTurnSecond;
                switch (_matchManager.MatchType)
                {
                    case Enumerators.MatchType.PVP:
                        foreach (CardInstance cardInstance in player.InitialPvPPlayerState.CardsInDeck)
                        {
                            deck.Add(cardInstance.FromProtobuf(player));
                        }

                        Debug.Log(
                            $"Player ID {instanceId}, local: {player.IsLocalPlayer}, added CardsInDeck:\n" +
                            String.Join(
                                "\n",
                                (IList<WorkingCard>) deck
                                    .OrderBy(card => card.InstanceId)
                                    .ToArray()
                                )
                        );

                        isMainTurnSecond = GameClient.Get<IPvPManager>().IsFirstPlayer();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                player.SetDeck(deck, isMainTurnSecond);

                _battlegroundController.UpdatePositionOfCardsInOpponentHand();
            }
        }

        private void GameStartedHandler()
        {
            _pvpManager.CardPlayedActionReceived += OnCardPlayedHandler;
            _pvpManager.CardAttackedActionReceived += OnCardAttackedHandler;
            _pvpManager.CardAbilityUsedActionReceived += OnCardAbilityUsedHandler;
            _pvpManager.OverlordSkillUsedActionReceived += OnOverlordSkillUsedHandler;
            _pvpManager.LeaveMatchReceived += OnLeaveMatchHandler;
            _pvpManager.RankBuffActionReceived += OnRankBuffHandler;
            _pvpManager.CheatDestroyCardsOnBoardActionReceived += OnCheatDestroyCardsOnBoardActionHandler;
            _pvpManager.PlayerLeftGameActionReceived += OnPlayerLeftGameActionHandler;
            _pvpManager.PlayerActionOutcomeReceived += OnPlayerActionOutcomeReceived;
        }

        private void OnPlayerLeftGameActionHandler(PlayerActionLeaveMatch leaveMatchAction)
        {
            if (leaveMatchAction.Winner == _backendDataControlMediator.UserDataModel.UserId)
            {
                _gameplayManager.OpponentPlayer.PlayerDie();
            }
            else
            {
                _gameplayManager.CurrentPlayer.PlayerDie();
            }
        }
        private void GameEndedHandler(Enumerators.EndGameType endGameType)
        {
            _pvpManager.CardPlayedActionReceived -= OnCardPlayedHandler;
            _pvpManager.CardAttackedActionReceived -= OnCardAttackedHandler;
            _pvpManager.CardAbilityUsedActionReceived -= OnCardAbilityUsedHandler;
            _pvpManager.OverlordSkillUsedActionReceived -= OnOverlordSkillUsedHandler;
            _pvpManager.LeaveMatchReceived -= OnLeaveMatchHandler;
            _pvpManager.RankBuffActionReceived -= OnRankBuffHandler;
            _pvpManager.CheatDestroyCardsOnBoardActionReceived -= OnCheatDestroyCardsOnBoardActionHandler;
            _pvpManager.PlayerLeftGameActionReceived -= OnPlayerLeftGameActionHandler;
            _pvpManager.PlayerActionOutcomeReceived -= OnPlayerActionOutcomeReceived;
        }

        private void OnPlayerActionOutcomeReceived(PlayerActionOutcome outcome)
        {
            if (!_pvpManager.UseBackendGameLogic)
                return;

            switch (outcome.OutcomeCase)
            {
                case PlayerActionOutcome.OutcomeOneofCase.None:
                    break;
                case PlayerActionOutcome.OutcomeOneofCase.Rage:
                    PlayerActionOutcome.Types.CardAbilityRageOutcome rageOutcome = outcome.Rage;
                    BoardUnitModel boardUnit = _battlegroundController.GetBoardUnitModelByInstanceId(rageOutcome.InstanceId.FromProtobuf());

                    boardUnit.BuffedDamage = rageOutcome.NewAttack;
                    boardUnit.CurrentDamage = rageOutcome.NewAttack;
                    break;
                case PlayerActionOutcome.OutcomeOneofCase.PriorityAttack:
                    // TODO
                    break;

                case PlayerActionOutcome.OutcomeOneofCase.Reanimate:
                    PlayerActionOutcome.Types.CardAbilityReanimateOutcome reanimateAbilityOutcome = outcome.Reanimate;
                    ReAnimateAbility(reanimateAbilityOutcome);
                    break;

                case PlayerActionOutcome.OutcomeOneofCase.ChangeStat:
                    PlayerActionOutcome.Types.CardAbilityChangeStatOutcome changeStatOutcome  = outcome.ChangeStat;

                    boardUnit = _battlegroundController.GetBoardUnitModelByInstanceId(changeStatOutcome.InstanceId.FromProtobuf());

                    if (changeStatOutcome.Stat == StatType.Types.Enum.Damage)
                    {
                        BoardObject targetObject =
                            _battlegroundController.GetBoardObjectByInstanceId(changeStatOutcome.TargetInstanceId
                                .FromProtobuf());

                        BoardUnitModel unitModel =
                            _battlegroundController.GetBoardUnitModelByInstanceId(
                                changeStatOutcome.InstanceId.FromProtobuf());

                        switch (targetObject)
                        {
                            case Player targetPlayer:
                                _battleController.AttackPlayerByUnit(unitModel, targetPlayer);
                                break;
                            case BoardUnitModel targetCardModel:
                                _battleController.AttackUnitByUnit(unitModel, targetCardModel);
                                break;
                        }

                        boardUnit.BuffedDamage = changeStatOutcome.NewAttack;
                        boardUnit.CurrentDamage = changeStatOutcome.NewAttack;
                    }
                    else if (changeStatOutcome.Stat == StatType.Types.Enum.Health)
                    {
                        boardUnit.BuffedHp = changeStatOutcome.NewDefense;
                        boardUnit.CurrentHp = changeStatOutcome.NewDefense;
                    }

                    break;

                case PlayerActionOutcome.OutcomeOneofCase.ReplaceUnitsWithTypeOnStrongerOnes:
                    PlayerActionOutcome.Types.CardAbilityReplaceUnitsWithTypeOnStrongerOnes replaceUnitWithTypeStatOutcome = outcome.ReplaceUnitsWithTypeOnStrongerOnes;
                    ReplaceUnitsWithTypeOnStrongerOnes(replaceUnitWithTypeStatOutcome);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ReplaceUnitsWithTypeOnStrongerOnes(PlayerActionOutcome.Types.CardAbilityReplaceUnitsWithTypeOnStrongerOnes replaceUnitWithTypeStatOutcome)
        {
            List<BoardUnitView> oldCardList = new List<BoardUnitView>();
            for (int i=0; i<replaceUnitWithTypeStatOutcome.OldInstanceIds.Count; i++)
            {
                InstanceId id = replaceUnitWithTypeStatOutcome.OldInstanceIds[i].FromProtobuf();
                BoardUnitModel unitModel = _battlegroundController.GetBoardUnitModelByInstanceId(id);
                BoardUnitView unit = _battlegroundController.GetBoardUnitViewByModel(unitModel);
                oldCardList.Add(unit);
            }
            ClearOldUnitsOnBoard(oldCardList);

            for (int i=0; i<replaceUnitWithTypeStatOutcome.NewCardInstances.Count; i++)
            {
                Player owner = _gameplayManager.CurrentPlayer;
                if (replaceUnitWithTypeStatOutcome.NewCardInstances[i].CardInstance.Owner != _backendDataControlMediator.UserDataModel.UserId)
                    owner = _gameplayManager.OpponentPlayer;

                ItemPosition itemPosition = new ItemPosition(replaceUnitWithTypeStatOutcome.NewCardInstances[i].Position);
                Card libraryCard = replaceUnitWithTypeStatOutcome.NewCardInstances[i].CardInstance.Prototype.FromProtobuf();
                BoardUnitView unitView = _cardsController.SpawnUnitOnBoard(owner, libraryCard.Name, itemPosition);
                if (unitView != null)
                {
                    AddUnitToBoardCards(owner, itemPosition, unitView);
                }
            }
        }

        private void ClearOldUnitsOnBoard(List<BoardUnitView> boardUnits)
        {
            foreach (BoardUnitView unit in boardUnits)
            {
                unit.Model.OwnerPlayer.BoardCards.Remove(unit);
                unit.Model.OwnerPlayer.RemoveCardFromBoard(unit.Model.Card);

                unit.DisposeGameObject();
            }
        }

        private void AddUnitToBoardCards(Player owner, ItemPosition position, BoardUnitView unit)
        {
            if (owner.IsLocalPlayer)
            {
                _battlegroundController.PlayerBoardCards.Insert(position, unit);
            }
            else
            {
                _battlegroundController.OpponentBoardCards.Insert(position, unit);
            }
        }

        private void ReAnimateAbility(PlayerActionOutcome.Types.CardAbilityReanimateOutcome reanimateAbilityOutcome)
        {
            Player owner = _gameplayManager.CurrentPlayer;
            if (reanimateAbilityOutcome.NewCardInstance.Owner != _backendDataControlMediator.UserDataModel.UserId)
                owner = _gameplayManager.OpponentPlayer;

            Card libraryCard = reanimateAbilityOutcome.NewCardInstance.Prototype.FromProtobuf();

            WorkingCard card = new WorkingCard(libraryCard, libraryCard, owner, reanimateAbilityOutcome.NewCardInstance.InstanceId.FromProtobuf());
            BoardUnitView unit = CreateBoardUnit(card, owner);

            owner.AddCardToBoard(card, ItemPosition.End);
            owner.BoardCards.Insert(ItemPosition.End, unit);

            if (owner.IsLocalPlayer)
            {
                _battlegroundController.PlayerBoardCards.Insert(ItemPosition.End, unit);
            }
            else
            {
                _battlegroundController.OpponentBoardCards.Insert(ItemPosition.End, unit);
            }

            _boardController.UpdateCurrentBoardOfPlayer(owner, null);

            // TODO : have to see... how to invoke this
            //InvokeActionTriggered(unit);
            AbilityData abilityData = AbilitiesController.GetAbilityDataByType(Enumerators.AbilityType.REANIMATE_UNIT);
            AbilityBase ability = new ReanimateAbility(libraryCard.CardKind, abilityData);
            AbilityViewBase abilityView = new ReanimateAbilityView((ReanimateAbility)ability);
            ability.InvokeActionTriggered(unit);
        }

        private BoardUnitView CreateBoardUnit(WorkingCard card, Player owner)
        {
            GameObject playerBoard = owner.IsLocalPlayer ?
                _battlegroundController.PlayerBoardObject :
                _battlegroundController.OpponentBoardObject;

            BoardUnitView boardUnitView = new BoardUnitView(new BoardUnitModel(), playerBoard.transform);
            boardUnitView.Transform.tag = owner.IsLocalPlayer ? SRTags.PlayerOwned : SRTags.OpponentOwned;
            boardUnitView.Transform.parent = playerBoard.transform;
            boardUnitView.Transform.position = new Vector2(2f * owner.BoardCards.Count, owner.IsLocalPlayer ? -1.66f : 1.66f);
            boardUnitView.Model.OwnerPlayer = owner;
            boardUnitView.SetObjectInfo(card);
            boardUnitView.Model.TutorialObjectId = card.TutorialObjectId;

            if (!owner.Equals(_gameplayManager.CurrentTurnPlayer))
            {
                boardUnitView.Model.IsPlayable = true;
            }

            boardUnitView.PlayArrivalAnimation();

            _gameplayManager.CanDoDragActions = true;

            return boardUnitView;
        }

        private void OnPlayerLeftGameActionHandler()
        {
            _gameplayManager.OpponentPlayer.PlayerDie();
        }

        #region event handlers

        private void OnCardPlayedHandler(PlayerActionCardPlay cardPlay)
        {
            GotActionPlayCard(cardPlay.Card.FromProtobuf(), cardPlay.Position);
        }

        private void OnLeaveMatchHandler()
        {
            _gameplayManager.OpponentPlayer.PlayerDie();
        }

        private void OnCardAttackedHandler(PlayerActionCardAttack actionCardAttack)
        {
            GotActionCardAttack(new CardAttackModel
            {
                CardId = actionCardAttack.Attacker.FromProtobuf(),
                TargetId = actionCardAttack.Target.InstanceId.FromProtobuf()
            });
        }

        private void OnCardAbilityUsedHandler(PlayerActionCardAbilityUsed actionUseCardAbility)
        {
            GotActionUseCardAbility(new UseCardAbilityModel
            {
                Card = actionUseCardAbility.Card.FromProtobuf(),
                Targets = actionUseCardAbility.Targets.Select(t => t.FromProtobuf()).ToList(),
                AbilityType = (Enumerators.AbilityType) actionUseCardAbility.AbilityType,
            });
        }

        private void OnOverlordSkillUsedHandler(PlayerActionOverlordSkillUsed actionUseOverlordSkill)
        {
            GotActionUseOverlordSkill(new UseOverlordSkillModel
            {
                SkillId = new SkillId(actionUseOverlordSkill.SkillId),
                TargetId = actionUseOverlordSkill.Target.InstanceId.FromProtobuf(),
            });
        }

        private void OnRankBuffHandler(PlayerActionRankBuff actionRankBuff)
        {
            GotActionRankBuff(
                actionRankBuff.Card.FromProtobuf(),
                actionRankBuff.Targets.Select(t => t.FromProtobuf()).ToList()
                );
        }

        private void OnCheatDestroyCardsOnBoardActionHandler(PlayerActionCheatDestroyCardsOnBoard actionCheatDestroyCardsOnBoard)
        {
            GotCheatDestroyCardsOnBoard(actionCheatDestroyCardsOnBoard.DestroyedCards.Select(id => id.FromProtobuf()));
        }

        #endregion


        #region Actions

        private void GotActionEndTurn(EndTurnModel model)
        {
            if (_gameplayManager.IsGameEnded)
                return;

            _battlegroundController.EndTurn();
        }

        private void GotActionPlayCard(InstanceId cardId, int position)
        {
            if (_gameplayManager.IsGameEnded)
                return;

            BoardUnitView boardUnitViewElement = null;
            _cardsController.PlayOpponentCard(_gameplayManager.OpponentPlayer,
                cardId,
                null,
                workingCard =>
                {
                    switch (workingCard.LibraryCard.CardKind)
                    {
                        case Enumerators.CardKind.CREATURE:
                            boardUnitViewElement = new BoardUnitView(new BoardUnitModel(), _battlegroundController.OpponentBoardObject.transform);
                            GameObject boardUnit = boardUnitViewElement.GameObject;
                            boardUnitViewElement.Model.OwnerPlayer = workingCard.Owner;
                            boardUnitViewElement.SetObjectInfo(workingCard);
                            boardUnitViewElement.Model.TutorialObjectId = workingCard.TutorialObjectId;

                            boardUnit.tag = SRTags.OpponentOwned;
                            boardUnit.transform.position = Vector3.up * 2f; // Start pos before moving cards to the opponents board
                            boardUnit.SetActive(false);

                            _gameplayManager.OpponentPlayer.BoardCards.Insert(Mathf.Clamp(position, 0,
                                _gameplayManager.OpponentPlayer.BoardCards.Count),
                                boardUnitViewElement);
                            _battlegroundController.OpponentBoardCards.Insert(Mathf.Clamp(position, 0,
                                _battlegroundController.OpponentBoardCards.Count),
                                boardUnitViewElement);

                            _actionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam
                            {
                                ActionType = Enumerators.ActionType.PlayCardFromHand,
                                Caller = boardUnitViewElement.Model,
                                TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                            });

                            _abilitiesController.ResolveAllAbilitiesOnUnit(boardUnitViewElement.Model);

                            break;
                        case Enumerators.CardKind.SPELL:
                            BoardSpell spell = new BoardSpell(null, workingCard); // todo improve it with game Object aht will be aniamted
                            _gameplayManager.OpponentPlayer.BoardSpellsInUse.Insert(ItemPosition.End, spell);
                            spell.OwnerPlayer = _gameplayManager.OpponentPlayer;
                            _actionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam
                            {
                                ActionType = Enumerators.ActionType.PlayCardFromHand,
                                Caller = spell,
                                TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                            });
                            break;
                    }

                    _gameplayManager.OpponentPlayer.CurrentGoo -= workingCard.InstanceCard.Cost;
                },
                (workingCard, boardObject) =>
                {
                    switch (workingCard.LibraryCard.CardKind)
                    {
                        case Enumerators.CardKind.CREATURE:
                            boardUnitViewElement.GameObject.SetActive(true);
                            boardUnitViewElement.PlayArrivalAnimation(playUniqueAnimation: true);
                            _boardController.UpdateCurrentBoardOfPlayer(_gameplayManager.OpponentPlayer, null);
                            break;
                    }
                }
            );
        }

        private void GotActionCardAttack(CardAttackModel model)
        {
            if (_gameplayManager.IsGameEnded)
                return;

            BoardUnitModel attackerUnit = _battlegroundController.GetBoardUnitModelByInstanceId(model.CardId);
            BoardObject target = _battlegroundController.GetTargetByInstanceId(model.TargetId);

            if(attackerUnit == null || target == null)
            {
                Helpers.ExceptionReporter.LogException("GotActionCardAttack Has Error: attackerUnit: " + attackerUnit + "; target: " + target);
                return;
            }

            Action callback = () =>
            {
                attackerUnit.DoCombat(target);
            };

            BoardUnitView attackerUnitView = _battlegroundController.GetBoardUnitViewByModel(attackerUnit);

            if (attackerUnitView != null)
            {
                _boardArrowController.DoAutoTargetingArrowFromTo<OpponentBoardArrow>(attackerUnitView.Transform, target, action: callback);
            }
            else
            {
                Debug.LogWarning("Attacker with card Id " + model.CardId + " not found on this client in match.");
            }
        }

        private void GotActionUseCardAbility(UseCardAbilityModel model)
        {
            if (_gameplayManager.IsGameEnded)
                return;

            BoardObject boardObjectCaller = _battlegroundController.GetBoardObjectByInstanceId(model.Card);

            if (boardObjectCaller == null)
            {
                // FIXME: why do we have recursion here??
                GameClient.Get<IQueueManager>().AddTask(async () =>
                {
                    await new WaitForUpdate();
                    GotActionUseCardAbility(model);
                });

                return;
            }

            List<ParametrizedAbilityBoardObject> parametrizedAbilityObjects = new List<ParametrizedAbilityBoardObject>();

            foreach(Unit unit in model.Targets)
            {
                parametrizedAbilityObjects.Add(new ParametrizedAbilityBoardObject(
                    _battlegroundController.GetTargetByInstanceId(unit.InstanceId),
                    new ParametrizedAbilityParameters
                    {
                        Attack = unit.Parameter.Attack,
                        Defense = unit.Parameter.Defense,
                        CardName = unit.Parameter.CardName,
                    }
                ));
            }

            WorkingCard workingCard;
            switch (boardObjectCaller)
            {
                case BoardSpell boardSpell:
                    workingCard = boardSpell.Card;
                    break;
                case BoardUnitModel boardUnitModel:
                    workingCard = boardUnitModel.Card;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(boardObjectCaller));
            }

            _abilitiesController.PlayAbilityFromEvent(
                model.AbilityType,
                boardObjectCaller,
                parametrizedAbilityObjects,
                workingCard,
                _gameplayManager.OpponentPlayer);
        }

        private void GotActionUseOverlordSkill(UseOverlordSkillModel model)
        {
            if (_gameplayManager.IsGameEnded)
                return;

            BoardSkill skill = _battlegroundController.GetSkillById(_gameplayManager.OpponentPlayer, model.SkillId);
            BoardObject target = _battlegroundController.GetTargetByInstanceId(model.TargetId);

            if (target == null)
            {
                Helpers.ExceptionReporter.LogException("GotActionUseOverlordSkill Has Error: target: " + target);
                return;
            }

            skill.StartDoSkill();

            if (skill.Skill.CanSelectTarget)
            {
                Action callback = () =>
                {
                    switch (target)
                    {
                        case Player player:
                            skill.FightTargetingArrow.SelectedPlayer = player;
                            break;
                        case BoardUnitModel boardUnitModel:
                            skill.FightTargetingArrow.SelectedCard = _battlegroundController.GetBoardUnitViewByModel(boardUnitModel);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(target), target.GetType(), null);
                    }

                    skill.EndDoSkill();
                };

                skill.FightTargetingArrow = _boardArrowController.DoAutoTargetingArrowFromTo<OpponentBoardArrow>(skill.SelfObject.transform, target, action: callback);
            }
            else
            {
                skill.EndDoSkill();
            }
        }

        private void GotActionMulligan(MulliganModel model)
        {
            if (_gameplayManager.IsGameEnded)
                return;

            // todo implement logic..
        }

        private void GotActionRankBuff(InstanceId card, IList<Unit> targets)
        {
            if (_gameplayManager.IsGameEnded)
                return;

            List<BoardUnitView> units = _battlegroundController.GetTargetsByInstanceId(targets)
                .Cast<BoardUnitModel>()
                .Select(x => _battlegroundController.GetBoardUnitViewByModel(x)).ToList();

            WorkingCard workingCard = _battlegroundController.GetWorkingCardByInstanceId(card);
            if (workingCard == null)
                throw new Exception($"Board unit with instance ID {card} not found");

            _ranksController.BuffAllyManually(units, workingCard);
        }

        private void GotCheatDestroyCardsOnBoard(IEnumerable<InstanceId> cards)
        {
            foreach (InstanceId cardId in cards)
            {
                BoardUnitModel card = (BoardUnitModel) _battlegroundController.GetTargetByInstanceId(cardId);
                if (card == null)
                {
                    Debug.LogError($"Card {cardId} not found on board");
                }
                else
                {
                    card.Die(withDeathEffect: false);
                }
            }
        }

        #endregion
    }

    #region models
    public class EndTurnModel
    {
        public InstanceId CallerId;
    }

    public class MulliganModel
    {
        public InstanceId CallerId;
        public List<InstanceId> CardsIds;
    }

    public class DrawCardModel
    {
        public string CardName;
        public InstanceId CallerId;
        public InstanceId FromDeckOfPlayerId;
        public InstanceId TargetId;
        public Enumerators.AffectObjectType AffectObjectType;
    }


    public class UseOverlordSkillModel
    {
        public SkillId SkillId;
        public InstanceId TargetId;
    }

    public class UseCardAbilityModel
    {
        public InstanceId Card;
        public Enumerators.AbilityType AbilityType;
        public List<Unit> Targets;
    }

    public class CardAttackModel
    {
        public InstanceId CardId;
        public InstanceId TargetId;
    }

    public class TargetUnitModel
    {
        public InstanceId Target;
        public Enumerators.AffectObjectType AffectObjectType;
    }

    #endregion
}
