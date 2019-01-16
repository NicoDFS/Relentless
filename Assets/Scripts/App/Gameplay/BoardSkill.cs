using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using Loom.ZombieBattleground.Gameplay;
using System;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Loom.ZombieBattleground
{
    public class BoardSkill : OwnableBoardObject, ISkillIdOwner
    {
        public event Action<BoardSkill, BoardObject> SkillUsed;

        public BattleBoardArrow FightTargetingArrow;

        public GameObject SelfObject;

        public HeroSkill Skill;

        private readonly ILoadObjectsManager _loadObjectsManager;

        private readonly IGameplayManager _gameplayManager;

        private readonly ITutorialManager _tutorialManager;

        private readonly PlayerController _playerController;

        private readonly SkillsController _skillsController;

        private readonly BoardArrowController _boardArrowController;

        private readonly GameObject _glowObject;

        private readonly GameObject _fightTargetingArrowPrefab;

        private int _initialCooldown;

        private readonly Animator _shutterAnimator;

        private readonly PointerEventSolver _pointerEventSolver;

        private int _cooldown;

        private bool _usedInThisTurn;

        private bool _isOpen;

        private bool _isAlreadyUsed;

        private bool _singleUse;

        private OnBehaviourHandler _behaviourHandler;

        private OverlordAbilityInfoObject _currentOverlordAbilityInfoObject;

        private SkillCoolDownTimer _coolDownTimer;

        public SkillId SkillId { get; }

        public BoardSkill(GameObject obj, Player player, HeroSkill skillInfo, bool isPrimary)
        {
            SelfObject = obj;
            Skill = skillInfo;
            OwnerPlayer = player;
            IsPrimary = isPrimary;

            _initialCooldown = skillInfo.InitialCooldown;
            _cooldown = skillInfo.Cooldown;
            _singleUse = skillInfo.SingleUse;

            _coolDownTimer = new SkillCoolDownTimer(SelfObject, _cooldown);

            _loadObjectsManager = GameClient.Get<ILoadObjectsManager>();
            _gameplayManager = GameClient.Get<IGameplayManager>();
            _tutorialManager = GameClient.Get<ITutorialManager>();

            _playerController = _gameplayManager.GetController<PlayerController>();
            _skillsController = _gameplayManager.GetController<SkillsController>();
            _boardArrowController = _gameplayManager.GetController<BoardArrowController>();

            _glowObject = SelfObject.transform.Find("OverlordAbilitySelection").gameObject;
            _glowObject.SetActive(false);

            string name = isPrimary ? Constants.OverlordRegularNeckR : Constants.OverlordRegularNeckL;

            _shutterAnimator = SelfObject.transform.parent.transform
                .Find("OverlordArea/RegularModel/RegularPosition/OverlordRegular/Shutters/" + name).GetComponent<Animator>();

            SkillId = new SkillId(isPrimary ? 0 : 1);

            OwnerPlayer.TurnStarted += TurnStartedHandler;
            OwnerPlayer.TurnEnded += TurnEndedHandler;

            _behaviourHandler = SelfObject.GetComponent<OnBehaviourHandler>();
            {
                _pointerEventSolver = new PointerEventSolver();
                _pointerEventSolver.DragStarted += PointerSolverDragStartedHandler;
                _pointerEventSolver.Clicked += PointerEventSolverClickedHandler;
                _pointerEventSolver.Ended += PointerEventSolverEndedHandler;
            }

            _coolDownTimer.SetAngle(_cooldown);

            _fightTargetingArrowPrefab =
                _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/Gameplay/Arrow/AttackArrowVFX_Object");

            _isOpen = false;
        }

        public bool IsSkillReady => _cooldown == 0 && (!_singleUse || !_isAlreadyUsed);

        public bool IsUsing { get; private set; }

        public bool IsPrimary { get; }

        public void CancelTargetingArrows()
        {
            if (FightTargetingArrow != null)
            {
                FightTargetingArrow.Dispose();
            }

            FightTargetingArrow = null;
        }

        public void BlockSkill()
        {
            _usedInThisTurn = true;
            SetHighlightingEnabled(false);
        }

        public void UnBlockSkill()
        {
            _usedInThisTurn = false;
        }

        public void SetCoolDown(int coolDownValue)
        {
            if (_isAlreadyUsed && _singleUse)
                return;

            _cooldown = coolDownValue;
            _coolDownTimer.SetAngle(_cooldown);

            SetHighlightingEnabled(IsSkillReady);
            _usedInThisTurn = false;
        }

        public void StartDoSkill(bool localPlayerOverride = false)
        {
            if (!IsSkillCanUsed())
                return;

            if (OwnerPlayer.IsLocalPlayer && !localPlayerOverride)
            {
                if (Skill.CanSelectTarget)
                {
                    FightTargetingArrow =
                        Object.Instantiate(_fightTargetingArrowPrefab).AddComponent<BattleBoardArrow>();
                    FightTargetingArrow.BoardCards = _gameplayManager.CurrentPlayer == OwnerPlayer ?
                        _gameplayManager.OpponentPlayer.BoardCards :
                        _gameplayManager.CurrentPlayer.BoardCards;
                    FightTargetingArrow.TargetsType = Skill.SkillTargetTypes;
                    FightTargetingArrow.ElementType = Skill.ElementTargetTypes;
                    FightTargetingArrow.TargetUnitStatusType = Skill.TargetUnitStatusType;
                    FightTargetingArrow.IgnoreHeavy = true;

                    FightTargetingArrow.Begin(SelfObject.transform.position);

                    if (_tutorialManager.IsTutorial)
                    {
                        _tutorialManager.DeactivateSelectTarget();
                    }
                }
            }

            IsUsing = true;
        }

        public void EndDoSkill()
        {
            if (!IsSkillCanUsed() || !IsUsing)
                return;

            _gameplayManager.GetController<ActionsQueueController>().AddNewActionInToQueue(
                 (parameter, completeCallback) =>
                 {
                     DoOnUpSkillAction(completeCallback);
                     IsUsing = false;
                 }, Enumerators.QueueActionType.OverlordSkillUsage);
        }

        public void UseSkill(BoardObject target)
        {
            SetHighlightingEnabled(false);
            _cooldown = _initialCooldown;
            _usedInThisTurn = true;
            _coolDownTimer.SetAngle(_cooldown, true);
            _isAlreadyUsed = true;
            GameClient.Get<IOverlordExperienceManager>().ReportExperienceAction(OwnerPlayer.SelfHero, Common.Enumerators.ExperienceActionType.UseOverlordAbility);

            _tutorialManager.ReportAction(Enumerators.TutorialReportAction.USE_ABILITY);

            SkillUsed?.Invoke(this, target);

            if (_gameplayManager.UseInifiniteAbility)
            {
                _usedInThisTurn = false;
                SetCoolDown(0);
            }

            if(_singleUse)
            {
                _coolDownTimer.Close();
            }
        }

        public void Hide()
        {
            SelfObject.SetActive(false);
        }

        public void Update()
        {
            if (!_gameplayManager.IsGameplayReady())
                return;
            {
                _pointerEventSolver.Update();

                if (Input.GetMouseButtonDown(0))
                {
                    if (_currentOverlordAbilityInfoObject != null)
                    {
                        GameClient.Get<ICameraManager>().FadeOut(level: 1);

                        _currentOverlordAbilityInfoObject.Dispose();
                        _currentOverlordAbilityInfoObject = null;
                    }
                }
            }
        }

        public void OnMouseDownEventHandler()
        {
            if (_boardArrowController.IsBoardArrowNowInTheBattle || !_gameplayManager.CanDoDragActions || _gameplayManager.IsGameplayInputBlocked)
                return;

            if (!_gameplayManager.IsGameplayReady())
                return;

            _pointerEventSolver.PushPointer();
        }

        public void OnMouseUpEventHandler()
        {
            if (!_gameplayManager.IsGameplayReady())
                return;

            _pointerEventSolver.PopPointer();
        }

        private void PointerSolverDragStartedHandler()
        {
            if (Skill.CanSelectTarget)
            {
                if (OwnerPlayer.IsLocalPlayer)
                {
                    StartDoSkill();
                }
            }
            else
            {
                DrawAbilityTooltip();
            }
        }

        private void PointerEventSolverClickedHandler()
        {
            if (Skill.CanSelectTarget)
            {
                DrawAbilityTooltip();
            }
            else
            {
                if ((IsSkillReady && !_usedInThisTurn) && OwnerPlayer.IsLocalPlayer)
                {
                    StartDoSkill();
                }
                else
                {
                    DrawAbilityTooltip();
                }
            }
        }

        private void PointerEventSolverEndedHandler()
        {
            if (OwnerPlayer.IsLocalPlayer)
            {
                EndDoSkill();
            }
        }

        private void TurnStartedHandler()
        {
            if (_gameplayManager.CurrentTurnPlayer != OwnerPlayer)
                return;

            if (OwnerPlayer.IsStunned)
            {
                BlockSkill();
            }
            else
            {
                if (IsSkillReady)
                {
                    SetHighlightingEnabled(true);
                }
            }

            if (!_singleUse || !_isAlreadyUsed)
            {
                _coolDownTimer.SetAngle(_cooldown);
            }
        }

        private void TurnEndedHandler()
        {
            if (_gameplayManager.CurrentTurnPlayer != OwnerPlayer)
                return;

            SetHighlightingEnabled(false);
            if (Constants.DevModeEnabled)
            {
                _cooldown = 0;
            }
            if (!_usedInThisTurn)
            {
                _cooldown = Mathf.Clamp(_cooldown - 1, 0, _initialCooldown);
            }

            _usedInThisTurn = false;

            // rewrite
            CancelTargetingArrows();
        }

        private void SetHighlightingEnabled(bool isActive)
        {
            _glowObject.SetActive(isActive);

            if (_isOpen != isActive)
            {
                _isOpen = isActive;
                _shutterAnimator.SetTrigger((isActive ? Enumerators.ShutterState.Open : Enumerators.ShutterState.Close).ToString());
            }
        }

        private void DoOnUpSkillAction(Action completeCallback)
        {
            if (OwnerPlayer.IsLocalPlayer && _tutorialManager.IsTutorial)
            {
                _tutorialManager.ActivateSelectTarget();
            }

            if (!Skill.CanSelectTarget)
            {
                _skillsController.DoSkillAction(this, completeCallback, OwnerPlayer);
            }
            else
            {
                if (OwnerPlayer.IsLocalPlayer)
                {
                    if (FightTargetingArrow != null)
                    {
                        _skillsController.DoSkillAction(this, completeCallback);
                        _playerController.IsCardSelected = false;
                    }
                    else
                    {
                        completeCallback?.Invoke();
                    }
                }
                else
                {
                    _skillsController.DoSkillAction(this, completeCallback);
                }
            }
        }

        private bool IsSkillCanUsed()
        {
            if (_tutorialManager.IsTutorial && _tutorialManager.CurrentTutorialDataStep.CanUseBoardSkill)
            {
                return true;
            }

            if (!IsSkillReady || _gameplayManager.CurrentTurnPlayer != OwnerPlayer || _usedInThisTurn ||
                _tutorialManager.IsTutorial)
            {
                return false;
            }

            return true;
        }

        private void DrawAbilityTooltip()
        {
            if (_gameplayManager.IsTutorial)
                return;

            if (_currentOverlordAbilityInfoObject != null)
                return;

            GameClient.Get<ICameraManager>().FadeIn(0.8f, 1);

            Vector3 position;

            if (OwnerPlayer.IsLocalPlayer)
            {
                if (IsPrimary)
                {
                    position = new Vector3(4f, 0.5f, 0);
                }
                else
                {
                    position = new Vector3(-4f, 0.5f, 0);
                }
            }
            else
            {
                if (IsPrimary)
                {
                    position = new Vector3(4f, -1.15f, 0);
                }
                else
                {
                    position = new Vector3(-4f, -1.15f, 0);
                }
            }

            _currentOverlordAbilityInfoObject = new OverlordAbilityInfoObject(Skill, SelfObject.transform, position);
        }

        public class OverlordAbilityInfoObject
        {
            private readonly ILoadObjectsManager _loadObjectsManager;

            private readonly GameObject _selfObject;

            private readonly SpriteRenderer _buffIconPicture;

            private readonly TextMeshPro _callTypeText;

            private readonly TextMeshPro _descriptionText;

            public OverlordAbilityInfoObject(HeroSkill skill, Transform parent, Vector3 position)
            {
                _loadObjectsManager = GameClient.Get<ILoadObjectsManager>();

                _selfObject =
                    Object.Instantiate(
                        _loadObjectsManager.GetObjectByPath<GameObject>(
                            "Prefabs/Gameplay/Tooltips/Tooltip_OverlordAbilityInfo"), parent, false);

                Transform.localPosition = position;

                _callTypeText = _selfObject.transform.Find("Text_Title").GetComponent<TextMeshPro>();
                _descriptionText = _selfObject.transform.Find("Text_Description").GetComponent<TextMeshPro>();

                _buffIconPicture = _selfObject.transform.Find("Image_IconBackground/Image_Icon")
                    .GetComponent<SpriteRenderer>();

                _callTypeText.text = skill.Title.ToUpperInvariant();
                _descriptionText.text = "    " + skill.Description;

                _buffIconPicture.sprite =
                    _loadObjectsManager.GetObjectByPath<Sprite>("Images/UI/Icons/" +
                        skill.IconPath.Replace(" ", string.Empty));
            }

            public Transform Transform => _selfObject.transform;

            public void Dispose()
            {
                Object.Destroy(_selfObject);
            }
        }
    }
}
