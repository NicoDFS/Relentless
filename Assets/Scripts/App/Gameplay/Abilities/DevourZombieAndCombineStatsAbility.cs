using System.Collections.Generic;
using System.Linq;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using UnityEngine;

namespace Loom.ZombieBattleground
{
    public class DevourZombiesAndCombineStatsAbility : AbilityBase
    {
        public int Value;

        private List<BoardUnitModel> _units;

        private bool _isTarget;

        public DevourZombiesAndCombineStatsAbility(Enumerators.CardKind cardKind, AbilityData ability)
            : base(cardKind, ability)
        {
            Value = ability.Value;

            _units = new List<BoardUnitModel>();
        }

        public override void Activate()
        {
            base.Activate();

            if (AbilityCallType != Enumerators.AbilityCallType.ENTRY)
                return;

            VfxObject = LoadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/GreenHealVFX");

            if (Value == -1)
            {
                DevourAllAllyZombies();
            }
        }

        public override void Update()
        {
        }

        public override void Dispose()
        {
        }

        protected override void InputEndedHandler()
        {
            base.InputEndedHandler();

            if (IsAbilityResolved && Value > 0)
            {
                _isTarget = true;

                _units.Add(TargetUnit);
                DevourTargetZombie(TargetUnit);
                InvokeActionTriggered(_units);
            }
        }

        private void DevourAllAllyZombies()
        {
            _isTarget = false;

            if (PredefinedTargets != null)
            {
                _units = PredefinedTargets.Select(x => x.BoardObject).Cast<BoardUnitModel>().ToList();
            }
            else
            {
                _units = PlayerCallerOfAbility.BoardCards.Select(x => x.Model).ToList();
            }

            foreach (BoardUnitModel unit in _units)
            {
                DevourTargetZombie(unit);
            }
            InvokeActionTriggered(_units);
        }

        protected override void VFXAnimationEndedHandler()
        {
            base.VFXAnimationEndedHandler();

            foreach (BoardUnitModel unit in _units)
            {
                if (unit == AbilityUnitOwner)
                    continue;

                BattlegroundController.DestroyBoardUnit(unit, false, true);
            }

            BoardController.UpdateCurrentBoardOfPlayer(PlayerCallerOfAbility, null);

            List<BoardObject> targets = _units.Cast<BoardObject>().ToList();

            AbilitiesController.ThrowUseAbilityEvent(MainWorkingCard, targets, AbilityData.AbilityType, Enumerators.AffectObjectType.Character);
        }

        private void DevourTargetZombie(BoardUnitModel unit)
        {
            if (unit == AbilityUnitOwner)
                return;

            int health = unit.InitialHp;
            int damage = unit.InitialDamage;

            AbilityUnitOwner.BuffedHp += health;
            AbilityUnitOwner.CurrentHp += health;

            AbilityUnitOwner.BuffedDamage += damage;
            AbilityUnitOwner.CurrentDamage += damage;
        }
    }
}
