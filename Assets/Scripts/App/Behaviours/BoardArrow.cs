using System.Collections.Generic;
using Loom.ZombieBattleground;
using Loom.ZombieBattleground.Common;
using UnityEngine;

public class BoardArrow : MonoBehaviour
{
    public List<Enumerators.SkillTargetType> TargetsType = new List<Enumerators.SkillTargetType>();

    public List<Enumerators.SetType> ElementType = new List<Enumerators.SetType>();

    protected IGameplayManager GameplayManager;

    protected BoardArrowController BoardArrowController;

    protected InputController InputController;

    protected GameObject TargetObjectsGroup, RootObjectsGroup, ArrowObject, TargetColliderObject;

    protected bool StartedDrag;

    protected Vector3 FromPosition, TargetPosition;

    private readonly float _defaultArrowScale = 6.25f;

    private GameObject _selfObject;

    private bool _isInverse = true;

    public BoardUnit SelectedCard { get; set; }

    public Player SelectedPlayer { get; set; }

    public bool IsDragging()
    {
        if (StartedDrag)
        {
            if (Vector3.Distance(FromPosition, TargetPosition) > Constants.PointerMinDragDelta) return true;
        }

        return false;
    }

    public void SetInverse(bool isInverse = true)
    {
        int scaleX = 1;

        if (isInverse) scaleX = -1;

        _selfObject.transform.localScale = new Vector3(scaleX, 1, 1);
    }

    public void Begin(Vector2 from, bool isInverse = true)
    {
        _isInverse = isInverse;

        StartedDrag = true;
        FromPosition = from;

        ArrowObject.transform.position = FromPosition;

        SetInverse(isInverse);
    }

    public void UpdateLength(Vector3 target, bool isInverse = true)
    {
        TargetColliderObject.transform.position = target;
        TargetObjectsGroup.transform.position = target;

        TargetPosition = target;

        float angle = Mathf.Atan2(target.y - FromPosition.y, target.x - FromPosition.x) * Mathf.Rad2Deg - 90.5f;
        float scaleX = 1f;

        if (!isInverse) scaleX = -1f;

        ArrowObject.transform.eulerAngles = new Vector3(0, 180, -angle);

        float scaleY = Vector3.Distance(FromPosition, target) / _defaultArrowScale;
        ArrowObject.transform.localScale = new Vector3(scaleX, scaleY, ArrowObject.transform.localScale.z);
    }

    public virtual void OnCardSelected(BoardUnit creature)
    {
    }

    public virtual void OnCardUnselected(BoardUnit creature)
    {
    }

    public virtual void OnPlayerSelected(Player player)
    {
    }

    public virtual void OnPlayerUnselected(Player player)
    {
    }

    public virtual void Dispose()
    {
        InputController.PlayerSelectingEvent -= PlayerSelectingEventHandler;
        InputController.UnitSelectingEvent -= UnitSelectingEventHandler;
        InputController.NoObjectsSelectedEvent -= NoObjectsSelectedEventHandler;

        ResetSelecting();

        Destroy(_selfObject);
    }

    protected void Init()
    {
        GameplayManager = GameClient.Get<IGameplayManager>();
        BoardArrowController = GameplayManager.GetController<BoardArrowController>();
        InputController = GameplayManager.GetController<InputController>();

        _selfObject = gameObject;

        TargetObjectsGroup = _selfObject.transform.Find("Group_TargetObjects").gameObject;
        RootObjectsGroup = _selfObject.transform.Find("Arrow/Group_RootObjects").gameObject;
        ArrowObject = _selfObject.transform.Find("Arrow").gameObject;
        TargetColliderObject = _selfObject.transform.Find("Target_Collider").gameObject;

        if (_isInverse) _selfObject.transform.localScale = new Vector3(-1, 1, 1);

        InputController.PlayerSelectingEvent += PlayerSelectingEventHandler;
        InputController.UnitSelectingEvent += UnitSelectingEventHandler;
        InputController.NoObjectsSelectedEvent += NoObjectsSelectedEventHandler;
    }

    protected virtual void Update()
    {
        if (StartedDrag)
        {
            BoardArrowController.CurrentBoardArrow = this;
            BoardArrowController.SetStatusOfBoardArrowOnBoard(true);

            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePosition.z = 0;
            UpdateLength(mousePosition, _isInverse);
        }
    }

    protected virtual void OnDestroy()
    {
        GameClient.Get<ITimerManager>().AddTimer(
            x =>
            {
                BoardArrowController.CurrentBoardArrow = null;
                BoardArrowController.SetStatusOfBoardArrowOnBoard(false);
            },
            null,
            0.25f);
    }

    private void Awake()
    {
        Init();
    }

    private void ResetSelecting()
    {
        if (SelectedCard != null)
        {
            if (SelectedCard.GameObject != null)
            {
                SelectedCard.SetSelectedUnit(false);
                OnCardUnselected(SelectedCard);
            }

            SelectedCard = null;
        }

        if (SelectedPlayer != null)
        {
            if (SelectedPlayer.AvatarObject != null)
            {
                SelectedPlayer.SetGlowStatus(false);
                OnPlayerUnselected(SelectedPlayer);
            }

            SelectedPlayer = null;
        }
    }

    private void PlayerSelectingEventHandler(Player player)
    {
        OnPlayerSelected(player);
    }

    private void UnitSelectingEventHandler(BoardUnit unit)
    {
        OnCardSelected(unit);
    }

    private void NoObjectsSelectedEventHandler()
    {
        ResetSelecting();
    }
}
