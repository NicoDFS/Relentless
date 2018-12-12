using System;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using Loom.ZombieBattleground.Helpers;
using System.Collections.Generic;
using Loom.ZombieBattleground.View;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Linq;

namespace Loom.ZombieBattleground
{
    public class SkillsController : IController
    {
        private ILoadObjectsManager _loadObjectsManager;

        private IGameplayManager _gameplayManager;

        private ITutorialManager _tutorialManager;

        private IUIManager _uiManager;

        private ITimerManager _timerManager;

        private ISoundManager _soundManager;

        private VfxController _vfxController;

        private BattleController _battleController;

        private ActionsQueueController _actionsQueueController;

        private CardsController _cardsController;

        private BattlegroundController _battlegroundController;

        private bool _skillsInitialized;

        private bool _isDirection;

        private GameObject _buildParticlePrefab;

        public BoardSkill OpponentPrimarySkill { get; private set; }

        public BoardSkill OpponentSecondarySkill { get; private set; }

        public BoardSkill PlayerPrimarySkill { get; private set; }

        public BoardSkill PlayerSecondarySkill { get; private set; }

        public void Dispose()
        {
        }

        public void Init()
        {
            _loadObjectsManager = GameClient.Get<ILoadObjectsManager>();
            _gameplayManager = GameClient.Get<IGameplayManager>();
            _tutorialManager = GameClient.Get<ITutorialManager>();
            _uiManager = GameClient.Get<IUIManager>();
            _timerManager = GameClient.Get<ITimerManager>();
            _soundManager = GameClient.Get<ISoundManager>();

            _vfxController = _gameplayManager.GetController<VfxController>();
            _battleController = _gameplayManager.GetController<BattleController>();
            _actionsQueueController = _gameplayManager.GetController<ActionsQueueController>();
            _cardsController = _gameplayManager.GetController<CardsController>();
            _battlegroundController = _gameplayManager.GetController<BattlegroundController>();

            _gameplayManager.GameEnded += GameplayManagerGameEnded;
        }

        public void Update()
        {
            if (_skillsInitialized)
            {
                PlayerPrimarySkill?.Update();
                PlayerSecondarySkill?.Update();
                OpponentPrimarySkill?.Update();
                OpponentSecondarySkill?.Update();
            }
        }

        public void ResetAll()
        {
            PlayerPrimarySkill = null;
            PlayerSecondarySkill = null;
            OpponentPrimarySkill = null;
            OpponentSecondarySkill = null;
        }

        public void InitializeSkills()
        {
            GameplayPage rootPage = _uiManager.GetPage<GameplayPage>();

            rootPage.PlayerPrimarySkillHandler.MouseDownTriggered += PrimarySkillHandlerMouseDownTriggeredHandler;
            rootPage.PlayerPrimarySkillHandler.MouseUpTriggered += PrimarySkillHandlerMouseUpTriggeredHandler;

            rootPage.PlayerSecondarySkillHandler.MouseDownTriggered += SecondarySkillHandlerMouseDownTriggeredHandler;
            rootPage.PlayerSecondarySkillHandler.MouseUpTriggered += SecondarySkillHandlerMouseUpTriggeredHandler;
            rootPage.OpponentPrimarySkillHandler.MouseDownTriggered +=
                OpponentPrimarySkillHandlerMouseDownTriggeredHandler;
            rootPage.OpponentPrimarySkillHandler.MouseUpTriggered += OpponentPrimarySkillHandlerMouseUpTriggeredHandler;

            rootPage.OpponentSecondarySkillHandler.MouseDownTriggered +=
                OpponentSecondarySkillHandlerMouseDownTriggeredHandler;
            rootPage.OpponentSecondarySkillHandler.MouseUpTriggered +=
                OpponentSecondarySkillHandlerMouseUpTriggeredHandler;

            HeroSkill primary = _gameplayManager.CurrentPlayer.SelfHero.GetSkill(_gameplayManager.CurrentPlayerDeck.PrimarySkill);
            HeroSkill secondary = _gameplayManager.CurrentPlayer.SelfHero.GetSkill(_gameplayManager.CurrentPlayerDeck.SecondarySkill);

            if (primary != null && secondary != null)
            {
                rootPage.SetupSkills(primary, secondary, false);
                SetPlayerSkills(rootPage, primary, secondary);
            }

            primary = _gameplayManager.OpponentPlayer.SelfHero.GetSkill(_gameplayManager.OpponentPlayerDeck.PrimarySkill);
            secondary = _gameplayManager.OpponentPlayer.SelfHero.GetSkill(_gameplayManager.OpponentPlayerDeck.SecondarySkill);

            if (primary != null && secondary != null)
            {
                rootPage.SetupSkills(primary, secondary, true);
                SetOpponentSkills(rootPage, primary, secondary);
            }

            _skillsInitialized = true;
        }

        public void DisableSkillsContent(Player player)
        {
            if (player.IsLocalPlayer)
            {
                PlayerPrimarySkill?.Hide();
                PlayerSecondarySkill?.Hide();
            }
            else
            {
                OpponentPrimarySkill?.Hide();
                OpponentSecondarySkill?.Hide();
            }
        }

        public void BlockSkill(Player player, Enumerators.SkillType type)
        {
            if (player.IsLocalPlayer)
            {
                PlayerPrimarySkill?.BlockSkill();
                PlayerSecondarySkill?.BlockSkill();
            }
            else
            {
                OpponentPrimarySkill?.BlockSkill();
                OpponentSecondarySkill?.BlockSkill();
            }
        }

        public void UnBlockSkill(Player player)
        {
            if (player.IsLocalPlayer)
            {
                PlayerPrimarySkill?.UnBlockSkill();
                PlayerSecondarySkill?.UnBlockSkill();
            }
            else
            {
                OpponentPrimarySkill?.UnBlockSkill();
                OpponentSecondarySkill?.UnBlockSkill();
            }
        }

        public void SetPlayerSkills(GameplayPage rootPage, HeroSkill primary, HeroSkill secondary)
        {
            PlayerPrimarySkill = new BoardSkill(rootPage.PlayerPrimarySkillHandler.gameObject,
                _gameplayManager.CurrentPlayer,
                primary,
                true);
            PlayerSecondarySkill = new BoardSkill(rootPage.PlayerSecondarySkillHandler.gameObject,
                _gameplayManager.CurrentPlayer,
                secondary,
                false);
        }

        public void SetOpponentSkills(GameplayPage rootPage, HeroSkill primary, HeroSkill secondary)
        {
            OpponentPrimarySkill = new BoardSkill(rootPage.OpponentPrimarySkillHandler.gameObject,
                _gameplayManager.OpponentPlayer,
                primary,
                true);
            OpponentSecondarySkill = new BoardSkill(rootPage.OpponentSecondarySkillHandler.gameObject,
                _gameplayManager.OpponentPlayer,
                secondary,
                false);
        }

        public void DoSkillAction(BoardSkill skill, Action completeCallback, BoardObject target = null)
        {
            if (skill == null || !skill.IsUsing)
            {
                completeCallback?.Invoke();
                return;
            }

            if (skill.FightTargetingArrow != null)
            {
                if (skill.FightTargetingArrow.SelectedPlayer != null)
                {
                    Player targetPlayer = skill.FightTargetingArrow.SelectedPlayer;

                    string soundFile = GetSoundBySkills(skill);
                    if (!string.IsNullOrEmpty(soundFile))
                    {
                        _soundManager.PlaySound(
                            Enumerators.SoundType.OVERLORD_ABILITIES,
                            soundFile,
                            Constants.OverlordAbilitySoundVolume,
                            false);
                    }

                    skill.UseSkill(targetPlayer);
                    CreateSkillVfx(
                        GetVfxPrefabBySkill(skill),
                        skill.SelfObject.transform.position,
                        targetPlayer,
                        (x) =>
                        {
                            DoActionByType(skill, targetPlayer, completeCallback);
                        }, _isDirection);

                    if (_gameplayManager.CurrentTurnPlayer == _gameplayManager.CurrentPlayer)
                    {
                        PlayOverlordSkill playOverlordSkill = new PlayOverlordSkill(skill, targetPlayer);
                        _gameplayManager.PlayerMoves.AddPlayerMove(
                            new PlayerMove(Enumerators.PlayerActionType.PlayOverlordSkill, playOverlordSkill));
                    }
                }
                else if (skill.FightTargetingArrow.SelectedCard != null)
                {
                    BoardUnitView targetUnitView = skill.FightTargetingArrow.SelectedCard;

                    string soundFile = GetSoundBySkills(skill);
                    if (!string.IsNullOrEmpty(soundFile))
                    {
                        _soundManager.PlaySound(
                            Enumerators.SoundType.OVERLORD_ABILITIES,
                            soundFile,
                            Constants.OverlordAbilitySoundVolume,
                            false);
                    }

                    skill.UseSkill(targetUnitView.Model);
                    CreateSkillVfx(
                        GetVfxPrefabBySkill(skill),
                        skill.SelfObject.transform.position,
                        targetUnitView,
                        (x) =>
                        {
                            DoActionByType(skill, targetUnitView.Model, completeCallback);
                        }, _isDirection);

                    if (_gameplayManager.CurrentTurnPlayer == _gameplayManager.CurrentPlayer)
                    {
                        PlayOverlordSkill playOverlordSkill = new PlayOverlordSkill(skill, targetUnitView.Model);
                        _gameplayManager.PlayerMoves.AddPlayerMove(
                            new PlayerMove(Enumerators.PlayerActionType.PlayOverlordSkill, playOverlordSkill));
                    }
                }
                else
                {
                    completeCallback?.Invoke();
                }

                skill.CancelTargetingArrows();
            }
            else if (target != null)
            {
                string soundFile = GetSoundBySkills(skill);
                if (!string.IsNullOrEmpty(soundFile))
                {
                    _soundManager.PlaySound(Enumerators.SoundType.OVERLORD_ABILITIES, soundFile, Constants.OverlordAbilitySoundVolume, false);
                }

                skill.UseSkill(target);
                CreateSkillVfx(
                    GetVfxPrefabBySkill(skill),
                    skill.SelfObject.transform.position,
                    target,
                    (x) =>
                    {
                        DoActionByType(skill, target, completeCallback);
                    }, _isDirection);

                if (_gameplayManager.CurrentTurnPlayer == _gameplayManager.CurrentPlayer)
                {
                    PlayOverlordSkill playOverlordSkill = new PlayOverlordSkill(skill, target);
                    _gameplayManager.PlayerMoves.AddPlayerMove(
                        new PlayerMove(Enumerators.PlayerActionType.PlayOverlordSkill, playOverlordSkill));
                }
            }
            else
            {
                completeCallback?.Invoke();
            }
        }

        private void CreateSkillVfx(GameObject prefab, Vector3 from, object target, Action<object> callbackComplete, bool isDirection = false)
        {
            if (_buildParticlePrefab == null)
            {
                _vfxController.CreateSkillVfx(prefab, from, target, callbackComplete, isDirection);
            }
            else
            {
                _vfxController.CreateSkillBuildVfx(_buildParticlePrefab, prefab, from, target, callbackComplete, isDirection);
            }
        }

        private void GameplayManagerGameEnded(Enumerators.EndGameType obj)
        {
            _skillsInitialized = false;
        }

        private void PrimarySkillHandlerMouseDownTriggeredHandler(GameObject obj)
        {
            PlayerPrimarySkill?.OnMouseDownEventHandler();
        }

        private void PrimarySkillHandlerMouseUpTriggeredHandler(GameObject obj)
        {
            PlayerPrimarySkill?.OnMouseUpEventHandler();
        }

        private void SecondarySkillHandlerMouseDownTriggeredHandler(GameObject obj)
        {
            PlayerSecondarySkill?.OnMouseDownEventHandler();
        }

        private void SecondarySkillHandlerMouseUpTriggeredHandler(GameObject obj)
        {
            PlayerSecondarySkill?.OnMouseUpEventHandler();
        }

        private void OpponentPrimarySkillHandlerMouseDownTriggeredHandler(GameObject obj)
        {
            OpponentPrimarySkill?.OnMouseDownEventHandler();
        }

        private void OpponentPrimarySkillHandlerMouseUpTriggeredHandler(GameObject obj)
        {
            OpponentPrimarySkill?.OnMouseUpEventHandler();
        }

        private void OpponentSecondarySkillHandlerMouseDownTriggeredHandler(GameObject obj)
        {
            OpponentSecondarySkill?.OnMouseDownEventHandler();
        }

        private void OpponentSecondarySkillHandlerMouseUpTriggeredHandler(GameObject obj)
        {
            OpponentSecondarySkill?.OnMouseUpEventHandler();
        }

        private GameObject GetVfxPrefabBySkill(BoardSkill skill)
        {
            _isDirection = false;
            _buildParticlePrefab = null;
            GameObject prefab;
            switch (skill.Skill.OverlordSkill)
            {
                case Enumerators.OverlordSkill.ICE_BOLT:
                    prefab = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/IceBoltVFX");
                    _isDirection = true;
                    break;
                case Enumerators.OverlordSkill.FREEZE:
                    prefab = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/FreezeVFX");
                    break;
                case Enumerators.OverlordSkill.SHATTER:
                    prefab = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/Shatter_Projectile");
                    _buildParticlePrefab = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/Shatter_BuildUp");
                    _isDirection = true;
                    break;
                case Enumerators.OverlordSkill.POISON_DART:
                case Enumerators.OverlordSkill.TOXIC_POWER:
                case Enumerators.OverlordSkill.INFECT:
                    _isDirection = true;
                    prefab = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/PoisonDartVFX");
                    break;
                case Enumerators.OverlordSkill.FIREBALL:
                    prefab = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/FireBallVFX");
                    break;
                case Enumerators.OverlordSkill.FIRE_BOLT:
                    prefab = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/FireBoltVFX");
                    break;
                case Enumerators.OverlordSkill.HEALING_TOUCH:
                case Enumerators.OverlordSkill.MEND:
                case Enumerators.OverlordSkill.HARDEN:
                case Enumerators.OverlordSkill.STONE_SKIN:
                case Enumerators.OverlordSkill.PUSH:
                case Enumerators.OverlordSkill.BREAKOUT:
                case Enumerators.OverlordSkill.DRAW:
                case Enumerators.OverlordSkill.BLIZZARD:
                case Enumerators.OverlordSkill.ENHANCE:
                case Enumerators.OverlordSkill.EPIDEMIC:
                case Enumerators.OverlordSkill.FORTIFY:
                case Enumerators.OverlordSkill.FORTRESS:
                case Enumerators.OverlordSkill.ICE_WALL:
                case Enumerators.OverlordSkill.LEVITATE:
                case Enumerators.OverlordSkill.MASS_RABIES:
                case Enumerators.OverlordSkill.METEOR_SHOWER:
                case Enumerators.OverlordSkill.PHALANX:
                case Enumerators.OverlordSkill.REANIMATE:
                case Enumerators.OverlordSkill.RESSURECT:
                case Enumerators.OverlordSkill.RETREAT:
                case Enumerators.OverlordSkill.WIND_SHIELD:
                default:
                    prefab = new GameObject();
                    break;
            }

            return prefab;
        }

        private string GetSoundBySkills(BoardSkill skill)
        {
            string soundFileName = string.Empty;
            switch (skill.Skill.OverlordSkill)
            {
                case Enumerators.OverlordSkill.ICE_BOLT:
                case Enumerators.OverlordSkill.FREEZE:
                case Enumerators.OverlordSkill.POISON_DART:
                case Enumerators.OverlordSkill.FIRE_BOLT:
                    soundFileName = skill.Skill.OverlordSkill.ToString().ToLowerInvariant();
                    break;
                case Enumerators.OverlordSkill.HEALING_TOUCH:
                case Enumerators.OverlordSkill.FIREBALL:
                case Enumerators.OverlordSkill.TOXIC_POWER:
                case Enumerators.OverlordSkill.MEND:
                case Enumerators.OverlordSkill.HARDEN:
                case Enumerators.OverlordSkill.STONE_SKIN:
                case Enumerators.OverlordSkill.PUSH:
                case Enumerators.OverlordSkill.DRAW:
                default:
                    break;
            }

            return soundFileName;
        }

        private void DoActionByType(BoardSkill skill, BoardObject target, Action completeCallback)
        {
            switch (skill.Skill.OverlordSkill)
            {
                case Enumerators.OverlordSkill.FREEZE:
                    FreezeAction(skill.OwnerPlayer, skill, skill.Skill, target);
                    break;
                case Enumerators.OverlordSkill.ICE_BOLT:
                    IceBoltAction(skill.OwnerPlayer, skill, skill.Skill, target);
                    break;
                case Enumerators.OverlordSkill.POISON_DART:
                    PoisonDartAction(skill.OwnerPlayer, skill, skill.Skill, target);
                    break;
                case Enumerators.OverlordSkill.TOXIC_POWER:
                    ToxicPowerAction(skill.OwnerPlayer, skill, skill.Skill, target);
                    break;
                case Enumerators.OverlordSkill.HEALING_TOUCH:
                    HealingTouchAction(skill.OwnerPlayer, skill, skill.Skill, target);
                    break;
                case Enumerators.OverlordSkill.MEND:
                    MendAction(skill.OwnerPlayer, skill, skill.Skill, target);
                    break;
                case Enumerators.OverlordSkill.FIRE_BOLT:
                    FireBoltAction(skill.OwnerPlayer, skill, skill.Skill, target);
                    break;
                case Enumerators.OverlordSkill.RABIES:
                    RabiesAction(skill.OwnerPlayer, skill, skill.Skill, target);
                    break;
                case Enumerators.OverlordSkill.HARDEN:
                    HardenAction(skill.OwnerPlayer, skill, skill.Skill, target);
                    break;
                case Enumerators.OverlordSkill.STONE_SKIN:
                    StoneskinAction(skill.OwnerPlayer, skill, skill.Skill, target);
                    break;
                case Enumerators.OverlordSkill.PUSH:
                    PushAction(skill.OwnerPlayer, skill, skill.Skill, target);
                    break;
                case Enumerators.OverlordSkill.DRAW:
                    DrawAction(skill.OwnerPlayer, skill, skill.Skill, target);
                    break;
                case Enumerators.OverlordSkill.WIND_SHIELD:
                    WindShieldAction(skill.OwnerPlayer, skill, skill.Skill, target);
                    break;
                case Enumerators.OverlordSkill.LEVITATE:
                    Levitate(skill.OwnerPlayer, skill, skill.Skill, target);
                    break;
                case Enumerators.OverlordSkill.RETREAT:
                    RetreatAction(skill.OwnerPlayer, skill, skill.Skill, target);
                    break;
                case Enumerators.OverlordSkill.BREAKOUT:
                    BreakoutAction(skill.OwnerPlayer, skill, skill.Skill, target);
                    break;
                case Enumerators.OverlordSkill.INFECT:
                    InfectAction(skill.OwnerPlayer, skill, skill.Skill, target);
                    break;
                case Enumerators.OverlordSkill.EPIDEMIC:
                    EpidemicAction(skill.OwnerPlayer, skill, skill.Skill, target);
                    break;
                case Enumerators.OverlordSkill.RESSURECT:
                    RessurectAction(skill.OwnerPlayer, skill, skill.Skill, target);
                    break;
                case Enumerators.OverlordSkill.REANIMATE:
                    ReanimateAction(skill.OwnerPlayer, skill, skill.Skill, target);
                    break;
                case Enumerators.OverlordSkill.ENHANCE:
                    EnhanceAction(skill.OwnerPlayer, skill, skill.Skill, target);
                    break;
                case Enumerators.OverlordSkill.ICE_WALL:
                    IceWallAction(skill.OwnerPlayer, skill, skill.Skill, target);
                    break;
                case Enumerators.OverlordSkill.SHATTER:
                    ShatterAction(skill.OwnerPlayer, skill, skill.Skill, target);
                    break;
                case Enumerators.OverlordSkill.BLIZZARD:
                    BlizzardAction(skill.OwnerPlayer, skill, skill.Skill, target);
                    break;
                case Enumerators.OverlordSkill.MASS_RABIES:
                    MassRabiesAction(skill.OwnerPlayer, skill, skill.Skill, target);
                    break;
                case Enumerators.OverlordSkill.METEOR_SHOWER:
                    MeteorShowerAction(skill.OwnerPlayer, skill, skill.Skill, target);
                    break;
                case Enumerators.OverlordSkill.FIREBALL:
                    FireballAction(skill.OwnerPlayer, skill, skill.Skill, target);
                    break;
                case Enumerators.OverlordSkill.FORTIFY:
                    FortifyAction(skill.OwnerPlayer, skill, skill.Skill, target);
                    break;
                case Enumerators.OverlordSkill.FORTRESS:
                    FortressAction(skill.OwnerPlayer, skill, skill.Skill, target);
                    break;
                case Enumerators.OverlordSkill.PHALANX:
                    PhalanxAction(skill.OwnerPlayer, skill, skill.Skill, target);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(skill.Skill.OverlordSkill), skill.Skill.OverlordSkill, null);
            }

            completeCallback?.Invoke();
        }

        #region actions

        // ADDITINAL

        private void AttackWithModifiers(
            Player owner,
            BoardSkill boardSkill,
            HeroSkill skill,
            object target,
            Enumerators.SetType attackType,
            Enumerators.SetType setType)
        {
            if (target is Player player)
            {
                // TODO additional damage to heros
                _battleController.AttackPlayerBySkill(owner, boardSkill, player);
            }
            else
            {
                BoardUnitModel creature = (BoardUnitModel) target;
                int attackModifier = 0;
                _battleController.AttackUnitBySkill(owner, boardSkill, creature, attackModifier);
            }
        }

        private Dictionary<T, int> GetRandomTargetsByAmount<T>(List<T> root, int count)
        {
            Dictionary<T, int> targets = InternalTools.GetRandomElementsFromList(root, count).ToDictionary(x => x, с => 1);

            if (targets.Count < count)
            {
                int delta = count - targets.Count;
                for (int i = 0; i < delta; i++)
                {
                    targets[InternalTools.GetRandomElementsFromList(root, 1)[0]]++;
                }
            }
            return targets;
        }

        // AIR

        private void PushAction(Player owner, BoardSkill boardSkill, HeroSkill skill, BoardObject target)
        {
            owner.CurrentGoo = 0;

            BoardUnitModel targetUnit = (BoardUnitModel) target;

            _vfxController.CreateVfx(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/PushVFX"),
                targetUnit);

            _soundManager.PlaySound(
                Enumerators.SoundType.OVERLORD_ABILITIES,
                skill.OverlordSkill.ToString().ToLowerInvariant(),
                Constants.OverlordAbilitySoundVolume,
                Enumerators.CardSoundType.NONE);

            _cardsController.ReturnCardToHand(_battlegroundController.GetBoardUnitViewByModel(targetUnit));

            _actionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.UseOverlordPowerOnCard,
                Caller = boardSkill,
                TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                {
                    new PastActionsPopup.TargetEffectParam()
                    {
                        ActionEffectType = Enumerators.ActionEffectType.Push,
                        Target = target
                    }
                }
            });
        }

        private void DrawAction(Player owner, BoardSkill boardSkill, HeroSkill skill, BoardObject target)
        {
            _cardsController.AddCardToHand(owner);

            owner.PlayDrawCardVFX();

            _soundManager.PlaySound(
                Enumerators.SoundType.OVERLORD_ABILITIES,
                skill.OverlordSkill.ToString().ToLowerInvariant(),
                Constants.OverlordAbilitySoundVolume,
                Enumerators.CardSoundType.NONE);

            _actionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.UseOverlordPower,
                Caller = boardSkill,
                TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
            });
        }

        private void WindShieldAction(Player owner, BoardSkill boardSkill, HeroSkill skill, BoardObject target)
        {
            List<PastActionsPopup.TargetEffectParam> TargetEffects = new List<PastActionsPopup.TargetEffectParam>();

            List<BoardUnitView> units =
                InternalTools.GetRandomElementsFromList(
                    owner.BoardCards.FindAll(x => x.Model.Card.LibraryCard.CardSetType == Enumerators.SetType.AIR),
                    skill.Value);

            foreach (BoardUnitView unit in units)
            {
                unit.Model.AddBuffShield();

                TargetEffects.Add(new PastActionsPopup.TargetEffectParam()
                {
                    ActionEffectType = Enumerators.ActionEffectType.ShieldBuff,
                    Target = unit
                });
            }

            if (TargetEffects.Count > 0)
            {
                _actionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam()
                {
                    ActionType = Enumerators.ActionType.UseOverlordPowerOnCard,
                    Caller = boardSkill,
                    TargetEffects = TargetEffects
                });
            }
        }

        private void Levitate(Player owner, BoardSkill boardSkill, HeroSkill skill, BoardObject target)
        {
            WorkingCard card = _cardsController.LowGooCostOfCardInHand(owner, null, skill.Value);

            if (owner.IsLocalPlayer)
            {
                BoardCard boardCard = _battlegroundController.PlayerHandCards.Find(x => x.WorkingCard.Equals(card));
                GameObject particle = Object.Instantiate(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/LevitateVFX"));
                particle.transform.position = boardCard.Transform.position;
                particle.transform.SetParent(boardCard.Transform, true);
                particle.transform.localEulerAngles = Vector3.zero;
                _gameplayManager.GetController<ParticlesController>().RegisterParticleSystem(particle, true, 6f);
            }

            _soundManager.PlaySound(
                Enumerators.SoundType.OVERLORD_ABILITIES,
                skill.OverlordSkill.ToString().ToLowerInvariant(),
                Constants.OverlordAbilitySoundVolume,
                Enumerators.CardSoundType.NONE);

            _actionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.UseOverlordPowerOnCard,
                Caller = boardSkill,
                TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                {
                    new PastActionsPopup.TargetEffectParam()
                    {
                        ActionEffectType = Enumerators.ActionEffectType.LowGooCost,
                        Target = card,
                        HasValue = true,
                        Value = skill.Value
                    }
                }
            });
        }

        private void RetreatAction(Player owner, BoardSkill boardSkill, HeroSkill skill, BoardObject target)
        {
            List<PastActionsPopup.TargetEffectParam> TargetEffects = new List<PastActionsPopup.TargetEffectParam>();

            List<BoardUnitView> units = new List<BoardUnitView>();
            units.AddRange(_gameplayManager.CurrentPlayer.BoardCards);
            units.AddRange(_gameplayManager.OpponentPlayer.BoardCards);

            Vector3 position = Vector3.left * 2f;

            _vfxController.CreateVfx(
                _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/RetreatVFX"),
                position, delay: 6f);

            InternalTools.DoActionDelayed(() =>
            {
                foreach (BoardUnitView unit in units)
                {
                    _vfxController.CreateVfx(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/PushVFX"), unit); // retreat vfx

                    _cardsController.ReturnCardToHand(unit);

                    TargetEffects.Add(new PastActionsPopup.TargetEffectParam()
                    {
                        ActionEffectType = Enumerators.ActionEffectType.ReturnToHand,
                        Target = unit
                    });
                }
            }, 2f);
            InternalTools.DoActionDelayed(() =>
            {
                _actionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam()
                {
                    ActionType = Enumerators.ActionType.UseOverlordPowerOnMultilpleCards,
                    Caller = boardSkill,
                    TargetEffects = TargetEffects
                });
            }, 4f);
        }

        // TOXIC

        private void ToxicPowerAction(Player owner, BoardSkill boardSkill, HeroSkill skill, BoardObject target)
        {
            if (target != null && target is BoardUnitModel unit)
            {
                _battleController.AttackUnitBySkill(owner, boardSkill, unit, 0);

                unit.BuffedDamage += skill.Attack;
                unit.CurrentDamage += skill.Attack;

                _vfxController.CreateVfx(
                    _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/ToxicPowerVFX"),
                    unit, isIgnoreCastVfx: true);

                _battlegroundController.GetBoardUnitViewByModel(unit).EnabledToxicPowerGlow();

                _actionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam()
                {
                    ActionType = Enumerators.ActionType.UseOverlordPowerOnCard,
                    Caller = boardSkill,
                    TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                    {
                        new PastActionsPopup.TargetEffectParam()
                        {
                            ActionEffectType = Enumerators.ActionEffectType.AttackBuff,
                            Target = target,
                            HasValue = true,
                            Value = skill.Attack
                        }
                    }
                });
            }
        }

        private void PoisonDartAction(Player owner, BoardSkill boardSkill, HeroSkill skill, BoardObject target)
        {
            AttackWithModifiers(owner, boardSkill, skill, target, Enumerators.SetType.TOXIC, Enumerators.SetType.LIFE);
            _vfxController.CreateVfx(
                _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/PoisonDart_ImpactVFX"),
                target, isIgnoreCastVfx: true);
            _soundManager.PlaySound(
                Enumerators.SoundType.OVERLORD_ABILITIES,
                skill.OverlordSkill.ToString().ToLowerInvariant() + "_Impact",
                Constants.OverlordAbilitySoundVolume,
                Enumerators.CardSoundType.NONE);

            Enumerators.ActionType actionType;

            switch (target)
            {
                case Player _:
                    actionType = Enumerators.ActionType.UseOverlordPowerOnOverlord;
                    break;
                case BoardUnitModel _:
                    actionType = Enumerators.ActionType.UseOverlordPowerOnCard;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(target), target, null);
            }

            _actionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = actionType,
                Caller = boardSkill,
                TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                {
                    new PastActionsPopup.TargetEffectParam()
                    {
                        ActionEffectType = Enumerators.ActionEffectType.ShieldDebuff,
                        Target = target,
                        HasValue = true,
                        Value = -skill.Value
                    }
                }
            });
        }

        private void BreakoutAction(Player owner, BoardSkill boardSkill, HeroSkill skill, BoardObject target)
        {
            List<PastActionsPopup.TargetEffectParam> TargetEffects = new List<PastActionsPopup.TargetEffectParam>();

            List<object> targets = new List<object>();

            Player opponent = _gameplayManager.GetOpponentByPlayer(owner);

            targets.Add(opponent);

            List<BoardUnitModel> boardCradsModels = opponent.BoardCards.Select((x) => x.Model).ToList();

            targets.AddRange(boardCradsModels);

             Dictionary<object, int> sortedTargets = GetRandomTargetsByAmount(targets, skill.Count);

            GameObject prefabMovedVfx = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/PoisonDartVFX");
            GameObject prefabImpactVfx = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/PoisonDart_ImpactVFX");

            int basicValue = skill.Value;
            int value = 0;

            foreach (object targetObject in sortedTargets.Keys)
            {
                _vfxController.CreateSkillVfx(
                prefabMovedVfx,
                boardSkill.SelfObject.transform.position,
                targetObject,
                (x) =>
                {
                    value = basicValue * sortedTargets[targetObject];

                    switch (targetObject)
                    {
                        case Player player:
                            _battleController.AttackPlayerBySkill(owner, boardSkill, player, value);
                            break;
                        case BoardUnitModel unit:
                            _battleController.AttackUnitBySkill(owner, boardSkill, unit, 0, value);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(targetObject), targetObject, null);
                    }

                    _vfxController.CreateVfx(
                        prefabImpactVfx,
                        targetObject, isIgnoreCastVfx: true);
                    _soundManager.PlaySound(
                        Enumerators.SoundType.OVERLORD_ABILITIES,
                        skill.Title.Trim().ToLowerInvariant() + "_Impact",
                        Constants.OverlordAbilitySoundVolume,
                        Enumerators.CardSoundType.NONE);

                    TargetEffects.Add(new PastActionsPopup.TargetEffectParam()
                    {
                        ActionEffectType = Enumerators.ActionEffectType.ShieldDebuff,
                        Target = targetObject,
                        HasValue = true,
                        Value = -skill.Value
                    });
                }, true);
            }

            _actionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.UseOverlordPowerOnCardsWithOverlord,
                Caller = boardSkill,
                TargetEffects = TargetEffects
            });
        }

        private void InfectAction(Player owner, BoardSkill boardSkill, HeroSkill skill, BoardObject target)
        {
            if (target != null && target is BoardUnitModel unit)
            {
                int unitAtk = unit.CurrentDamage;

                _vfxController.CreateVfx(
                _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/InfectVFX"),
                target, delay: 8f, isIgnoreCastVfx:true);

                InternalTools.DoActionDelayed(() =>
                {
                    _vfxController.CreateVfx(
                    _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/Infect_ExplosionVFX"),
                    target, delay: 6f, isIgnoreCastVfx: true);
                    _battlegroundController.DestroyBoardUnit(unit, false);

                    List<BoardUnitView> opponentUnits = _gameplayManager.GetOpponentByPlayer(owner).BoardCards;

                    if (opponentUnits.Count == 0)
                        return;

                    BoardUnitView targetUnit = opponentUnits[UnityEngine.Random.Range(0, opponentUnits.Count)];
                    
                    _vfxController.CreateSkillVfx(
                    _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/PoisonDartVFX"),
                    _battlegroundController.GetBoardUnitViewByModel(unit).Transform.position,
                    targetUnit,
                    (x) =>
                    {
                        _vfxController.CreateVfx(
                        _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/PoisonDart_ImpactVFX"),
                        targetUnit, isIgnoreCastVfx: true);
                        _battleController.AttackUnitBySkill(owner, boardSkill, targetUnit.Model, 0);

                        _actionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam()
                        {
                            ActionType = Enumerators.ActionType.UseOverlordPowerOnCard,
                            Caller = boardSkill,
                            TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                            {
                                new PastActionsPopup.TargetEffectParam()
                                {
                                    ActionEffectType = Enumerators.ActionEffectType.ShieldDebuff,
                                    Target = target,
                                    HasValue = true,
                                    Value = -unitAtk
                                },
                                new PastActionsPopup.TargetEffectParam()
                                {
                                    ActionEffectType = Enumerators.ActionEffectType.DeathMark,
                                    Target = unit,
                                }
                            }
                        });
                    }, true);                    
                }, 3.5f);
            }
        }

        private void EpidemicAction(Player owner, BoardSkill boardSkill, HeroSkill skill, BoardObject target)
        {
            List<PastActionsPopup.TargetEffectParam> TargetEffects = new List<PastActionsPopup.TargetEffectParam>();

            List<BoardUnitView> units = owner.BoardCards.FindAll(x => x.Model.Card.LibraryCard.CardSetType == Enumerators.SetType.TOXIC);

            if (units.Count == 0)
                return;

            units = InternalTools.GetRandomElementsFromList(units, skill.Count);
            List<BoardUnitView> opponentUnits =
                InternalTools.GetRandomElementsFromList(_gameplayManager.GetOpponentByPlayer(owner).BoardCards, skill.Count);

            int unitAtk = 0;

            BoardUnitView opponentUnitView = null;
            BoardUnitView unitView = null;

            for (int i = 0; i < units.Count; i++)
            {
                unitView = units[i];
                unitAtk = units[i].Model.CurrentDamage;                   

                TargetEffects.Add(new PastActionsPopup.TargetEffectParam()
                {
                    ActionEffectType = Enumerators.ActionEffectType.DeathMark,
                    Target = units[i]
                });

                if (opponentUnits.Count > 0)
                {
                    opponentUnitView = opponentUnits[UnityEngine.Random.Range(0, opponentUnits.Count)];

                    TargetEffects.Add(new PastActionsPopup.TargetEffectParam()
                    {
                        ActionEffectType = Enumerators.ActionEffectType.ShieldDebuff,
                        Target = opponentUnitView,
                        HasValue = true,
                        Value = -unitAtk
                    });

                    opponentUnits.Remove(opponentUnitView);

                    EpidemicUnit(owner, boardSkill, skill, unitView, opponentUnitView);
                }
            }

            _actionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.UseOverlordPowerOnMultilpleCards,
                Caller = boardSkill,
                TargetEffects = TargetEffects
            });
        }

        private void EpidemicUnit(Player owner, BoardSkill boardSkill, HeroSkill skill, BoardUnitView unit, BoardUnitView target)
        {
            int unitAtk = unit.Model.CurrentDamage;

            _vfxController.CreateVfx(
            _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/InfectVFX"),
            unit, delay: 8f, isIgnoreCastVfx: true);

            InternalTools.DoActionDelayed(() =>
            {
                _vfxController.CreateVfx(
                _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/Infect_ExplosionVFX"),
                unit, delay: 6f, isIgnoreCastVfx: true);
                _battlegroundController.DestroyBoardUnit(unit.Model, false);

                _vfxController.CreateSkillVfx(
                _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/PoisonDartVFX"),
                _battlegroundController.GetBoardUnitViewByModel(unit.Model).Transform.position,
                target,
                (x) =>
                {
                    _vfxController.CreateVfx(
                    _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/PoisonDart_ImpactVFX"),
                    target);
                    _battleController.AttackUnitBySkill(owner, boardSkill, target.Model, 0, unitAtk);
                }, true);
            }, 3.5f);
        }

        // LIFE

        private void HealingTouchAction(Player owner, BoardSkill boardSkill, HeroSkill skill, BoardObject target)
        {
            if (target is Player player)
            {
                _battleController.HealPlayerBySkill(owner, boardSkill, player);

                _vfxController.CreateVfx(
                    _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/HealingTouchVFX"),
                    player);
                _soundManager.PlaySound(
                    Enumerators.SoundType.OVERLORD_ABILITIES,
                    skill.OverlordSkill.ToString().ToLowerInvariant(),
                    Constants.OverlordAbilitySoundVolume,
                    Enumerators.CardSoundType.NONE);
            }
            else
            {
                BoardUnitModel unit = (BoardUnitModel) target;

                _battleController.HealUnitBySkill(owner, boardSkill, unit);

                _vfxController.CreateVfx(
                    _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/HealingTouchVFX"),
                    unit);
                _soundManager.PlaySound(
                    Enumerators.SoundType.OVERLORD_ABILITIES,
                    skill.OverlordSkill.ToString().ToLowerInvariant(),
                    Constants.OverlordAbilitySoundVolume,
                    Enumerators.CardSoundType.NONE);
            }

            _actionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.UseOverlordPowerOnCard,
                Caller = boardSkill,
                TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                {
                    new PastActionsPopup.TargetEffectParam()
                    {
                        ActionEffectType = Enumerators.ActionEffectType.ShieldBuff,
                        Target = target,
                        HasValue = true,
                        Value = skill.Value
                    }
                }
            });
        }

        private void MendAction(Player owner, BoardSkill boardSkill, HeroSkill skill, BoardObject target)
        {
            owner.Defense = Mathf.Clamp(owner.Defense + skill.Value, 0, owner.MaxCurrentHp);

            // TODO: remove this empty gameobject logic
            Transform transform = new GameObject().transform;
            transform.position = owner.AvatarObject.transform.position;
            transform.position += Vector3.up * 2;
            _vfxController.CreateVfx(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/MendVFX"), transform);
            _soundManager.PlaySound(
                Enumerators.SoundType.OVERLORD_ABILITIES,
                skill.OverlordSkill.ToString().ToLowerInvariant(),
                Constants.OverlordAbilitySoundVolume,
                Enumerators.CardSoundType.NONE);

            _actionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.UseOverlordPower,
                Caller = boardSkill,
                TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                {
                   new PastActionsPopup.TargetEffectParam()
                   {
                       ActionEffectType = Enumerators.ActionEffectType.ShieldBuff,
                       Target = owner,
                       HasValue = true,
                       Value = skill.Value
                   }
                }
            });
        }

        private void RessurectAction(Player owner, BoardSkill boardSkill, HeroSkill skill, BoardObject target)
        {
            List<PastActionsPopup.TargetEffectParam> TargetEffects = new List<PastActionsPopup.TargetEffectParam>();

            List<WorkingCard> cards = owner.CardsInGraveyard.FindAll(x => x.LibraryCard.CardSetType == Enumerators.SetType.LIFE
                && x.LibraryCard.CardKind == Enumerators.CardKind.CREATURE
                && x.InstanceCard.Cost == skill.Value);

            cards = InternalTools.GetRandomElementsFromList(cards, skill.Count);

            BoardUnitView unit = null;

            foreach (WorkingCard card in cards)
            {
                unit = _cardsController.SpawnUnitOnBoard(owner, card.LibraryCard.Name, onComplete: () =>
                {
                    _vfxController.CreateVfx(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/ResurrectVFX"), unit, delay: 6, isIgnoreCastVfx: true);
                    InternalTools.DoActionDelayed(() =>
                    {
                        unit.ChangeModelVisibility(true);
                    }, 3f);

                    TargetEffects.Add(new PastActionsPopup.TargetEffectParam()
                    {
                        ActionEffectType = Enumerators.ActionEffectType.SpawnOnBoard,
                        Target = target,
                    });
                });
                unit.ChangeModelVisibility(false);
            }

            _actionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.UseOverlordPowerOnMultilpleCards,
                Caller = boardSkill,
                TargetEffects = TargetEffects
            });
        }

        private void EnhanceAction(Player owner, BoardSkill boardSkill, HeroSkill skill, BoardObject target)
        {
            List<PastActionsPopup.TargetEffectParam> targetEffects = new List<PastActionsPopup.TargetEffectParam>();

            List<object> targets = new List<object>();

            targets.Add(owner);
            targets.AddRange(owner.BoardCards.FindAll(x => x.Model.Card.LibraryCard.CardSetType == Enumerators.SetType.LIFE));

            foreach (object targetObject in targets)
            {
                switch (targetObject)
                {
                    case BoardUnitView unit:
                        {
                            _battleController.HealUnitBySkill(owner, boardSkill, unit.Model);
                            _vfxController.CreateVfx(
                            _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/HealingTouchVFX"), unit);
                            _soundManager.PlaySound(
                                Enumerators.SoundType.OVERLORD_ABILITIES,
                                skill.OverlordSkill.ToString().ToLowerInvariant(),
                                Constants.OverlordAbilitySoundVolume,
                                Enumerators.CardSoundType.NONE);
                        }
                        break;
                    case Player player:
                        {
                            _battleController.HealPlayerBySkill(owner, boardSkill, player);
                            Transform transform = new GameObject().transform;
                            transform.position = owner.AvatarObject.transform.position;
                            transform.position += Vector3.up * 2.25f;
                            _vfxController.CreateVfx(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/MendVFX"), transform);
                            _soundManager.PlaySound(
                                Enumerators.SoundType.OVERLORD_ABILITIES,
                                skill.OverlordSkill.ToString().ToLowerInvariant(),
                                Constants.OverlordAbilitySoundVolume,
                                Enumerators.CardSoundType.NONE);
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(target), target, null);
                }

                targetEffects.Add(new PastActionsPopup.TargetEffectParam()
                {
                    ActionEffectType = Enumerators.ActionEffectType.LifeGain,
                    Target = targetObject,
                    HasValue = true,
                    Value = skill.Value
                });
            }

            _actionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.UseOverlordPowerOnCardsWithOverlord,
                Caller = boardSkill,
                TargetEffects = targetEffects
            });
        }

        private void ReanimateAction(Player owner, BoardSkill boardSkill, HeroSkill skill, BoardObject target)
        {
            List<PastActionsPopup.TargetEffectParam> TargetEffects = new List<PastActionsPopup.TargetEffectParam>();

            List<WorkingCard> cards = owner.CardsInGraveyard.FindAll(x => x.LibraryCard.CardSetType == Enumerators.SetType.LIFE
                && x.LibraryCard.CardKind == Enumerators.CardKind.CREATURE);

            cards = InternalTools.GetRandomElementsFromList(cards, skill.Count);

            List<BoardUnitView> units = new List<BoardUnitView>();

            foreach (WorkingCard card in cards)
            {
                units.Add(_cardsController.SpawnUnitOnBoard(owner, card.LibraryCard.Name, onComplete: () =>
                {
                    ReanimateUnit(units);
                }));
                units[units.Count - 1].ChangeModelVisibility(false);

                TargetEffects.Add(new PastActionsPopup.TargetEffectParam()
                {
                    ActionEffectType = Enumerators.ActionEffectType.Reanimate,
                    Target = target
                });
            }

            _actionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.UseOverlordPowerOnMultilpleCards,
                Caller = boardSkill,
                TargetEffects = TargetEffects
            });
        }

        private void ReanimateUnit(List<BoardUnitView> units)
        {
            foreach (var unit in units)
            {
                _vfxController.CreateVfx(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/ResurrectVFX"), unit, delay: 6, isIgnoreCastVfx: true);
                InternalTools.DoActionDelayed(() =>
                {
                    unit.ChangeModelVisibility(true);
                }, 3f);
            }
        }

        // WATER

        private void FreezeAction(Player owner, BoardSkill boardSkill, HeroSkill skill, BoardObject target)
        {
            Enumerators.ActionType actionType;

            switch (target)
            {
                case BoardUnitModel unit:
                    unit.Stun(Enumerators.StunType.FREEZE, skill.Value);

                    _vfxController.CreateVfx(
                        _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/Freeze_ImpactVFX"),
                        unit, isIgnoreCastVfx: true);

                    _soundManager.PlaySound(
                        Enumerators.SoundType.OVERLORD_ABILITIES,
                        skill.OverlordSkill.ToString().ToLowerInvariant() + "_Impact",
                        Constants.OverlordAbilitySoundVolume,
                        Enumerators.CardSoundType.NONE);

                    actionType = Enumerators.ActionType.UseOverlordPowerOnCard;

                    break;
                case Player player:
                    player.Stun(Enumerators.StunType.FREEZE, skill.Value);

                    _vfxController.CreateVfx(
                        _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/Freeze_ImpactVFX"),
                        player, isIgnoreCastVfx: true);
                    _soundManager.PlaySound(
                        Enumerators.SoundType.OVERLORD_ABILITIES,
                        skill.OverlordSkill.ToString().ToLowerInvariant() + "_Impact",
                        Constants.OverlordAbilitySoundVolume,
                        Enumerators.CardSoundType.NONE);

                    actionType = Enumerators.ActionType.UseOverlordPowerOnOverlord;

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(target), target, null);
            }

            _actionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = actionType,
                Caller = boardSkill,
                TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                {
                    new PastActionsPopup.TargetEffectParam()
                    {
                        ActionEffectType = Enumerators.ActionEffectType.Freeze,
                        Target = target
                    }
                }
            });
        }

        private void IceBoltAction(Player owner, BoardSkill boardSkill, HeroSkill skill, BoardObject target)
        {
            if (target != null && target is BoardUnitModel unit)
            {
                _battleController.AttackUnitBySkill(owner, boardSkill, unit, 0);

                if (unit.CurrentHp > 0)
                {
                    unit.Stun(Enumerators.StunType.FREEZE, 1);
                }

                _vfxController.CreateVfx(
                    _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/IceBolt_Impact"),
                    unit, isIgnoreCastVfx: true);
                _soundManager.PlaySound(
                    Enumerators.SoundType.OVERLORD_ABILITIES,
                    skill.OverlordSkill.ToString().ToLowerInvariant() + "_Impact",
                    Constants.OverlordAbilitySoundVolume,
                    Enumerators.CardSoundType.NONE);
            }

            _actionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.UseOverlordPowerOnCard,
                Caller = boardSkill,
                TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                {
                    new PastActionsPopup.TargetEffectParam()
                    {
                        ActionEffectType = Enumerators.ActionEffectType.ShieldDebuff,
                        Target = target,
                        HasValue = true,
                        Value = -skill.Value
                    },
                    new PastActionsPopup.TargetEffectParam()
                    {
                        ActionEffectType = Enumerators.ActionEffectType.Freeze,
                        Target = target
                    }
                }
            });
        }

        private void IceWallAction(Player owner, BoardSkill boardSkill, HeroSkill skill, BoardObject target)
        {
            Enumerators.ActionType actionType;

            _vfxController.CreateVfx(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/IceWallVFX"), target, delay: 8, isIgnoreCastVfx: true);

            InternalTools.DoActionDelayed(() =>
            {
                switch (target)
                {
                    case BoardUnitModel unit:
                        unit.BuffedHp += skill.Value;
                        unit.CurrentHp += skill.Value;
                        actionType = Enumerators.ActionType.UseOverlordPowerOnCard;
                        break;
                    case Player player:
                        _battleController.HealPlayerBySkill(owner, boardSkill, player);
                        actionType = Enumerators.ActionType.UseOverlordPowerOnOverlord;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(target), target, null);
                }

                _soundManager.PlaySound(
                    Enumerators.SoundType.OVERLORD_ABILITIES,
                    skill.Title.Trim().ToLowerInvariant(),
                    Constants.OverlordAbilitySoundVolume,
                    Enumerators.CardSoundType.NONE);


                _actionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam()
                {
                    ActionType = actionType,
                    Caller = boardSkill,
                    TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                    {
                        new PastActionsPopup.TargetEffectParam()
                        {
                            ActionEffectType = Enumerators.ActionEffectType.ShieldBuff,
                            Target = target,
                            HasValue = true,
                            Value = skill.Value
                        }
                    }
                });
            }, 2f);
        }

        private void ShatterAction(Player owner, BoardSkill boardSkill, HeroSkill skill, BoardObject target)
        {
            _vfxController.CreateVfx(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/Shatter_ImpactVFX"), target, isIgnoreCastVfx: true);

            if (target is BoardUnitModel boardUnitModel)
            {
                Vector3 position = _battlegroundController.GetBoardUnitViewByModel((BoardUnitModel)target).Transform.position + Vector3.up * 0.34f;

                boardUnitModel.LastAttackingSetType = owner.SelfHero.HeroElement;
                _battlegroundController.DestroyBoardUnit(boardUnitModel, false);

                _soundManager.PlaySound(
                    Enumerators.SoundType.OVERLORD_ABILITIES,
                    skill.OverlordSkill.ToString().ToLowerInvariant() + "_Impact",
                    Constants.OverlordAbilitySoundVolume,
                    Enumerators.CardSoundType.NONE);
            }

            _actionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.UseOverlordPowerOnCard,
                Caller = boardSkill,
                TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                {
                    new PastActionsPopup.TargetEffectParam()
                    {
                        ActionEffectType = Enumerators.ActionEffectType.DeathMark,
                        Target = target
                    }
                }
            });
        }

        private void BlizzardAction(Player owner, BoardSkill boardSkill, HeroSkill skill, BoardObject target)
        {
            List<PastActionsPopup.TargetEffectParam> TargetEffects = new List<PastActionsPopup.TargetEffectParam>();

            List<BoardUnitView> units = _gameplayManager.GetOpponentByPlayer(owner).BoardCards;
            units = InternalTools.GetRandomElementsFromList(units, skill.Count);

            _vfxController.CreateVfx(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/BlizzardVFX"), Vector3.zero, true, 8);

            GameObject prefabFreeze = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/Blizzard_Freeze");
            Vector3 targetPosition = Vector3.zero;

            foreach (BoardUnitView unit in units)
            {
                targetPosition = unit.Transform.position + Vector3.up * 0.7f;

                _vfxController.CreateVfx(prefabFreeze, targetPosition, true, 6);

                InternalTools.DoActionDelayed(() =>
                {
                    unit.Model.Stun(Enumerators.StunType.FREEZE, skill.Value);

                    TargetEffects.Add(new PastActionsPopup.TargetEffectParam()
                    {
                        ActionEffectType = Enumerators.ActionEffectType.Freeze,
                        Target = unit
                    });
                }, 3.5f);
            }

            _soundManager.PlaySound(
                Enumerators.SoundType.OVERLORD_ABILITIES,
                skill.Title.Trim().ToLowerInvariant(),
                Constants.OverlordAbilitySoundVolume,
                Enumerators.CardSoundType.NONE);

            _actionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.UseOverlordPowerOnMultilpleCards,
                Caller = boardSkill,
                TargetEffects = TargetEffects
            });
        }

        // FIRE

        private void FireBoltAction(Player owner, BoardSkill boardSkill, HeroSkill skill, BoardObject target)
        {
            AttackWithModifiers(owner, boardSkill, skill, target, Enumerators.SetType.FIRE, Enumerators.SetType.TOXIC);
            _vfxController.CreateVfx(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/FireBolt_ImpactVFX"), target);
            _soundManager.PlaySound(
                Enumerators.SoundType.OVERLORD_ABILITIES,
                skill.OverlordSkill.ToString().ToLowerInvariant() + "_Impact",
                Constants.OverlordAbilitySoundVolume,
                Enumerators.CardSoundType.NONE);

            Enumerators.ActionType actionType;

            switch (target)
            {
                case Player _:
                    actionType = Enumerators.ActionType.UseOverlordPowerOnOverlord;
                    break;
                case BoardUnitModel _:
                    actionType = Enumerators.ActionType.UseOverlordPowerOnCard;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(target), target, null);
            }

            _actionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = actionType,
                Caller = boardSkill,
                TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                {
                    new PastActionsPopup.TargetEffectParam()
                    {
                        ActionEffectType = Enumerators.ActionEffectType.ShieldDebuff,
                        Target = target,
                        HasValue = true,
                        Value = -skill.Value
                    }
                }
            });
        }

        private void RabiesAction(Player owner, BoardSkill boardSkill, HeroSkill skill, BoardObject target)
        {
            if (target != null && target is BoardUnitModel unit)
            {
                unit.SetAsFeralUnit();

                _vfxController.CreateVfx(
                    _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/RabiesVFX"),
                    unit, delay: 14f, isIgnoreCastVfx: true);
                _soundManager.PlaySound(
                    Enumerators.SoundType.OVERLORD_ABILITIES,
                    skill.OverlordSkill.ToString().ToLowerInvariant(),
                    Constants.OverlordAbilitySoundVolume,
                    Enumerators.CardSoundType.NONE);

                _actionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam()
                {
                    ActionType = Enumerators.ActionType.UseOverlordPowerOnCard,
                    Caller = boardSkill,
                    TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                    {
                        new PastActionsPopup.TargetEffectParam()
                        {
                            ActionEffectType = Enumerators.ActionEffectType.Feral,
                            Target = target
                        }
                    }
                });
            }
        }

        private void FireballAction(Player owner, BoardSkill boardSkill, HeroSkill skill, BoardObject target)
        {
            AttackWithModifiers(owner, boardSkill, skill, target, Enumerators.SetType.FIRE, Enumerators.SetType.TOXIC);

            _vfxController.CreateVfx(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/FireBall_ImpactVFX"), target, isIgnoreCastVfx: true); // vfx Fireball
            _soundManager.PlaySound(
                Enumerators.SoundType.OVERLORD_ABILITIES,
                skill.Title.Trim().ToLowerInvariant(),
                Constants.OverlordAbilitySoundVolume,
                Enumerators.CardSoundType.NONE);

            Enumerators.ActionType actionType;

            switch (target)
            {
                case Player _:
                    actionType = Enumerators.ActionType.UseOverlordPowerOnOverlord;
                    break;
                case BoardUnitModel _:
                    actionType = Enumerators.ActionType.UseOverlordPowerOnCard;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(target), target, null);
            }

            _actionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = actionType,
                Caller = boardSkill,
                TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                {
                    new PastActionsPopup.TargetEffectParam()
                    {
                        ActionEffectType = Enumerators.ActionEffectType.ShieldDebuff,
                        Target = target,
                        HasValue = true,
                        Value = -skill.Value
                    }
                }
            });
        }

        private void MassRabiesAction(Player owner, BoardSkill boardSkill, HeroSkill skill, BoardObject target)
        {
            List<PastActionsPopup.TargetEffectParam> TargetEffects = new List<PastActionsPopup.TargetEffectParam>();

            List<BoardUnitView> units = owner.BoardCards.FindAll((x) => !x.Model.HasFeral &&
            x.Model.Card.LibraryCard.CardSetType == owner.SelfHero.HeroElement);
            units = InternalTools.GetRandomElementsFromList(units, skill.Value);

            foreach (BoardUnitView unit in units)
            {
                unit.Model.SetAsFeralUnit();

                _vfxController.CreateVfx(
                    _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/RabiesVFX"),
                    unit, delay: 14f, isIgnoreCastVfx: true);
                _soundManager.PlaySound(
                    Enumerators.SoundType.OVERLORD_ABILITIES,
                    skill.Title.Trim().ToLowerInvariant(),
                    Constants.OverlordAbilitySoundVolume,
                    Enumerators.CardSoundType.NONE);

                TargetEffects.Add(new PastActionsPopup.TargetEffectParam()
                {
                    ActionEffectType = Enumerators.ActionEffectType.Feral,
                    Target = unit
                });
            }

            _actionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.UseOverlordPowerOnMultilpleCards,
                Caller = boardSkill,
                TargetEffects = TargetEffects
            });
        }

        private void MeteorShowerAction(Player owner, BoardSkill boardSkill, HeroSkill skill, BoardObject target)
        {
            List<PastActionsPopup.TargetEffectParam> TargetEffects = new List<PastActionsPopup.TargetEffectParam>();

            List<BoardUnitModel> units = new List<BoardUnitModel>();
            List<BoardUnitView> unitsViews = new List<BoardUnitView>();

            unitsViews.AddRange(_gameplayManager.CurrentPlayer.BoardCards);
            unitsViews.AddRange(_gameplayManager.OpponentPlayer.BoardCards);

            units = unitsViews.Select((x) => x.Model).ToList();

            GameObject vfxObject = null;

            foreach (BoardUnitModel unit in units)
            {
                InternalTools.DoActionDelayed(() =>
                {
                    AttackWithModifiers(owner, boardSkill, skill, unit, Enumerators.SetType.FIRE, Enumerators.SetType.TOXIC);
                }, 2.5f);

                vfxObject = Object.Instantiate(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/MeteorShowerVFX"));
                vfxObject.transform.position =  _battlegroundController.GetBoardUnitViewByModel(unit).Transform.position;
                _gameplayManager.GetController<ParticlesController>().RegisterParticleSystem(vfxObject, true, 8);

                TargetEffects.Add(new PastActionsPopup.TargetEffectParam()
                {
                    ActionEffectType = Enumerators.ActionEffectType.ShieldDebuff,
                    Target = unit,
                    HasValue = true,
                    Value = -skill.Value
                });
            }

            _actionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.UseOverlordPowerOnMultilpleCards,
                Caller = boardSkill,
                TargetEffects = TargetEffects
            });
        }

        // EARTH

        private void StoneskinAction(Player owner, BoardSkill boardSkill, HeroSkill skill, BoardObject target)
        {
            if (target != null && target is BoardUnitModel unit)
            {
                unit.BuffedHp += skill.Value;
                unit.CurrentHp += skill.Value;

                Vector3 position = _battlegroundController.GetBoardUnitViewByModel(unit).Transform.position;
                position -= Vector3.up * 3.7f;

                _vfxController.CreateVfx(
                    _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/HardenStoneSkinVFX"),
                    position, isIgnoreCastVfx:true);
                _soundManager.PlaySound(
                    Enumerators.SoundType.OVERLORD_ABILITIES,
                    skill.OverlordSkill.ToString().ToLowerInvariant(),
                    Constants.OverlordAbilitySoundVolume,
                    Enumerators.CardSoundType.NONE);

                _actionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam()
                {
                    ActionType = Enumerators.ActionType.UseOverlordPowerOnCard,
                    Caller = boardSkill,
                    TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                    {
                        new PastActionsPopup.TargetEffectParam()
                        {
                            ActionEffectType = Enumerators.ActionEffectType.ShieldBuff,
                            Target = target,
                            HasValue = true,
                            Value = skill.Value
                        }
                    }
                });
            }
        }

        private void HardenAction(Player owner, BoardSkill boardSkill, HeroSkill skill, BoardObject target)
        {
            _battleController.HealPlayerBySkill(owner, boardSkill, owner);

            Vector3 position = owner.AvatarObject.transform.position;
            position -= Vector3.up * 3.7f;

            _vfxController.CreateVfx(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/HardenStoneSkinVFX"),
                position, isIgnoreCastVfx: true);

            _actionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.UseOverlordPower,
                Caller = boardSkill,
                TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                {
                    new PastActionsPopup.TargetEffectParam()
                    {
                        ActionEffectType = Enumerators.ActionEffectType.ShieldBuff,
                        Target = owner,
                        HasValue = true,
                        Value = skill.Value
                    }
                }
            });
        }

        private void FortifyAction(Player owner, BoardSkill boardSkill, HeroSkill skill, BoardObject target)
        {
            if (target != null && target is BoardUnitModel unit)
            {
                unit.SetAsHeavyUnit();

                _vfxController.CreateVfx(
                    _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/FortressVFX"),
                    unit, isIgnoreCastVfx: true);

                _soundManager.PlaySound(
                    Enumerators.SoundType.OVERLORD_ABILITIES,
                    skill.OverlordSkill.ToString().ToLowerInvariant(),
                    Constants.OverlordAbilitySoundVolume,
                    Enumerators.CardSoundType.NONE);

                _actionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam()
                {
                    ActionType = Enumerators.ActionType.UseOverlordPowerOnCard,
                    Caller = boardSkill,
                    TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                    {
                        new PastActionsPopup.TargetEffectParam()
                        {
                            ActionEffectType = Enumerators.ActionEffectType.Heavy,
                            Target = target
                        }
                    }
                });
            }
        }

        private void PhalanxAction(Player owner, BoardSkill boardSkill, HeroSkill skill, BoardObject target)
        {
            List<PastActionsPopup.TargetEffectParam> TargetEffects = new List<PastActionsPopup.TargetEffectParam>();

            List<BoardUnitView> units = owner.BoardCards.FindAll(x => x.Model.Card.LibraryCard.CardSetType == Enumerators.SetType.EARTH);

            Vector3 position;
            foreach (BoardUnitView unit in units)
            {
                unit.Model.BuffedHp += skill.Value;
                unit.Model.CurrentHp += skill.Value;

                position = unit.Transform.position;
                position -= Vector3.up * 3.3f;

                _vfxController.CreateVfx(
                    _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/HardenStoneSkinVFX"),
                    position, delay: 8f, isIgnoreCastVfx: true); // vfx phalanx
                _soundManager.PlaySound(
                    Enumerators.SoundType.OVERLORD_ABILITIES,
                    skill.Title.Trim().ToLowerInvariant(),
                    Constants.OverlordAbilitySoundVolume,
                    Enumerators.CardSoundType.NONE);

                TargetEffects.Add(new PastActionsPopup.TargetEffectParam()
                {
                    ActionEffectType = Enumerators.ActionEffectType.ShieldBuff,
                    Target = unit,
                    HasValue = true,
                    Value = skill.Value
                });
            }

            _actionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.UseOverlordPowerOnMultilpleCards,
                Caller = boardSkill,
                TargetEffects = TargetEffects
            });
        }

        private void FortressAction(Player owner, BoardSkill boardSkill, HeroSkill skill, BoardObject target)
        {
            List<PastActionsPopup.TargetEffectParam> TargetEffects = new List<PastActionsPopup.TargetEffectParam>();

            List<BoardUnitView> units =
                InternalTools.GetRandomElementsFromList(
                    owner.BoardCards.FindAll(x => x.Model.Card.LibraryCard.CardSetType == Enumerators.SetType.EARTH),
                    skill.Count);

            foreach (BoardUnitView unit in units)
            {
                unit.Model.SetAsHeavyUnit();

                _vfxController.CreateVfx(
                    _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/FortressVFX"), unit.Transform.position, true, 6f);

                TargetEffects.Add(new PastActionsPopup.TargetEffectParam()
                {
                    ActionEffectType = Enumerators.ActionEffectType.Heavy,
                    Target = unit
                });
            }

            _actionsQueueController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.UseOverlordPowerOnMultilpleCards,
                Caller = boardSkill,
                TargetEffects = TargetEffects
            });
        }

        #endregion

    }
}
