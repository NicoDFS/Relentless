using DG.Tweening;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Loom.ZombieBattleground
{
    public class TutorialDescriptionTooltipItem
    {
        private readonly ITutorialManager _tutorialManager;
        private readonly ILoadObjectsManager _loadObjectsManager;
        private readonly IGameplayManager _gameplayManager;

        private const float KoefSize = 0.88f;

        private const float AdditionalInterval = 0.5f;

        private const float MinIntervalFromDifferentAlign = 1f;

        private static Vector2 DefaultTextSize = new Vector3(3.2f, 1.4f);

        private GameObject _selfObject;

        private GameObject _currentBackground;

        private TextMeshPro _textDescription;

        public int Id;

        public bool IsActiveInThisClick;

        public bool NotDestroyed => _selfObject != null;

        public Enumerators.TooltipAlign Align => _align;

        public float Width;

        public Enumerators.TutorialObjectOwner OwnerType;

        private Enumerators.TooltipAlign _align;

        private Vector3 _currentPosition;

        private BoardUnitView _ownerUnit;

        private bool _dynamicPosition;

        private Enumerators.TutorialObjectLayer _layer = Enumerators.TutorialObjectLayer.Default;

        private bool _isDrawing;

        private int _ownerId;

        private bool _canBeClosed = false;

        private float _minimumShowTime;

        private Sequence _showingSequence;

        public TutorialDescriptionTooltipItem(int id,
                                                string description,
                                                Enumerators.TooltipAlign align,
                                                Enumerators.TutorialObjectOwner owner,
                                                Vector3 position,
                                                bool resizable,
                                                bool dynamicPosition,
                                                int ownerId = 0,
                                                Enumerators.TutorialObjectLayer layer = Enumerators.TutorialObjectLayer.Default,
                                                BoardObject boardObjectOwner = null,
                                                float minimumShowTime = Constants.DescriptionTooltipMinimumShowTime)
        {
            _tutorialManager = GameClient.Get<ITutorialManager>();
            _loadObjectsManager = GameClient.Get<ILoadObjectsManager>();
            _gameplayManager = GameClient.Get<IGameplayManager>();

            this.Id = id;
            OwnerType = owner;
            _ownerId = ownerId;
            _align = align;
            _dynamicPosition = dynamicPosition;
            _currentPosition = position;
            _layer = layer;
            _minimumShowTime = minimumShowTime;

            _selfObject = MonoBehaviour.Instantiate(
                _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/Gameplay/Tutorials/TutorialDescriptionTooltip"));

            _textDescription = _selfObject.transform.Find("Text").GetComponent<TextMeshPro>();


            description = description.Replace("\n", " ");

            _textDescription.text = description;

            SetBackgroundType(align);
            if (resizable && _currentBackground != null)
            {
                _textDescription.ForceMeshUpdate();                
                RectTransform rect = _textDescription.GetComponent<RectTransform>();
                Vector2 defaultSize = rect.sizeDelta;
                float koef = 1;
                while (rect.sizeDelta.y < _textDescription.renderedHeight)
                {
                    rect.sizeDelta = defaultSize * koef;
                    koef += Time.deltaTime;
                    _textDescription.ForceMeshUpdate();
                }
                Vector2 backgroundSize = Vector2.one / DefaultTextSize * rect.sizeDelta;
                float value = (backgroundSize.x > backgroundSize.y ? backgroundSize.x : backgroundSize.y);
                _currentBackground.transform.localScale = Vector3.one * value;
            }
            UpdateTextPosition();

            if (ownerId > 0)
            {
                switch (OwnerType)
                {
                    case Enumerators.TutorialObjectOwner.PlayerBattleframe:
                        _ownerUnit = _gameplayManager.CurrentPlayer.BoardCards.First((x) =>
                            x.TutorialObjectId == ownerId);
                        break;
                    case Enumerators.TutorialObjectOwner.EnemyBattleframe:
                        _ownerUnit = _gameplayManager.OpponentPlayer.BoardCards.First((x) =>
                            x.TutorialObjectId == ownerId);
                        break;
                    default: break;
                }
            }
            else if(boardObjectOwner != null)
            {
                switch(OwnerType)
                {
                    case Enumerators.TutorialObjectOwner.Battleframe:
                    case Enumerators.TutorialObjectOwner.EnemyBattleframe:
                    case Enumerators.TutorialObjectOwner.PlayerBattleframe:
                        _ownerUnit = _gameplayManager.GetController<BattlegroundController>().GetBoardUnitViewByModel(boardObjectOwner as BoardUnitModel);
                        break;
                    case Enumerators.TutorialObjectOwner.HandCard:
                        break;

                }
            }

            SetPosition();

            UpdatePosition();

            _isDrawing = true;

            StartShowTimer();
        }

        public void UpdatePosition()
        {
            if(_dynamicPosition)
            {
                TutorialDescriptionTooltipItem tooltip;
                float distance = 0;

                foreach (int index in _tutorialManager.CurrentTutorialStep.TutorialDescriptionTooltipsToActivate)
                {
                    if (index != Id)
                    {
                        tooltip = _tutorialManager.GetDescriptionTooltip(index);

                        if (tooltip == null)
                            continue;

                        distance = Mathf.Abs(_selfObject.transform.position.x - tooltip._selfObject.transform.position.x);

                        if (tooltip.Align == Enumerators.TooltipAlign.CenterLeft ||
                            tooltip.Align == Enumerators.TooltipAlign.CenterRight)
                        {

                            if ((_align == tooltip.Align && distance < Width + AdditionalInterval) ||
                                (_align != tooltip.Align && distance < MinIntervalFromDifferentAlign))
                            {
                                _align = _align == Enumerators.TooltipAlign.CenterLeft ? Enumerators.TooltipAlign.CenterRight : Enumerators.TooltipAlign.CenterLeft;
                                SetBackgroundType(_align);
                                _currentPosition.x *= -1f;
                                SetPosition();
                                UpdateTextPosition();
                                Helpers.InternalTools.DoActionDelayed(tooltip.UpdatePosition, Time.deltaTime);
                            }
                        }
                        else
                        {
                            if(_selfObject.transform.position.x > tooltip._selfObject.transform.position.x)
                            {
                                _align = Enumerators.TooltipAlign.CenterLeft;
                                _currentPosition.x = Mathf.Abs(_currentPosition.x);
                            }
                            else
                            {
                                _align = Enumerators.TooltipAlign.CenterRight;
                                _currentPosition.x = -Mathf.Abs(_currentPosition.x);
                            }
                            SetBackgroundType(_align);
                            SetPosition();
                            UpdateTextPosition();
                        }
                    }
                }
            }

            switch (_layer)
            {
                case Enumerators.TutorialObjectLayer.Default:
                    _textDescription.renderer.sortingLayerName = SRSortingLayers.GameUI2;
                    UpdateBackgroundLayers(SRSortingLayers.GameUI2, 1);
                    _textDescription.renderer.sortingOrder = 2;
                    break;
                case Enumerators.TutorialObjectLayer.AboveUI:
                    _textDescription.renderer.sortingLayerName = SRSortingLayers.GameUI3;
                    UpdateBackgroundLayers(SRSortingLayers.GameUI3, 1);
                    _textDescription.renderer.sortingOrder = 2;
                    break;
                default:
                    _textDescription.renderer.sortingLayerName = SRSortingLayers.GameUI2;
                    UpdateBackgroundLayers(SRSortingLayers.GameUI2, 0);
                    _textDescription.renderer.sortingOrder = 1;
                    break;
            }
        }

        private void UpdateBackgroundLayers(string name, int order)
        {
            foreach (SpriteRenderer child in _currentBackground.GetComponentsInChildren<SpriteRenderer>())
            {
                child.sortingLayerName = name;
                child.sortingOrder = order;
            }
        }

        public void Show(Vector3? position = null)
        {
            _selfObject?.SetActive(true);
            IsActiveInThisClick = true;
            if (position != null)
            {
                _currentPosition = (Vector3)position;
                SetPosition();
            }
            _isDrawing = true;
            UpdatePossibilityForClose();
        }

        public void Hide()
        {
            if (!_isDrawing || !_canBeClosed)
                return;

            _tutorialManager.ReportActivityAction(Enumerators.TutorialActivityAction.DescriptionTooltipClosed);

            _selfObject?.SetActive(false);

            _isDrawing = false;

            if(_showingSequence != null)
            {
                _showingSequence.Kill();
                _showingSequence = null;
            }
        }

        public void Dispose()
        {
            _isDrawing = false;
            if (_selfObject != null)
            {
                MonoBehaviour.Destroy(_selfObject);
            }
        }

        public void Update()
        {
            if (_isDrawing)
            {
                switch (OwnerType)
                {
                    case Enumerators.TutorialObjectOwner.Battleframe:
                    case Enumerators.TutorialObjectOwner.EnemyBattleframe:
                    case Enumerators.TutorialObjectOwner.PlayerBattleframe:
                        if (_ownerUnit != null && !_ownerUnit.Model.IsDead && _ownerUnit.GameObject != null && _ownerUnit.GameObject)
                        {
                            _selfObject.transform.position = _ownerUnit.Transform.TransformPoint(_currentPosition);
                        }
                        else if(_ownerId != 0)
                        {
                            UpdatePossibilityForClose();
                            Hide();
                        }
                        break;
                    case Enumerators.TutorialObjectOwner.HandCard:
                        break;
                }
            }
        }

        private void StartShowTimer()
        {
            _canBeClosed = false;
            if (_minimumShowTime > 0f)
            {
                _showingSequence = InternalTools.DoActionDelayed(UpdatePossibilityForClose, _minimumShowTime);
            }
            else
            {
                UpdatePossibilityForClose();
            }
        }

        private void UpdatePossibilityForClose()
        {
            _canBeClosed = true;
        }

        private void UpdateTextPosition()
        {
            Vector3 centerOfChilds = Vector3.zero;
            SpriteRenderer[] childs = _currentBackground.GetComponentsInChildren<SpriteRenderer>();
            Width = childs[0].bounds.size.x * 4;

            foreach (SpriteRenderer child in childs)
            {
                centerOfChilds += child.transform.position;
            }
            centerOfChilds /= childs.Length;
            Vector3 textPosition = centerOfChilds;

            switch (_align)
            {
                case Enumerators.TooltipAlign.CenterRight:
                case Enumerators.TooltipAlign.CenterLeft:
                    textPosition.x *= 1.03f;
                    break;
                default:
                    break;
            }
            _textDescription.transform.position = textPosition;
        }

        private void SetPosition()
        {
            if (_ownerUnit != null)
            {
                _selfObject.transform.position = _ownerUnit.Transform.TransformPoint(_currentPosition);
            }
            else
            {
                _selfObject.transform.position = _currentPosition;
            }           
        }

        private void SetBackgroundType(Enumerators.TooltipAlign align)
        {
            Vector3 size = Vector3.one;
            if(_currentBackground != null)
            {
                _currentBackground.gameObject.SetActive(false);
                size = _currentBackground.transform.localScale;
            }

            switch (align)
            {
                case Enumerators.TooltipAlign.CenterLeft:
                case Enumerators.TooltipAlign.CenterRight:
                case Enumerators.TooltipAlign.TopMiddle:
                case Enumerators.TooltipAlign.BottomMiddle:
                    _currentBackground = _selfObject.transform.Find("ArrowType/Arrow_" + align.ToString()).gameObject;
                    _currentBackground.gameObject.SetActive(true);
                    break;
                default:
                    throw new NotImplementedException(nameof(align) + " doesn't implemented");
            }

            _currentBackground.transform.localScale = size;
        }
    }
}
