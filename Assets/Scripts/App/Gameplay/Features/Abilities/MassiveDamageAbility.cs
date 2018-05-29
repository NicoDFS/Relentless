﻿using System;
using System.Collections.Generic;
using GrandDevs.CZB.Common;
using CCGKit;
using UnityEngine;
using GrandDevs.CZB.Data;

namespace GrandDevs.CZB
{
    public class MassiveDamageAbility : AbilityBase
    {
        public int value = 1;

        public MassiveDamageAbility(Enumerators.CardKind cardKind, AbilityData ability) : base(cardKind, ability)
        {
            this.value = ability.value;
        }

        public override void Activate()
        {
            base.Activate();
            if (abilityCallType != Enumerators.AbilityCallType.AT_START)
				return;
            Action();
            //_vfxObject = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/healVFX");
        }

        public override void Update() { }

        public override void Dispose() { }

        protected override void OnInputEndEventHandler()
        {
            base.OnInputEndEventHandler();
        }

        protected override void CreatureOnDieEventHandler()
        {
            base.CreatureOnDieEventHandler();
			if (abilityCallType != Enumerators.AbilityCallType.AT_DEATH)
				return;
            Debug.Log("CreatureOnDieEventHandler");
            Action();
        }

        private void Action()
        {

			foreach (var target in abilityTargetTypes)
			{
				switch (target)
				{
					case Enumerators.AbilityTargetType.OPPONENT_ALL_CARDS:
						BoardCreature[] creatures = new BoardCreature[playerCallerOfAbility.opponentBoardCardsList.Count];
                        playerCallerOfAbility.opponentBoardCardsList.CopyTo(creatures);
						foreach (var cardOpponent in creatures)
						{
							playerCallerOfAbility.FightCreatureBySkill(value, cardOpponent.card);
							
						}
                        CreateVFX(Vector3.up * 1.5f);
                        Array.Clear(creatures, 0, creatures.Length);
						creatures = null;
						break;
                    case Enumerators.AbilityTargetType.PLAYER_ALL_CARDS:
						RuntimeCard[] cards = new RuntimeCard[playerCallerOfAbility.boardZone.cards.Count];
						playerCallerOfAbility.boardZone.cards.CopyTo(cards);
						foreach (var cardPlayer in cards)
						{
							playerCallerOfAbility.FightCreatureBySkill(value, cardPlayer);
							//CreateVFX(cardPlayer.transform.position);
						}
						Array.Clear(cards, 0, cards.Length);
						cards = null;

						// foreach (var card in cardCaller.boardZone.cards)
						//   cardCaller.FightCreatureBySkill(value, card);

						break;
					case Enumerators.AbilityTargetType.OPPONENT:
						playerCallerOfAbility.FightPlayerBySkill(value);
						//CreateVFX(targetCreature.transform.position);
						break;
					case Enumerators.AbilityTargetType.PLAYER:
						playerCallerOfAbility.FightPlayerBySkill(value, false);
						//CreateVFX(targetCreature.transform.position);
						break;
					default: break;
				}
			}
        }

        protected override void CreateVFX(Vector3 pos)
        {
            int playerPos = playerCallerOfAbility is DemoHumanPlayer ? 1 : -1;
            
            switch (abilityEffectType)
            {
                case Enumerators.AbilityEffectType.MASSIVE_WATER_WAVE:
                    _vfxObject = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Spells/tsunamiSpellVFX");
                    break;
                case Enumerators.AbilityEffectType.MASSIVE_FIRE:
                    _vfxObject = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Spells/SpellMassiveFireVFX");
                    break;
                case Enumerators.AbilityEffectType.MASSIVE_LIGHTNING:
                    _vfxObject = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Spells/LightningVFX");
                    pos = Vector3.up * 0.5f;
                    break;
                case Enumerators.AbilityEffectType.MASSIVE_TOXIC_ALL:
                    _vfxObject = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Spells/ToxicMassiveAllVFX");
                    pos = Vector3.zero;
                    break;
                default:
                    break;
            }
            pos = pos * playerPos;

            DestroyCurrentParticle();

            base.CreateVFX(pos);
        }
    }
}