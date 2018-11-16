using System;
using Loom.ZombieBattleground.BackendCommunication;
using Loom.ZombieBattleground.Common;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Loom.ZombieBattleground
{
    public class LoadDataMessagePopup : IUIPopup
    {
        public event Action PopupHiding;

        private ILoadObjectsManager _loadObjectsManager;

        private IUIManager _uiManager;

        private TextMeshProUGUI _text;

        private ButtonShiftingContent _gotItButton;

        private BackendDataControlMediator _backendDataControlMediator;

        public GameObject Self { get; private set; }

        private bool _isRetryButtonPressed = false;

        public void Init()
        {
            _loadObjectsManager = GameClient.Get<ILoadObjectsManager>();
            _uiManager = GameClient.Get<IUIManager>();
            _backendDataControlMediator = GameClient.Get<BackendDataControlMediator>();
        }

        public void Dispose()
        {
        }

        public void Hide()
        {
            PopupHiding?.Invoke();

            if (Self == null)
                return;

            Self.SetActive(false);
            Object.Destroy(Self);
            Self = null;
        }

        public void SetMainPriority()
        {
        }

        public void Show()
        {
            if (Self != null)
                return;

            Self = Object.Instantiate(
                _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/UI/Popups/LoadDataMessagePopup"));
            Self.transform.SetParent(_uiManager.Canvas3.transform, false);

            _gotItButton = Self.transform.Find("Button_Retry").GetComponent<ButtonShiftingContent>();
            _gotItButton.onClick.AddListener(RetryButtonHandler);

            _text = Self.transform.Find("Text_Message").GetComponent<TextMeshProUGUI>();
        }

        public void Show(object data)
        {
            Show();

            _text.text = (string) data;
        }

        public void Update()
        {
        }

        private async void RetryButtonHandler()
        {
            if (_isRetryButtonPressed)
                return;

            _isRetryButtonPressed = true;
            GameClient.Get<ISoundManager>()
                .PlaySound(Enumerators.SoundType.CLICK, Constants.SfxSoundVolume, false, false, true);

            bool success = true;
            try
            {
                await _backendDataControlMediator.LoginAndLoadData();
            }
            catch (GameVersionMismatchException e)
            {
                success = false;
                _uiManager.GetPopup<LoginPopup>().Show(e);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                success = false;
                _uiManager.DrawPopup<LoginPopup>();
            }

            Hide();
            _isRetryButtonPressed = false;

            if (success)
            {
                GameClient.Get<IAppStateManager>().ChangeAppState(Enumerators.AppState.MAIN_MENU);
            }
        }
    }
}
