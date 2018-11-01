using DG.Tweening;
using Loom.ZombieBattleground.Common;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Loom.ZombieBattleground
{
    public class FreezeNumberOfRandomAllyAbilityView : AbilityViewBase<FreezeNumberOfRandomAllyAbility>
    {
        private BattlegroundController _battlegroundController;

        private List<BoardObject> _allies;

        public FreezeNumberOfRandomAllyAbilityView(FreezeNumberOfRandomAllyAbility ability) : base(ability)
        {
            _battlegroundController = GameClient.Get<IGameplayManager>().GetController<BattlegroundController>();
            _allies = new List<BoardObject>();
        }

        protected override void OnAbilityAction(object info = null)
        {
            if (info != null)
            {
                _allies = (List<BoardObject>)info;
            }
            ActionCompleted();
        }

        private void ActionCompleted()
        {
            ClearParticles();

            if (Ability.AbilityData.HasVisualEffectType(Enumerators.VisualEffectType.Impact))
            {
                Vector3 targetPosition = Vector3.zero;

                VfxObject = LoadObjectsManager.GetObjectByPath<GameObject>(Ability.AbilityData.GetVisualEffectByType(Enumerators.VisualEffectType.Impact).Path);

                for (int i = 0; i < _allies.Count; i++)
                {
                    object ally = _allies[i];
                    switch (ally)
                    {
                        case Player player:
                            targetPosition = Utilites.CastVfxPosition(player.AvatarObject.transform.position);
                            CreateVfx(targetPosition, true, 5f, true);
                            break;
                        case BoardUnitModel unit:
                            targetPosition = Utilites.CastVfxPosition(_battlegroundController.GetBoardUnitViewByModel(unit).Transform.position);
                            CreateVfx(targetPosition, true, 5f);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(ally), ally, null);
                    }
                }
            }

            Ability.InvokeVFXAnimationEnded();
        }


        protected override void CreateVfx(Vector3 pos, bool autoDestroy = false, float duration = 3, bool justPosition = false)
        {
            base.CreateVfx(pos, true, 5f);
        }
    }
}
