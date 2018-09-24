using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Loom.ZombieBattleground.BackendCommunication;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Loom.ZombieBattleground
{
    public class GameplayPage : IUIElement
    {
        public OnBehaviourHandler PlayerPrimarySkillHandler, PlayerSecondarySkillHandler;

        public OnBehaviourHandler OpponentPrimarySkillHandler, OpponentSecondarySkillHandler;

        private IUIManager _uiManager;

        private ILoadObjectsManager _loadObjectsManager;

        private IDataManager _dataManager;

        private IGameplayManager _gameplayManager;

        private ISoundManager _soundManager;

        private ITutorialManager _tutorialManager;

        private BackendDataControlMediator _backendDataControlMediator;

        private BattlegroundController _battlegroundController;

        private GameObject _selfPage;

        private Button _buttonBack;

        private ButtonShiftingContent _buttonKeep;

        private PlayerManaBarItem _playerManaBar, _opponentManaBar;

        private List<CardZoneOnBoardStatus> _deckStatus, _graveyardStatus;

        private TextMeshPro _playerHealthText,
            _opponentHealthText,
            _playerCardDeckCountText,
            _opponentCardDeckCountText,
            _playerNameText,
            _opponentNameText;

        private SpriteRenderer _playerDeckStatusTexture,
            _opponentDeckStatusTexture,
            _playerGraveyardStatusTexture,
            _opponentGraveyardStatusTexture;

        private GameObject _zippingVfx;

        private bool _isPlayerInited;

        private PastActionReportPanel _reportGameActionsPanel;

        private GameObject _endTurnButton;

        public int CurrentDeckId { get; set; }

        public void Init()
        {
            _uiManager = GameClient.Get<IUIManager>();
            _loadObjectsManager = GameClient.Get<ILoadObjectsManager>();
            _dataManager = GameClient.Get<IDataManager>();
            _gameplayManager = GameClient.Get<IGameplayManager>();
            _soundManager = GameClient.Get<ISoundManager>();
            _tutorialManager = GameClient.Get<ITutorialManager>();
            _backendDataControlMediator = GameClient.Get<BackendDataControlMediator>();

            _gameplayManager.GameInitialized += GameInitializedHandler;
            _gameplayManager.GameEnded += GameEndedHandler;

            _deckStatus = new List<CardZoneOnBoardStatus>();
            _deckStatus.Add(new CardZoneOnBoardStatus(null, 0));
            _deckStatus.Add(new CardZoneOnBoardStatus(
                _loadObjectsManager.GetObjectByPath<Sprite>("Images/BoardCardsStatuses/deck_single"), 15));
            _deckStatus.Add(new CardZoneOnBoardStatus(
                _loadObjectsManager.GetObjectByPath<Sprite>("Images/BoardCardsStatuses/deck_couple"), 40));
            _deckStatus.Add(new CardZoneOnBoardStatus(
                _loadObjectsManager.GetObjectByPath<Sprite>("Images/BoardCardsStatuses/deck_bunch"), 60));
            _deckStatus.Add(new CardZoneOnBoardStatus(
                _loadObjectsManager.GetObjectByPath<Sprite>("Images/BoardCardsStatuses/deck_full"), 80));

            _graveyardStatus = new List<CardZoneOnBoardStatus>();
            _graveyardStatus.Add(new CardZoneOnBoardStatus(null, 0));
            _graveyardStatus.Add(new CardZoneOnBoardStatus(
                _loadObjectsManager.GetObjectByPath<Sprite>("Images/BoardCardsStatuses/graveyard_single"), 10));
            _graveyardStatus.Add(new CardZoneOnBoardStatus(
                _loadObjectsManager.GetObjectByPath<Sprite>("Images/BoardCardsStatuses/graveyard_couple"), 40));
            _graveyardStatus.Add(new CardZoneOnBoardStatus(
                _loadObjectsManager.GetObjectByPath<Sprite>("Images/BoardCardsStatuses/graveyard_bunch"), 75));
            _graveyardStatus.Add(new CardZoneOnBoardStatus(
                _loadObjectsManager.GetObjectByPath<Sprite>("Images/BoardCardsStatuses/graveyard_full"), 100));
        }

        public void Hide()
        {
            _isPlayerInited = false;

            if (_selfPage == null)
                return;

            _selfPage.SetActive(false);
            _reportGameActionsPanel.Dispose();
            _reportGameActionsPanel = null;
            Object.Destroy(_selfPage);
            _selfPage = null;
        }

        public void Dispose()
        {
        }

        public void Update()
        {
            if (_selfPage != null && _selfPage.activeInHierarchy)
            {
                if (_reportGameActionsPanel != null)
                {
                    _reportGameActionsPanel.Update();
                }
            }
        }

        public void Show()
        {
            _selfPage = Object.Instantiate(
                _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/UI/Pages/GameplayPage"));
            _selfPage.transform.SetParent(_uiManager.Canvas.transform, false);

            _buttonBack = _selfPage.transform.Find("Button_Back").GetComponent<Button>();
            _buttonKeep = _selfPage.transform.Find("Button_Keep").GetComponent<ButtonShiftingContent>();

            _buttonBack.onClick.AddListener(BackButtonOnClickHandler);
            _buttonKeep.onClick.AddListener(KeepButtonOnClickHandler);

            _reportGameActionsPanel = new PastActionReportPanel(_selfPage.transform.Find("ActionReportPanel").gameObject);

            if (_zippingVfx == null)
            {
                _zippingVfx = GameObject.Find("Background/Zapping").gameObject;
                _zippingVfx.SetActive(false);
            }

            if (_gameplayManager.IsTutorial)
            {
                _buttonBack.gameObject.SetActive(false);
            }

            StartGame();
            KeepButtonVisibility(false);
        }

        public void SetEndTurnButtonStatus(bool status)
        {
            _endTurnButton.GetComponent<EndTurnButton>().SetEnabled(status);
        }

        public void StartGame()
        {
            if (_battlegroundController == null)
            {
                _battlegroundController = _gameplayManager.GetController<BattlegroundController>();

                _battlegroundController.PlayerGraveyardUpdated += PlayerGraveyardUpdatedHandler;
                _battlegroundController.OpponentGraveyardUpdated += OpponentGraveyardUpdatedHandler;
            }

            _gameplayManager.PlayerDeckId = CurrentDeckId;

            OpponentDeck randomOpponentDeck =
                _dataManager.CachedOpponentDecksData.Decks[
                    Random.Range(0, _dataManager.CachedOpponentDecksData.Decks.Count)];
            _gameplayManager.OpponentDeckId = randomOpponentDeck.Id;

            //Debug.Log(_tutorialManager.CurrentTutorial.SpecificBattlegroundInfo);
            //Debug.Log(_tutorialManager.CurrentTutorial.SpecificBattlegroundInfo.OpponentInfo);
            //Debug.Log(_tutorialManager.CurrentTutorial.SpecificBattlegroundInfo.OpponentInfo.HeroId);
            int heroId = 0; //Constants.TutorialPlayerHeroId; // TUTORIAL
            int opponentHeroId = 0;

            if (!_gameplayManager.IsTutorial)
            {
                heroId = _dataManager.CachedDecksData.Decks.First(o => o.Id == CurrentDeckId).HeroId;
                opponentHeroId = randomOpponentDeck.HeroId;
            }
            else
            {
                heroId = _tutorialManager.CurrentTutorial.SpecificBattlegroundInfo.PlayerInfo.HeroId;
                opponentHeroId = _tutorialManager.CurrentTutorial.SpecificBattlegroundInfo.OpponentInfo.HeroId;
            }


            Hero currentPlayerHero = _dataManager.CachedHeroesData.HeroesParsed[heroId];
            Hero currentOpponentHero = _dataManager.CachedHeroesData.HeroesParsed[opponentHeroId];

            _playerDeckStatusTexture = GameObject.Find("Player/Deck_Illustration/Deck").GetComponent<SpriteRenderer>();
            _opponentDeckStatusTexture =
                GameObject.Find("Opponent/Deck_Illustration/Deck").GetComponent<SpriteRenderer>();
            _playerGraveyardStatusTexture = GameObject.Find("Player/Graveyard_Illustration/Graveyard")
                .GetComponent<SpriteRenderer>();
            _opponentGraveyardStatusTexture = GameObject.Find("Opponent/Graveyard_Illustration/Graveyard")
                .GetComponent<SpriteRenderer>();

            _playerHealthText = GameObject.Find("Player/Avatar/LivesCircle/DefenceText").GetComponent<TextMeshPro>();
            _opponentHealthText =
                GameObject.Find("Opponent/Avatar/LivesCircle/DefenceText").GetComponent<TextMeshPro>();

            _playerManaBar = new PlayerManaBarItem(GameObject.Find("PlayerManaBar"), "GooOverflowPlayer",
                new Vector3(-3.55f, 0, -6.07f));
            _opponentManaBar = new PlayerManaBarItem(GameObject.Find("OpponentManaBar"), "GooOverflowOpponent",
                new Vector3(9.77f, 0, 4.75f));

            // improve find to get it from OBJECTS ON BOARD!!
            _playerNameText = GameObject.Find("Player/NameBoard/NameText").GetComponent<TextMeshPro>();
            _opponentNameText = GameObject.Find("Opponent/NameBoard/NameText").GetComponent<TextMeshPro>();

            _playerCardDeckCountText = GameObject.Find("Player/CardDeckText").GetComponent<TextMeshPro>();
            _opponentCardDeckCountText = GameObject.Find("Opponent/CardDeckText").GetComponent<TextMeshPro>();

            _endTurnButton = GameObject.Find("EndTurnButton");
            
            PlayerPrimarySkillHandler =
                GameObject.Find("Player").transform.Find("Object_SpellPrimary").GetComponent<OnBehaviourHandler>();
            PlayerSecondarySkillHandler =
                GameObject.Find("Player").transform.Find("Object_SpellSecondary").GetComponent<OnBehaviourHandler>();

            OpponentPrimarySkillHandler =
                GameObject.Find("Opponent").transform.Find("Object_SpellPrimary").GetComponent<OnBehaviourHandler>();
            OpponentSecondarySkillHandler =
                GameObject.Find("Opponent").transform.Find("Object_SpellSecondary").GetComponent<OnBehaviourHandler>();

            if (currentPlayerHero != null)
            {
                SetHeroInfo(currentPlayerHero, "Player", PlayerPrimarySkillHandler.gameObject,
                    PlayerSecondarySkillHandler.gameObject);
                string playerNameText = currentPlayerHero.FullName;
                if (_backendDataControlMediator.LoadUserDataModel())
                {
                    playerNameText = _backendDataControlMediator.UserDataModel.UserId;
                }

                _playerNameText.text = playerNameText;
            }

            if (currentOpponentHero != null)
            {
                SetHeroInfo(currentOpponentHero, "Opponent", OpponentPrimarySkillHandler.gameObject,
                    OpponentSecondarySkillHandler.gameObject);
                _opponentNameText.text = currentOpponentHero.FullName;
            }

            _isPlayerInited = true;
        }

        public void SetHeroInfo(Hero hero, string objectName, GameObject skillPrimary, GameObject skillSecondary)
        {
            HeroSkill skillPrim = hero.Skills[hero.PrimarySkill];
            HeroSkill skillSecond = hero.Skills[hero.SecondarySkill];

            skillPrimary.transform.Find("Icon").GetComponent<SpriteRenderer>().sprite =
                _loadObjectsManager.GetObjectByPath<Sprite>("Images/HeroesIcons/heroability_" + hero.HeroElement + "_" +
                    skillPrim.Skill.ToLower());
            skillSecondary.transform.Find("Icon").GetComponent<SpriteRenderer>().sprite =
                _loadObjectsManager.GetObjectByPath<Sprite>("Images/HeroesIcons/heroability_" + hero.HeroElement + "_" +
                    skillSecond.Skill.ToLower());

            Texture2D heroTexture =
                _loadObjectsManager.GetObjectByPath<Texture2D>("Images/Heroes/CZB_2D_Hero_Portrait_" + hero.HeroElement + "_EXP");
            Transform transfHeroObject = GameObject.Find(objectName + "/Avatar/Hero_Object").transform;

            Material heroAvatarMaterial = new Material(Shader.Find("Sprites/Default"));
            heroAvatarMaterial.mainTexture = heroTexture;

            for (int i = 0; i < transfHeroObject.childCount; i++)
            {
                transfHeroObject.GetChild(i).GetComponent<Renderer>().material = heroAvatarMaterial;
            }

            Sprite heroHighlight =
                _loadObjectsManager.GetObjectByPath<Sprite>("Images/Heroes/CZB_2D_Hero_Decor_" + hero.HeroElement + "_EXP");
            GameObject.Find(objectName + "/Avatar/HeroHighlight").GetComponent<SpriteRenderer>().sprite = heroHighlight;
        }

        private void GameEndedHandler(Enumerators.EndGameType endGameType)
        {
            SetEndTurnButtonStatus(true);

            _reportGameActionsPanel?.Clear();
        }

        private int GetPercentFromMaxDeck(int index)
        {
            return 100 * index / (int) Constants.DeckMaxSize;
        }

        private class CardZoneOnBoardStatus
        {
            public readonly int Percent;

            public readonly Sprite StatusSprite;

            public CardZoneOnBoardStatus(Sprite statusSprite, int percent)
            {
                StatusSprite = statusSprite;
                Percent = percent;
            }
        }

        #region event handlers

        private void GameInitializedHandler()
        {
            Player player = _gameplayManager.CurrentPlayer;
            Player opponent = _gameplayManager.OpponentPlayer;

            player.DeckChanged += OnPlayerDeckChangedHandler;
            player.PlayerHpChanged += OnPlayerHpChanged;
            player.PlayerGooChanged += OnPlayerGooChanged;
            player.PlayerVialGooChanged += OnPlayerVialGooChanged;
            opponent.DeckChanged += OnOpponentDeckChangedHandler;
            opponent.PlayerHpChanged += OnOpponentHpChanged;
            opponent.PlayerGooChanged += OnOpponentGooChanged;
            opponent.PlayerVialGooChanged += OnOpponentVialGooChanged;

            player.TurnStarted += TurnStartedHandler;

            OnPlayerDeckChangedHandler(player.CardsInDeck.Count);
            OnPlayerHpChanged(player.Health);
            OnPlayerGooChanged(player.Goo);
            OnPlayerVialGooChanged(player.GooOnCurrentTurn);
            OnOpponentDeckChangedHandler(opponent.CardsInDeck.Count);
            OnOpponentHpChanged(opponent.Health);
            OnOpponentGooChanged(opponent.GooOnCurrentTurn);
            OnOpponentVialGooChanged(opponent.GooOnCurrentTurn);
        }

        private void OnPlayerDeckChangedHandler(int index)
        {
            if (!_isPlayerInited)
                return;

            _playerCardDeckCountText.text = index.ToString();

            if (index == 0)
            {
                _playerDeckStatusTexture.sprite = _deckStatus.Find(x => x.Percent == index).StatusSprite;
            }
            else
            {
                int percent = GetPercentFromMaxDeck(index);

                CardZoneOnBoardStatus nearest =
                    _deckStatus
                        .OrderBy(x => Math.Abs(x.Percent - percent))
                        .First(y => y.Percent > 0);

                _playerDeckStatusTexture.sprite = nearest.StatusSprite;
            }
        }

        private void PlayerGraveyardUpdatedHandler(int index)
        {
            if (!_isPlayerInited)
                return;

            if (index == 0)
            {
                _playerGraveyardStatusTexture.sprite = _graveyardStatus.Find(x => x.Percent == index).StatusSprite;
            }
            else
            {
                int percent = GetPercentFromMaxDeck(index);

                List<CardZoneOnBoardStatus> nearestObjects = _graveyardStatus
                    .OrderBy(x => Math.Abs(x.Percent - percent)).Where(y => y.Percent > 0).ToList();

                CardZoneOnBoardStatus nearest;

                if (nearestObjects[0].Percent > 0)
                {
                    nearest = nearestObjects[0];
                }
                else
                {
                    nearest = nearestObjects[1];
                }

                _playerGraveyardStatusTexture.sprite = nearest.StatusSprite;
            }
        }

        private void OnOpponentDeckChangedHandler(int index)
        {
            if (!_isPlayerInited)
                return;

            _opponentCardDeckCountText.text = index.ToString();

            if (index == 0)
            {
                _opponentDeckStatusTexture.sprite = _deckStatus.Find(x => x.Percent == index).StatusSprite;
            }
            else
            {
                int percent = GetPercentFromMaxDeck(index);

                CardZoneOnBoardStatus nearest = _deckStatus.OrderBy(x => Math.Abs(x.Percent - percent))
                    .Where(y => y.Percent > 0).First();

                _opponentDeckStatusTexture.sprite = nearest.StatusSprite;
            }
        }

        private void OpponentGraveyardUpdatedHandler(int index)
        {
            if (!_isPlayerInited)
                return;

            if (index == 0)
            {
                _opponentGraveyardStatusTexture.sprite = _deckStatus.Find(x => x.Percent == index).StatusSprite;
            }
            else
            {
                int percent = GetPercentFromMaxDeck(index);

                List<CardZoneOnBoardStatus> nearestObjects = _graveyardStatus
                    .OrderBy(x => Math.Abs(x.Percent - percent)).Where(y => y.Percent > 0).ToList();

                CardZoneOnBoardStatus nearest;
                if (nearestObjects[0].Percent > 0)
                {
                    nearest = nearestObjects[0];
                }
                else
                {
                    nearest = nearestObjects[1];
                }

                _opponentGraveyardStatusTexture.sprite = nearest.StatusSprite;
            }
        }

        private void OnPlayerHpChanged(int health)
        {
            if (!_isPlayerInited)
                return;

            _playerHealthText.text = health.ToString();

            if (health > 9)
            {
                _playerHealthText.color = Color.white;
            }
            else
            {
                _playerHealthText.color = Color.red;
            }
        }

        private void OnPlayerGooChanged(int goo)
        {
            if (!_isPlayerInited)
                return;

            _playerManaBar.SetGoo(goo);
        }

        private void OnPlayerVialGooChanged(int currentTurnGoo)
        {
            if (!_isPlayerInited)
                return;

            _playerManaBar.SetVialGoo(currentTurnGoo);
        }

        private void OnOpponentHpChanged(int health)
        {
            if (!_isPlayerInited)
                return;

            _opponentHealthText.text = health.ToString();

            if (health > 9)
            {
                _opponentHealthText.color = Color.white;
            }
            else
            {
                _opponentHealthText.color = Color.red;
            }
        }

        private void OnOpponentGooChanged(int goo)
        {
            if (!_isPlayerInited)
                return;

            _opponentManaBar.SetGoo(goo);
        }

        private void OnOpponentVialGooChanged(int currentTurnGoo)
        {
            if (!_isPlayerInited)
                return;

            _opponentManaBar.SetVialGoo(currentTurnGoo);
        }

        private void TurnStartedHandler()
        {
            _zippingVfx.SetActive(_gameplayManager.GetController<PlayerController>().IsActive);
        }

        #endregion

        #region Buttons Handlers

        public void BackButtonOnClickHandler()
        {
            Action[] actions = new Action[2];
            actions[0] = () =>
            {
                _uiManager.HidePopup<YourTurnPopup>();

                _gameplayManager.EndGame(Enumerators.EndGameType.CANCEL);
                GameClient.Get<IMatchManager>().FinishMatch(Enumerators.AppState.MAIN_MENU);

                _soundManager.StopPlaying(Enumerators.SoundType.TUTORIAL);
                _soundManager.CrossfaidSound(Enumerators.SoundType.BACKGROUND, null, true);
            };
            actions[1] = () => { };

            _uiManager.DrawPopup<ConfirmationPopup>(actions);
            _soundManager.PlaySound(Enumerators.SoundType.CLICK, Constants.SfxSoundVolume, false, false, true);
        }

        public void KeepButtonOnClickHandler()
        {
            _gameplayManager.GetController<CardsController>().EndCardDistribution();
            KeepButtonVisibility(false);
        }

        public void KeepButtonVisibility(bool visible)
        {
            _buttonKeep.gameObject.SetActive(visible);
        }

        #endregion

    }
}
