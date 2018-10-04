using System;
using Loom.ZombieBattleground.BackendCommunication;
using Loom.ZombieBattleground.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Loom.ZombieBattleground
{
    public class PvPSelectionPage : IUIElement
    {
        private IUIManager _uiManager;

        private ILoadObjectsManager _loadObjectsManager;

        private IAppStateManager _stateManager;

        private ISoundManager _soundManager;

        private IDataManager _dataManager;

        private GameObject _selfPage;

        private Button _backButton,
                       _buttonCasualType,
                       _buttonRankedType,
                       _buttonFriendlyType;

        private ButtonShiftingContent _buttonTutorial;
                                      

        public void Init()
        {
            _uiManager = GameClient.Get<IUIManager>();
            _loadObjectsManager = GameClient.Get<ILoadObjectsManager>();
            _stateManager = GameClient.Get<IAppStateManager>();
            _soundManager = GameClient.Get<ISoundManager>();
            _dataManager = GameClient.Get<IDataManager>();
        }

        public void Update()
        {
        }

        public void Show()
        {
            _selfPage = Object.Instantiate(
                _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/UI/Pages/PvPSelectionPage"));
            _selfPage.transform.SetParent(_uiManager.Canvas.transform, false);

            _buttonTutorial = _selfPage.transform.Find("Button_Tutorial").GetComponent<ButtonShiftingContent>();
            _buttonCasualType = _selfPage.transform.Find("Button_CasualType").GetComponent<Button>();
            _buttonRankedType = _selfPage.transform.Find("Button_RankedType").GetComponent<Button>();
            _buttonFriendlyType = _selfPage.transform.Find("Button_FriendlyType").GetComponent<Button>();
            _backButton = _selfPage.transform.Find("Button_Back").GetComponent<Button>();

            _buttonTutorial.onClick.AddListener(TutorialButtonOnClickHandler);
            _buttonCasualType.onClick.AddListener(CasualTypeButtonOnClickHandler);
            _buttonRankedType.onClick.AddListener(RankedTypeButtonOnClickHandler);
            _buttonFriendlyType.onClick.AddListener(FriendlyTypeButtonOnClickHandler);
            _backButton.onClick.AddListener(BackButtonOnClickHandler);
        }

        public void Hide()
        {
            if (_selfPage == null)
                return;

            _selfPage.SetActive(false);
            Object.Destroy(_selfPage);
            _selfPage = null;
        }

        public void Dispose()
        {
        }

        #region Buttons Handlers

        private void TutorialButtonOnClickHandler()
        {
            _soundManager.PlaySound(Enumerators.SoundType.CLICK, Constants.SfxSoundVolume, false, false, true);

            _dataManager.CachedUserLocalData.CurrentTutorialId = 0;
            _dataManager.CachedUserLocalData.Tutorial = true;
            GameClient.Get<IGameplayManager>().IsTutorial = true;
            _uiManager.GetPage<GameplayPage>().CurrentDeckId = 0;
            GameClient.Get<IMatchManager>().FindMatch(Enumerators.MatchType.LOCAL);
        }

        private void CasualTypeButtonOnClickHandler()
        {
            _soundManager.PlaySound(Enumerators.SoundType.CLICK, Constants.SfxSoundVolume, false, false, true);
            GameClient.Get<IMatchManager>().MatchType = Enumerators.MatchType.PVP;

            _stateManager.ChangeAppState(Enumerators.AppState.HordeSelection);
        }

        private void RankedTypeButtonOnClickHandler()
        {
            _soundManager.PlaySound(Enumerators.SoundType.CLICK, Constants.SfxSoundVolume, false, false, true);
            _uiManager.DrawPopup<WarningPopup>(
                        $"Ranked Games are Disabled\nfor version {BuildMetaInfo.Instance.DisplayVersionName}\n\n Thanks for helping us make this game Awesome\n\n-Loom Team");
        }

        private void FriendlyTypeButtonOnClickHandler()
        {
            _soundManager.PlaySound(Enumerators.SoundType.CLICK, Constants.SfxSoundVolume, false, false, true);
            _uiManager.DrawPopup<WarningPopup>(
                         $"Friendly Games are Disabled\nfor version {BuildMetaInfo.Instance.DisplayVersionName}\n\n Thanks for helping us make this game Awesome\n\n-Loom Team");
        }

        private void BackButtonOnClickHandler()
        {
            _soundManager.PlaySound(Enumerators.SoundType.CLICK, Constants.SfxSoundVolume, false, false, true);
            _stateManager.BackAppState();
        }
        #endregion

    }
}
