// Copyright (c) 2018 - Loom Network. All rights reserved.
// https://loomx.io/



using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using LoomNetwork.CZB.Common;
using TMPro;
using System;
using System.Linq;
using DG.Tweening;
using LoomNetwork.CZB.BackendCommunication;
using LoomNetwork.CZB.Data;
using LoomNetwork.Internal;
using Deck = LoomNetwork.CZB.Data.Deck;
using Hero = LoomNetwork.CZB.Data.Hero;

namespace LoomNetwork.CZB
{
    public class HordeSelectionPage : IUIElement
    {
        private IUIManager _uiManager;
        private ILoadObjectsManager _loadObjectsManager;
        private ILocalizationManager _localizationManager;
        private IDataManager _dataManager;
        private ISoundManager _soundManager;
        private IAppStateManager _appStateManager;
        private IMatchManager _matchManager;
        private BackendFacade _backendFacade;
        private BackendDataControlMediator _backendDataControlMediator;

        private GameObject _selfPage;

        private Button _backButton,
                        _battleButton,
                        _battleButtonWarning,
                       _leftArrowButton,
                       _rightArrowButton,
                       _deleteButton,
                       _editButton;

        private ButtonShiftingContent _buttonArmy;

        private Image _firstSkill,
                      _secondSkill;

        private Transform _containerOfDecks, _hordeSelection;

        private List<HordeDeckObject> _hordeDecks;
        private int _selectedDeckId = -1;
        private int _scrolledDeck = -1;

        private const int HORDE_ITEM_SPACE = 570,
                            HORDE_CONTAINER_XOFFSET = 60;
      //  private int _decksCount = 3;

        // new horde deck object
        private GameObject _newHordeDeckObject;
        private Button _newHordeDeckButton;



        public void Init()
        {
            _uiManager = GameClient.Get<IUIManager>();
            _loadObjectsManager = GameClient.Get<ILoadObjectsManager>();
            _localizationManager = GameClient.Get<ILocalizationManager>();
            _dataManager = GameClient.Get<IDataManager>();
            _soundManager = GameClient.Get<ISoundManager>();
            _appStateManager = GameClient.Get<IAppStateManager>();
            _matchManager = GameClient.Get<IMatchManager>();
            _backendFacade = GameClient.Get<BackendFacade>();
            _backendDataControlMediator = GameClient.Get<BackendDataControlMediator>();

            _selfPage = MonoBehaviour.Instantiate(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/UI/Pages/HordeSelectionPage"), _uiManager.Canvas.transform, false);

            _containerOfDecks = _selfPage.transform.Find("Panel_DecksContainer/Group");

            _buttonArmy = _selfPage.transform.Find("Button_Army").GetComponent<ButtonShiftingContent>();
            _backButton = _selfPage.transform.Find("Button_Back").GetComponent<Button>();
            _battleButton = _selfPage.transform.Find("Button_Battle").GetComponent<Button>();
            _battleButtonWarning = _selfPage.transform.Find("Button_Battle_Warning").GetComponent<Button>();
            _leftArrowButton = _selfPage.transform.Find("Button_LeftArrow").GetComponent<Button>();
            _rightArrowButton = _selfPage.transform.Find("Button_RightArrow").GetComponent<Button>();


            _editButton = _selfPage.transform.Find("Panel_DecksContainer/Selection/Panel_SelectedHordeObjects/Button_Edit").GetComponent<Button>();
            _deleteButton = _selfPage.transform.Find("Panel_DecksContainer/Selection/Panel_SelectedHordeObjects/Button_Delete").GetComponent<Button>();
            _firstSkill = _selfPage.transform.Find("Panel_DecksContainer/Selection/Panel_SelectedHordeObjects/Image_FirstSkil/Image_Skill").GetComponent<Image>();
            _secondSkill = _selfPage.transform.Find("Panel_DecksContainer/Selection/Panel_SelectedHordeObjects/Image_SecondSkil/Image_Skill").GetComponent<Image>();


            _hordeSelection = _selfPage.transform.Find("Panel_DecksContainer/Selection");

            // new horde deck object
            _newHordeDeckObject = _containerOfDecks.transform.Find("Item_HordeSelectionNewHorde").gameObject;
            _newHordeDeckButton = _newHordeDeckObject.transform.Find("Image_BaackgroundGeneral").GetComponent<Button>();

            _buttonArmy.onClick.AddListener(CollectionButtonOnClickHandler);
            _backButton.onClick.AddListener(BackButtonOnClickHandler);
            _battleButton.onClick.AddListener(BattleButtonOnClickHandler);
            _battleButtonWarning.onClick.AddListener(BattleButtonWarningOnClickHandler);
            _leftArrowButton.onClick.AddListener(LeftArrowButtonOnClickHandler);
            _rightArrowButton.onClick.AddListener(RightArrowButtonOnClickHandler);

            _editButton.onClick.AddListener(EditButtonOnClickHandler);
            _deleteButton.onClick.AddListener(DeleteButtonOnClickHandler);

            _firstSkill.GetComponent<MultiPointerClickHandler>().SingleClickReceived += () => SkillButtonOnSingleClickHandler(0);
            _secondSkill.GetComponent<MultiPointerClickHandler>().SingleClickReceived += () => SkillButtonOnSingleClickHandler(1);

            _firstSkill.GetComponent<MultiPointerClickHandler>().DoubleClickReceived += () => SkillButtonOnDoubleClickHandler(0);
            _secondSkill.GetComponent<MultiPointerClickHandler>().DoubleClickReceived += () => SkillButtonOnDoubleClickHandler(1);

            _newHordeDeckButton.onClick.AddListener(NewHordeDeckButtonOnClickHandler);

            _battleButton.interactable = true;

            Hide();
        }

        public void Update()
        {
        }

        public void Show()
        {
            //todod improve I guess
            _selectedDeckId = _dataManager.CachedUserLocalData.lastSelectedDeckId;
            _hordeSelection.gameObject.SetActive(false);

            LoadDeckObjects();
            _selfPage.SetActive(true);
        }

        public void Hide()
        {
            _selfPage.SetActive(false);
            ResetHordeDecks();

            _scrolledDeck = -1;
        }

        public void Dispose()
        {

        }


        private void FillHordeDecks()
        {
            ResetHordeDecks();
            _hordeDecks = new List<HordeDeckObject>();

            HordeDeckObject hordeDeck = null;
            for (int i = 0; i < _dataManager.CachedDecksData.decks.Count; i++)
            {
                hordeDeck = new HordeDeckObject(_containerOfDecks,
                                                _dataManager.CachedDecksData.decks[i],
                                                _dataManager.CachedHeroesData.Heroes.Find(x => x.heroId == _dataManager.CachedDecksData.decks[i].heroId),
                                                i);
                hordeDeck.HordeDeckSelectedEvent += HordeDeckSelectedEventHandler;
                hordeDeck.DeleteDeckEvent += DeleteDeckEventHandler;

                _hordeDecks.Add(hordeDeck);
            }
            _newHordeDeckObject.transform.localPosition = Vector3.right * HORDE_ITEM_SPACE * _hordeDecks.Count;
        }


        private void ResetHordeDecks()
        {
            _hordeSelection.SetParent(_containerOfDecks.parent, false);
            _hordeSelection.gameObject.SetActive(false);
            if (_hordeDecks != null)
            {
                foreach (var element in _hordeDecks)
                    element.Dispose();
                _hordeDecks.Clear();
                _hordeDecks = null;
            }
        }

        private async void DeleteDeckEventHandler(HordeDeckObject deck)
        {
            // HACK for offline mode in online mode, local data should only be saved after
            // backend operation has succeeded
            _dataManager.CachedDecksData.decks.Remove(deck.SelfDeck);
            _dataManager.CachedUserLocalData.lastSelectedDeckId = -1;
            _dataManager.CachedDecksLastModificationTimestamp = Utilites.GetCurrentUnixTimestampMillis();
            await _dataManager.SaveAllCache();

            try
            {
                await _backendFacade.DeleteDeck(
                    _backendDataControlMediator.UserDataModel.UserId, 
                    deck.SelfDeck.id,
                    _dataManager.CachedDecksLastModificationTimestamp
                    );
                CustomDebug.Log($" ====== Delete Deck {deck.SelfDeck.id} Successfully ==== ");
            } catch (Exception e)
            {
                // HACK for offline mode
                if (false)
                {
                    CustomDebug.Log("Result === " + e);
                    OpenAlertDialog($"Not able to Delete Deck {deck.SelfDeck.id}: " + e.Message);
                    return;
                }
            }

            LoadDeckObjects();
        }

        private void HordeDeckSelectedEventHandler(HordeDeckObject deck)
        {
            if (_hordeSelection.gameObject.activeSelf)
            {
                HordeDeckObject horde = _hordeDecks.FirstOrDefault(o => o.SelfDeck.id == _selectedDeckId);
                horde.Deselect();
            }
            deck.Select();

            _firstSkill.sprite = _loadObjectsManager.GetObjectByPath<Sprite>("Images/HeroesIcons/heroability_" + deck.SelfHero.element.ToUpper() + "_" + deck.SelfHero.skills[deck.SelfHero.primarySkill].skill.ToLower());
            _secondSkill.sprite = _loadObjectsManager.GetObjectByPath<Sprite>("Images/HeroesIcons/heroability_" + deck.SelfHero.element.ToUpper() + "_" + deck.SelfHero.skills[deck.SelfHero.secondarySkill].skill.ToLower());

            _hordeSelection.transform.SetParent(deck.selectionContainer, false);
            _hordeSelection.gameObject.SetActive(true);

            _selectedDeckId = (int) deck.SelfDeck.id;
            _dataManager.CachedUserLocalData.lastSelectedDeckId = _selectedDeckId;

            _dataManager.SaveAllCache();
            deck.selectionContainer.parent.SetAsLastSibling();

            BattleButtonUpdate();

        }

        private void BattleButtonUpdate()
        {
            if (_hordeDecks.Count == 0 || 
                _selectedDeckId == -1 ||
                _hordeDecks.First(o => o.SelfDeck.id == _selectedDeckId).SelfDeck.GetNumCards() < Constants.MIN_DECK_SIZE && 
                !Constants.DEV_MODE)
            {
                _battleButton.interactable = false;
                _battleButtonWarning.gameObject.SetActive(true);
            }
            else
            {
                _battleButton.interactable = true;
                _battleButtonWarning.gameObject.SetActive(false);
            }
        }

        private void LoadDeckObjects()
        {
            FillHordeDecks();

            _newHordeDeckObject.transform.SetAsLastSibling();
            _newHordeDeckObject.SetActive(true);

            var deck = _hordeDecks.Find(x => x.SelfDeck.id == _dataManager.CachedUserLocalData.lastSelectedDeckId);
            if (deck != null)
            {
                HordeDeckSelectedEventHandler(deck);
            } else
            {

            }

            if (_hordeDecks.Count > 0) {
                HordeDeckObject foundDeck = _hordeDecks.FirstOrDefault(o => o.SelfDeck.id == _selectedDeckId);
				if (foundDeck == null) {
					_selectedDeckId = (int) _hordeDecks[0].SelfDeck.id;
				}
			} else {
				_selectedDeckId = -1;
			}

            CenterTheSelectedDeck();
            BattleButtonUpdate();
        }

        private void CenterTheSelectedDeck()
        {
			if (_hordeDecks.Count < 1)
				return;

            _scrolledDeck = _hordeDecks.IndexOf(_hordeDecks.Find(x => x.IsSelected));

            if (_scrolledDeck < 2)
                _scrolledDeck = 0;
            else
                _scrolledDeck--;

            _containerOfDecks.transform.localPosition = new Vector3(HORDE_CONTAINER_XOFFSET - HORDE_ITEM_SPACE * _scrolledDeck, 420 , 0);
        }


        #region Buttons Handlers

        private void CollectionButtonOnClickHandler()
        {
            _soundManager.PlaySound(Enumerators.SoundType.CLICK, Constants.SFX_SOUND_VOLUME, false, false, true);
            _appStateManager.ChangeAppState(Enumerators.AppState.COLLECTION);
        }

        private void BackButtonOnClickHandler()
        {
            _soundManager.PlaySound(Enumerators.SoundType.CLICK, Constants.SFX_SOUND_VOLUME, false, false, true);
            _appStateManager.ChangeAppState(Enumerators.AppState.MAIN_MENU);
        }

        private void BattleButtonOnClickHandler()
        {
            _soundManager.PlaySound(Enumerators.SoundType.CLICK, Constants.SFX_SOUND_VOLUME, false, false, true);
            _uiManager.GetPage<GameplayPage>().CurrentDeckId = _selectedDeckId;
            _matchManager.FindMatch(Enumerators.MatchType.LOCAL);
        }

        private void BattleButtonWarningOnClickHandler()
        {
            _soundManager.PlaySound(Enumerators.SoundType.CLICK, Constants.SFX_SOUND_VOLUME, false, false, true);

            if (_hordeDecks.Count == 0 || 
                _selectedDeckId == -1 ||
                _hordeDecks.First(o => o.SelfDeck.id == _selectedDeckId).SelfDeck.GetNumCards() < Constants.MIN_DECK_SIZE && !Constants.DEV_MODE)
            {
                _uiManager.DrawPopup<WarningPopup>("Select a valid horde with " + Constants.MIN_DECK_SIZE + " cards.");
                return;
            }
        }

        private void LeftArrowButtonOnClickHandler()
        {
            _soundManager.PlaySound(Enumerators.SoundType.CLICK, Constants.SFX_SOUND_VOLUME, false, false, true);

            SwitchOverlordObject(-1);
        }

        private void RightArrowButtonOnClickHandler()
        {
            _soundManager.PlaySound(Enumerators.SoundType.CLICK, Constants.SFX_SOUND_VOLUME, false, false, true);
            SwitchOverlordObject(1);
        }

        private void SkillButtonOnSingleClickHandler(int skillIndex)
        {
            _soundManager.PlaySound(Enumerators.SoundType.CLICK, Constants.SFX_SOUND_VOLUME, false, false, true);
            HordeDeckObject deck = _hordeDecks.FirstOrDefault(o => o.SelfDeck.id == _selectedDeckId);
            if (deck != null)
            {
                HeroSkill skill =
                    skillIndex == 0 ?
                        deck.SelfHero.skills[deck.SelfHero.primarySkill] :
                        deck.SelfHero.skills[deck.SelfHero.secondarySkill];

                _uiManager.DrawPopup<OverlordAbilityTooltipPopup>(skill);
            }

        }

		private void SkillButtonOnDoubleClickHandler(int skillIndex)
		{
			_soundManager.PlaySound(Enumerators.SoundType.CLICK, Constants.SFX_SOUND_VOLUME, false, false, true);
		    HordeDeckObject deck = _hordeDecks.FirstOrDefault(o => o.SelfDeck.id == _selectedDeckId);
		    if (deck != null)
		    {
		        _uiManager.DrawPopup<OverlordAbilitySelectionPopup>(deck.SelfHero);
		    }
		}

        // new horde deck object
        private void NewHordeDeckButtonOnClickHandler()
        {
            if (ShowConnectionLostPopupIfNeeded())
                return;

            _soundManager.PlaySound(Enumerators.SoundType.CLICK, Constants.SFX_SOUND_VOLUME, false, false, true);

            _uiManager.GetPage<DeckEditingPage>().CurrentDeckId = -1;

            _appStateManager.ChangeAppState(Enumerators.AppState.HERO_SELECTION);
        }

		private void DeleteButtonOnClickHandler()
		{
		    if (ShowConnectionLostPopupIfNeeded())
		        return;

			HordeDeckObject deck = _hordeDecks.FirstOrDefault(o => o.SelfDeck.id == _selectedDeckId);
			if (deck != null) {
				_soundManager.PlaySound (Enumerators.SoundType.CLICK, Constants.SFX_SOUND_VOLUME, false, false, true);

				_uiManager.GetPopup<QuestionPopup> ().ConfirmationEvent += ConfirmDeleteDeckEventHandler;

				_uiManager.DrawPopup<QuestionPopup> ("Do you really want to delete " + deck.SelfDeck.name + "?");
			}
		}

		private void EditButtonOnClickHandler()
		{
		    if (ShowConnectionLostPopupIfNeeded())
		        return;

			if (_selectedDeckId != -1) {
				_soundManager.PlaySound (Enumerators.SoundType.CLICK, Constants.SFX_SOUND_VOLUME, false, false, true);

				_uiManager.GetPage<DeckEditingPage>().CurrentDeckId = _selectedDeckId;
				_appStateManager.ChangeAppState (Enumerators.AppState.DECK_EDITING);
			}
		}

		private void ConfirmDeleteDeckEventHandler(bool status)
		{
			_uiManager.GetPopup<QuestionPopup>().ConfirmationEvent -= ConfirmDeleteDeckEventHandler;

		    if (!status)
		        return;

		    HordeDeckObject deckToDelete = _hordeDecks.FirstOrDefault(o => o.SelfDeck.id == _selectedDeckId);
		    if (deckToDelete != null)
		    {
		        DeleteDeckEventHandler(deckToDelete);
		    }
            BattleButtonUpdate();
        }

        //private void BuyButtonHandler()
        //{
        //          GameClient.Get<ISoundManager>().PlaySound(Common.Enumerators.SoundType.CLICK, Constants.SFX_SOUND_VOLUME, false, false, true);
        //          GameClient.Get<IAppStateManager>().ChangeAppState(Common.Enumerators.AppState.SHOP);
        //      }
        //      private void OpenButtonHandler()
        //{
        //          GameClient.Get<ISoundManager>().PlaySound(Common.Enumerators.SoundType.CLICK, Constants.SFX_SOUND_VOLUME, false, false, true);
        //          GameClient.Get<IAppStateManager>().ChangeAppState(Common.Enumerators.AppState.PACK_OPENER);
        //      }

        #endregion

        private void OpenAlertDialog(string msg)
        {
            _uiManager.DrawPopup<WarningPopup>(msg);
        }

        private void SwitchOverlordObject(int direction)
        {
            bool isChanged = false;

            if (_hordeDecks.Count < 1)
                return;
            var oldIndex = _scrolledDeck;
            _scrolledDeck += direction;

            if (_scrolledDeck > _hordeDecks.Count - 2)
                _scrolledDeck = _hordeDecks.Count - 2;
            if (_scrolledDeck < 0)
                _scrolledDeck = 0;

            if (oldIndex != _scrolledDeck)
                _containerOfDecks.transform.localPosition = new Vector3(HORDE_CONTAINER_XOFFSET - HORDE_ITEM_SPACE * _scrolledDeck, 420, 0);
        }

        private bool ShowConnectionLostPopupIfNeeded()
        {
            // HACK for offline mode
            return false;
            if (_backendFacade.IsConnected)
                return false;

            _uiManager.DrawPopup<WarningPopup>("Sorry, modifications are only available in online mode.");
            return true;
        }

        public class HordeDeckObject
        {
            public event Action<HordeDeckObject> HordeDeckSelectedEvent;
            public event Action<HordeDeckObject> DeleteDeckEvent;

            private ILoadObjectsManager _loadObjectsManager;
            private IUIManager _uiManager;
            private IAppStateManager _appStateManager;
            private IDataManager _dataManager;
            private ISoundManager _soundManager;

            private GameObject _selfObject;

            private Image _setTypeIcon;
            private Image _hordePicture;

            private TextMeshProUGUI _descriptionText,
                                    _cardsInDeckCountText;

            private Button _buttonSelect;

            public Deck SelfDeck { get; private set; }
            public Hero SelfHero { get; private set; }

            public bool IsSelected { get; private set; }

            public Transform selectionContainer;

            public HordeDeckObject(Transform parent, Deck deck, Hero hero, int index)
            {
                SelfDeck = deck;
                SelfHero = hero;

                _loadObjectsManager = GameClient.Get<ILoadObjectsManager>();
                _uiManager = GameClient.Get<IUIManager>();
                _appStateManager = GameClient.Get<IAppStateManager>();
                _dataManager = GameClient.Get<IDataManager>();
                _soundManager = GameClient.Get<ISoundManager>();

                _selfObject = MonoBehaviour.Instantiate(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/UI/Elements/Item_HordeSelectionObject"), parent, false);
                _selfObject.transform.localPosition = Vector3.right * HORDE_ITEM_SPACE * index;

                selectionContainer = _selfObject.transform.Find("SelectionContainer");

                _setTypeIcon = _selfObject.transform.Find("Panel_HordeType/Image").GetComponent<Image>();
                _hordePicture = _selfObject.transform.Find("Image_HordePicture").GetComponent<Image>();

                _descriptionText = _selfObject.transform.Find("Panel_Description/Text_Description").GetComponent<TextMeshProUGUI>();
                _cardsInDeckCountText = _selfObject.transform.Find("Panel_DeckFillInfo/Text_CardsCount").GetComponent<TextMeshProUGUI>();

                _buttonSelect = _selfObject.transform.Find("Button_Select").GetComponent<Button>();

                _cardsInDeckCountText.text = SelfDeck.GetNumCards() + "/" + Constants.MAX_DECK_SIZE;
                _descriptionText.text = deck.name;

                _setTypeIcon.sprite = _loadObjectsManager.GetObjectByPath<Sprite>("Images/UI/ElementIcons/Icon_element_" + SelfHero.element.ToLower());
                _hordePicture.sprite = _loadObjectsManager.GetObjectByPath<Sprite>("Images/UI/ChooseHorde/hordeselect_deck_" + SelfHero.element.ToLower());

                _buttonSelect.onClick.AddListener(SelectButtonOnClickHandler);
            }


            public void Dispose()
            {
                MonoBehaviour.Destroy(_selfObject);
            }

            public void Select()
            {
                if (IsSelected)
                    return;
                _buttonSelect.gameObject.SetActive(false);

                IsSelected = true;
            }

            public void Deselect()
            {
                if (!IsSelected)
                    return;
                _buttonSelect.gameObject.SetActive(true);

                IsSelected = false;
            }

            private void SelectButtonOnClickHandler()
            {
                _soundManager.PlaySound(Enumerators.SoundType.CLICK, Constants.SFX_SOUND_VOLUME, false, false, true);
               HordeDeckSelectedEvent?.Invoke(this);
            }
        }
    }
}