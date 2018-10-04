using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using System.Collections.Generic;
using UnityEngine;

namespace Loom.ZombieBattleground
{
    public class SwingAbility : AbilityBase
    {
        public int Value { get; }

        public SwingAbility(Enumerators.CardKind cardKind, AbilityData ability)
            : base(cardKind, ability)
        {
            Value = ability.Value;
        }

        public override void Activate()
        {
            base.Activate();

            VfxObject = LoadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/PushVFX");
        }

        public override void Action(object info = null)
        {
            base.Action(info);

            BoardUnitModel unit = (BoardUnitModel) info;

            int targetIndex = -1;
            for (int i = 0; i < unit.OwnerPlayer.BoardCards.Count; i++)
            {
                if (unit.OwnerPlayer.BoardCards[i].Model == unit)
                {
                    targetIndex = i;
                    break;
                }
            }

            if (targetIndex > -1)
            {
                if (targetIndex - 1 > -1)
                {
                    TakeDamageToUnit(unit.OwnerPlayer.BoardCards[targetIndex - 1]);
                }

                if (targetIndex + 1 < unit.OwnerPlayer.BoardCards.Count)
                {
                    TakeDamageToUnit(unit.OwnerPlayer.BoardCards[targetIndex + 1]);
                }
            }

            AbilitiesController.ThrowUseAbilityEvent(MainWorkingCard, new List<BoardObject>()
            {
              unit.OwnerPlayer.BoardCards[targetIndex - 1].Model,
              unit.OwnerPlayer.BoardCards[targetIndex + 1].Model,
            }, AbilityData.AbilityType, Protobuf.AffectObjectType.Character);
        }

        protected override void UnitAttackedHandler(BoardObject info, int damage, bool isAttacker)
        {
            base.UnitAttackedHandler(info, damage, isAttacker);

            if (AbilityCallType != Enumerators.AbilityCallType.ATTACK || !isAttacker)
                return;

            if (info is BoardUnitModel)
            {
                Action(info);
            }
        }

        private void TakeDamageToUnit(BoardUnitView unit)
        {
            BattleController.AttackUnitByAbility(AbilityUnitOwner, AbilityData, unit.Model);
            CreateVfx(unit.Transform.position, true, 5f);
        }
    }
}
