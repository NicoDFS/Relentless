using System;
using UnityEngine;

namespace Loom.ZombieBattleground
{
    public class OpponentBoardArrow : BattleBoardArrow
    {
        private object _target;

        public override void SetTarget(object target)
        {
            _target = target;

            switch (_target)
            {
                case Player player:
                    TargetPosition = player.AvatarObject.transform.position;
                    player.SetGlowStatus(true);
                    break;
                case BoardUnitModel unit:
                    BoardUnitView unitView = BattlegroundController.GetBoardUnitViewByModel(unit);
                    TargetPosition = unitView.Transform.position;
                    unitView.SetSelectedUnit(true);
                    break;
                case BoardUnitView view:
                    TargetPosition = view.Transform.position;
                    view.SetSelectedUnit(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(target), target, null);
            }

            TargetPosition.z = 0;

            UpdateLength(TargetPosition, false);
        }

        public override void Dispose()
        {
            if (_target != null)
            {
                switch (_target)
                {
                    case Player player:
                        player.SetGlowStatus(false);
                        break;
                    case BoardUnitModel unit:
                        BoardUnitView unitView = BattlegroundController.GetBoardUnitViewByModel(unit);
                        unitView.SetSelectedUnit(false);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(_target), _target, null);
                }

                _target = null;
            }

            base.Dispose();
        }

        protected override void Update()
        {
            UpdateLength(TargetPosition, false);
        }

        private void Awake()
        {
            Init();
            SetInverse();
        }
    }
}
