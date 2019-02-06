using System;
using System.Collections.Generic;
using System.Linq;
using Loom.ZombieBattleground.BackendCommunication;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using Loom.ZombieBattleground.Helpers;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Loom.ZombieBattleground
{
    public class GameplayManager : IService, IGameplayManager
    {
        private IDataManager _dataManager;

        private IMatchManager _matchManager;

        private ISoundManager _soundManager;

        private IUIManager _uiManager;

        private ITimerManager _timerManager;

        private ITutorialManager _tutorialManager;

        private IPvPManager _pvpManager;

        private BackendDataControlMediator _backendDataControlMediator;

        private List<IController> _controllers;

        private ActionCollectorUploader ActionLogCollectorUploader { get; } = new ActionCollectorUploader();

        public Enumerators.StartingTurn StartingTurn { get; set; }

        public event Action GameStarted;

        public event Action GameInitialized;

        public event Action<Enumerators.EndGameType> GameEnded;

#pragma warning disable 67
        public event Action TurnStarted;

        public event Action TurnEnded;
#pragma warning restore 67

        public int PlayerDeckId { get; set; }

        public int OpponentDeckId { get; set; }

        public bool IsGameStarted { get; set; }

        public bool IsGameEnded { get; set; }

        public bool IsTutorial { get; set; }

        public bool IsPreparingEnded { get; set; }

        public bool IsDesyncDetected { get; set; }

        public Player CurrentTurnPlayer { get; set; }

        public Player CurrentPlayer { get; set; }

        public Player OpponentPlayer { get; set; }

        public bool IsSpecificGameplayBattleground { get; set; }

        public bool CanDoDragActions { get; set; }

        public bool IsGameplayInputBlocked { get; set; }

        public PlayerMoveAction PlayerMoves { get; set; }

        public Deck CurrentPlayerDeck { get; set; }

        public Deck OpponentPlayerDeck { get; set; }

        public int OpponentIdCheat { get; set; }

        public bool AvoidGooCost { get; set; }

        public bool UseInifiniteAbility { get; set; }

        public AnalyticsTimer MatchDuration { get; set; }

        public Action TutorialStartAction { get; private set; }

        public Action TutorialGameplayBeginAction { get; private set; }

        public T GetController<T>()
            where T : IController
        {
            return (T) _controllers.Find(x => x is T);
        }

        public void RearrangeHands()
        {
            GetController<BoardController>().UpdateCurrentBoardOfPlayer(CurrentPlayer, null);
            GetController<BoardController>().UpdateCurrentBoardOfPlayer(OpponentPlayer, null);
        }

        public void EndGame(Enumerators.EndGameType endGameType, float timer = 4f)
        {
            if (IsGameEnded)
                return;

            IsGameEnded = true;

            MatchDuration.FinishTimer();

            _soundManager.PlaySound(Enumerators.SoundType.BACKGROUND, 128, Constants.BackgroundSoundVolume, null, true);

            if (endGameType != Enumerators.EndGameType.CANCEL)
            {
                InternalTools.DoActionDelayed(() =>
                     {
                         switch (endGameType)
                         {
                             case Enumerators.EndGameType.WIN:
                                 _uiManager.DrawPopup<YouWonPopup>();
                                 break;
                             case Enumerators.EndGameType.LOSE:
                                 _uiManager.DrawPopup<YouLosePopup>();
                                 break;
                             case Enumerators.EndGameType.CANCEL:
                                 break;
                             default:
                                 throw new ArgumentOutOfRangeException(nameof(endGameType), endGameType, null);
                         }
                     },
                     timer);
            }

            _soundManager.CrossfaidSound(Enumerators.SoundType.BACKGROUND, null, true);

            StopGameplay();

            StartingTurn = Enumerators.StartingTurn.UnDecided;
            

            _tutorialManager.PlayerWon = endGameType == Enumerators.EndGameType.WIN;
            _tutorialManager.ReportActivityAction(Enumerators.TutorialActivityAction.EndMatchPopupAppear);
            //GameClient.Get<IQueueManager>().StopNetworkThread();

            GameEnded?.Invoke(endGameType);
            
        }

        public void StartGameplay()
        {
            _uiManager.DrawPopup<PreparingForBattlePopup>();

            MatchDuration.StartTimer();

            _timerManager.AddTimer(
                x =>
                {
                    _uiManager.HidePopup<PreparingForBattlePopup>();

                    IsGameStarted = true;
                    IsGameEnded = false;
                    IsPreparingEnded = false;
                    IsDesyncDetected = false;

                    CanDoDragActions = true;

                    GameStarted?.Invoke();

                    StartInitializeGame();
                },
                null,
                2f);
        }

        public void StopGameplay()
        {
            IsGameStarted = false;
            IsGameEnded = true;
            IsPreparingEnded = false;

            CanDoDragActions = false;
            AvoidGooCost = false;
        }

        public bool IsLocalPlayerTurn()
        {
            return CurrentTurnPlayer == CurrentPlayer;
        }

        public Player GetOpponentByPlayer(Player player)
        {
            return player.IsLocalPlayer ? OpponentPlayer : CurrentPlayer;
        }

        public Player GetPlayerById(int id)
        {
            return CurrentPlayer.InstanceId.Id == id ? CurrentPlayer : OpponentPlayer;
        }

        public void ResetWholeGameplayScene()
        {
            foreach (IController controller in _controllers)
            {
                controller.ResetAll();
            }
        }

        public bool IsGameplayReady()
        {
            return !IsGameEnded && IsGameStarted && IsPreparingEnded;
        }

        public void Dispose()
        {
            foreach (IController item in _controllers)
            {
                item.Dispose();
            }
        }

        public void Init()
        {
            _dataManager = GameClient.Get<IDataManager>();
            _matchManager = GameClient.Get<IMatchManager>();
            _soundManager = GameClient.Get<ISoundManager>();
            _uiManager = GameClient.Get<IUIManager>();
            _timerManager = GameClient.Get<ITimerManager>();
            _tutorialManager = GameClient.Get<ITutorialManager>();
            _pvpManager = GameClient.Get<IPvPManager>();
            _backendDataControlMediator = GameClient.Get<BackendDataControlMediator>();

            _matchManager.MatchFinished += MatchFinishedHandler;

            InitControllers();

            if (!_dataManager.CachedUserLocalData.Tutorial)
            {
                Constants.ZombiesSoundVolume = 0.25f;
                Constants.CreatureAttackSoundVolume *= 3;
            }

            OpponentIdCheat = -1;
            AvoidGooCost = false;
            UseInifiniteAbility = false;
            MatchDuration = new AnalyticsTimer();
        }

        public void Update()
        {
            foreach (IController item in _controllers)
            {
                item.Update();
            }
        }

        private void InitControllers()
        {
            _controllers = new List<IController>
            {
                new VfxController(),
                new ParticlesController(),
                new AbilitiesController(),
                new ActionsQueueController(),
                new PlayerController(),
                new AIController(),
                new CardsController(),
                new BattlegroundController(),
                new AnimationsController(),
                new BattleController(),
                new BoardArrowController(),
                new SkillsController(),
                new RanksController(),
                new InputController(),
                new OpponentController(),
                new UniqueAnimationsController(),
                new BoardController(),
                new OverlordsTalkingController(),
                new HandPointerController()
            };

            foreach (IController controller in _controllers)
            {
                controller.Init();
            }
        }

        private void StartInitializeGame()
        {
            if (Constants.DevModeEnabled) {
                AvoidGooCost = true;
            }

            if (IsTutorial)
            {
                IsSpecificGameplayBattleground = true;
            }

            PlayerMoves = new PlayerMoveAction();

            switch (_matchManager.MatchType)
            {
                case Enumerators.MatchType.LOCAL:
                    GetController<PlayerController>().InitializePlayer(new InstanceId(0));
                    GetController<AIController>().InitializePlayer(new InstanceId(1));
                    break;
                case Enumerators.MatchType.PVP:
                    bool localPlayerHasZeroIndex =
                        _pvpManager.InitialGameState.PlayerStates[0].Id == _backendDataControlMediator.UserDataModel.UserId;
                    GetController<PlayerController>().InitializePlayer(new InstanceId(localPlayerHasZeroIndex ? 0 : 1));
                    GetController<OpponentController>().InitializePlayer(new InstanceId(!localPlayerHasZeroIndex ? 0 : 1));
                    AvoidGooCost = _pvpManager.DebugCheats.Enabled && _pvpManager.DebugCheats.IgnoreGooRequirements;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(_matchManager.MatchType), _matchManager.MatchType, null);
            }

            GetController<SkillsController>().InitializeSkills();
            GetController<BattlegroundController>().InitializeBattleground();

            if (IsTutorial)
            {
                CurrentTurnPlayer = _tutorialManager.CurrentTutorial.TutorialContent.ToGameplayContent().
                                    SpecificBattlegroundInfo.PlayerTurnFirst ? CurrentPlayer : OpponentPlayer;

                StartingTurn = CurrentTurnPlayer == CurrentPlayer ?
                    Enumerators.StartingTurn.Player : Enumerators.StartingTurn.Enemy;

                TutorialGameplayBeginAction = () =>
                {
                    GetController<PlayerController>().SetHand();
                    GetController<CardsController>().StartCardDistribution();

                    if (!_tutorialManager.CurrentTutorial.TutorialContent.ToGameplayContent().GameplayFlowBeginsManually)
                    {
                        if (_dataManager.CachedUserLocalData.Tutorial && !_tutorialManager.IsTutorial)
                        {
                            _tutorialManager.StartTutorial();
                        }
                    }

                    if (_tutorialManager.CurrentTutorial.IsGameplayTutorial() &&
                        _tutorialManager.CurrentTutorial.TutorialContent.ToGameplayContent().SpecificBattlegroundInfo.DisabledInitialization)
                    {
                        OpponentPlayer.SetFirstHandForLocalMatch(false);
                    }
                };

                TutorialStartAction = () =>
                {
                    if (_tutorialManager.CurrentTutorial.TutorialContent.ToGameplayContent().PlayerOrderScreenShouldAppear)
                    {
                        _uiManager.DrawPopup<PlayerOrderPopup>(new object[]
                        {
                            CurrentPlayer.SelfHero, OpponentPlayer.SelfHero
                        });
                    }
                    else
                    {
                        TutorialGameplayBeginAction();
                    }
                };

                if (_tutorialManager.CurrentTutorial.IsGameplayTutorial())
                {
                    if (!_tutorialManager.CurrentTutorial.TutorialContent.ToGameplayContent().GameplayFlowBeginsManually)
                    {
                        TutorialStartAction();
                    }
                    else
                    {
                        if (_dataManager.CachedUserLocalData.Tutorial && !_tutorialManager.IsTutorial)
                        {
                            _tutorialManager.StartTutorial();
                        }
                    }
                }
            }
            else
            {
                IsSpecificGameplayBattleground = false;

                switch (_matchManager.MatchType)
                {
                    case Enumerators.MatchType.LOCAL:
                        switch (StartingTurn)
                        {
                            case Enumerators.StartingTurn.UnDecided:
                                CurrentTurnPlayer = Random.Range(0, 100) > 50 ? CurrentPlayer : OpponentPlayer;
                                StartingTurn = CurrentTurnPlayer == CurrentPlayer ?
                                    Enumerators.StartingTurn.Player : Enumerators.StartingTurn.Enemy;
                                break;
                            case Enumerators.StartingTurn.Player:
                                CurrentTurnPlayer = CurrentPlayer;
                                break;
                            case Enumerators.StartingTurn.Enemy:
                                CurrentTurnPlayer = OpponentPlayer;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        OpponentPlayer.SetFirstHandForLocalMatch(false);
                        break;
                    case Enumerators.MatchType.PVP:
                        CurrentTurnPlayer = GameClient.Get<IPvPManager>().IsFirstPlayer() ? CurrentPlayer : OpponentPlayer;
                        List<WorkingCard> opponentCardsInHand =
                            OpponentPlayer.InitialPvPPlayerState.CardsInHand
                                .Select(instance => instance.FromProtobuf(OpponentPlayer))
                                .ToList();

                        Debug.Log(
                            $"Player ID {OpponentPlayer.InstanceId}, local: {OpponentPlayer.IsLocalPlayer}, added CardsInHand:\n" +
                            String.Join(
                                "\n",
                                (IList<WorkingCard>)opponentCardsInHand
                                    .OrderBy(card => card.InstanceId)
                                    .ToArray()
                            )
                        );

                        OpponentPlayer.SetFirstHandForPvPMatch(opponentCardsInHand, false);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(_matchManager.MatchType), _matchManager.MatchType, null);
                }

                _uiManager.DrawPopup<PlayerOrderPopup>(new object[]
                {
                    CurrentPlayer.SelfHero, OpponentPlayer.SelfHero
                });
            }

            IsGameEnded = false;

            GameInitialized?.Invoke();
        }

        private void MatchFinishedHandler()
        {
            CurrentPlayer = null;
            OpponentPlayer = null;
            CurrentTurnPlayer = null;
            PlayerMoves = null;
        }
    }
}
