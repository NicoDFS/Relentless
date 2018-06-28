﻿using GrandDevs.CZB.Common;
using GrandDevs.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GrandDevs.CZB
{
    public class VFXController : IController
    {
        private ISoundManager _soundManager;
        private ITimerManager _timerManager;

        private ParticlesController _particlesController;

        public void Init()
        {
            _timerManager = GameClient.Get<ITimerManager>();
        }

        public void Dispose()
        {
        }

        public void Update()
        {
        }


        public void PlayAttackVFX(Enumerators.CardType type, Vector3 target, int damage)
        {
            GameObject effect;
            GameObject vfxPrefab;
            target = Utilites.CastVFXPosition(target);

            if (type == Enumerators.CardType.FERAL)
            {
                vfxPrefab = GameClient.Get<ILoadObjectsManager>().GetObjectByPath<GameObject>("Prefabs/VFX/FeralAttackVFX");
                effect = GameObject.Instantiate(vfxPrefab);
                effect.transform.position = target;
                _soundManager.PlaySound(Enumerators.SoundType.FERAL_ATTACK, Constants.CREATURE_ATTACK_SOUND_VOLUME, false, false, true);

                _particlesController.RegisterParticleSystem(effect, true, 5f);

                if (damage > 3 && damage < 7)
                {
                    GameClient.Get<ITimerManager>().AddTimer((a) =>
                    {
                        effect = GameObject.Instantiate(vfxPrefab);
                        effect.transform.position = target;
                        effect.transform.localScale = new Vector3(-1, 1, 1);
                        _particlesController.RegisterParticleSystem(effect, true, 5f);


                    }, null, 0.5f, false);
                }
                if (damage > 6)
                {
                    GameClient.Get<ITimerManager>().AddTimer((a) =>
                    {
                        effect = GameObject.Instantiate(vfxPrefab);
                        effect.transform.position = target - Vector3.right;
                        effect.transform.eulerAngles = Vector3.forward * 90;

                        _particlesController.RegisterParticleSystem(effect, true, 5f);

                    }, null, 1.0f, false);
                }
                //GameClient.Get<ITimerManager>().AddTimer((a) =>
                //{
                //    _soundManager.PlaySound(Enumerators.SoundType.FERAL_ATTACK, Constants.CREATURE_ATTACK_SOUND_VOLUME, false, false, true);
                //}, null, 0.75f, false);
            }
            else if (type == Enumerators.CardType.HEAVY)
            {
                var soundType = Enumerators.SoundType.HEAVY_ATTACK_1;
                var prefabName = "Prefabs/VFX/HeavyAttackVFX";
                if (damage > 4)
                {
                    prefabName = "Prefabs/VFX/HeavyAttack2VFX";
                    soundType = Enumerators.SoundType.HEAVY_ATTACK_2;
                }
                vfxPrefab = GameClient.Get<ILoadObjectsManager>().GetObjectByPath<GameObject>(prefabName);
                effect = GameObject.Instantiate(vfxPrefab);
                effect.transform.position = target;

                _particlesController.RegisterParticleSystem(effect, true, 5f);

                _soundManager.PlaySound(soundType, Constants.CREATURE_ATTACK_SOUND_VOLUME, false, false, true);
                /* GameClient.Get<ITimerManager>().AddTimer((a) =>
                     {
                     }, null, 0.75f, false);*/
            }
            else
            {
                vfxPrefab = GameClient.Get<ILoadObjectsManager>().GetObjectByPath<GameObject>("Prefabs/VFX/WalkerAttackVFX");
                effect = GameObject.Instantiate(vfxPrefab);
                effect.transform.position = target;

                _particlesController.RegisterParticleSystem(effect, true, 5f);

                if (damage > 4)
                {
                    GameClient.Get<ITimerManager>().AddTimer((a) =>
                    {
                        effect = GameObject.Instantiate(vfxPrefab);
                        effect.transform.position = target;

                        effect.transform.localScale = new Vector3(-1, 1, 1);
                        _particlesController.RegisterParticleSystem(effect, true, 5f);


                    }, null, 0.5f, false);
                    //  GameClient.Get<ITimerManager>().AddTimer((a) =>
                    //  {
                    _soundManager.PlaySound(Enumerators.SoundType.WALKER_ATTACK_2, Constants.CREATURE_ATTACK_SOUND_VOLUME, false, false, true);
                    // }, null, 0.75f, false);
                }
                else
                {
                    //    GameClient.Get<ITimerManager>().AddTimer((a) =>
                    //   {
                    _soundManager.PlaySound(Enumerators.SoundType.WALKER_ATTACK_1, Constants.CREATURE_ATTACK_SOUND_VOLUME, false, false, true);
                    //     }, null, 0.75f, false);
                }
            }

        }

        public void PlayArrivalAnimationDelay(object[] param)
        {
            BoardCreature currentCreature = null;
            if (param != null)
            {
                currentCreature = param[0] as BoardCreature;
                currentCreature.PlayArrivalAnimation();
            }
        }
    }
}