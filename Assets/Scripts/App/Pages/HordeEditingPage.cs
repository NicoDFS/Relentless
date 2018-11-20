using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Loom.ZombieBattleground.BackendCommunication;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Loom.ZombieBattleground
{
    public class HordeEditingPage : IUIElement
    {
        private const int CardsPerPage = 5;

        private readonly Dictionary<Enumerators.SetType, Enumerators.SetType> _against =
            new Dictionary<Enumerators.SetType, Enumerators.SetType>
            {
                {
                    Enumerators.SetType.FIRE, Enumerators.SetType.WATER
                },
                {
                    Enumerators.SetType.TOXIC, Enumerators.SetType.FIRE
                },
                {
                    Enumerators.SetType.LIFE, Enumerators.SetType.TOXIC
                },
                {
                    Enumerators.SetType.EARTH, Enumerators.SetType.LIFE
                },
                {
                    Enumerators.SetType.AIR, Enumerators.SetType.EARTH
                },
                {
                    Enumerators.SetType.WATER, Enumerators.SetType.AIR
                }
            };

        private IUIManager _uiManager;

        private ILoadObjectsManager _loadObjectsManager;

        private IAnalyticsManager _analyticsManager;

        private IDataManager _dataManager;

        private BackendFacade _backendFacade;

        private BackendDataControlMediator _backendDataControlMediator;

        private GameObject _selfPage;

        private TMP_InputField _deckNameInputField;

        private ButtonShiftingContent _buttonBuy, _buttonSave, _buttonArmy;

        private Button _buttonArmyArrowLeft,
            _buttonArmyArrowRight,
            _buttonHordeArrowLeft,
            _buttonHordeArrowRight,
            _buttonBack;

        private TMP_Text _cardAmountText;

        private Deck _currentDeck;

        private int _numSets, _currentElementPage, _numElementPages, _numHordePages, _currentHordePage;

        private Enumerators.SetType _currentSet;

        private Toggle _airToggle, _earthToggle, _fireToggle, _waterToggle, _toxicTogggle, _lifeToggle, _itemsToggle;

        private GameObject _cardCreaturePrefab, _cardItemPrefab;

        private CollectionData _collectionData;

        private int _currentDeckId, _currentHeroId;

        private List<BoardCard> _createdArmyCards, _createdHordeCards;

        private ToggleGroup _toggleGroup;

        private RectTransform _armyCardsContainer;

        private RectTransform _hordeCardsContainer;

        private SimpleScrollNotifier _armyScrollNotifier;

        private SimpleScrollNotifier _hordeScrollNotifier;

        private CardInfoPopupHandler _cardInfoPopupHandler;

        private GameObject _draggingObject;

        private GameObject _hordeAreaObject, _armyAreaObject;

        private CardHighlightingVFXItem _highlightingVFXItem;

        private bool _isDragging;

        public int CurrentDeckId
        {
            set => _currentDeckId = value;
        }

        public int CurrentHeroId
        {
            set => _currentHeroId = value;
        }

        public void Init()
        {
            _uiManager = GameClient.Get<IUIManager>();
            _loadObjectsManager = GameClient.Get<ILoadObjectsManager>();
            _dataManager = GameClient.Get<IDataManager>();
            _backendFacade = GameClient.Get<BackendFacade>();
            _backendDataControlMediator = GameClient.Get<BackendDataControlMediator>();
            _analyticsManager = GameClient.Get<IAnalyticsManager>();

            _cardInfoPopupHandler = new CardInfoPopupHandler();
            _cardInfoPopupHandler.Init();
            _cardInfoPopupHandler.PreviewCardInstantiated += boardCard =>
            {
                boardCard.Transform.Find("Amount").gameObject.SetActive(false);
                boardCard.Transform.Find("AmountForArmy").gameObject.SetActive(false);
                boardCard.SetAmountOfCardsInEditingPage(true, 0, 0);
            };

            _collectionData = new CollectionData();
            _collectionData.Cards = new List<CollectionCardData>();

            _cardCreaturePrefab =
                _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/Gameplay/Cards/CreatureCard");
            _cardItemPrefab = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/Gameplay/Cards/ItemCard");

            _createdArmyCards = new List<BoardCard>();
            _createdHordeCards = new List<BoardCard>();
        }

        public void Update()
        {
            if (_selfPage != null && _selfPage.activeInHierarchy)
            {
                UpdateNumCardsText();

                _cardInfoPopupHandler.Update();
            }
        }

        public void Show()
        {
            _selfPage = Object.Instantiate(
                _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/UI/Pages/HordeEditingPage"));
            _selfPage.transform.SetParent(_uiManager.Canvas.transform, false);

            _toggleGroup = _selfPage.transform.Find("ElementsToggles").GetComponent<ToggleGroup>();
            _airToggle = _selfPage.transform.Find("ElementsToggles/Air").GetComponent<Toggle>();
            _lifeToggle = _selfPage.transform.Find("ElementsToggles/Life").GetComponent<Toggle>();
            _waterToggle = _selfPage.transform.Find("ElementsToggles/Water").GetComponent<Toggle>();
            _toxicTogggle = _selfPage.transform.Find("ElementsToggles/Toxic").GetComponent<Toggle>();
            _fireToggle = _selfPage.transform.Find("ElementsToggles/Fire").GetComponent<Toggle>();
            _earthToggle = _selfPage.transform.Find("ElementsToggles/Earth").GetComponent<Toggle>();
            _itemsToggle = _selfPage.transform.Find("ElementsToggles/Items").GetComponent<Toggle>();

            _cardAmountText = _selfPage.transform.Find("CardsAmount/CardsAmountText").GetComponent<TMP_Text>();

            _deckNameInputField = _selfPage.transform.Find("DeckTitleInputText").GetComponent<TMP_InputField>();

            _buttonSave = _selfPage.transform.Find("Button_Save").GetComponent<ButtonShiftingContent>();
            _buttonArmy = _selfPage.transform.Find("Button_Army").GetComponent<ButtonShiftingContent>();
            _buttonBuy = _selfPage.transform.Find("Button_Buy").GetComponent<ButtonShiftingContent>();
            _buttonBack = _selfPage.transform.Find("Button_Back").GetComponent<Button>();
            _buttonArmyArrowLeft = _selfPage.transform.Find("Army/ArrowLeftButton").GetComponent<Button>();
            _buttonArmyArrowRight = _selfPage.transform.Find("Army/ArrowRightButton").GetComponent<Button>();
            _armyCardsContainer = _selfPage.transform.Find("Army/Cards").GetComponent<RectTransform>();
            _armyScrollNotifier = _selfPage.transform.Find("Army/ScrollArea").GetComponent<SimpleScrollNotifier>();

            _buttonHordeArrowLeft = _selfPage.transform.Find("Horde/ArrowLeftButton").GetComponent<Button>();
            _buttonHordeArrowRight = _selfPage.transform.Find("Horde/ArrowRightButton").GetComponent<Button>();
            _hordeCardsContainer = _selfPage.transform.Find("Horde/Cards").GetComponent<RectTransform>();
            _hordeScrollNotifier = _selfPage.transform.Find("Horde/ScrollArea").GetComponent<SimpleScrollNotifier>();

            _highlightingVFXItem = new CardHighlightingVFXItem(Object.Instantiate(
                _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/UI/ArmyCardSelection"), _selfPage.transform, true));

            _buttonBack.onClick.AddListener(BackButtonHandler);
            _buttonBuy.onClick.AddListener(BuyButtonHandler);
            _buttonSave.onClick.AddListener(SaveButtonHandler);
            _buttonArmy.onClick.AddListener(ArmyButtonHandler);

            _airToggle.onValueChanged.AddListener(
                state =>
                {
                    if (state)
                    {
                        ToggleChooseOnValueChangedHandler(Enumerators.SetType.AIR);
                    }
                });
            _lifeToggle.onValueChanged.AddListener(
                state =>
                {
                    if (state)
                    {
                        ToggleChooseOnValueChangedHandler(Enumerators.SetType.LIFE);
                    }
                });
            _waterToggle.onValueChanged.AddListener(
                state =>
                {
                    if (state)
                    {
                        ToggleChooseOnValueChangedHandler(Enumerators.SetType.WATER);
                    }
                });
            _toxicTogggle.onValueChanged.AddListener(
                state =>
                {
                    if (state)
                    {
                        ToggleChooseOnValueChangedHandler(Enumerators.SetType.TOXIC);
                    }
                });
            _fireToggle.onValueChanged.AddListener(
                state =>
                {
                    if (state)
                    {
                        ToggleChooseOnValueChangedHandler(Enumerators.SetType.FIRE);
                    }
                });
            _earthToggle.onValueChanged.AddListener(
                state =>
                {
                    if (state)
                    {
                        ToggleChooseOnValueChangedHandler(Enumerators.SetType.EARTH);
                    }
                });
            _itemsToggle.onValueChanged.AddListener(
                state =>
                {
                    if (state)
                    {
                        ToggleChooseOnValueChangedHandler(Enumerators.SetType.ITEM);
                    }
                });

            _buttonArmyArrowLeft.onClick.AddListener(ArmyArrowLeftButtonHandler);
            _buttonArmyArrowRight.onClick.AddListener(ArmyArrowRightButtonHandler);
            _buttonHordeArrowLeft.onClick.AddListener(HordeArrowLeftButtonHandler);
            _buttonHordeArrowRight.onClick.AddListener(HordeArrowRightButtonHandler);

            _armyScrollNotifier.Scrolled += v =>
            {
                ScrollCardList(false, v);
            };

            _hordeScrollNotifier.Scrolled += v =>
            {
                ScrollCardList(true, v);
            };

            _deckNameInputField.onEndEdit.AddListener(OnDeckNameInputFieldEndedEdit);

            _uiManager.GetPopup<WarningPopup>().PopupHiding += OnCloseAlertDialogEventHandler;
            FillCollectionData();

            _selfPage.SetActive(true);
            if (_currentDeckId == -1)
            {
                _currentDeck = new Deck();
                _currentDeck.Id = -1;
                _currentDeck.Name = "HORDE " + _dataManager.CachedDecksData.Decks.Count;
                _currentDeck.Cards = new List<DeckCardData>();
                _currentDeck.HeroId = _currentHeroId;
            }
            else
            {
                _currentDeck = _dataManager.CachedDecksData.Decks.First(d => d.Id == _currentDeckId).Clone();
            }

            LoadDeckInfo(_currentDeck);
            InitObjects();

            _hordeAreaObject = _selfPage.transform.Find("Horde/ScrollArea").gameObject;
            _armyAreaObject = _selfPage.transform.Find("Army/ScrollArea").gameObject;
        }

        public void Hide()
        {
            Dispose();

            if (_draggingObject != null)
            {
                Object.Destroy(_draggingObject);
                _draggingObject = null;
                _isDragging = false;
            }

            if (_selfPage == null)
                return;

            _selfPage.SetActive(false);
            Object.Destroy(_selfPage);
            _selfPage = null;
        }

        public void Dispose()
        {
            ResetArmyCards();
            ResetHordeItems();
            _uiManager.GetPopup<WarningPopup>().PopupHiding -= OnCloseAlertDialogEventHandler;

            _cardInfoPopupHandler.Dispose();
        }

        public void MoveCardsPage(int direction)
        {
            _currentElementPage += direction;

            if (_currentElementPage < 0)
            {
                _currentSet += direction;

                if (_currentSet < 0)
                {
                    _currentSet = (Enumerators.SetType)(_numSets - 1);
                    CalculateNumberOfPages();
                    _currentElementPage = _numElementPages - 1;
                }
                else
                {
                    CalculateNumberOfPages();

                    _currentElementPage = _numElementPages - 1;

                    _currentElementPage = _currentElementPage < 0 ? 0 : _currentElementPage;
                }
            }
            else if (_currentElementPage >= _numElementPages)
            {
                _currentSet += direction;

                if ((int)_currentSet >= _numSets)
                {
                    _currentSet = 0;
                    _currentElementPage = 0;
                }
                else
                {
                    _currentElementPage = 0;
                }
            }

            UpdateCardsPage();
        }

        public void LoadCards(int page, Enumerators.SetType setType)
        {
            if (setType == Enumerators.SetType.NONE ||
                setType == Enumerators.SetType.OTHERS ||
                (int)setType >= Enum.GetNames(typeof(Enumerators.SetType)).Length)
            {
                setType = Enumerators.SetType.FIRE;
            }


            _toggleGroup.transform.GetChild((int)setType).GetComponent<Toggle>().isOn = true;

            CardSet set = SetTypeUtility.GetCardSet(_dataManager, setType);

            List<Card> cards = set.Cards;
            int startIndex = page * CardsPerPage;
            int endIndex = Mathf.Min(startIndex + CardsPerPage, cards.Count);

            ResetArmyCards();
            CollectionCardData collectionCardData = null;
            for (int i = startIndex; i < endIndex; i++)
            {
                if (i >= cards.Count)
                {
                    break;
                }

                Card card = cards[i];
                CollectionCardData cardData = _dataManager.CachedCollectionData.GetCardData(card.Name);

                if (cardData == null)
                {
                    continue;
                }

                BoardCard boardCard = CreateCard(card, Vector3.zero, _armyCardsContainer);
                boardCard.Transform.Find("Amount").gameObject.SetActive(false);

                DeckBuilderCard deckBuilderCard = boardCard.GameObject.AddComponent<DeckBuilderCard>();
                deckBuilderCard.Scene = this;
                deckBuilderCard.Card = card;

                OnBehaviourHandler eventHandler = boardCard.GameObject.GetComponent<OnBehaviourHandler>();

                eventHandler.DragBegan += BoardCardDragBeganHandler;
                eventHandler.DragEnded += BoardCardDragEndedHandler;
                eventHandler.DragUpdated += BoardCardDragUpdatedHandler;

                _createdArmyCards.Add(boardCard);

                collectionCardData = _collectionData.GetCardData(card.Name);
                UpdateCardAmount(true, card.Name, collectionCardData.Amount);

            }
        }

        public BoardCard CreateCard(Card card, Vector3 worldPos, RectTransform root)
        {
            BoardCard boardCard;
            GameObject go;
            switch (card.CardKind)
            {
                case Enumerators.CardKind.CREATURE:
                    go = Object.Instantiate(_cardCreaturePrefab);
                    boardCard = new UnitBoardCard(go);
                    break;
                case Enumerators.CardKind.SPELL:
                    go = Object.Instantiate(_cardItemPrefab);
                    boardCard = new SpellBoardCard(go);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(card.CardKind), card.CardKind, null);
            }

            int amount = _collectionData.GetCardData(card.Name).Amount;

            boardCard.Init(card, amount);
            boardCard.SetHighlightingEnabled(false);
            boardCard.Transform.position = worldPos;
            boardCard.Transform.localScale = Vector3.one * 0.3f;
            boardCard.GameObject.GetComponent<SortingGroup>().sortingLayerID = SRSortingLayers.GameUI1;

            boardCard.Transform.SetParent(_uiManager.Canvas.transform, true);
            RectTransform cardRectTransform = boardCard.GameObject.AddComponent<RectTransform>();

            if (root != null)
            {
                cardRectTransform.SetParent(root);
            }

            Vector3 anchoredPos = boardCard.Transform.localPosition;
            anchoredPos.z = 0;
            boardCard.Transform.localPosition = anchoredPos;

            return boardCard;
        }

        public void OnDeckNameInputFieldEndedEdit(string value)
        {
            _currentDeck.Name = value;
        }

        public void LoadDeckInfo(Deck deck)
        {
            _deckNameInputField.text = deck.Name;

            ResetHordeItems();

            foreach (DeckCardData card in deck.Cards)
            {
                Card libraryCard = _dataManager.CachedCardsLibraryData.GetCardFromName(card.CardName);

                bool itemFound = false;
                foreach (BoardCard item in _createdHordeCards)
                {
                    if (item.LibraryCard.Name == card.CardName)
                    {
                        itemFound = true;
                        break;
                    }
                }

                if (!itemFound)
                {
                    BoardCard boardCard = CreateCard(libraryCard, Vector3.zero, _hordeCardsContainer);
                    boardCard.Transform.Find("Amount").gameObject.SetActive(false);

                    DeckBuilderCard deckBuilderCard = boardCard.GameObject.AddComponent<DeckBuilderCard>();
                    deckBuilderCard.Scene = this;
                    deckBuilderCard.Card = libraryCard;
                    deckBuilderCard.IsHordeItem = true;

                    _createdHordeCards.Add(boardCard);

                    boardCard.SetAmountOfCardsInEditingPage(true, GetMaxCopiesValue(libraryCard), card.Amount);

                    _collectionData.GetCardData(card.CardName).Amount -= card.Amount;
                    UpdateNumCardsText();
                }
            }

            UpdateTopDeck();
        }

        public void RemoveCardFromDeck(DeckBuilderCard sender, Card card)
        {
            GameClient.Get<ISoundManager>().PlaySound(Enumerators.SoundType.DECKEDITING_REMOVE_CARD,
                Constants.SfxSoundVolume, false, false, true);
            CollectionCardData collectionCardData = _collectionData.GetCardData(card.Name);
            collectionCardData.Amount++;
            UpdateCardAmount(false, card.Name, collectionCardData.Amount);
            BoardCard boardCard = _createdHordeCards.Find(item => item.LibraryCard.Id == card.Id);
            boardCard.CardsAmountDeckEditing--;
            _currentDeck.RemoveCard(card.Name);

            // Animated moving card
            if (sender != null)
            {
                GetSetAndIndexForCard(boardCard.LibraryCard, out int setIndex, out int cardIndex);
                _currentSet = SetTypeUtility.GetCardSetType(_dataManager, setIndex);
                _currentElementPage = cardIndex / CardsPerPage;
                UpdateCardsPage();

                Vector3 senderPosition = sender.transform.position;

                // Wait for 1 frame for UI to rebuild itself
                Sequence waitSequence = DOTween.Sequence();
                waitSequence.AppendInterval(Time.fixedDeltaTime);
                waitSequence.AppendCallback(
                    () =>
                    {
                        CreateExchangeAnimationCard(
                            senderPosition,
                            boardCard.LibraryCard,
                            true,
                            _createdArmyCards,
                            pageIndex =>
                            {
                            });
                    });
            }

            if (boardCard.CardsAmountDeckEditing == 0)
            {
                _createdHordeCards.Remove(boardCard);

                Object.DestroyImmediate(boardCard.GameObject);

                int currentHordePage = _currentHordePage;
                UpdateHordePagesCount();
                if (currentHordePage >= _numHordePages)
                {
                    _currentHordePage = _numHordePages - 1;
                }

                RepositionHordeCards();
                UpdateNumCardsText();

                if (_highlightingVFXItem.CardId == boardCard.LibraryCard.Id)
                {
                    _highlightingVFXItem.ChangeState(false);
                    _highlightingVFXItem.CardId = -1;
                }
            }
            else
            {
                boardCard.SetAmountOfCardsInEditingPage(false, GetMaxCopiesValue(boardCard.LibraryCard),
                    boardCard.CardsAmountDeckEditing);
            }
        }

        public void AddCardToDeck(DeckBuilderCard sender, Card card)
        {
            if (_currentDeck == null)
            {
                return;
            }

            if (_against[_dataManager.CachedHeroesData.HeroesParsed[_currentHeroId].HeroElement] == card.CardSetType)
            {
                OpenAlertDialog(
                    "It's not possible to add cards to the deck \n from the faction from which the hero is weak against");
                return;
            }

            CollectionCardData collectionCardData = _collectionData.GetCardData(card.Name);
            if (collectionCardData.Amount == 0)
            {
                OpenAlertDialog(
                    "You don't have enough cards of this type. \n Buy or earn new packs to get more cards!");
                return;
            }

            DeckCardData existingCards = _currentDeck.Cards.Find(x => x.CardName == card.Name);

            uint maxCopies = GetMaxCopiesValue(card);

            if (existingCards != null && existingCards.Amount == maxCopies)
            {
                OpenAlertDialog("You cannot have more than " + maxCopies + " copies of the " +
                    card.CardRank.ToString().ToLowerInvariant() + " card in your deck.");
                return;
            }

            if (_currentDeck.GetNumCards() == Constants.DeckMaxSize)
            {
                OpenAlertDialog("You can not add more than " + Constants.DeckMaxSize +
                    " Cards in a single Horde.");
                return;
            }

            bool itemFound = false;
            BoardCard foundItem = null;
            foreach (BoardCard item in _createdHordeCards)
            {
                if (item.LibraryCard.Id == card.Id)
                {
                    foundItem = item;
                    itemFound = true;

                    break;
                }
            }

            GameClient.Get<ISoundManager>().PlaySound(Enumerators.SoundType.DECKEDITING_ADD_CARD,
                Constants.SfxSoundVolume, false, false, true);
            collectionCardData.Amount--;
            UpdateCardAmount(false, card.Name, collectionCardData.Amount);

            if (!itemFound)
            {
                BoardCard boardCard = CreateCard(card, Vector3.zero, _hordeCardsContainer);
                boardCard.Transform.Find("Amount").gameObject.SetActive(false);
                foundItem = boardCard;

                DeckBuilderCard deckBuilderCard = boardCard.GameObject.AddComponent<DeckBuilderCard>();
                deckBuilderCard.Scene = this;
                deckBuilderCard.Card = card;
                deckBuilderCard.IsHordeItem = true;

                _createdHordeCards.Add(boardCard);

                UpdateHordePagesCount();
                CalculateVisibility();
            }

            _currentDeck.AddCard(card.Name);

            foundItem.SetAmountOfCardsInEditingPage(false, GetMaxCopiesValue(card),
                _currentDeck.Cards.Find(x => x.CardName == foundItem.LibraryCard.Name).Amount);

            // Animated moving card
            if (sender != null)
            {
                CreateExchangeAnimationCard(
                    sender.transform.position,
                    foundItem.LibraryCard,
                    itemFound,
                    _createdHordeCards,
                    pageIndex =>
                    {
                        _currentHordePage = pageIndex;
                        CalculateVisibility();
                        Canvas.ForceUpdateCanvases();
                    });
            } else {
                int foundItemIndex = _createdHordeCards.FindIndex(c => c.LibraryCard.Id == foundItem.LibraryCard.Id);
                _currentHordePage = foundItemIndex / CardsPerPage;
                CalculateVisibility();
                Canvas.ForceUpdateCanvases();
            }
        }

        public uint GetMaxCopiesValue(Card card)
        {
            Enumerators.CardRank rank = card.CardRank;
            uint maxCopies = 0;

            string setName = GameClient.Get<IGameplayManager>().GetController<CardsController>().GetSetOfCard(card);

            if (setName.ToLowerInvariant().Equals("item"))
            {
                maxCopies = Constants.CardItemMaxCopies;
                return maxCopies;
            }

            switch (rank)
            {
                case Enumerators.CardRank.MINION:
                    maxCopies = Constants.CardMinionMaxCopies;
                    break;
                case Enumerators.CardRank.OFFICER:
                    maxCopies = Constants.CardOfficerMaxCopies;
                    break;
                case Enumerators.CardRank.COMMANDER:
                    maxCopies = Constants.CardCommanderMaxCopies;
                    break;
                case Enumerators.CardRank.GENERAL:
                    maxCopies = Constants.CardGeneralMaxCopies;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rank), rank, null);
            }

            return maxCopies;
        }

        public void UpdateCardAmount(bool init, string cardId, int amount)
        {
            foreach (BoardCard card in _createdArmyCards)
            {
                if (card.LibraryCard.Name == cardId)
                {
                    card.SetAmountOfCardsInEditingPage(init, GetMaxCopiesValue(card.LibraryCard), amount, true);
                    break;
                }
            }
        }

        public void UpdateNumCardsText()
        {
            if (_currentDeck != null)
            {
                _cardAmountText.text = _currentDeck.GetNumCards() + " / " + Constants.DeckMaxSize;
            }
        }

        public async void OnDoneButtonPressed()
        {
            if (string.IsNullOrWhiteSpace(_currentDeck.Name))
            {
                OpenAlertDialog("Saving Horde with an empty name is not allowed.");
                return;
            }

            foreach (Deck deck in _dataManager.CachedDecksData.Decks)
            {
                if (_currentDeckId != deck.Id &&
                    deck.Name.Trim().Equals(_currentDeck.Name.Trim(), StringComparison.CurrentCultureIgnoreCase))
                {
                    OpenAlertDialog("Not able to Edit Deck: \n Deck Name already exists.");
                    return;
                }
            }

            bool success = true;
            if (_currentDeckId == -1)
            {
                _currentDeck.HeroId = _currentHeroId;

                try
                {
                    long newDeckId = await _backendFacade.AddDeck(_backendDataControlMediator.UserDataModel.UserId, _currentDeck);
                    _currentDeck.Id = newDeckId;
                    _dataManager.CachedDecksData.Decks.Add(_currentDeck);
                    _analyticsManager.SetEvent(AnalyticsManager.EventDeckCreated);
                    Debug.Log(" ====== Add Deck " + newDeckId + " Successfully ==== ");
                }
                catch (Exception e)
                {
                    Debug.Log("Result === " + e);

                    success = false;
                    OpenAlertDialog("Not able to Add Deck: \n" + e.Message);
                }
            }
            else
            {
                try
                {
                    await _backendFacade.EditDeck(_backendDataControlMediator.UserDataModel.UserId, _currentDeck);

                    for (int i = 0; i < _dataManager.CachedDecksData.Decks.Count; i++)
                    {
                        if (_dataManager.CachedDecksData.Decks[i].Id == _currentDeckId)
                        {
                            _dataManager.CachedDecksData.Decks[i] = _currentDeck;
                            break;
                        }
                    }

                    _analyticsManager.SetEvent(AnalyticsManager.EventDeckEdited);
                    Debug.Log(" ====== Edit Deck Successfully ==== ");
                }
                catch (Exception e)
                {
                    Debug.Log("Result === " + e);

                    success = false;

                    string message = e.Message;

                    string[] description = e.Message.Split('=');
                    if (description.Length > 0)
                    {
                        message = description[description.Length - 1].TrimStart(' ');
                        message = char.ToUpper(message[0]) + message.Substring(1);
                    }
                    OpenAlertDialog("Not able to Edit Deck: \n" + message);
                }
            }

            if (success)
            {
                _dataManager.CachedUserLocalData.LastSelectedDeckId = (int)_currentDeck.Id;
                await _dataManager.SaveCache(Enumerators.CacheDataType.USER_LOCAL_DATA);
                GameClient.Get<IAppStateManager>().ChangeAppState(Enumerators.AppState.HordeSelection);
            }
        }

        public void ScrollCardList(bool isHordeItem, Vector2 scrollDelta)
        {
            if (isHordeItem)
            {
                if (scrollDelta.y > 0.5f)
                {
                    MoveHordeToRight();
                }
                else if (scrollDelta.y < -0.5f)
                {
                    MoveHordeToLeft();
                }
            }
            else
            {
                MoveCardsPage(Mathf.RoundToInt(scrollDelta.y));
            }
        }

        public void SelectCard(DeckBuilderCard deckBuilderCard, Card card)
        {
            BoardCard boardCard;
            if (deckBuilderCard.IsHordeItem)
            {
                boardCard = _createdHordeCards.First(c => c.LibraryCard.Id == card.Id);
                _highlightingVFXItem.SetActiveCard(boardCard);
            }
            else
            {
                boardCard = _createdArmyCards.First(c => c.LibraryCard.Id == card.Id);
            }

            _cardInfoPopupHandler.SelectCard(boardCard);
        }

        private void FillCollectionData()
        {
            _collectionData.Cards.Clear();
            CollectionCardData cardData;
            foreach (CollectionCardData card in _dataManager.CachedCollectionData.Cards)
            {
                cardData = new CollectionCardData();
                cardData.Amount = card.Amount;
                cardData.CardName = card.CardName;

                _collectionData.Cards.Add(cardData);
            }
        }

        private void ResetArmyCards()
        {
            if (_createdArmyCards != null)
            {
                foreach (BoardCard item in _createdArmyCards)
                {
                    item.Dispose();
                }

                _createdArmyCards.Clear();
            }
        }

        private void ResetHordeItems()
        {
            if (_createdHordeCards != null)
            {
                foreach (BoardCard item in _createdHordeCards)
                {
                    item.Dispose();
                }

                _createdHordeCards.Clear();
            }
        }

        private void OpenAlertDialog(string msg)
        {
            GameClient.Get<ISoundManager>().PlaySound(Enumerators.SoundType.CHANGE_SCREEN, Constants.SfxSoundVolume,
                false, false, true);
            _uiManager.DrawPopup<WarningPopup>(msg);
        }

        private void OnCloseAlertDialogEventHandler()
        {
        }

        private void InitObjects()
        {
            _numSets = _dataManager.CachedCardsLibraryData.Sets.Count - 1;

            Enumerators.SetType heroSetType = _dataManager.CachedHeroesData.HeroesParsed
                .Find(x => x.HeroId == _currentDeck.HeroId).HeroElement;

            _currentElementPage = 0;
            _currentSet = heroSetType;
            CalculateNumberOfPages();
            LoadCards(_currentElementPage, _currentSet);
        }

        private void GetSetAndIndexForCard(Card card, out int setIndex, out int cardIndex)
        {
            CardSet set = _dataManager.CachedCardsLibraryData.Sets.Find(x => x.Cards.Find(c => c.Id == card.Id) != null);
            setIndex = _dataManager.CachedCardsLibraryData.Sets.IndexOf(set);
            cardIndex = set.Cards.FindIndex(c => c.Id == card.Id);
        }

        private void UpdateCardsPage()
        {
            CalculateNumberOfPages();
            LoadCards(_currentElementPage, _currentSet);
        }

        private void CalculateNumberOfPages()
        {
            _numElementPages = Mathf.CeilToInt(SetTypeUtility.GetCardSet(_dataManager, _currentSet).Cards.Count /
                (float) CardsPerPage);
        }

        private void UpdateTopDeck()
        {
            UpdateHordePagesCount();
            _currentHordePage = 0;
            RepositionHordeCards();
        }

        private void UpdateHordePagesCount()
        {
            _numHordePages = Mathf.CeilToInt((float) _createdHordeCards.Count / CardsPerPage);
        }

        private void CalculateVisibility()
        {
            for (int i = 0; i < _createdHordeCards.Count; i++)
            {
                if (i + 1 > _currentHordePage * CardsPerPage && i + 1 < (_currentHordePage + 1) * CardsPerPage + 1)
                {
                    _createdHordeCards[i].GameObject.SetActive(true);
                    if (_createdHordeCards[i].LibraryCard.Id == _highlightingVFXItem.CardId)
                    {
                        _highlightingVFXItem.ChangeState(true);
                    }
                }
                else
                {
                    _createdHordeCards[i].GameObject.SetActive(false);
                    if (_createdHordeCards[i].LibraryCard.Id == _highlightingVFXItem.CardId)
                    {
                        _highlightingVFXItem.ChangeState(false);
                    }
                }
            }
        }

        private void RepositionHordeCards()
        {
            CalculateVisibility();
        }

        private void CreateExchangeAnimationCard(
            Vector3 sourceCardPosition, Card targetLibraryCard, bool targetCardWasAlreadyPresent,
            List<BoardCard> targetRowCards, Action<int> setPageIndexAction)
        {
            BoardCard animatedCard = CreateCard(targetLibraryCard, sourceCardPosition, null);
            animatedCard.Transform.Find("Amount").gameObject.SetActive(false);
            animatedCard.GameObject.GetComponent<SortingGroup>().sortingOrder++;

            int foundItemIndex = targetRowCards.FindIndex(c => c.LibraryCard.Id == targetLibraryCard.Id);
            setPageIndexAction(foundItemIndex / CardsPerPage);

            BoardCard targetCard = targetRowCards.First(card => card.LibraryCard.Id == targetLibraryCard.Id);
            Vector3 animatedCardDestination = targetCard.Transform.position;

            if (!targetCardWasAlreadyPresent)
            {
                targetCard.GameObject.SetActive(false);
            }

            Sequence animatedCardSequence = DOTween.Sequence();
            animatedCardSequence.Append(animatedCard.Transform.DOMove(animatedCardDestination, .3f));
            animatedCardSequence.AppendCallback(() => Object.Destroy(animatedCard.GameObject));
            animatedCardSequence.AppendCallback(
                () =>
                {
                    if (!targetCardWasAlreadyPresent)
                    {
                        targetCard.GameObject.SetActive(true);
                    }
                });
        }

        private void BoardCardDragBeganHandler(PointerEventData eventData, GameObject onOnject)
        {
            if (_isDragging)
            {
                return;
            }

            _draggingObject = Object.Instantiate(onOnject);
            _draggingObject.transform.localScale = Vector3.one * 0.3f;
            _draggingObject.transform.Find("Amount").gameObject.SetActive(false);
            _draggingObject.transform.Find("AmountForArmy").gameObject.SetActive(false);
            _draggingObject.transform.Find("DeckEditingGroupUI").gameObject.SetActive(false);
            _draggingObject.name = onOnject.GetInstanceID().ToString();

            _isDragging = true;

            Vector3 position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            position.z = _draggingObject.transform.position.z;
            _draggingObject.transform.position = position;
        }

        private void BoardCardDragEndedHandler(PointerEventData eventData, GameObject onOnject)
        {
            if (!_isDragging)
            {
                return;
            }

            Vector3 point = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            RaycastHit2D[] hits = Physics2D.RaycastAll(point, Vector3.forward, Mathf.Infinity, SRLayerMask.Default);

            if (hits.Length > 0)
            {
                foreach (RaycastHit2D hit in hits)
                {
                    if (hit.collider.gameObject == _hordeAreaObject)
                    {
                        BoardCard armyCard = _createdArmyCards.Find(x =>
                            x.GameObject.GetInstanceID().ToString() == _draggingObject.name);

                        AddCardToDeck(null, armyCard.LibraryCard);
                    }
                }
            }

            Object.Destroy(_draggingObject);
            _draggingObject = null;
            _isDragging = false;
        }

        private void BoardCardDragUpdatedHandler(PointerEventData eventData, GameObject onOnject)
        {
            if (!_isDragging)
            {
                return;
            }

            Vector3 position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            position.z = _draggingObject.transform.position.z;
            _draggingObject.transform.position = position;
        }

        #region button handlers

        private void ToggleChooseOnValueChangedHandler(Enumerators.SetType type)
        {
            if (type == _currentSet)
                return;

            GameClient.Get<ISoundManager>().PlaySound(Enumerators.SoundType.CHANGE_SCREEN, Constants.SfxSoundVolume,
                false, false, true);

            _currentSet = type;
            _currentElementPage = 0;
            LoadCards(_currentElementPage, _currentSet);
        }

        private void BackButtonHandler()
        {
            GameClient.Get<ISoundManager>()
                .PlaySound(Enumerators.SoundType.CLICK, Constants.SfxSoundVolume, false, false, true);
            _uiManager.GetPopup<QuestionPopup>().ConfirmationReceived += ConfirmQuitReceivedHandler;
            _uiManager.DrawPopup<QuestionPopup>(new object[]
            {
                "Would you like to save the current horde?", true
            });
        }

        private void ConfirmQuitReceivedHandler(bool status)
        {
            _uiManager.GetPopup<QuestionPopup>().ConfirmationReceived -= ConfirmQuitReceivedHandler;
            if (status)
            {
                OnDoneButtonPressed();
            }

            GameClient.Get<IAppStateManager>().ChangeAppState(Enumerators.AppState.HordeSelection);
        }

        private void BuyButtonHandler()
        {
            GameClient.Get<ISoundManager>()
                .PlaySound(Enumerators.SoundType.CLICK, Constants.SfxSoundVolume, false, false, true);
            GameClient.Get<IAppStateManager>().ChangeAppState(Enumerators.AppState.SHOP);
        }

        private void ArmyButtonHandler()
        {
            GameClient.Get<ISoundManager>()
                .PlaySound(Enumerators.SoundType.CLICK, Constants.SfxSoundVolume, false, false, true);
            GameClient.Get<IAppStateManager>().ChangeAppState(Enumerators.AppState.ARMY);
        }

        private void SaveButtonHandler()
        {
            GameClient.Get<ISoundManager>()
                .PlaySound(Enumerators.SoundType.CLICK, Constants.SfxSoundVolume, false, false, true);
            OnDoneButtonPressed();
        }

        private void ArmyArrowLeftButtonHandler()
        {
            GameClient.Get<ISoundManager>()
                .PlaySound(Enumerators.SoundType.CLICK, Constants.SfxSoundVolume, false, false, true);
            MoveCardsPage(-1);
        }

        private void ArmyArrowRightButtonHandler()
        {
            GameClient.Get<ISoundManager>()
                .PlaySound(Enumerators.SoundType.CLICK, Constants.SfxSoundVolume, false, false, true);
            MoveCardsPage(1);
        }

        private void HordeArrowLeftButtonHandler()
        {
            MoveHordeToLeft();
            GameClient.Get<ISoundManager>()
                .PlaySound(Enumerators.SoundType.CLICK, Constants.SfxSoundVolume, false, false, true);
        }

        private void MoveHordeToLeft()
        {
            _currentHordePage--;
            if (_currentHordePage < 0)
            {
                _currentHordePage = _numHordePages - 1;
            }

            CalculateVisibility();
        }

        private void HordeArrowRightButtonHandler()
        {
            MoveHordeToRight();
            GameClient.Get<ISoundManager>()
                .PlaySound(Enumerators.SoundType.CLICK, Constants.SfxSoundVolume, false, false, true);
        }

        private void MoveHordeToRight()
        {
            _currentHordePage++;

            if (_currentHordePage >= _numHordePages)
            {
                _currentHordePage = 0;
            }

            CalculateVisibility();
        }

        #endregion

    }
}
