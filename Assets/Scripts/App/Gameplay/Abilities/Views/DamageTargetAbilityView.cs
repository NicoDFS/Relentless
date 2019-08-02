using System.Collections;
using DG.Tweening;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Helpers;
using Loom.ZombieBattleground.Gameplay;
using UnityEngine;

namespace Loom.ZombieBattleground
{
    public class DamageTargetAbilityView : AbilityViewBase<DamageTargetAbility>
    {
        private float _delayBeforeMove;
        private float _delayBeforeDestroyMoved;
        private float _delayAfterImpact;
        private float _delayBeforeDestroyImpact;

        private BattlegroundController _battlegroundController;
        
        public Coroutine CorrectVfxPositionCoroutine;

        public DamageTargetAbilityView(DamageTargetAbility ability) : base(ability)
        {
            _battlegroundController = GameClient.Get<IGameplayManager>().GetController<BattlegroundController>();
        }

        protected override void OnAbilityAction(object info = null)
        {
            SetDelays();

            float durationOfMoving = 0.5f;
            Vector3 offset = Vector3.zero;
            Vector3 localOffset = Vector3.zero;
            bool isRotate = false;

            if (Ability.AbilityData.HasVisualEffectType(Enumerators.VisualEffectType.Moving))
            {
                Vector3 targetPosition = Ability.AffectObjectType == Enumerators.AffectObjectType.Character ?
                _battlegroundController.GetCardViewByModel<BoardUnitView>(Ability.TargetUnit).Transform.position :
                Ability.TargetPlayer.AvatarObject.transform.position;

                VfxObject = LoadObjectsManager.GetObjectByPath<GameObject>(Ability.AbilityData.GetVisualEffectByType(Enumerators.VisualEffectType.Moving).Path);

                AbilityEffectInfoView effectInfo = VfxObject.GetComponent<AbilityEffectInfoView>();
                if (effectInfo != null)
                {
                    durationOfMoving = effectInfo.delayAfterEffect;
                    _delayBeforeDestroyMoved = effectInfo.delayBeforeEffect;
                    _delayBeforeMove = effectInfo.delayForChangeState;
                    delayBeforeSound = effectInfo.delayForSound;
                    offset = effectInfo.offset;
                    localOffset = effectInfo.localOffset;
                    soundClipTitle = effectInfo.soundName;
                    delayBeforeSound = effectInfo.delayForSound;
                    isRotate = effectInfo.isRotate;
                }

                if (isRotate)
                {
                    SetRotation(targetPosition);
                }

                targetPosition += VfxObject.transform.up * localOffset.y;
                targetPosition += VfxObject.transform.right * localOffset.x;
                targetPosition += VfxObject.transform.forward * localOffset.z;

                if( effectInfo != null && (effectInfo.cardName == "Gargantua") )
                {
                    GameObject movingVfx = VfxObject;                    
                    
                    InternalTools.DoActionDelayed(() =>
                    {
                        movingVfx = Object.Instantiate(movingVfx);
                        movingVfx.transform.position = targetPosition;
                        
                        InternalTools.DoActionDelayed(() =>
                        {
                            Object.Destroy(movingVfx);
                        }, effectInfo.delayAfterEffect);
                    }, effectInfo.delayBeforeEffect);
                    
                    ActionCompleted();
                }
                else
                {  
                    VfxObject = Object.Instantiate(VfxObject);
                    VfxObject.transform.position = _battlegroundController.GetCardViewByModel<BoardUnitView>(Ability.AbilityUnitOwner).Transform.position;
                    InternalTools.DoActionDelayed(() =>
                    {
                        VfxObject.transform.DOMove(targetPosition, durationOfMoving).OnComplete(ActionCompleted);
                        ParticleIds.Add(ParticlesController.RegisterParticleSystem(VfxObject));
                    }, _delayBeforeMove);
                }
                
                PlaySound(soundClipTitle, 0);
            }
            else
            {
                ActionCompleted();
            }
        }

        private void ActionCompleted()
        {
            InternalTools.DoActionDelayed(ClearParticles, _delayBeforeDestroyMoved);

            soundClipTitle = string.Empty;

            Vector3 offset = Vector3.zero;
            bool isRotate = false;
            Vector3 vfxRotation = Vector3.zero;
            if (Ability.AbilityData.HasVisualEffectType(Enumerators.VisualEffectType.Impact))
            {
                Vector3 targetPosition = Ability.AffectObjectType == Enumerators.AffectObjectType.Character ?
                _battlegroundController.GetCardViewByModel<BoardUnitView>(Ability.TargetUnit).Transform.position :
                Ability.TargetPlayer.AvatarObject.transform.position;

                VfxObject = LoadObjectsManager.GetObjectByPath<GameObject>(Ability.AbilityData.GetVisualEffectByType(Enumerators.VisualEffectType.Impact).Path);

                AbilityEffectInfoView effectInfo = VfxObject.GetComponent<AbilityEffectInfoView>();
                if (effectInfo != null)
                {
                    _delayAfterImpact = effectInfo.delayAfterEffect;
                    _delayBeforeDestroyImpact = effectInfo.delayBeforeEffect;
                    soundClipTitle = effectInfo.soundName;
                    delayBeforeSound = effectInfo.delayForSound;
                    offset = effectInfo.offset;
                    isRotate = effectInfo.isRotate;
                    vfxRotation = Ability.PlayerCallerOfAbility.IsLocalPlayer ?
                        effectInfo.localPlayerAbilityEffectRotation :
                        effectInfo.opponentPlayerAbilityEffectRotation;
                }

                if (effectInfo != null && (effectInfo.cardName == "Gargantua"))
                {
                    targetPosition = _battlegroundController.GetCardViewByModel<BoardUnitView>(Ability.AbilityUnitOwner).Transform.position;
                }

                CreateVfx(targetPosition + offset, true, _delayBeforeDestroyImpact, true);
                VfxObject.transform.eulerAngles = vfxRotation;

                if (effectInfo != null)
                {
                    Transform cameraVFXObj = VfxObject.transform.Find("!! Camera shake");
                    Transform cameraGroupTransform = GameClient.Get<ICameraManager>().GetGameplayCameras();

                    if
                    (
                        effectInfo.cardName == "Harpoon" ||
                        effectInfo.cardName == "Shovel Damage"
                    )
                    {
                        cameraVFXObj.transform.position = Vector3.zero;
                        cameraGroupTransform.SetParent
                        (
                           cameraVFXObj
                        );

                        cameraGroupTransform.localPosition = VfxObject.transform.position * -1;
                        if (isRotate && !Ability.PlayerCallerOfAbility.IsLocalPlayer)
                        {
                            cameraGroupTransform.localPosition = VfxObject.transform.position;
                        }

                        Ability.VFXAnimationEnded += () =>
                        {
                            cameraGroupTransform.SetParent(null);
                            cameraGroupTransform.position = Vector3.zero;
                        };
                    }
                    else if(effectInfo.cardName == "Gargantua")
                    {
                        CorrectVfxPositionCoroutine = MainApp.Instance.StartCoroutine
                        (
                            CorrectVfxPositionDuringCameraShake
                            (
                                cameraGroupTransform,
                                VfxObject.transform.Find("Gargantua_Anim"),
                                cameraVFXObj
                            )
                        );
    
                        Ability.VFXAnimationEnded += () =>
                        {
                            cameraGroupTransform.SetParent(null);
                            cameraGroupTransform.position = Vector3.zero;
    
                            if (CorrectVfxPositionCoroutine != null)
                            {
                                Debug.LogWarning("Stop coroutine called");
                                MainApp.Instance.StopCoroutine(CorrectVfxPositionCoroutine);
                            }
                            CorrectVfxPositionCoroutine = null;
                        };
                    }
                }

                PlaySound(soundClipTitle, delayBeforeSound);
            }

            InternalTools.DoActionDelayed(Ability.InvokeVFXAnimationEnded, _delayAfterImpact);
        }

        public IEnumerator CorrectVfxPositionDuringCameraShake( Transform cameraGroupTransform, Transform vfxTransform, Transform shakingAnimationObject )
        {
            while(true)
            {
                if ( cameraGroupTransform != null && vfxTransform != null && shakingAnimationObject != null)
                {
                    cameraGroupTransform.position = shakingAnimationObject.localPosition;
                    vfxTransform.localPosition = cameraGroupTransform.localPosition * -1f;
                }
                yield return null;
            }
        }

        protected override void CreateVfx(Vector3 pos, bool autoDestroy = false, float duration = 3, bool justPosition = false)
        {
            base.CreateVfx(pos, autoDestroy, duration, justPosition);
        }

        private void SetDelays()
        {
            _delayBeforeMove = 0;
            _delayAfterImpact = 0;
            _delayBeforeDestroyImpact = 0;
            _delayBeforeDestroyMoved = 0;
        }

        private float AngleBetweenVector3(Vector3 from, Vector3 target)
        {
            Vector3 diference = target - from;
            float sign = (target.x < from.x) ? 1.0f : -1.0f;
            return Vector3.Angle(Vector3.up, diference) * sign;
        }

        private void SetRotation(Vector3 targetPosition)
        {
            float angle = AngleBetweenVector3(_battlegroundController.GetCardViewByModel<BoardUnitView>(Ability.AbilityUnitOwner).Transform.position, targetPosition);
            VfxObject.transform.eulerAngles = new Vector3(VfxObject.transform.eulerAngles.x, VfxObject.transform.eulerAngles.y, angle);
        }
    }
}
