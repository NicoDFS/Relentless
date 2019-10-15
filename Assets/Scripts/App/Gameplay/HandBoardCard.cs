using DG.Tweening;
using Loom.ZombieBattleground.Common;
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Loom.ZombieBattleground
{
    public class HandBoardCard : IOwnableBoardObject
    {
        public GameObject BoardZone;

        public bool Enabled = true;

        public CardModel CardModel { get; protected set; }

        public BoardCardView BoardCardView { get; protected set; }

        public bool IsReturnToHand { get; private set; }

    public Collider2D HandCardCollider { get; private set; }

    protected bool StartedDrag;

        protected Vector3 InitialPos;

        protected Vector3 InitialRotation;

        private readonly IGameplayManager _gameplayManager;

        private readonly ISoundManager _soundManager;

        private readonly ITutorialManager _tutorialManager;

        private readonly PlayerController _playerController;

        private readonly CardsController _cardsController;

        private readonly OnBehaviourHandler _behaviourHandler;

        private readonly SortingGroup _sortingGroup;

        private bool _isHandCard = true;

        private bool _alreadySelected;

        private bool _canceledPlay;

        private int _normalSortingOrder;

        private bool _isScaledUp;

        private ActionsQueueController _actionsQueueController;

        public HandBoardCard(GameObject selfObject, BoardCardView boardCardView)
        {
            GameObject = selfObject;

            Transform cardBack = selfObject.transform.Find("Back");
            if (cardBack != null)
            {
                IGameplayManager gameplayManager = GameClient.Get<IGameplayManager>();

                if (gameplayManager.CurrentPlayerDeck != null)
                {
                    if (gameplayManager.CurrentPlayerDeck.Back == 1)
                    {
                        cardBack.GetComponent<SpriteRenderer>().sprite = 
                        GameClient.Get<ILoadObjectsManager>().GetObjectByPath<Sprite>("Images/UI/CardBack/CZB_Card_Back_Backer");
                    }
                    else if (gameplayManager.CurrentPlayerDeck.Back == 0)
                    {
                        cardBack.GetComponent<SpriteRenderer>().sprite = 
                        GameClient.Get<ILoadObjectsManager>().GetObjectByPath<Sprite>("Images/UI/CardBack/cardback");
                    }
                }
            }

            BoardCardView = boardCardView;
            CardModel = boardCardView.Model;
            HandCardCollider = GameObject.GetComponent<Collider2D>();

            _gameplayManager = GameClient.Get<IGameplayManager>();
            _soundManager = GameClient.Get<ISoundManager>();
            _tutorialManager = GameClient.Get<ITutorialManager>();

            _actionsQueueController = _gameplayManager.GetController<ActionsQueueController>();

            _playerController = _gameplayManager.GetController<PlayerController>();
            _cardsController = _gameplayManager.GetController<CardsController>();

            _behaviourHandler = GameObject.GetComponent<OnBehaviourHandler>();

            _sortingGroup = GameObject.GetComponent<SortingGroup>();

            _behaviourHandler.MouseUpTriggered += MouseUp;
            _behaviourHandler.Updating += UpdatingHandler;
        }

        public Transform Transform => GameObject.transform;

        public GameObject GameObject { get; }

        public Player OwnerPlayer => CardModel.OwnerPlayer;

        public void UpdatingHandler(GameObject obj)
        {
            if (!Enabled)
                return;

            if (StartedDrag)
            {
                Transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector3 newPos = Transform.position;
                newPos.z = 0;
                newPos.y += (4.7f*Transform.localScale.x/Constants.DefaultScaleForZoomedCardInHand.x);
                Transform.position = newPos;
                newPos.y -= (4.7f*Transform.localScale.x/Constants.DefaultScaleForZoomedCardInHand.x);

                if (BoardZone.GetComponent<BoxCollider2D>().bounds.Contains(newPos) && _isHandCard)
                {
                    _cardsController.HoverPlayerCardOnBattleground(OwnerPlayer, BoardCardView);
                    if (_isScaledUp)
                    {
                        Transform.DOScale(BoardCardView.ScaleOnHand, Constants.DurationHoveringHandCard);
                        _isScaledUp = false;
                    }
                }
                else
                {
                    _cardsController.ResetPlayerCardsOnBattlegroundPosition();
                    if (!_isScaledUp)
                    {
                        Transform.DOScale(Constants.DefaultScaleForZoomedCardInHand, Constants.DurationHoveringHandCard);
                        _isScaledUp = true;
                    }
                }

                if (!Input.GetMouseButton(0) || Input.GetKeyDown(KeyCode.Escape))
                {
                    MouseUp(null);
                }
            }
        }

        public void OnSelected()
        {
            if (!Enabled)
                return;

            if (_playerController.IsActive && !_playerController.IsCardSelected && CardModel.CanBePlayed(OwnerPlayer) && !IsReturnToHand && !_alreadySelected &&
                Enabled)
            {

                if (_gameplayManager.IsTutorial &&
                    !_tutorialManager.CurrentTutorial.TutorialContent.ToGameplayContent().SpecificBattlegroundInfo.DisabledInitialization)
                {
                    if (CardModel.CanBeBuyed(OwnerPlayer))
                    {
                        if (!_tutorialManager.GetCurrentTurnInfo()
                            .PlayCardsSequence.Exists(info =>
                                info.TutorialObjectId == CardModel.Card.TutorialObjectId))
                        {
                            _tutorialManager.ReportActivityAction(Enumerators.TutorialActivityAction
                                .PlayerOverlordTriedToPlayUnsequentionalCard);
                            return;
                        }

                        _tutorialManager.DeactivateSelectHandPointer(Enumerators.TutorialObjectOwner.PlayerCardInHand);
                    }
                }

                StartedDrag = true;
                InitialPos = BoardCardView.PositionOnHand;
                InitialRotation = BoardCardView.RotationOnHand;

                Transform.eulerAngles = Vector3.zero;

                _playerController.IsCardSelected = true;
                _alreadySelected = true;

                _isScaledUp = true;
                Transform.DOScale(Constants.DefaultScaleForZoomedCardWhileDragging, Constants.DurationHoveringHandCard);
            }
        }

        public void CheckStatusOfHighlight()
        {
            bool enableHighlight = CardModel.CanBePlayed(OwnerPlayer) && CardModel.CanBeBuyed(OwnerPlayer);
            BoardCardView.SetHighlightingEnabled(enableHighlight);
        }

        public void MouseUp(GameObject obj)
        {
            if (!StartedDrag || _gameplayManager.IsGameEnded)
                return;

            _cardsController.ResetPlayerCardsOnBattlegroundPosition();

            _alreadySelected = false;
            StartedDrag = false;
            _playerController.IsCardSelected = false;

            bool playable = !_canceledPlay &&
                CardModel.CanBeBuyed(OwnerPlayer) &&
                _actionsQueueController.RootQueue.GetChildCount() <= 0 &&
                !_gameplayManager.GetController<CardsController>().BlockPlayFromHand &&
                Enabled &&
                (CardModel.Card.Prototype.Kind != Enumerators.CardKind.CREATURE ||
                    OwnerPlayer.CardsOnBoard.Count < OwnerPlayer.MaxCardsInPlay);

            if (playable)
            {
                Vector3 currentPosition = Transform.position;
                currentPosition.y -= (4.7f*Transform.localScale.x/Constants.DefaultScaleForZoomedCardInHand.x);

                if (BoardZone.GetComponent<BoxCollider2D>().bounds.Contains(currentPosition) && _isHandCard && BoardCardView.Model.CanBePlayed(BoardCardView.Model.Card.Owner))
                {
                    _isHandCard = false;
                    _cardsController.PlayPlayerCard(OwnerPlayer,
                        BoardCardView,
                        this,
                        PlayCardOnBoard =>
                        {
                            if (OwnerPlayer == _gameplayManager.CurrentPlayer)
                            {
                                PlayerMove playerMove = new PlayerMove(Enumerators.PlayerActionType.PlayCardOnBoard, PlayCardOnBoard);
                                _gameplayManager.PlayerMoves.AddPlayerMove(playerMove);
                            }
                        });

                    BoardCardView.SetHighlightingEnabled(false);
                }
                else
                {
                    ReturnToHandAnim();

                    if (_tutorialManager.IsTutorial)
                    {
                        _tutorialManager.ActivateSelectHandPointer(Enumerators.TutorialObjectOwner.PlayerCardInHand);
                    }
                }
            }
            else
            {
                _canceledPlay = false;
                ReturnToHandAnim();

                if (_tutorialManager.IsTutorial)
                {
                    _tutorialManager.ActivateSelectHandPointer(Enumerators.TutorialObjectOwner.PlayerCardInHand);
                }
            }
        }

        private void ReturnToHandAnim()
        {
            IsReturnToHand = true;

            _soundManager.PlaySound(Enumerators.SoundType.CARD_FLY_HAND, Constants.CardsMoveSoundVolume);

            Transform.DOKill();
            _isScaledUp = false;
            Transform.DOScale(BoardCardView.ScaleOnHand, 0.5f);
            Transform.DOMove(InitialPos, 0.5f)
                .OnComplete(
                    () =>
                    {
                        Transform.position = InitialPos;
                        Transform.eulerAngles = InitialRotation;
                        IsReturnToHand = false;

                        _gameplayManager.CanDoDragActions = true;
                    });
        }

        public void ResetToInitialPosition()
        {
            _gameplayManager.CanDoDragActions = true;

            Transform.position = InitialPos;
            Transform.eulerAngles = InitialRotation;
        }

        public void ResetToHandAnimation()
        {
            _gameplayManager.CanDoDragActions = true;

            _canceledPlay = false;
            _alreadySelected = false;
            StartedDrag = false;
            IsReturnToHand = true;
            _isHandCard = true;
            Enabled = true;
            GameObject.GetComponent<SortingGroup>().sortingLayerID = SortingLayer.NameToID("HandCards");
            GameObject.GetComponent<SortingGroup>().sortingOrder = 0;

            _soundManager.PlaySound(Enumerators.SoundType.CARD_FLY_HAND, Constants.CardsMoveSoundVolume);

            Transform.DOKill();
            _isScaledUp = false;
            Transform.DOScale(BoardCardView.ScaleOnHand, 0.5f);
            Transform.DOMove(InitialPos, 0.5f)
                .OnComplete(
                    () =>
                    {
                        Transform.position = InitialPos;
                        IsReturnToHand = false;
                    });
        }

        public void HoveringAndZoom()
        {
            Transform.DOScale(Constants.DefaultScaleForZoomedCardInHand, Constants.DurationHoveringHandCard);
            _isScaledUp = true;
            Transform.DOMove(new Vector3(BoardCardView.PositionOnHand.x, -3.6f, 0), Constants.DurationHoveringHandCard);
            Transform.DORotate(Vector3.zero, 0.15f);
            _normalSortingOrder = _sortingGroup.sortingOrder;
            _sortingGroup.sortingOrder = 100;
        }

        public void ResetHoveringAndZoom(bool isMove = true, Action onComplete = null)
        {
            if (_gameplayManager.IsGameEnded || GameObject == null)
                return;

            _isScaledUp = false;
            Transform.DOMove(BoardCardView.PositionOnHand, Constants.DurationHoveringHandCard);

            Transform.DOScale(BoardCardView.ScaleOnHand, Constants.DurationHoveringHandCard);
            Transform.DORotate(BoardCardView.RotationOnHand, Constants.DurationHoveringHandCard).OnComplete(() =>
            {
                onComplete?.Invoke();
            });

            _sortingGroup.sortingOrder = _normalSortingOrder;
        }
    }
}
