using System;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using UnityEngine.TestTools;
using System.Linq;

namespace Loom.ZombieBattleground.Test.MultiplayerTests
{
    public class ToxicCardsTests : BaseIntegrationTest
    {
        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator RelentleZZ()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 0,
                    new DeckCardData("RelentleZZ", 10)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 0,
                    new DeckCardData("RelentleZZ", 10)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck)
                {
                    Player1HasFirstTurn = true
                };

                InstanceId playerCardId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "RelentleZZ", 1);
                InstanceId opponentCardId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "RelentleZZ", 1);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                {
                       player => {},
                       opponent => {},
                       player => {},
                       opponent => {},
                       player => player.CardPlay(playerCardId, ItemPosition.Start),
                       opponent => opponent.CardPlay(opponentCardId, ItemPosition.Start),
                };

                Action validateEndState = () =>
                {
                    Assert.AreEqual(2, ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerCardId)).CurrentHp);
                    Assert.AreEqual(2, ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentCardId)).CurrentHp);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Zpitter()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 0,
                    new DeckCardData("Zpitter", 1),
                    new DeckCardData("Burrrnn", 10)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 0,
                    new DeckCardData("Zpitter", 1),
                    new DeckCardData("Burrrnn", 10)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck)
                {
                    Player1HasFirstTurn = true
                };

                InstanceId playerBurnId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Burrrnn", 1);
                InstanceId playerBurn2Id = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Burrrnn", 2);
                InstanceId playerZpitterId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Zpitter", 1);

                InstanceId opponentBurnId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Burrrnn", 1);
                InstanceId opponentBurn2Id = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Burrrnn", 2);
                InstanceId opponentZpitterId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Zpitter", 1);
                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                   {
                       player => {},
                       opponent => {},
                       player => {},
                       opponent => {},
                       player =>
                       {
                           player.CardPlay(playerBurnId, ItemPosition.Start);
                           player.CardPlay(playerBurn2Id, ItemPosition.Start);
                       },
                       opponent =>
                       {
                           opponent.CardPlay(opponentBurnId, ItemPosition.Start);
                           opponent.CardPlay(opponentBurn2Id, ItemPosition.Start);
                       },
                       player => player.CardPlay(playerZpitterId, ItemPosition.Start),
                       opponent => opponent.CardPlay(opponentZpitterId, ItemPosition.Start),
                   };

                Action validateEndState = () =>
                {
                    Assert.IsNotNull(TestHelper.BattlegroundController.PlayerBoardCards.Select(card => card.Model.CurrentHp == card.Model.InitialHp - 1));
                    Assert.IsNotNull(TestHelper.BattlegroundController.OpponentBoardCards.Select(card => card.Model.CurrentHp == card.Model.InitialHp - 1));
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Ectoplasm()
        {
            return AsyncTest(async () =>
            {

                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 4,
                    new DeckCardData("Ectoplasm", 10)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 4,
                    new DeckCardData("Ectoplasm", 10)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck)
                {
                    Player1HasFirstTurn = true
                };

                InstanceId playerCardId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Ectoplasm", 1);
                InstanceId opponentCardId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Ectoplasm", 1);
                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                   {
                       player => {},
                       opponent => {},
                       player =>
                       {
                           player.CardPlay(playerCardId, ItemPosition.Start);
                           Assert.AreEqual(3, pvpTestContext.GetCurrentPlayer().GooVials);
                       },
                       opponent =>
                       {
                           opponent.CardPlay(opponentCardId, ItemPosition.Start);
                           Assert.AreEqual(3, pvpTestContext.GetOpponentPlayer().GooVials);
                       }
                   };

                Action validateEndState = () =>
                {
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Ghoul()
        {
            return AsyncTest(async () =>
            {

                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 0,
                    new DeckCardData("Ghoul", 10)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 0,
                    new DeckCardData("Ghoul", 10)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck)
                {
                    Player1HasFirstTurn = true
                };

                InstanceId playerCardId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Ghoul", 1);
                InstanceId opponentCardId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Ghoul", 1);
                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                   {
                       player => {},
                       opponent => {},
                       player =>
                       {
                           player.CardAbilityUsed(playerCardId, Enumerators.AbilityType.CHANGE_STAT, new List<ParametrizedAbilityInstanceId>());
                           player.CardAbilityUsed(playerCardId, Enumerators.AbilityType.CHANGE_STAT, new List<ParametrizedAbilityInstanceId>());
                           player.CardPlay(playerCardId, ItemPosition.Start);
                       },
                       opponent =>
                       {
                           opponent.CardAbilityUsed(opponentCardId, Enumerators.AbilityType.CHANGE_STAT, new List<ParametrizedAbilityInstanceId>());
                           opponent.CardAbilityUsed(opponentCardId, Enumerators.AbilityType.CHANGE_STAT, new List<ParametrizedAbilityInstanceId>());
                           opponent.CardPlay(opponentCardId, ItemPosition.Start);
                       },
                       player => player.CardAttack(playerCardId, pvpTestContext.GetOpponentPlayer().InstanceId),
                       opponent => opponent.CardAttack(opponentCardId, pvpTestContext.GetCurrentPlayer().InstanceId),
                       player => {},
                       opponent => {}
                   };

                Action validateEndState = () =>
                {
                    Assert.AreEqual(pvpTestContext.GetCurrentPlayer().InitialHp - 3, pvpTestContext.GetCurrentPlayer().Defense);
                    Assert.AreEqual(pvpTestContext.GetOpponentPlayer().InitialHp - 3, pvpTestContext.GetOpponentPlayer().Defense);
                    BoardUnitModel playerUnit = (BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerCardId);
                    BoardUnitModel opponentUnit = (BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerCardId);
                    Assert.AreEqual(playerUnit.InitialHp - 1, playerUnit.CurrentHp);
                    Assert.AreEqual(opponentUnit.InitialHp - 1, opponentUnit.CurrentHp);
                    Assert.AreEqual(playerUnit.InitialDamage - 1, playerUnit.CurrentDamage);
                    Assert.AreEqual(opponentUnit.InitialDamage - 1, opponentUnit.CurrentDamage);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Wazte()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 4,
                    new DeckCardData("Wazte", 10)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 4,
                    new DeckCardData("Wazte", 10)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck)
                {
                    Player1HasFirstTurn = true
                };

                InstanceId playerCardId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Wazte", 1);
                InstanceId opponentCardId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Wazte", 1);
                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                   {
                       player => {},
                       opponent => {},
                       player =>
                       {
                           player.CardPlay(playerCardId, ItemPosition.Start);
                           Assert.AreEqual(3, pvpTestContext.GetCurrentPlayer().GooVials);
                       },
                       opponent =>
                       {
                           opponent.CardPlay(opponentCardId, ItemPosition.Start);
                           Assert.AreEqual(3, pvpTestContext.GetOpponentPlayer().GooVials);
                       }
                   };

                Action validateEndState = () =>
                {
                    Assert.AreEqual(true, ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerCardId)).IsHeavyUnit);
                    Assert.AreEqual(true, ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentCardId)).IsHeavyUnit);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Poizom()
        {
            return AsyncTest(async () =>
            {

                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 0,
                    new DeckCardData("Poizom", 2),
                    new DeckCardData("Wood", 2),
                    new DeckCardData("Slab", 10)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 0,
                    new DeckCardData("Poizom", 2),
                    new DeckCardData("Wood", 2),
                    new DeckCardData("Slab", 10)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck)
                {
                    Player1HasFirstTurn = true
                };

                InstanceId playerPoizomId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Poizom", 1);
                InstanceId playerPoizom2Id = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Poizom", 2);
                InstanceId playerWoodId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Wood", 1);
                InstanceId playerSlabId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Slab", 1);
                InstanceId opponentPoizomId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Poizom", 1);
                InstanceId opponentPoizom2Id = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Poizom", 2);
                InstanceId opponentWoodId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Wood", 1);
                InstanceId opponentSlabId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Slab", 1);


                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                   {
                       player => {},
                       opponent => {},
                       player => {},
                       opponent => {},
                       player =>
                       {
                           player.CardPlay(playerPoizomId, ItemPosition.Start);
                           player.CardPlay(playerPoizom2Id, ItemPosition.Start);
                           player.CardPlay(playerWoodId, ItemPosition.Start);
                           player.CardPlay(playerSlabId, ItemPosition.Start);
                       },
                       opponent =>
                        {
                           opponent.CardPlay(opponentPoizomId, ItemPosition.Start);
                           opponent.CardPlay(opponentPoizom2Id, ItemPosition.Start);
                           opponent.CardPlay(opponentWoodId, ItemPosition.Start);
                           opponent.CardPlay(opponentSlabId, ItemPosition.Start);
                       },
                       player =>
                       {
                           player.CardAttack(playerPoizomId, opponentWoodId);
                           player.CardAttack(playerPoizom2Id, opponentSlabId);
                       },
                       opponent =>
                       {
                           opponent.CardAttack(opponentPoizomId, playerWoodId);
                           opponent.CardAttack(opponentPoizom2Id, playerSlabId);
                       },
                       player => {},
                       opponent => {},
                   };

                Action validateEndState = () =>
                {
                    BoardUnitModel playerWoodUnit = (BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerWoodId);
                    BoardUnitModel playerSlabUnit = (BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerSlabId);
                    BoardUnitModel opponentWoodUnit = (BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentWoodId);
                    BoardUnitModel opponentSlabUnit = (BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentSlabId);

                    Assert.AreEqual(playerWoodUnit.InitialHp - 2, playerWoodUnit.CurrentHp);
                    Assert.AreEqual(playerSlabUnit.InitialHp - 1, playerSlabUnit.CurrentHp);
                    Assert.AreEqual(opponentWoodUnit.InitialHp - 2, opponentWoodUnit.CurrentHp);
                    Assert.AreEqual(opponentSlabUnit.InitialHp - 1, opponentSlabUnit.CurrentHp);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Azzazzin()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 0,
                    new DeckCardData("Azzazzin", 2),
                    new DeckCardData("Cerberus", 10)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 0,
                    new DeckCardData("Azzazzin", 2),
                    new DeckCardData("Cerberus", 10)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck)
                {
                    Player1HasFirstTurn = true
                };

                InstanceId playerCerberuzId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Cerberus", 1);
                InstanceId opponentCerberuzId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Cerberus", 1);
                InstanceId playerAzzazzinId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Azzazzin", 1);
                InstanceId opponentAzzazzinId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Azzazzin", 1);
                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                   {
                       player => {},
                       opponent => {},
                       player => {},
                       opponent => {},
                       player => player.CardPlay(playerCerberuzId, ItemPosition.Start),
                       opponent => opponent.CardPlay(opponentCerberuzId, ItemPosition.Start),
                       player =>
                       {
                           player.CardPlay(playerAzzazzinId, ItemPosition.Start);
                           player.CardAbilityUsed(playerAzzazzinId, Enumerators.AbilityType.DESTROY_TARGET_UNIT_AFTER_ATTACK, new List<ParametrizedAbilityInstanceId>());
                           player.CardAttack(playerAzzazzinId, opponentCerberuzId);
                       },
                       opponent =>
                       {
                           opponent.CardPlay(opponentAzzazzinId, ItemPosition.Start);
                           opponent.CardAbilityUsed(opponentAzzazzinId, Enumerators.AbilityType.DESTROY_TARGET_UNIT_AFTER_ATTACK, new List<ParametrizedAbilityInstanceId>());
                           opponent.CardAttack(opponentAzzazzinId, playerCerberuzId);
                       },
                       player => {},
                       opponent => {},
                   };

                Action validateEndState = () =>
                {
                    Assert.AreEqual(0, pvpTestContext.GetCurrentPlayer().BoardCards.Count);
                    Assert.AreEqual(0, pvpTestContext.GetOpponentPlayer().BoardCards.Count);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Hazzard()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 4,
                    new DeckCardData("Hazzard", 2),
                    new DeckCardData("Cerberus", 10)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 4,
                    new DeckCardData("Hazzard", 2),
                    new DeckCardData("Cerberus", 10)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck)
                {
                    Player1HasFirstTurn = true
                };

                InstanceId playerCerberusId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Cerberus", 1);
                InstanceId playerHazzardId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Hazzard", 1);
                InstanceId opponentCerberusId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Cerberus", 1);
                InstanceId opponentHazzardId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Hazzard", 1);
                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                   {
                       player => {},
                       opponent => {},
                       player =>
                       {
                           player.CardPlay(playerCerberusId, ItemPosition.Start);
                           player.CardPlay(playerHazzardId, ItemPosition.Start);
                           player.CardAbilityUsed(playerHazzardId, Enumerators.AbilityType.DESTROY_TARGET_UNIT_AFTER_ATTACK, new List<ParametrizedAbilityInstanceId>());

                       },
                       opponent =>
                       {
                           opponent.CardPlay(opponentCerberusId, ItemPosition.Start);
                           opponent.CardPlay(opponentHazzardId, ItemPosition.Start);
                           opponent.CardAbilityUsed(opponentHazzardId, Enumerators.AbilityType.DESTROY_TARGET_UNIT_AFTER_ATTACK, new List<ParametrizedAbilityInstanceId>());
                       },
                       player => player.CardAttack(playerHazzardId, opponentCerberusId),
                       opponent => opponent.CardAttack(opponentHazzardId, playerCerberusId),
                       player => {},
                       opponent => {},
                   };

                Action validateEndState = () =>
                {
                    Assert.AreEqual(0, TestHelper.BattlegroundController.PlayerBoardCards.Count);
                    Assert.AreEqual(0, TestHelper.BattlegroundController.OpponentBoardCards.Count);
                    Assert.IsTrue(pvpTestContext.GetCurrentPlayer().CardsInHand.FindAll(x => x.LibraryCard.MouldId == 156).Count > 0);
                    Assert.IsTrue(pvpTestContext.GetOpponentPlayer().CardsInHand.FindAll(x => x.LibraryCard.MouldId == 156).Count > 0);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Zlimey()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 0,
                    new DeckCardData("Zlimey", 10)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 0,
                    new DeckCardData("Zlimey", 10)
                );
               
                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck)
                {
                    Player1HasFirstTurn = true
                };

                InstanceId playerCardId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Zlimey", 1);
                InstanceId opponentCardId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Zlimey", 1);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                   {
                       player => player.CardPlay(playerCardId, ItemPosition.Start),
                       opponent => opponent.CardPlay(opponentCardId, ItemPosition.Start),
                   };

                Action validateEndState = () =>
                {
                    Assert.AreEqual(pvpTestContext.GetCurrentPlayer().InitialHp - 2, pvpTestContext.GetCurrentPlayer().Defense);
                    Assert.AreEqual(pvpTestContext.GetOpponentPlayer().InitialHp - 2, pvpTestContext.GetOpponentPlayer().Defense);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Kabomb_Just_Local()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 0,
                    new DeckCardData("Kabomb", 1),
                    new DeckCardData("Slab", 10)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 0,
                    new DeckCardData("Kabomb", 1),
                    new DeckCardData("Slab", 10)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck)
                {
                    Player1HasFirstTurn = true
                };

                InstanceId playerKabombId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Kabomb", 1);
                InstanceId playerSlabId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Slab", 1);
                InstanceId opponentKabombId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Kabomb", 1);
                InstanceId opponentSlabId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Slab", 1);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                {
                       player => 
                       {
                           player.CardPlay(playerKabombId, ItemPosition.Start);
                       },
                       opponent => 
                       {
                           opponent.CardPlay(opponentSlabId, ItemPosition.Start);
                       },
                       player => 
                       {
                           player.CardAttack(playerKabombId, opponentSlabId);
                       },
                       opponent => {},
                       player => {}
                };

                Action validateEndState = () =>
                {
                    Assert.IsTrue(TestHelper.GetOpponentPlayer().Defense == TestHelper.GetOpponentPlayer().InitialHp-5
                    || (TestHelper.BattlegroundController.OpponentBoardCards.Select(card => card.Model.Card.LibraryCard.MouldId == 101)).ToList().Count == 0);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState, false);
            }, 600);
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Hazmaz()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 0,
                    new DeckCardData("Hazmaz", 10)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 0,
                    new DeckCardData("Hazmaz", 10)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck)
                {
                    Player1HasFirstTurn = true
                };

                InstanceId playerCardId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Hazmaz", 1);
                InstanceId opponentCardId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Hazmaz", 1);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                {
                       player => {},
                       opponent => {},
                       player => {},
                       opponent => {},
                       player => player.CardPlay(playerCardId, ItemPosition.Start),
                       opponent => opponent.CardPlay(opponentCardId, ItemPosition.Start),
                };

                Action validateEndState = () =>
                {
                    Assert.AreEqual(true, ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerCardId)).HasFeral);
                    Assert.AreEqual(true, ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentCardId)).HasFeral);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Zeptic()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 0,
                    new DeckCardData("Zeptic", 10)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 0,
                    new DeckCardData("Zeptic", 10)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck)
                {
                    Player1HasFirstTurn = true
                };

                InstanceId playerCardId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Zeptic", 1);
                InstanceId opponentCardId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Zeptic", 1);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                   {
                       player => {},
                       opponent => {},
                       player => {},
                       opponent => {},
                       player => player.CardPlay(playerCardId, ItemPosition.Start),
                       opponent => opponent.CardPlay(opponentCardId, ItemPosition.Start)
                   };

                Action validateEndState = () =>
                {
                    Assert.AreEqual(pvpTestContext.GetCurrentPlayer().InitialHp - 2, pvpTestContext.GetCurrentPlayer().Defense);
                    Assert.AreEqual(pvpTestContext.GetOpponentPlayer().InitialHp - 2, pvpTestContext.GetOpponentPlayer().Defense);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Germ()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 4,
                    new DeckCardData("Germ", 10)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 4,
                    new DeckCardData("Germ", 10)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck)
                {
                    Player1HasFirstTurn = true
                };

                InstanceId playerCardId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Germ", 1);
                InstanceId opponentCardId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Germ", 1);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                   {
                       player => {},
                       opponent => {},
                       player => {},
                       opponent => {},
                       player => player.CardPlay(playerCardId, ItemPosition.Start),
                       opponent => opponent.CardPlay(opponentCardId, ItemPosition.Start)
                   };

                Action validateEndState = () =>
                {
                    Assert.IsTrue(pvpTestContext.GetCurrentPlayer().CardsInHand.FindAll(x => x.LibraryCard.MouldId == 155).Count > 0);
                    Assert.IsTrue(pvpTestContext.GetOpponentPlayer().CardsInHand.FindAll(x => x.LibraryCard.MouldId == 155).Count > 0);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Zcavenger()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 0,
                    new DeckCardData("Zcavenger", 20),
                    new DeckCardData("Boomstick", 1)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 0,
                    new DeckCardData("Zcavenger", 20),
                    new DeckCardData("Boomstick", 1)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck)
                {
                    Player1HasFirstTurn = true
                };

                InstanceId playerCardId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Zcavenger", 1);
                InstanceId opponentCardId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Zcavenger", 1);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                   {
                       player => {},
                       opponent => {},
                       player => {},
                       opponent => {},
                       player => player.CardPlay(playerCardId, ItemPosition.Start),
                       opponent => opponent.CardPlay(opponentCardId, ItemPosition.Start)
                   };

                Action validateEndState = () =>
                {
                    Assert.IsNotNull((TestHelper.BattlegroundController.PlayerHandCards.Select(card => card.LibraryCard.CardSetType == Enumerators.SetType.ITEM)));
                    Assert.IsNotNull((TestHelper.BattlegroundController.OpponentHandCards.Select(card => card.WorkingCard.LibraryCard.CardSetType == Enumerators.SetType.ITEM)));
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Scab()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 0,
                    new DeckCardData("Scab", 10)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 0,
                    new DeckCardData("Scab", 10)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck)
                {
                    Player1HasFirstTurn = true
                };

                InstanceId playerCardId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Scab", 1);
                InstanceId opponentCardId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Scab", 1);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                {
                       player => {},
                       opponent => {},
                       player => {},
                       opponent => {},
                       player => player.CardPlay(playerCardId, ItemPosition.Start),
                       opponent => opponent.CardPlay(opponentCardId, ItemPosition.Start),
                       player => {},
                       opponent => {}
                };

                Action validateEndState = () =>
                {
                    Assert.AreEqual(true, ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerCardId)).HasFeral);
                    Assert.AreEqual(true, ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentCardId)).HasFeral);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Spikez()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 0,
                    new DeckCardData("Spikez", 1),
                    new DeckCardData("Slab", 10)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 0,
                    new DeckCardData("Spikez", 1),
                    new DeckCardData("Slab", 10)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck)
                {
                    Player1HasFirstTurn = true
                };

                InstanceId playerSpikezId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Spikez", 1);
                InstanceId playerSlabId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Slab", 1);
                InstanceId opponentSpikezId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Spikez", 1);
                InstanceId opponentSlabId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Slab", 1);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                {
                       player => {},
                       opponent => {},
                       player => {},
                       opponent => {},
                       player =>
                       {
                           player.CardPlay(playerSpikezId, ItemPosition.Start);
                           player.CardAbilityUsed(playerSpikezId, Enumerators.AbilityType.MODIFICATOR_STATS, new List<ParametrizedAbilityInstanceId>());
                           player.CardPlay(playerSlabId, ItemPosition.Start);
                       },
                       opponent =>
                       {
                           opponent.CardPlay(opponentSpikezId, ItemPosition.Start);
                           opponent.CardAbilityUsed(opponentSpikezId, Enumerators.AbilityType.MODIFICATOR_STATS, new List<ParametrizedAbilityInstanceId>());
                           opponent.CardPlay(opponentSlabId, ItemPosition.Start);
                       },
                       player => player.CardAttack(playerSlabId, opponentSpikezId),
                       opponent => opponent.CardAttack(opponentSlabId, playerSpikezId),
                       player => {},
                       opponent => {},
                };

                Action validateEndState = () =>
                {
                    Assert.AreEqual(0, TestHelper.BattlegroundController.PlayerBoardCards.Count);
                    Assert.AreEqual(0, TestHelper.BattlegroundController.OpponentBoardCards.Count);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Bane()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 0,
                    new DeckCardData("Bane", 10)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 0,
                    new DeckCardData("Bane", 10)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck)
                {
                    Player1HasFirstTurn = true
                };

                InstanceId playerCardId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Bane", 1);
                InstanceId opponentCardId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Bane", 1);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                   {
                       player => player.CardPlay(playerCardId, ItemPosition.Start),
                       opponent => opponent.CardPlay(opponentCardId, ItemPosition.Start),
                   };

                Action validateEndState = () =>
                {
                    Assert.AreEqual(true, ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerCardId)).HasFeral);
                    Assert.AreEqual(true, ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentCardId)).HasFeral);
                    Assert.AreEqual(pvpTestContext.GetCurrentPlayer().InitialHp - 1, pvpTestContext.GetCurrentPlayer().Defense);
                    Assert.AreEqual(pvpTestContext.GetOpponentPlayer().InitialHp - 1, pvpTestContext.GetOpponentPlayer().Defense);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Polluter()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 0,
                    new DeckCardData("Polluter", 1),
                    new DeckCardData("Spiker", 10)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 0,
                    new DeckCardData("Polluter", 1),
                    new DeckCardData("Spiker", 10)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck)
                {
                    Player1HasFirstTurn = true
                };

                InstanceId playerPolluterId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Polluter", 1);
                InstanceId playerSpikerbId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Spiker", 1);
                InstanceId opponentPolluterId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Polluter", 1);
                InstanceId opponentSpikerId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Spiker", 1);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                {
                       player => {},
                       opponent => {},
                       player => {},
                       opponent => {},
                       player =>
                       {
                           player.CardPlay(playerPolluterId, ItemPosition.Start);
                           player.CardAbilityUsed(playerPolluterId, Enumerators.AbilityType.ADD_GOO_VIAL, new List<ParametrizedAbilityInstanceId>());
                           player.CardPlay(playerSpikerbId, ItemPosition.Start);
                       },
                       opponent =>
                       {
                           opponent.CardPlay(opponentPolluterId, ItemPosition.Start);
                           opponent.CardAbilityUsed(opponentPolluterId, Enumerators.AbilityType.ADD_GOO_VIAL, new List<ParametrizedAbilityInstanceId>());
                           opponent.CardPlay(opponentSpikerId, ItemPosition.Start);
                       },
                       player =>
                       {
                            player.CardAttack(playerSpikerbId, opponentPolluterId);
                            
                       },
                       opponent =>
                       {
                            Assert.AreEqual(5, pvpTestContext.GetOpponentPlayer().GooVials);
                            opponent.CardAttack(opponentSpikerId, playerPolluterId);
                       },
                       player => Assert.AreEqual(6, pvpTestContext.GetCurrentPlayer().GooVials),
                };

                Action validateEndState = () =>
                {
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Ztink()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 4,
                    new DeckCardData("Ztink", 2),
                    new DeckCardData("Slab", 10)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 4,
                    new DeckCardData("Ztink", 2),
                    new DeckCardData("Slab", 10)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck)
                {
                    Player1HasFirstTurn = true
                };

                InstanceId playerSlabId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Slab", 1);
                InstanceId playerZtinkId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Ztink", 1);
                InstanceId opponentSlabId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Slab", 1);
                InstanceId opponentZtinkId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Ztink", 1);
                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                   {
                       player => {},
                       opponent => {},
                       player =>
                       {
                           player.CardPlay(playerSlabId, ItemPosition.Start);
                           player.CardPlay(playerZtinkId, ItemPosition.Start);
                           player.CardAbilityUsed(playerZtinkId, Enumerators.AbilityType.DESTROY_TARGET_UNIT_AFTER_ATTACK, new List<ParametrizedAbilityInstanceId>());

                       },
                       opponent =>
                       {
                           opponent.CardPlay(opponentSlabId, ItemPosition.Start);
                           opponent.CardPlay(opponentZtinkId, ItemPosition.Start);
                           opponent.CardAbilityUsed(opponentZtinkId, Enumerators.AbilityType.DESTROY_TARGET_UNIT_AFTER_ATTACK, new List<ParametrizedAbilityInstanceId>());
                       },
                       player => player.CardAttack(playerZtinkId, opponentSlabId),
                       opponent => opponent.CardAttack(opponentZtinkId, playerSlabId),
                       player => {},
                       opponent => {},
                   };

                Action validateEndState = () =>
                {
                    Assert.AreEqual(0, TestHelper.BattlegroundController.PlayerBoardCards.Count);
                    Assert.AreEqual(0, TestHelper.BattlegroundController.OpponentBoardCards.Count);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator GooZilla()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 0,
                    new DeckCardData("GooZilla", 10)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 0,
                    new DeckCardData("GooZilla", 10)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck)
                {
                    Player1HasFirstTurn = true
                };

                InstanceId playerCardId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "GooZilla", 1);
                InstanceId opponentCardId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "GooZilla", 1);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                   {
                       player => {},
                       opponent => {},
                       player => {},
                       opponent => {},
                       player => {},
                       opponent => {},
                       player => {},
                       opponent => {},
                       player => {},
                       opponent => {},
                       player => {},
                       opponent => {},
                       player => {},
                       opponent => {},
                       player => {},
                       opponent => {},
                       player => {
                           player.CardPlay(playerCardId, ItemPosition.Start);
                           player.CardAbilityUsed(playerCardId, Enumerators.AbilityType.USE_ALL_GOO_TO_INCREASE_STATS, new List<ParametrizedAbilityInstanceId>());
                       },
                       opponent => {
                           opponent.CardPlay(opponentCardId, ItemPosition.Start);
                           opponent.CardAbilityUsed(opponentCardId, Enumerators.AbilityType.USE_ALL_GOO_TO_INCREASE_STATS, new List<ParametrizedAbilityInstanceId>());
                       },
                   };

                Action validateEndState = () =>
                {
                    Assert.AreEqual(10, ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerCardId)).CurrentDamage);
                    Assert.AreEqual(10, ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerCardId)).CurrentHp);
                    Assert.AreEqual(10, ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentCardId)).CurrentDamage);
                    Assert.AreEqual(10, ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentCardId)).CurrentHp);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Chernobill()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 4,
                    new DeckCardData("Cherno-bill", 1),
                    new DeckCardData("Spiker", 1),
                    new DeckCardData("Enrager", 10)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 4,
                    new DeckCardData("Cherno-bill", 1),
                    new DeckCardData("Spiker", 1),
                    new DeckCardData("Enrager", 10)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck)
                {
                    Player1HasFirstTurn = true
                };

                InstanceId playerChernobillId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Cherno-bill", 1);
                InstanceId playerSpikerId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Spiker", 1);
                InstanceId playerEnrager1Id = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Enrager", 1);
                InstanceId playerEnrager2Id = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Enrager", 2);
                InstanceId opponentChernobillId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Cherno-bill", 1);
                InstanceId opponentSpikerId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Spiker", 1);
                InstanceId opponentEnrager1Id = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Enrager", 1);
                InstanceId opponentEnrager2Id = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Enrager", 2);
                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                   {
                       player => {},
                       opponent => {},
                       player =>
                       {
                           player.CardPlay(playerSpikerId, ItemPosition.Start);
                           player.CardPlay(playerEnrager1Id, ItemPosition.Start);
                           player.CardPlay(playerEnrager2Id, ItemPosition.Start);
                           player.CardPlay(playerChernobillId, ItemPosition.Start);
                           player.CardAbilityUsed(playerChernobillId, Enumerators.AbilityType.DESTROY_TARGET_UNIT_AFTER_ATTACK, new List<ParametrizedAbilityInstanceId>());

                       },
                       opponent => {},
                       player => {},
                       opponent =>
                       {
                           opponent.CardPlay(opponentChernobillId, ItemPosition.Start);
                           opponent.CardAbilityUsed(opponentChernobillId, Enumerators.AbilityType.DESTROY_TARGET_UNIT_AFTER_ATTACK, new List<ParametrizedAbilityInstanceId>());
                       },
                       player => {},
                       opponent => {},
                       player =>
                       {
                           player.CardAttack(playerEnrager1Id, opponentChernobillId);
                           player.CardAttack(playerEnrager2Id, opponentChernobillId);
                       },
                       opponent =>
                       {
                           opponent.CardPlay(opponentSpikerId, ItemPosition.Start);
                           opponent.CardPlay(opponentEnrager1Id, ItemPosition.Start);
                           opponent.CardPlay(opponentEnrager2Id, ItemPosition.Start);
                           opponent.CardAttack(opponentEnrager1Id, playerChernobillId);
                           opponent.CardAttack(opponentEnrager1Id, playerChernobillId);
                       },
                       player => {},
                       opponent => {},
                   };

                Action validateEndState = () =>
                {
                    Assert.AreEqual(0, TestHelper.BattlegroundController.PlayerBoardCards.Count);
                    Assert.AreEqual(1, TestHelper.BattlegroundController.OpponentBoardCards.Count);
                    Assert.AreEqual(1, ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentSpikerId)).CurrentHp);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }


        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Zlopper()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 0,
                    new DeckCardData("Zlopper", 2),
                    new DeckCardData("Boomer", 2),
                    new DeckCardData("MonZoon", 10)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 0,
                    new DeckCardData("Zlopper", 2),
                    new DeckCardData("Boomer", 2),
                    new DeckCardData("MonZoon", 10)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck)
                {
                    Player1HasFirstTurn = true
                };

                InstanceId playerZlopperId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Zlopper", 1);
                InstanceId playerBoomerId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Boomer", 1);
                InstanceId playerBoomer2Id = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Boomer", 2);
                InstanceId playerMonZoonId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "MonZoon", 1);
                InstanceId opponentZlopperId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Zlopper", 1);
                InstanceId opponentBoomerId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Boomer", 1);
                InstanceId opponentBoomer2Id = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Boomer", 2);
                InstanceId opponentMonZoonId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "MonZoon", 1);
                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                   {
                       player => {},
                       opponent => {},
                       player =>
                       {
                           player.CardPlay(playerBoomerId, ItemPosition.Start);
                           player.CardPlay(playerBoomer2Id, ItemPosition.Start);
                           player.CardPlay(playerMonZoonId, ItemPosition.Start);
                           player.CardPlay(playerZlopperId, ItemPosition.Start);
                       },
                       opponent =>
                       {
                           opponent.CardPlay(opponentBoomerId, ItemPosition.Start);
                           opponent.CardPlay(opponentBoomer2Id, ItemPosition.Start);
                           opponent.CardPlay(opponentMonZoonId, ItemPosition.Start);
                           opponent.CardPlay(opponentZlopperId, ItemPosition.Start);
                       },
                       player => {},
                       opponent => {},
                   };

                Action validateEndState = () =>
                {
                    BoardUnitModel playerBoomerUnit = (BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerBoomerId);
                    BoardUnitModel playerBoomer2Unit = (BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerBoomer2Id);
                    BoardUnitModel playerMonZoonUnit = (BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerMonZoonId);
                    BoardUnitModel opponentBoomerUnit = (BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentBoomerId);
                    BoardUnitModel opponentBoomer2Unit = (BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentBoomer2Id);
                    BoardUnitModel opponentMonZoonUnit = (BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentMonZoonId);
                    Assert.AreEqual(playerBoomerUnit.InitialDamage + 1, playerBoomerUnit.CurrentDamage);
                    Assert.AreEqual(playerBoomer2Unit.InitialDamage + 1, playerBoomer2Unit.CurrentDamage);
                    Assert.AreEqual(playerMonZoonUnit.InitialDamage, playerMonZoonUnit.CurrentDamage);
                    Assert.AreEqual(opponentBoomerUnit.InitialDamage + 1, opponentBoomerUnit.CurrentDamage);
                    Assert.AreEqual(opponentBoomer2Unit.InitialDamage + 1, opponentBoomer2Unit.CurrentDamage);
                    Assert.AreEqual(playerMonZoonUnit.InitialDamage, playerMonZoonUnit.CurrentDamage);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Zeeter()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 0,
                    new DeckCardData("Zeeter", 1),
                    new DeckCardData("Boomer", 1),
                    new DeckCardData("MonZoon", 10)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 0,
                    new DeckCardData("Zeeter", 1),
                    new DeckCardData("Boomer", 1),
                    new DeckCardData("MonZoon", 10)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck)
                {
                    Player1HasFirstTurn = true
                };

                InstanceId playerZeeterId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Zeeter", 1);
                InstanceId playerBoomerId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Boomer", 1);
                InstanceId playerMonZoonId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "MonZoon", 1);
                InstanceId opponentZeeterId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Zeeter", 1);
                InstanceId opponentBoomerId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Boomer", 1);
                InstanceId opponentMonZoonId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "MonZoon", 1);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                   {
                       player => {},
                       opponent => {},
                       player =>
                       {
                           player.CardPlay(playerBoomerId, ItemPosition.Start);
                           player.CardPlay(playerMonZoonId, ItemPosition.Start);
                       },
                       opponent =>
                       {
                           opponent.CardPlay(opponentBoomerId, ItemPosition.Start);
                           opponent.CardPlay(opponentMonZoonId, ItemPosition.Start);
                       },
                       player => player.CardPlay(playerZeeterId, ItemPosition.Start, playerMonZoonId),
                       opponent => opponent.CardPlay(opponentZeeterId, ItemPosition.Start, opponentBoomerId),
                       player => {},
                       opponent => {},
                   };

                Action validateEndState = () =>
                {
                    BoardUnitModel playerZeeterUnit = (BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerZeeterId);
                    BoardUnitModel opponentZeeterUnit = (BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentZeeterId);

                    Assert.AreEqual(playerZeeterUnit.InitialDamage + 6, playerZeeterUnit.CurrentDamage);
                    Assert.AreEqual(playerZeeterUnit.InitialHp + 6, playerZeeterUnit.CurrentHp);
                    Assert.AreEqual(opponentZeeterUnit.InitialDamage + 6, opponentZeeterUnit.CurrentDamage);
                    Assert.AreEqual(opponentZeeterUnit.InitialHp + 6, opponentZeeterUnit.CurrentHp);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Kabomb()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 0,
                    new DeckCardData("Kabomb", 2),
                    new DeckCardData("Cerberus", 2),
                    new DeckCardData("MonZoon", 10)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 0,
                    new DeckCardData("Kabomb", 2),
                    new DeckCardData("Cerberus", 2),
                    new DeckCardData("MonZoon", 10)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck)
                {
                    Player1HasFirstTurn = true
                };

                InstanceId playerKabombId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Kabomb", 1);
                InstanceId playerCerberusId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Cerberus", 1);
                InstanceId playerMonZoonId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "MonZoon", 1);
                InstanceId opponentKabombrId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Kabomb", 1);
                InstanceId opponentCerberusId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Cerberus", 1);
                InstanceId opponentMonZoonId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "MonZoon", 1);
                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                   {
                       player => {},
                       opponent => {},
                       player =>
                       {
                           player.CardPlay(playerCerberusId, ItemPosition.Start);
                           player.CardPlay(playerMonZoonId, ItemPosition.Start);
                           player.CardPlay(playerKabombId, ItemPosition.Start);
                           player.CardAbilityUsed(playerKabombId, Enumerators.AbilityType.TAKE_DAMAGE_RANDOM_ENEMY, new List<ParametrizedAbilityInstanceId>());
                       },
                       opponent =>
                       {
                           opponent.CardPlay(opponentCerberusId, ItemPosition.Start);
                           opponent.CardPlay(opponentMonZoonId, ItemPosition.Start);
                           opponent.CardPlay(opponentKabombrId, ItemPosition.Start);
                           opponent.CardAbilityUsed(opponentKabombrId, Enumerators.AbilityType.TAKE_DAMAGE_RANDOM_ENEMY, new List<ParametrizedAbilityInstanceId>());
                       },
                       player =>  player.CardAttack(playerKabombId, opponentKabombrId),
                       opponent => opponent.CardAttack(opponentKabombrId, playerKabombId),
                       player => {},
                       opponent => {},
                   };

                Action validateEndState = () =>
                {
                    Assert.IsTrue(pvpTestContext.GetCurrentPlayer().BoardCards.FindAll(card => card.Model.CurrentHp == card.Model.InitialHp - 5).Count > 0);
                    Assert.IsTrue(pvpTestContext.GetOpponentPlayer().BoardCards.FindAll(card => card.Model.CurrentHp == card.Model.InitialHp - 5).Count > 0);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Zteroid()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 0,
                    new DeckCardData("Zteroid", 2),
                    new DeckCardData("Cerberus", 2),
                    new DeckCardData("MonZoon", 10)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 0,
                    new DeckCardData("Zteroid", 2),
                    new DeckCardData("Cerberus", 2),
                    new DeckCardData("MonZoon", 10)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck)
                {
                    Player1HasFirstTurn = true
                };

                InstanceId playerZteroidId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Zteroid", 1);
                InstanceId playerCerberusId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Cerberus", 1);
                InstanceId playerMonZoonId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "MonZoon", 1);
                InstanceId opponentZteroidId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Zteroid", 1);
                InstanceId opponentCerberusId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Cerberus", 1);
                InstanceId opponentMonZoonId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "MonZoon", 1);
                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                   {
                       player => {},
                       opponent => {},
                       player =>
                       {
                           player.CardPlay(playerCerberusId, ItemPosition.Start);
                           player.CardPlay(playerMonZoonId, ItemPosition.Start);
                           player.CardPlay(playerZteroidId, ItemPosition.Start);
                       },
                       opponent =>
                       {
                           opponent.CardPlay(opponentCerberusId, ItemPosition.Start);
                           opponent.CardPlay(opponentMonZoonId, ItemPosition.Start);
                           opponent.CardPlay(opponentZteroidId, ItemPosition.Start);
                       },
                       player =>
                       {
                           player.CardAttack(playerZteroidId, opponentZteroidId);
                           Assert.AreEqual(9, ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerCerberusId)).CurrentDamage);
                           Assert.AreEqual(8, ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerMonZoonId)).CurrentDamage);
                           Assert.AreEqual(5, ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerZteroidId)).CurrentDamage);
                       },
                       opponent => {
                           Assert.AreEqual(9, ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentCerberusId)).CurrentDamage);
                           Assert.AreEqual(8, ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentMonZoonId)).CurrentDamage);
                           Assert.AreEqual(5, ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentZteroidId)).CurrentDamage);
                       },
                   };

                Action validateEndState = () =>
                {
                    Assert.AreEqual(7, ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerCerberusId)).CurrentDamage);
                    Assert.AreEqual(6, ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerMonZoonId)).CurrentDamage);
                    Assert.AreEqual(5, ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerZteroidId)).CurrentDamage);
                    Assert.AreEqual(7, ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentCerberusId)).CurrentDamage);
                    Assert.AreEqual(6, ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentMonZoonId)).CurrentDamage);
                    Assert.AreEqual(5, ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentZteroidId)).CurrentDamage);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }


        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Boomer()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 0,
                    new DeckCardData("Boomer", 1),
                    new DeckCardData("MonZoon", 10)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 0,
                    new DeckCardData("Boomer", 1),
                    new DeckCardData("MonZoon", 10)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck)
                {
                    Player1HasFirstTurn = true
                };

                InstanceId playerBoomerId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Boomer", 1);
                InstanceId playerMonZoonId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "MonZoon", 1);
                InstanceId playerMonZoon2Id = pvpTestContext.GetCardInstanceIdByName(playerDeck, "MonZoon", 2);
                InstanceId playerMonZoon3Id = pvpTestContext.GetCardInstanceIdByName(playerDeck, "MonZoon", 3);
                InstanceId opponentBoomerId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Boomer", 1);
                InstanceId opponentMonZoonId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "MonZoon", 1);
                InstanceId opponentMonZoon2Id = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "MonZoon", 2);
                InstanceId opponentMonZoon3Id = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "MonZoon", 3);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                {
                    player => {},
                    opponent => {},
                    player =>
                    {
                        player.CardPlay(playerMonZoonId, ItemPosition.Start);
                        player.CardPlay(playerMonZoon2Id, ItemPosition.Start);
                        player.CardPlay(playerBoomerId, ItemPosition.Start);
                        player.CardPlay(playerMonZoon3Id, ItemPosition.Start);
                        player.CardAbilityUsed(playerBoomerId, Enumerators.AbilityType.ADJACENT_UNITS_GET_STAT, new List<ParametrizedAbilityInstanceId>());

                    },
                    opponent =>
                    {
                        opponent.CardPlay(opponentMonZoonId, ItemPosition.Start);
                        opponent.CardPlay(opponentMonZoon2Id, ItemPosition.Start);
                        opponent.CardPlay(opponentBoomerId, ItemPosition.Start);
                        opponent.CardPlay(opponentMonZoon3Id, ItemPosition.Start);
                        opponent.CardAbilityUsed(opponentBoomerId, Enumerators.AbilityType.ADJACENT_UNITS_GET_STAT, new List<ParametrizedAbilityInstanceId>());
                    },
                    player => player.CardAttack(playerMonZoonId, opponentBoomerId),
                    opponent => opponent.CardAttack(opponentMonZoonId, playerBoomerId),
                    player => {},
                    opponent => {},
                };

                Action validateEndState = () =>
                {
                    BoardUnitModel playerMonZoon2Unit = (BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerMonZoon2Id);
                    BoardUnitModel playerMonZoon3Unit = (BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerMonZoon3Id);
                    BoardUnitModel opponentMonZoon2Unit = (BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentMonZoon2Id);
                    BoardUnitModel opponentMonZoon3Unit = (BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentMonZoon3Id);

                    Assert.AreEqual(playerMonZoon2Unit.InitialDamage + 2, playerMonZoon2Unit.CurrentDamage);
                    Assert.AreEqual(playerMonZoon3Unit.InitialDamage + 2, playerMonZoon3Unit.CurrentDamage);
                    Assert.AreEqual(opponentMonZoon2Unit.InitialDamage + 2, opponentMonZoon2Unit.CurrentDamage);
                    Assert.AreEqual(opponentMonZoon3Unit.InitialDamage + 2, opponentMonZoon3Unit.CurrentDamage);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Zludge()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 0,
                    new DeckCardData("Zludge", 1),
                    new DeckCardData("Slab", 10)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 0,
                    new DeckCardData("Zludge", 1),
                    new DeckCardData("Slab", 10)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck)
                {
                    Player1HasFirstTurn = true
                };

                InstanceId playerZludgeId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Zludge", 1);
                InstanceId playerSlabId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Slab", 1);
                InstanceId opponentZludgeId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Zludge", 1);
                InstanceId opponentSlabId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Slab", 1);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                {
                    player => {},
                    opponent => {},
                    player =>
                    {
                        player.CardPlay(playerSlabId, ItemPosition.Start);
                        player.CardAbilityUsed(playerZludgeId, Enumerators.AbilityType.RAGE, new List<ParametrizedAbilityInstanceId>());
                        player.CardPlay(playerZludgeId, ItemPosition.Start);
                    },
                    opponent =>
                    {
                        opponent.CardPlay(opponentSlabId, ItemPosition.Start);
                        opponent.CardAbilityUsed(opponentZludgeId, Enumerators.AbilityType.RAGE, new List<ParametrizedAbilityInstanceId>());
                        opponent.CardPlay(opponentZludgeId, ItemPosition.Start);
                    },
                    player => player.CardAttack(playerSlabId, opponentZludgeId),
                    opponent => opponent.CardAttack(opponentSlabId, playerZludgeId),
                    player => {},
                    opponent => {},
                };

                Action validateEndState = () =>
                {
                    Assert.AreEqual(2, ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerZludgeId)).CurrentHp);
                    Assert.AreEqual(6, ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerZludgeId)).CurrentDamage);
                    Assert.AreEqual(2, ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentZludgeId)).CurrentHp);
                    Assert.AreEqual(6, ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentZludgeId)).CurrentDamage);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }
    }
}
