using System;
using System.Threading.Tasks;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Protobuf;
using UnityEngine;
using Deck = Loom.ZombieBattleground.Data.Deck;

namespace Loom.ZombieBattleground
{
    public class MatchManager : IService, IMatchManager
    {
        public event Action MatchFinished;

        private IUIManager _uiManager;

        private IScenesManager _sceneManager;

        private IAppStateManager _appStateManager;

        private IGameplayManager _gameplayManager;

        private ITutorialManager _tutorialManager;

        private IPvPManager _pvpManager;

        private Enumerators.AppState _finishMatchAppState;

        public Enumerators.MatchType MatchType { get; set; }

        public AnalyticsTimer FindOpponentTime { get; set; }

        public void FinishMatch(Enumerators.AppState appStateAfterMatch)
        {
            MatchFinished?.Invoke();

            _tutorialManager.StopTutorial();

            if (_gameplayManager.IsTutorial &&
                !_tutorialManager.IsTutorial &&
                appStateAfterMatch != Enumerators.AppState.MAIN_MENU)
            {
                _sceneManager.ChangeScene(Enumerators.AppState.GAMEPLAY, true);
                return;
            }

            _finishMatchAppState = appStateAfterMatch;

            _uiManager.HideAllPages();
            _uiManager.HideAllPopups();
            _uiManager.DrawPopup<LoadingGameplayPopup>();

            _gameplayManager.ResetWholeGameplayScene();

            _sceneManager.ChangeScene(Enumerators.AppState.APP_INIT);
        }

        public async Task FindMatch()
        {
            switch (MatchType)
            {
                case Enumerators.MatchType.LOCAL:
                    CreateLocalMatch();
                    break;
                case Enumerators.MatchType.PVP:
                    {
                        try
                        {
                            FindOpponentTime.StartTimer();
                            GameClient.Get<IQueueManager>().Clear();

                            MatchMakingPopup matchMakingPopup = _uiManager.GetPopup<MatchMakingPopup>();
                            matchMakingPopup.CancelMatchmakingClicked += MatchMakingPopupOnCancelMatchmakingClicked;
                            matchMakingPopup.Show();
                            await _pvpManager.StartMatchmaking((int)_gameplayManager.CurrentPlayerDeck.Id);
                            _pvpManager.MatchMakingFlowController.StateChanged += MatchMakingFlowControllerOnStateChanged;

                            _pvpManager.GameStartedActionReceived -= OnPvPManagerGameStartedActionReceived;
                            _pvpManager.GameStartedActionReceived += OnPvPManagerGameStartedActionReceived;
                        }
                        catch (Exception e) {

                            Helpers.ExceptionReporter.LogException(e);

                            Debug.LogWarning(e);
                            MatchMakingPopup matchMakingPopup = _uiManager.GetPopup<MatchMakingPopup>();
                            matchMakingPopup.CancelMatchmakingClicked -= MatchMakingPopupOnCancelMatchmakingClicked;
                            matchMakingPopup.Hide();
                            _uiManager.DrawPopup<WarningPopup>($"Error while finding a match:\n{e.Message}");
                        }
                    }
                    break;
                default:
                    throw new NotImplementedException(MatchType + " not implemented yet.");
            }
        }

        private void MatchMakingFlowControllerOnStateChanged(MatchMakingFlowController.MatchMakingState state)
        {
            MatchMakingPopup matchMakingPopup = _uiManager.GetPopup<MatchMakingPopup>();
            matchMakingPopup.SetUIState(state);
        }

        private async void MatchMakingPopupOnCancelMatchmakingClicked()
        {
            MatchMakingPopup matchMakingPopup = _uiManager.GetPopup<MatchMakingPopup>();
            MatchMakingFlowController matchMakingFlowController = _pvpManager.MatchMakingFlowController;

            try
            {
                matchMakingPopup.SetUIState(MatchMakingFlowController.MatchMakingState.Canceled);
                await _pvpManager.StopMatchmaking();
            }
            catch (Exception e)
            {
                Helpers.ExceptionReporter.LogException(e);
                Debug.LogWarning(e);
                _uiManager.GetPopup<MatchMakingPopup>().Hide();
                _uiManager.DrawPopup<WarningPopup>($"Error while canceling finding a match:\n{e.Message}");
            }
            finally
            {
                HandleEndMatchmaking(matchMakingPopup, matchMakingFlowController);
            }
        }

        public async Task FindMatch(Enumerators.MatchType matchType)
        {
            MatchType = matchType;
            await FindMatch();
        }

        public void Dispose()
        {
            _sceneManager.SceneForAppStateWasLoadedEvent -= SceneForAppStateWasLoadedEventHandler;
        }

        public void Init()
        {
            MatchType = Enumerators.MatchType.LOCAL;
            _uiManager = GameClient.Get<IUIManager>();
            _sceneManager = GameClient.Get<IScenesManager>();
            _appStateManager = GameClient.Get<IAppStateManager>();
            _gameplayManager = GameClient.Get<IGameplayManager>();
            _tutorialManager = GameClient.Get<ITutorialManager>();
            _pvpManager = GameClient.Get<IPvPManager>();

            _sceneManager.SceneForAppStateWasLoadedEvent += SceneForAppStateWasLoadedEventHandler;
            _pvpManager.MatchingFailed += OnPvPManagerMatchingFailed;

            FindOpponentTime = new AnalyticsTimer();
        }

        public void Update()
        {

        }

        private void CreateLocalMatch()
        {
            StartLoadMatch();
        }

        private void StartPvPMatch()
        {
            _uiManager.HidePopup<ConnectionPopup>();
            CreateLocalMatch();
        }

        private void OnPvPManagerGameStartedActionReceived()
        {
            HandleEndMatchmaking(_uiManager.GetPopup<MatchMakingPopup>(), _pvpManager.MatchMakingFlowController);
            StartPvPMatch();
        }

        private void HandleEndMatchmaking(MatchMakingPopup matchMakingPopup, MatchMakingFlowController matchMakingFlowController)
        {
            FindOpponentTime.FinishTimer();

            if (matchMakingFlowController != null)
            {
                matchMakingFlowController.StateChanged -= MatchMakingFlowControllerOnStateChanged;
            }
            matchMakingPopup.CancelMatchmakingClicked -= MatchMakingPopupOnCancelMatchmakingClicked;
            matchMakingPopup.Hide();
            _pvpManager.GameStartedActionReceived -= OnPvPManagerGameStartedActionReceived;
        }

        private void OnPvPManagerMatchingFailed()
        {
            _uiManager.GetPopup<ConnectionPopup>().Hide();
            _uiManager.DrawPopup<WarningPopup>("Couldn't find an opponent.");
        }

        private void StartLoadMatch()
        {
            _uiManager.HideAllPages();
            _uiManager.DrawPopup<LoadingGameplayPopup>();

            _sceneManager.ChangeScene(Enumerators.AppState.GAMEPLAY);
        }

        private void SceneForAppStateWasLoadedEventHandler(Enumerators.AppState state)
        {
            switch (state)
            {
                case Enumerators.AppState.GAMEPLAY:
                    {
                        ForceStartGameplay(_gameplayManager.IsTutorial);
                    }
                    break;
                case Enumerators.AppState.APP_INIT:
                    {
                        _appStateManager.ChangeAppState(_finishMatchAppState);

                        _tutorialManager.CheckNextTutorial();
                    }
                    break;
            }
        }

        private void ForceStartGameplay(bool force = false)
        {
            Debug.Log(_gameplayManager.IsTutorial);
            if (_gameplayManager.IsTutorial)
            {
                _tutorialManager.SetupTutorialById(GameClient.Get<IDataManager>().CachedUserLocalData.CurrentTutorialId);
            }

            _appStateManager.ChangeAppState(Enumerators.AppState.GAMEPLAY, force);

            _uiManager.HidePopup<LoadingGameplayPopup>();

            _gameplayManager.StartGameplay();
        }
    }
}
