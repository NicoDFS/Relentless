using DG.Tweening;
using GrandDevs.CZB;
using GrandDevs.CZB.Common;
using UnityEngine;

[RequireComponent(typeof(CardView))]
public class HandCard : MonoBehaviour
{
    private IGameplayManager _gameplayManager;
    private PlayerController _playerController;
    private CardsController _cardsController;

    public Player ownerPlayer;
    public GameObject boardZone;

    protected CardView cardView;

    protected bool startedDrag;
    protected Vector3 initialPos;

    private bool _isHandCard = true;

    private bool _isReturnToHand = false;
    private bool _alreadySelected = false;

    private int _handInd;

    private void Awake()
    {
        cardView = GetComponent<CardView>();
        _handInd = this.GetHashCode();


        _gameplayManager = GameClient.Get<IGameplayManager>();
        _playerController = _gameplayManager.GetController<PlayerController>();
        _cardsController = _gameplayManager.GetController<CardsController>();
    }

    private void Start()
    {
        CheckStatusOfHighlight();
    }

    private void Update()
    {
        if (startedDrag)
        {
            transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var newPos = transform.position;
            newPos.z = 0;
            transform.position = newPos;
        }
    }

    public void OnSelected()
    {
        if (_playerController.IsActive &&
            cardView.CanBePlayed(ownerPlayer) && !_isReturnToHand && !_alreadySelected)
        {
            startedDrag = true;
            initialPos = transform.position;
            _playerController.IsCardSelected = true;
            _alreadySelected = true;
        }
    }

    public void CheckStatusOfHighlight()
    {
        if (cardView.CanBePlayed(ownerPlayer) && cardView.CanBeBuyed(ownerPlayer))
        {
            cardView.SetHighlightingEnabled(true);
        }
        else
        {
            cardView.SetHighlightingEnabled(false);
        }
    }

    public void OnMouseUp()
    {
        if (!startedDrag)
        {
            return;
        }
        _alreadySelected = false;
        startedDrag = false;
        _playerController.IsCardSelected = false;

        bool playable = true;
        if (!cardView.CanBeBuyed(ownerPlayer) || (cardView.WorkingCard.libraryCard.cardKind == Enumerators.CardKind.CREATURE &&
                                                     ownerPlayer.BoardCards.Count >= Constants.MAX_BOARD_CREATURES))
            playable = false;

        if (playable)
        {
            if (boardZone.GetComponent<BoxCollider2D>().bounds.Contains(transform.position) && _isHandCard)
            {
                _isHandCard = false;
                _cardsController.PlayCard(ownerPlayer, cardView, this);
                cardView.SetHighlightingEnabled(false);
            }
            else
            {
                transform.position = initialPos;
                if (GameClient.Get<ITutorialManager>().IsTutorial)
                {
                    GameClient.Get<ITutorialManager>().ActivateSelectTarget();
                }
            }
        }
        else
        {
            _isReturnToHand = true;

            GameClient.Get<ISoundManager>().PlaySound(Enumerators.SoundType.CARD_FLY_HAND, Constants.CARDS_MOVE_SOUND_VOLUME, false, false);

            transform.DOMove(initialPos, 0.5f).OnComplete(() => 
            {
                transform.position = initialPos;
                _isReturnToHand = false;
            });
        }
    }

    public void ResetToInitialPosition()
    {
        transform.position = initialPos;
    }

    public void ResetToHandAnimation()
    {
        enabled = true;
        _alreadySelected = false;
        startedDrag = false;
        _isReturnToHand = true;
        _isHandCard = true;

        GameClient.Get<ISoundManager>().PlaySound(Enumerators.SoundType.CARD_FLY_HAND, Constants.CARDS_MOVE_SOUND_VOLUME, false, false);

        transform.DOMove(initialPos, 0.5f).OnComplete(() =>
        {
            transform.position = initialPos;
            _isReturnToHand = false;
        });
    }
}
