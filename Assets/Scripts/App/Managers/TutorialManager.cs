using System;
using System.Collections.Generic;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using UnityEngine;
using Newtonsoft.Json;
using Object = UnityEngine.Object;
using DG.Tweening;
using Loom.ZombieBattleground.BackendCommunication;
using System.Globalization;
using Newtonsoft.Json.Converters;
using Loom.ZombieBattleground.Helpers;

namespace Loom.ZombieBattleground
{
    public class TutorialManager : IService, ITutorialManager
    {
        private const string TutorialDataPath = "Data/tutorial_data";

        private IUIManager _uiManager;

        private ISoundManager _soundManager;

        private ILoadObjectsManager _loadObjectsManager;

        private BackendFacade _backendFacade;

        private BackendDataControlMediator _backendDataControlMediator;

        private IDataManager _dataManager;

        private IGameplayManager _gameplayManager;

        private BattlegroundController _battlegroundController;

        private IAnalyticsManager _analyticsManager;

        private OverlordsChatController _overlordsChatController;

        private HandPointerController _handPointerController;

        private List<TutorialDescriptionTooltipItem> _tutorialDescriptionTooltipItems;

        private List<Enumerators.TutorialActivityAction> _activitiesDoneDuringThisTurn;

        public bool IsTutorial { get; private set; }

        private List<TutorialData> _tutorials;
        private List<TutorialStep> _tutorialSteps;
        private int _currentTutorialStepIndex;

        public TutorialData CurrentTutorial { get; private set; }
        public TutorialStep CurrentTutorialStep { get; private set; }

        public AnalyticsTimer TutorialDuration { get; set; }

        public int TutorialsCount
        {
            get { return _tutorials.Count; }
        }

        public void Dispose()
        {
        }

        public void Init()
        {
            _uiManager = GameClient.Get<IUIManager>();
            _soundManager = GameClient.Get<ISoundManager>();
            _loadObjectsManager = GameClient.Get<ILoadObjectsManager>();
            _dataManager = GameClient.Get<IDataManager>();
            _gameplayManager = GameClient.Get<IGameplayManager>();
            _analyticsManager = GameClient.Get<IAnalyticsManager>();
            _backendFacade = GameClient.Get<BackendFacade>();
            _backendDataControlMediator = GameClient.Get<BackendDataControlMediator>();

            _overlordsChatController = _gameplayManager.GetController<OverlordsChatController>();
            _handPointerController = _gameplayManager.GetController<HandPointerController>();

            _battlegroundController = _gameplayManager.GetController<BattlegroundController>();

            var settings = new JsonSerializerSettings
            {
                Culture = CultureInfo.InvariantCulture,
                Converters = {
                    new StringEnumConverter()
                },
                CheckAdditionalContent = true,
                MissingMemberHandling = MissingMemberHandling.Error,
                TypeNameHandling = TypeNameHandling.Auto,
                Error = (sender, args) =>
                {
                    Debug.LogException(args.ErrorContext.Error);
                }
            };

            _tutorials = JsonConvert.DeserializeObject<List<TutorialData>>(_loadObjectsManager
                        .GetObjectByPath<TextAsset>(TutorialDataPath).text, settings);

            TutorialDuration = new AnalyticsTimer();

            _tutorialDescriptionTooltipItems = new List<TutorialDescriptionTooltipItem>();
            _activitiesDoneDuringThisTurn = new List<Enumerators.TutorialActivityAction>();
        }

        public void Update()
        {
        }

        public void SetupTutorialById(int id)
        {
            if (CheckAvailableTutorial())
            {
                CurrentTutorial = _tutorials.Find(tutor => tutor.Id == id);
                _currentTutorialStepIndex = 0;
                _tutorialSteps = CurrentTutorial.TutorialContent.TutorialSteps;
                CurrentTutorialStep = _tutorialSteps[_currentTutorialStepIndex];
                FillTutorialDeck();
            }

            IsTutorial = false;
        }

        private bool CheckAvailableTutorial()
        {
            int id = _dataManager.CachedUserLocalData.CurrentTutorialId;

            TutorialData tutorial = _tutorials.Find((x) => !x.Ignore &&
                x.Id >= _dataManager.CachedUserLocalData.CurrentTutorialId);

            if (tutorial != null)
            {
                _dataManager.CachedUserLocalData.CurrentTutorialId = tutorial.Id;
                _dataManager.SaveCache(Enumerators.CacheDataType.USER_LOCAL_DATA);
                return true;
            }
            return false;
        }

        public void StartTutorial()
        {
            if (IsTutorial)
                return;

            IsTutorial = true;

            if (CurrentTutorial.IsGameplayTutorial())
            {
                _battlegroundController.SetupBattlegroundAsSpecific(CurrentTutorial.TutorialContent.ToGameplayContent().SpecificBattlegroundInfo);

                _battlegroundController.TurnStarted += TurnStartedHandler;

                _gameplayManager.GetController<InputController>().UnitSelectedEvent += UnitSelectedEventHandler;
                _gameplayManager.GetController<InputController>().UnitDeselectedEvent += UnitDeselectedEventHandler;
                _gameplayManager.GetController<InputController>().PlayerSelectedEvent += PlayerSelectedEventHandler;
                _gameplayManager.GetController<InputController>().PlayerPointerEnteredEvent += PlayerSelectedEventHandler;
                _gameplayManager.GetController<InputController>().UnitPointerEnteredEvent += UnitSelectedEventHandler;
            }

            EnableStepContent(CurrentTutorialStep);

            TutorialDuration.StartTimer();
            _analyticsManager.SetEvent(AnalyticsManager.EventStartedTutorial);
        }

        private void PlayerSelectedEventHandler(Player player)
        {
            SetTooltipsPlayerIfHas(player, true);
        }

        private void UnitSelectedEventHandler(BoardUnitView unit)
        {
            SetTooltipsStateIfHas(unit, true);
        }

        private void UnitDeselectedEventHandler(BoardUnitView unit)
        {
            SetTooltipsStateIfHas(unit, false);
        }

        private void TurnStartedHandler()
        {
            _activitiesDoneDuringThisTurn.Clear();
        }

        public void StopTutorial()
        {
            if (!IsTutorial)
                return;


            if (CurrentTutorial.IsGameplayTutorial())
            {
                _battlegroundController.TurnStarted -= TurnStartedHandler;

                _gameplayManager.GetController<InputController>().UnitSelectedEvent -= UnitSelectedEventHandler;
                _gameplayManager.GetController<InputController>().UnitDeselectedEvent -= UnitDeselectedEventHandler;
                _gameplayManager.GetController<InputController>().PlayerSelectedEvent -= PlayerSelectedEventHandler;
                _gameplayManager.GetController<InputController>().PlayerPointerEnteredEvent -= PlayerSelectedEventHandler;
                _gameplayManager.GetController<InputController>().UnitPointerEnteredEvent -= UnitSelectedEventHandler;
            }

            _uiManager.HidePopup<TutorialAvatarPopup>();

            _soundManager.StopPlaying(Enumerators.SoundType.TUTORIAL);

            if (_dataManager.CachedUserLocalData.CurrentTutorialId >= _tutorials.Count - 1)
            {
                _dataManager.CachedUserLocalData.CurrentTutorialId = 0;
                _gameplayManager.IsTutorial = false;
                _dataManager.CachedUserLocalData.Tutorial = false;
                _gameplayManager.IsSpecificGameplayBattleground = false;
            }

            _dataManager.CachedUserLocalData.CurrentTutorialId++;

            if (!CheckAvailableTutorial())
            {
                _gameplayManager.IsTutorial = false;
                _dataManager.CachedUserLocalData.Tutorial = false;
                _gameplayManager.IsSpecificGameplayBattleground = false;
            }


            IsTutorial = false;
            _dataManager.SaveCache(Enumerators.CacheDataType.USER_LOCAL_DATA);

            TutorialDuration.FinishTimer();
            Dictionary<string, object> eventParameters = new Dictionary<string, object>();
            eventParameters.Add(AnalyticsManager.PropertyTutorialTimeToComplete, TutorialDuration.GetTimeDiffrence());
            _analyticsManager.SetEvent(AnalyticsManager.EventCompletedTutorial, eventParameters);
        }

        public SpecificTurnInfo GetCurrentTurnInfo()
        {
            if (!IsTutorial)
                return null;

            return CurrentTutorial.TutorialContent.ToGameplayContent().SpecificTurnInfos.Find(x => x.TurnIndex == _battlegroundController.CurrentTurn - 1);
        }

        public bool IsCompletedActivitiesForThisTurn()
        {
            foreach (Enumerators.TutorialActivityAction activityAction in GetCurrentTurnInfo().RequiredActivitiesToDoneDuringTurn)
            {
                if (!_activitiesDoneDuringThisTurn.Contains(activityAction))
                    return false;
            }

            return true;
        }

        public void ReportActivityAction(Enumerators.TutorialActivityAction action, int sender = 0)
        {
            if (!IsTutorial)
                return;

            if (action == Enumerators.TutorialActivityAction.TapOnScreen)
            {
                HideAllActiveDescriptionTooltip();
            }

            bool skip = false;
            foreach (TutorialData tutorial in _tutorials)
            {
                foreach (ActionActivityHandler activityHandler in tutorial.TutorialContent.ActionActivityHandlers)
                {
                    if (activityHandler.TutorialActivityAction == action)
                    {
                        DoActionByActivity(activityHandler);
                        skip = true;
                        break;
                    }
                }

                if (skip)
                    break;
            }

            if (CurrentTutorial.IsGameplayTutorial() && _battlegroundController.CurrentTurn > 1)
            {
                SpecificTurnInfo specificTurnInfo = GetCurrentTurnInfo();

                if (specificTurnInfo != null)
                {
                    if (specificTurnInfo.ActionActivityHandlers != null)
                    {
                        foreach (var activity in specificTurnInfo.ActionActivityHandlers)
                        {
                            if (!_activitiesDoneDuringThisTurn.Contains(activity.ConnectedTutorialActivityAction) &&
                                activity.TutorialActivityAction == action)
                            {
                                DoActionByActivity(activity);
                                break;
                            }
                        }
                    }

                    if (specificTurnInfo.RequiredActivitiesToDoneDuringTurn != null)
                    {
                        if (specificTurnInfo.RequiredActivitiesToDoneDuringTurn.Contains(action) &&
                           action != CurrentTutorialStep.ActionToEndThisStep)
                        {

                            List<TutorialStep> steps = CurrentTutorial.TutorialContent.TutorialSteps.FindAll(x =>
                                                         x.ToGameplayStep().ConnectedTurnIndex == specificTurnInfo.TurnIndex);

                            foreach (TutorialStep step in steps)
                            {
                                if (step.ActionToEndThisStep == action && !step.IsDone)
                                {
                                    if (step.ToGameplayStep().TutorialObjectIdStepOwner == 0 || step.ToGameplayStep().TutorialObjectIdStepOwner == sender)
                                    {
                                        step.IsDone = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            _activitiesDoneDuringThisTurn.Add(action);

            if (CurrentTutorialStep != null && action == CurrentTutorialStep.ActionToEndThisStep)
            {
                MoveToNextStep();
            }
        }

        private void MoveToNextStep()
        {
            if (CurrentTutorialStep != null)
            {
                _handPointerController.ResetAll();
                CurrentTutorialStep.IsDone = true;
            }

            if (_currentTutorialStepIndex + 1 >= _tutorialSteps.Count)
            {
                GameClient.Get<IGameplayManager>().EndGame(Enumerators.EndGameType.WIN, 0);
                ClearToolTips();
            }
            else
            {
                CurrentTutorialStep = GetNextNotDoneStep();

                EnableStepContent(CurrentTutorialStep);
            }
        }

        private TutorialStep GetNextNotDoneStep()
        {
            for(int i = _currentTutorialStepIndex + 1; i < _tutorialSteps.Count; i++)
            {
                if (!_tutorialSteps[i].IsDone)
                {
                    _currentTutorialStepIndex = i;
                    return _tutorialSteps[i];
                }
            }

            return _tutorialSteps[_currentTutorialStepIndex];
        }

        private async void EnableStepContent(TutorialStep step)
        {
            switch (step)
            {
                case TutorialGameplayStep gameStep:

                    if (gameStep.HandPointers != null)
                    {
                        foreach (HandPointerInfo handPointer in gameStep.HandPointers)
                        {
                            DrawPointer(handPointer.TutorialHandPointerType,
                                        (Vector3)handPointer.StartPosition,
                                        (Vector3)handPointer.EndPosition,
                                        handPointer.AppearDelay,
                                        handPointer.AppearOnce,
                                        handPointer.TutorialObjectIdStepOwner);
                        }
                    }

                    if (gameStep.OverlordSayTooltips != null)
                    {
                        foreach (OverlordSayTooltipInfo tooltip in gameStep.OverlordSayTooltips)
                        {
                            DrawOverlordSayPopup(tooltip.Description, tooltip.TutorialTooltipAlign, tooltip.TutorialTooltipOwner, tooltip.AppearDelay);
                        }
                    }

                    if (gameStep.TutorialDescriptionTooltipsToActivate != null)
                    {
                        foreach (int tooltipId in gameStep.TutorialDescriptionTooltipsToActivate)
                        {
                            TutorialDescriptionTooltip tooltip = CurrentTutorial.TutorialContent.TutorialDescriptionTooltips.Find(x => x.Id == tooltipId);

                            DrawDescriptionTooltip(tooltip.Id,
                                                   tooltip.Description,
                                                   tooltip.TutorialTooltipAlign,
                                                   tooltip.TutorialTooltipOwner,
                                                   tooltip.TutorialTooltipOwnerName,
                                                   (Vector3)tooltip.Position,
                                                   tooltip.Resizable);
                        }
                    }

                    if (gameStep.TutorialDescriptionTooltipsToDeactivate != null)
                    {
                        foreach (int tooltipId in gameStep.TutorialDescriptionTooltipsToDeactivate)
                        {
                            DeactivateDescriptionTooltip(tooltipId);
                        }
                    }

                    if (gameStep.TutorialAvatar != null)
                    {
                        DrawAvatar(gameStep.TutorialAvatar.Description, gameStep.TutorialAvatar.DescriptionTooltipCloseText, gameStep.TutorialAvatar.Pose);
                    }

                    if(gameStep.LaunchGameplayManually)
                    {
                        _battlegroundController.StartGameplayTurns();
                    }

                    if(!string.IsNullOrEmpty(gameStep.SoundToPlay))
                    {
                        PlayTutorialSound(gameStep.SoundToPlay, gameStep.SoundToPlayBeginDelay);
                    }

                    if (gameStep.LaunchAIBrain)
                    {
                       await _gameplayManager.GetController<AIController>().LaunchAIBrain();
                    }

                    break;
                case TutorialMenuStep menuStep:
                    break;
            }
        }

        public string GetCardNameById(int id)
        {
            SpecificBattlegroundInfo battleInfo = CurrentTutorial.TutorialContent.ToGameplayContent().SpecificBattlegroundInfo;

            List<SpecificBattlegroundInfo.OverlordCardInfo> cards = new List<SpecificBattlegroundInfo.OverlordCardInfo>();

            cards.AddRange(battleInfo.PlayerInfo.CardsInDeck);
            cards.AddRange(battleInfo.PlayerInfo.CardsInHand);
            cards.AddRange(battleInfo.OpponentInfo.CardsInDeck);
            cards.AddRange(battleInfo.OpponentInfo.CardsInHand);

            return cards.Find(x => x.TutorialObjectId == id)?.Name;
        }

        public void SetTooltipsStateIfHas(BoardUnitView unit, bool isActive)
        {
            int ownerId = unit.Model.TutorialObjectId;

            TutorialStep step = CurrentTutorial.TutorialContent.TutorialSteps.Find(x => x.ToGameplayStep().TutorialObjectIdStepOwner == ownerId &&
                                                               x.ToGameplayStep().TutorialDescriptionTooltipsToActivate.Count > 0);
            if (step != null)
            {
                foreach (int id in step.TutorialDescriptionTooltipsToActivate)
                {
                    ActivateDescriptionTooltip(id);
                }
            }
        }

        public void SetTooltipsPlayerIfHas(Player player, bool isActive)
        {
            List<TutorialDescriptionTooltipItem> tooltips = _tutorialDescriptionTooltipItems.FindAll(x => x.OwnerType == Enumerators.TooltipOwner.PlayerOverlord ||
                x.OwnerType == Enumerators.TooltipOwner.EnemyOverlord);

            if(tooltips.Count > 0)
            {
                foreach (TutorialDescriptionTooltipItem tooltip in tooltips)
                {
                    if (isActive)
                    {
                        ActivateDescriptionTooltip(tooltip.Id);
                    }
                    else
                    {
                        HideDescriptionTooltip(tooltip.Id);
                    }
                }
            }
        }

        public void SetupBattleground(SpecificBattlegroundInfo specificBattleground)
        {
            _battlegroundController.SetupBattlegroundAsSpecific(specificBattleground);
        }

        public void FillTutorialDeck()
        {
            _gameplayManager.CurrentPlayerDeck =
                         new Deck(0, CurrentTutorial.TutorialContent.ToGameplayContent().SpecificBattlegroundInfo.PlayerInfo.OverlordId,
                         "TutorialDeck", new List<DeckCardData>(),
                         CurrentTutorial.TutorialContent.ToGameplayContent().SpecificBattlegroundInfo.PlayerInfo.PrimaryOverlordAbility,
                         CurrentTutorial.TutorialContent.ToGameplayContent().SpecificBattlegroundInfo.PlayerInfo.SecondaryOverlordAbility);

            _gameplayManager.OpponentPlayerDeck =
                        new Deck(0, CurrentTutorial.TutorialContent.ToGameplayContent().SpecificBattlegroundInfo.OpponentInfo.OverlordId,
                        "TutorialDeckOpponent", new List<DeckCardData>(),
                        CurrentTutorial.TutorialContent.ToGameplayContent().SpecificBattlegroundInfo.OpponentInfo.PrimaryOverlordAbility,
                        CurrentTutorial.TutorialContent.ToGameplayContent().SpecificBattlegroundInfo.OpponentInfo.SecondaryOverlordAbility);
        }

        public void PlayTutorialSound(string sound, float delay = 0f)
        {
            InternalTools.DoActionDelayed(() =>
            {
                _soundManager.PlaySound(Enumerators.SoundType.TUTORIAL, 0, sound, Constants.TutorialSoundVolume, false);
            }, delay);
        }

        public void DrawAvatar(string description, string hideAvatarButtonText, Enumerators.TutorialAvatarPose pose)
        {
            _uiManager.DrawPopup<TutorialAvatarPopup>(new object[]
            {
                description,
                hideAvatarButtonText,
                pose
            });
        }

        public void DrawPointer(Enumerators.TutorialHandPointerType type,
                                Vector3 begin,
                                Vector3? end = null,
                                float appearDelay = 0,
                                bool appearOnce = false,
                                int tutorialObjectIdStepOwner = 0)
        {
            _handPointerController.DrawPointer(type, begin, end, appearDelay, appearOnce, tutorialObjectIdStepOwner);
        }

        public void DrawDescriptionTooltip(int id,
                                           string description,
                                           Enumerators.TooltipAlign align,
                                           Enumerators.TooltipOwner owner,
                                           string ownerName,
                                           Vector3 position,
                                           bool resizable)
        {
            TutorialDescriptionTooltipItem tooltipItem = new TutorialDescriptionTooltipItem(id, description, align, owner, ownerName, position, resizable);

            _tutorialDescriptionTooltipItems.Add(tooltipItem);
        }

        public void ActivateDescriptionTooltip(int id)
        {
           _tutorialDescriptionTooltipItems.Find(x => x.Id == id)?.Show();
        }

        public void HideDescriptionTooltip(int id)
        {
            _tutorialDescriptionTooltipItems.Find(x => x.Id == id)?.Hide();
        }

        public void HideAllActiveDescriptionTooltip()
        {
            foreach (TutorialDescriptionTooltipItem tooltip in _tutorialDescriptionTooltipItems)
            {
                tooltip?.Hide();
            }
        }

        public void DeactivateDescriptionTooltip(int id)
        {
            TutorialDescriptionTooltipItem tooltip = _tutorialDescriptionTooltipItems.Find(x => x.Id == id);

            if(tooltip != null)
            {
                tooltip.Dispose();
                _tutorialDescriptionTooltipItems.Remove(tooltip);
            }
        }

        private void ClearToolTips()
        {
            foreach (TutorialDescriptionTooltipItem tooltip in _tutorialDescriptionTooltipItems)
            {
                tooltip.Dispose();
            }
            _tutorialDescriptionTooltipItems.Clear();
        }

        public void DrawOverlordSayPopup(string description, Enumerators.TooltipAlign align, Enumerators.TooltipOwner owner, float appearDelay)
        {
            InternalTools.DoActionDelayed(() =>
            {
                _overlordsChatController.DrawOverlordSayPopup(description, align, owner);
            }, appearDelay);
            
        }

        private void DoActionByActivity(ActionActivityHandler activity)
        {
            switch (activity.TutorialActivityActionHandler)
            {
                case Enumerators.TutorialActivityActionHandler.OverlordSayTooltip:
                    {
                        OverlordSayTooltipInfo data = activity.TutorialActivityActionHandlerData as OverlordSayTooltipInfo;
                        DrawOverlordSayPopup(data.Description, data.TutorialTooltipAlign, data.TutorialTooltipOwner, data.AppearDelay);
                    }
                    break;
                case Enumerators.TutorialActivityActionHandler.DrawDescriptionTooltips:
                    {
                        DrawDescriptionTooltipsInfo data = activity.TutorialActivityActionHandlerData as DrawDescriptionTooltipsInfo;
                        foreach (int id in data.TutorialDescriptionTooltipsToActivate)
                        {
                            ActivateDescriptionTooltip(id);
                        }
                    }
                    break;
            }
        }
    }

}
