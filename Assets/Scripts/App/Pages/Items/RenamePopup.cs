

using System;
using log4net;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Loom.ZombieBattleground
{
    public class RenamePopup : IUIPopup
    {
        private static readonly ILog Log = Logging.GetLog(nameof(RenamePopup));

        private Button _buttonSaveRenameDeck;
        private Button _buttonCancel;
        private Button _buttonBack;
        private Button _buttonContinue;

        private Button _normalBack;
        private Button _ksBack;

        private Image _glow;

        private Image _lockKsBack;

        private TMP_InputField _inputFieldRenameDeckName;

        private IUIManager _uiManager;
        private ILoadObjectsManager _loadObjectsManager;

        private IDataManager _dataManager;

        public GameObject Self { get; private set;  }

        public static Action<string> OnSelectDeckName;
        public static Action<string> OnSaveNewDeckName;

        private Deck _deck;
        private bool _openFromCreatingNewHorde;

        private string _deckName;

        private int _cardBack;

        public void Init()
        {
            _uiManager = GameClient.Get<IUIManager>();
            _loadObjectsManager = GameClient.Get<ILoadObjectsManager>();
            _dataManager = GameClient.Get<IDataManager>();
        }

        public void Show()
        {
            if (Self != null)
                return;

            Self = Object.Instantiate(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/UI/Popups/RenamePopup"),
                _uiManager.Canvas2.transform,
                false);

            _inputFieldRenameDeckName = Self.transform.Find("Tab_Rename/Panel_Content/InputText_DeckName").GetComponent<TMP_InputField>();
            _inputFieldRenameDeckName.onEndEdit.AddListener(OnInputFieldRenameEndedEdit);
            _inputFieldRenameDeckName.text = "Deck Name";

            _buttonBack = Self.transform.Find("Tab_Rename/Panel_Deco/Button_Back").GetComponent<Button>();
            _buttonBack.onClick.AddListener(ButtonBackHandler);

            _buttonContinue = Self.transform.Find("Tab_Rename/Panel_Deco/Button_Continue").GetComponent<Button>();
            _buttonContinue.onClick.AddListener(ButtonContinueHandler);

            _buttonSaveRenameDeck = Self.transform.Find("Tab_Rename/Panel_Deco/Button_Save").GetComponent<Button>();
            _buttonSaveRenameDeck.onClick.AddListener(ButtonSaveRenameDeckHandler);

            _buttonCancel = Self.transform.Find("Tab_Rename/Panel_Deco/Button_Cancel").GetComponent<Button>();
            _buttonCancel.onClick.AddListener(ButtonCancelHandler);

            _normalBack = Self.transform.Find("Tab_Rename/Panel_Content/CardBacks/Normal_Back").GetComponent<Button>();
            _normalBack.onClick.AddListener(OnPressNormalBack);
            _ksBack = Self.transform.Find("Tab_Rename/Panel_Content/CardBacks/KS_Back").GetComponent<Button>();
            _ksBack.onClick.AddListener(OnPressKSBack);
            _glow = Self.transform.Find("Tab_Rename/Panel_Content/CardBacks/Glow").GetComponent<Image>();
            _lockKsBack = Self.transform.Find("Tab_Rename/Panel_Content/CardBacks/KS_Back/Locked").GetComponent<Image>();

            if (_dataManager.userUnlockables.unlockedCardBack)
            {
                _lockKsBack.gameObject.SetActive(false);
                _ksBack.interactable = true;
            }
            else
            {
                _lockKsBack.gameObject.SetActive(true);
                _ksBack.interactable = false;
            }

            EnablePanelButtons();

            _deckName = _deck.FinalName;

            _cardBack = _deck.Back;

            if (_cardBack == 0)
            {
                OnPressNormalBack();
            }
            else if (_cardBack == 1)
            {
                OnPressKSBack();
            }

            // set the input field not intractable, if there is tutorial
            bool isTutorial = GameClient.Get<ITutorialManager>().IsTutorial;
            _inputFieldRenameDeckName.interactable = !isTutorial;
            SetName(isTutorial ? Constants.TutorialDefaultDeckName : _deckName);
        }

        private void OnPressNormalBack()
        {
            _glow.transform.position = _normalBack.transform.position;
            _cardBack = 0;
        }

        private void OnPressKSBack()
        {
            _glow.transform.position = _ksBack.transform.position;
            _cardBack = 1;
        }

        private void EnablePanelButtons()
        {
            if (_deck.Id.Id == -1)
            {
                if (_openFromCreatingNewHorde)
                {
                    _buttonBack.gameObject.SetActive(true);
                    _buttonCancel.gameObject.SetActive(false);
                }
                else
                {
                    _buttonBack.gameObject.SetActive(false);
                    _buttonCancel.gameObject.SetActive(true);
                }

                _buttonSaveRenameDeck.gameObject.SetActive(false);
                _buttonContinue.gameObject.SetActive(true);
            }
            else
            {
                _buttonBack.gameObject.SetActive(false);
                _buttonCancel.gameObject.SetActive(true);
                _buttonSaveRenameDeck.gameObject.SetActive(true);
                _buttonContinue.gameObject.SetActive(false);
            }
        }

        public void Show(object data)
        {
            if (data is object[] param)
            {
                _deck = (Deck) param[0];
                _openFromCreatingNewHorde = (bool) param[1];
            }

            Show();
        }

        public void Hide()
        {
            if (Self == null)
                return;

            Self.SetActive(false);
            Object.Destroy(Self);
            Self = null;
        }

        public void Update()
        {

        }



        public void Dispose()
        {

        }

        public void SetMainPriority()
        {

        }

        private void OnInputFieldRenameEndedEdit(string value)
        {
            _deckName = value;
        }

        private void SetName(string name)
        {
            _inputFieldRenameDeckName.text = name;
        }

        private void ButtonSaveRenameDeckHandler()
        {
            if (GameClient.Get<ITutorialManager>().BlockAndReport(_buttonSaveRenameDeck.name))
                return;

            DataUtilities.PlayClickSound();
            string newDeckName = _deckName + "|" + _cardBack;
            ;
            DeckGeneratorController deckGeneratorController = GameClient.Get<IGameplayManager>().GetController<DeckGeneratorController>();
            if (!deckGeneratorController.VerifyDeckName(newDeckName))
                return;

            _deck.FinalName = _deckName;

            _deck.Name = newDeckName;

            deckGeneratorController.FinishEditDeck += FinishEditDeckName;
            deckGeneratorController.ProcessEditDeck(_deck);

            Hide();
        }

        private void FinishEditDeckName(bool success, Deck deck)
        {
            DeckGeneratorController deckGeneratorController = GameClient.Get<IGameplayManager>().GetController<DeckGeneratorController>();
            deckGeneratorController.FinishEditDeck -= FinishEditDeckName;

            OnSaveNewDeckName?.Invoke(deck.Name);
        }

        private void ButtonBackHandler()
        {
            if (GameClient.Get<ITutorialManager>().BlockAndReport(_buttonBack.name))
                return;
                
            DataUtilities.PlayClickSound();
            Hide();
            _uiManager.DrawPopup<SelectOverlordAbilitiesPopup>();
        }


        private void ButtonContinueHandler()
        {
            DataUtilities.PlayClickSound();

            string newDeckName = _deckName;
            DeckGeneratorController deckGeneratorController = GameClient.Get<IGameplayManager>().GetController<DeckGeneratorController>();
            if (!deckGeneratorController.VerifyDeckName(newDeckName))
                return;

            OnSelectDeckName?.Invoke(newDeckName);

            Hide();

        }

        private void ButtonCancelHandler()
        {
            if (GameClient.Get<ITutorialManager>().BlockAndReport(_buttonCancel.name))
                return;

            DataUtilities.PlayClickSound();
            Hide();
        }
    }
}

