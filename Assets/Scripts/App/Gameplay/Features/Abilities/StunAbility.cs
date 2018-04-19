﻿﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GrandDevs.CZB.Common;
using CCGKit;
using UnityEngine;
using GrandDevs.CZB.Data;

namespace GrandDevs.CZB
{
    public class StunAbility : AbilityBase
    {
        public Enumerators.StatType statType;
        public int value = 1;
        private Server _server;


        public StunAbility(Enumerators.CardKind cardKind, AbilityData ability) : base(cardKind, ability)
        {
            this.statType = ability.abilityStatType;
            this.value = ability.value;
        }

        public override void Activate()
        {
            base.Activate();

            switch (abilityEffectType)
            {
                case Enumerators.AbilityEffectType.STUN_FREEZES:
                    _vfxObject = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/FrozenVFX");
                    break;
                default:
                    _vfxObject = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/fireDamageVFX");
                    break;
            }
            
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

        protected override void CreatureOnAttackEventHandler(object info)
        {
            base.CreatureOnAttackEventHandler(info);
            if (abilityCallType != Enumerators.AbilityCallType.AT_ATTACK)
                return;
            if(info is BoardCreature)
            {
                var creature = info as BoardCreature;
                creature.Stun(value);
				CreateVFX(creature.transform.position);
			}
        }
    }
}