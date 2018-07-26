﻿// Copyright (c) 2018 - Loom Network. All rights reserved.
// https://loomx.io/


using LoomNetwork.CZB.Common;
using LoomNetwork.CZB.Data;

namespace LoomNetwork.CZB
{
    public class UseAllGooToIncreaseStatsAbility : AbilityBase
    {
        public int value;

        public UseAllGooToIncreaseStatsAbility(Enumerators.CardKind cardKind, AbilityData ability) : base(cardKind, ability)
        {
            value = ability.value;
        }

        public override void Activate()
        {
            base.Activate();

            if (abilityCallType != Enumerators.AbilityCallType.AT_START)
                return;

            Action();
        }

        public override void Update()
        {
            base.Update();
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        protected override void OnInputEndEventHandler()
        {
            base.OnInputEndEventHandler();
        }

        protected override void UnitOnAttackEventHandler(object info, int damage)
        {
            base.UnitOnAttackEventHandler(info, damage);
        }

        public override void Action(object info = null)
        {
            base.Action(info);

            if (playerCallerOfAbility.Goo == 0)
                return;

            int increaseOn = 0;

            playerCallerOfAbility.Goo = 0;

            increaseOn = abilityUnitOwner.CurrentHP * value - abilityUnitOwner.CurrentHP;
            abilityUnitOwner.BuffedHP += increaseOn;
            abilityUnitOwner.CurrentHP += increaseOn;

            increaseOn = abilityUnitOwner.CurrentDamage * value - abilityUnitOwner.CurrentDamage;
            abilityUnitOwner.BuffedDamage += increaseOn;
            abilityUnitOwner.CurrentDamage += increaseOn;
        }
    }
}