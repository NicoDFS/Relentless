using System;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Loom.ZombieBattleground.BackendCommunication;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using Loom.ZombieBattleground.Protobuf;
using UnityEngine;
using UnityEngine.TestTools;
using Deck = Loom.ZombieBattleground.Data.Deck;
using InstanceId = Loom.ZombieBattleground.Data.InstanceId;

namespace Loom.ZombieBattleground.Test
{
    public class MultiplayerTests : BaseIntegrationTest
    {
        [UnityTest]
        [Timeout(150 * 1000 * TestHelper.TestTimeScale)]
        public IEnumerator Slab()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = new Deck(
                    0,
                    0,
                    "test deck",
                    new List<DeckCardData>
                    {
                        new DeckCardData("Slab", 30)
                    },
                    Enumerators.OverlordSkill.NONE,
                    Enumerators.OverlordSkill.NONE
                );

                Deck opponentDeck = new Deck(
                    0,
                    0,
                    "test deck2",
                    new List<DeckCardData>
                    {
                        new DeckCardData("Slab", 30)
                    },
                    Enumerators.OverlordSkill.NONE,
                    Enumerators.OverlordSkill.NONE
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck) {
                    Player1HasFirstTurn = true
                };

                InstanceId playerSlabId = pvpTestContext.GetCardInstanceIdByIndex(playerDeck, 0);
                InstanceId opponentSlabId = pvpTestContext.GetCardInstanceIdByIndex(opponentDeck, 0);
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
                       player => player.CardPlay(playerSlabId, ItemPosition.Start),
                       opponent => opponent.CardPlay(opponentSlabId, ItemPosition.Start),
                       player => player.CardAttack(playerSlabId, pvpTestContext.GetOpponentPlayer().InstanceId),
                       opponent => opponent.CardAttack(opponentSlabId, pvpTestContext.GetCurrentPlayer().InstanceId),
                       player => player.CardAttack(playerSlabId, pvpTestContext.GetOpponentPlayer().InstanceId),
                       opponent => opponent.CardAttack(opponentSlabId, pvpTestContext.GetCurrentPlayer().InstanceId),
                       player => player.CardAttack(playerSlabId, pvpTestContext.GetOpponentPlayer().InstanceId),
                       opponent => {},
                       player => player.CardAttack(playerSlabId, pvpTestContext.GetOpponentPlayer().InstanceId),
                       opponent => {},
                       player => player.CardAttack(playerSlabId, pvpTestContext.GetOpponentPlayer().InstanceId),
                       opponent => {},
                       player => player.CardAttack(playerSlabId, pvpTestContext.GetOpponentPlayer().InstanceId),
                       opponent => {},
                       player => player.CardAttack(playerSlabId, pvpTestContext.GetOpponentPlayer().InstanceId),
                       opponent => {},
                       player => player.CardAttack(playerSlabId, pvpTestContext.GetOpponentPlayer().InstanceId),
                       opponent => {},
                       player => player.CardAttack(playerSlabId, pvpTestContext.GetOpponentPlayer().InstanceId),
                       opponent => {},
                       player => player.CardAttack(playerSlabId, pvpTestContext.GetOpponentPlayer().InstanceId),
                   };

                await PvPTestUtility.GenericPvPTest(
                    pvpTestContext,
                    turns,
                    () =>
                    {
                        // FIXME: references to the players are nulled immediately after the game ends,
                        // so we can't assert the state at that moment?
                        //Assert.AreEqual(0, pvpTestContext.GetOpponentPlayer().Defense);
                    }
                );
            });
        }

        [UnityTest]
        [Timeout(150 * 1000 * TestHelper.TestTimeScale)]
        public IEnumerator Cynderman()
        {
            return AsyncTest(async () =>
            {
                Deck opponentDeck = new Deck(
                    0,
                    0,
                    "test deck",
                    new List<DeckCardData>
                    {
                        new DeckCardData("Cynderman", 2),
                        new DeckCardData("Slab", 2)
                    },
                    Enumerators.OverlordSkill.NONE,
                    Enumerators.OverlordSkill.NONE
                );

                Deck playerDeck = new Deck(
                    0,
                    0,
                    "test deck2",
                    new List<DeckCardData>
                    {
                        new DeckCardData("Cynderman", 2),
                        new DeckCardData("Slab", 2)
                    },
                    Enumerators.OverlordSkill.NONE,
                    Enumerators.OverlordSkill.NONE
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck) {
                    Player1HasFirstTurn = true
                };

                InstanceId playerSlabId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Slab", 1);
                InstanceId opponentSlabId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Slab", 1);
                InstanceId playerCyndermanId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Cynderman", 1);
                InstanceId opponentCyndermanId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Cynderman", 1);
                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                   {
                       player => {},
                       opponent => {},
                       player => {},
                       opponent => {},
                       player => player.CardPlay(playerSlabId, ItemPosition.Start),
                       opponent =>
                       {
                           opponent.CardPlay(opponentSlabId, ItemPosition.Start);
                           opponent.CardPlay(opponentCyndermanId, ItemPosition.Start, playerSlabId);
                       },
                       player =>
                       {
                           player.CardPlay(playerCyndermanId, ItemPosition.Start, opponentCyndermanId);
                       },
                   };

                Action validateEndState = () =>
                {
                    Assert.AreEqual(2, ((BoardUnitModel) TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerSlabId)).CurrentHp);
                    Assert.AreEqual(2, ((BoardUnitModel) TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentCyndermanId)).CurrentHp);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }


        [UnityTest]
        [Timeout(150 * 1000 * TestHelper.TestTimeScale)]
        public IEnumerator Bane()
        {
            return AsyncTest(async () =>
            {
                Deck opponentDeck = new Deck(
                    0,
                    0,
                    "test deck",
                    new List<DeckCardData>
                    {
                        new DeckCardData("Slab", 30)
                    },
                    Enumerators.OverlordSkill.NONE,
                    Enumerators.OverlordSkill.NONE
                );

                Deck playerDeck = new Deck(
                    0,
                    0,
                    "test deck2",
                    new List<DeckCardData>
                    {
                        new DeckCardData("Bane", 30)
                    },
                    Enumerators.OverlordSkill.NONE,
                    Enumerators.OverlordSkill.NONE
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck) {
                    Player1HasFirstTurn = true
                };

                InstanceId playerBaneId = pvpTestContext.GetCardInstanceIdByIndex(playerDeck, 0);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                   {
                       player => {},
                       opponent => {},
                       player => {},
                       opponent => {},
                       player => {},
                       opponent => {},

                       player => player.CardPlay(playerBaneId, ItemPosition.Start),
                       opponent => {}
                   };

                Action validateEndState = () =>
                {
                    Assert.AreEqual(19, pvpTestContext.GetCurrentPlayer().Defense);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }


        [UnityTest]
        [Timeout(150 * 1000 * TestHelper.TestTimeScale)]
        public IEnumerator Ectoplasm()
        {
            return AsyncTest(async () =>
            {
                Deck opponentDeck = new Deck(
                    0,
                    0,
                    "test deck",
                    new List<DeckCardData>
                    {
                        new DeckCardData("Slab", 30)
                    },
                    Enumerators.OverlordSkill.NONE,
                    Enumerators.OverlordSkill.NONE
                );

                Deck playerDeck = new Deck(
                    0,
                    0,
                    "test deck2",
                    new List<DeckCardData>
                    {
                        new DeckCardData("Ectoplasm", 30)
                    },
                    Enumerators.OverlordSkill.NONE,
                    Enumerators.OverlordSkill.NONE
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck) {
                    Player1HasFirstTurn = true
                };

                InstanceId playerEctoplasmId = pvpTestContext.GetCardInstanceIdByIndex(playerDeck, 0);
                InstanceId opponentSlabId = pvpTestContext.GetCardInstanceIdByIndex(opponentDeck, 0);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                   {
                       player => {},
                       opponent => {},

                       player => player.CardPlay(playerEctoplasmId, ItemPosition.Start),
                       opponent => {},
                   };

                Action validateEndState = () =>
                {
                    Assert.AreEqual(3, pvpTestContext.GetCurrentPlayer().GooVials);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }


        [UnityTest]
        [Timeout(150 * 1000 * TestHelper.TestTimeScale)]
        public IEnumerator Poizom()
        {
            return AsyncTest(async () =>
            {
                Deck opponentDeck = new Deck(
                    0,
                    0,
                    "test deck",
                    new List<DeckCardData>
                    {
                        new DeckCardData("Wood", 30)
                    },
                    Enumerators.OverlordSkill.NONE,
                    Enumerators.OverlordSkill.NONE
                );

                Deck playerDeck = new Deck(
                    0,
                    0,
                    "test deck2",
                    new List<DeckCardData>
                    {
                        new DeckCardData("Poizom", 30)
                    },
                    Enumerators.OverlordSkill.NONE,
                    Enumerators.OverlordSkill.NONE
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck) {
                    Player1HasFirstTurn = true
                };

                InstanceId playerPoizomId = pvpTestContext.GetCardInstanceIdByIndex(playerDeck, 0);
                InstanceId opponentWoodId = pvpTestContext.GetCardInstanceIdByIndex(opponentDeck, 0);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                   {
                       player => {},
                       opponent => {},
                       player => {},
                       opponent => {},

                       player => player.CardPlay(playerPoizomId, ItemPosition.Start),
                       opponent => opponent.CardPlay(opponentWoodId, ItemPosition.Start),

                       player => player.CardAttack(playerPoizomId, opponentWoodId),
                       opponent => {}
                   };

                Action validateEndState = () =>
                    {
                        Assert.AreEqual(1,
                            ((BoardUnitModel) TestHelper.BattlegroundController.GetBoardObjectByInstanceId(
                                opponentWoodId)).CurrentHp);
                    };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }


        [UnityTest]
        [Timeout(150 * 1000 * TestHelper.TestTimeScale)]
        public IEnumerator Zlimey()
        {
            return AsyncTest(async () =>
            {
                Deck opponentDeck = new Deck(
                    0,
                    0,
                    "test deck",
                    new List<DeckCardData>
                    {
                        new DeckCardData("Slab", 30)
                    },
                    Enumerators.OverlordSkill.NONE,
                    Enumerators.OverlordSkill.NONE
                );

                Deck playerDeck = new Deck(
                    0,
                    0,
                    "test deck2",
                    new List<DeckCardData>
                    {
                        new DeckCardData("Zlimey", 30)
                    },
                    Enumerators.OverlordSkill.NONE,
                    Enumerators.OverlordSkill.NONE
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck) {
                    Player1HasFirstTurn = true
                };

                InstanceId playerZlimeyId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Zlimey", 1);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                   {
                       player => player.CardPlay(playerZlimeyId, ItemPosition.Start),
                       opponent => {}
                   };

                Action validateEndState = () =>
                {
                    Assert.AreEqual(18, pvpTestContext.GetCurrentPlayer().Defense);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }

        [UnityTest]
        [Timeout(150 * 1000 * TestHelper.TestTimeScale)]
        public IEnumerator Zeptic_Lose()
        {
            return AsyncTest(async () =>
            {
                Deck opponentDeck = new Deck(
                    0,
                    0,
                    "test deck",
                    new List<DeckCardData>
                    {
                        new DeckCardData("Slab", 30)
                    },
                    Enumerators.OverlordSkill.NONE,
                    Enumerators.OverlordSkill.NONE
                );

                Deck playerDeck = new Deck(
                    0,
                    0,
                    "test deck2",
                    new List<DeckCardData>
                    {
                        new DeckCardData("Zeptic", 30)
                    },
                    Enumerators.OverlordSkill.NONE,
                    Enumerators.OverlordSkill.NONE
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck) {
                    Player1HasFirstTurn = true
                };

                InstanceId playerZepticId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Zeptic", 1);
                InstanceId playerZepticId2 = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Zeptic", 2);
                InstanceId opponentSlabId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Slab", 1);

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
                       opponent => opponent.CardPlay(opponentSlabId, ItemPosition.Start),

                       player => player.CardPlay(playerZepticId, ItemPosition.Start),
                       opponent => opponent.CardAttack(opponentSlabId, pvpTestContext.GetCurrentPlayer().InstanceId),

                       player => {},
                       opponent => opponent.CardAttack(opponentSlabId, pvpTestContext.GetCurrentPlayer().InstanceId),

                       player => {},
                       opponent => opponent.CardAttack(opponentSlabId, pvpTestContext.GetCurrentPlayer().InstanceId),

                       player => {},
                       opponent => opponent.CardAttack(opponentSlabId, pvpTestContext.GetCurrentPlayer().InstanceId),

                       player => {},
                       opponent => opponent.CardAttack(opponentSlabId, pvpTestContext.GetCurrentPlayer().InstanceId),

                       player => {},
                       opponent => opponent.CardAttack(opponentSlabId, pvpTestContext.GetCurrentPlayer().InstanceId),

                       player => {},
                       opponent => opponent.CardAttack(opponentSlabId, pvpTestContext.GetCurrentPlayer().InstanceId),

                       player => {},
                       opponent => opponent.CardAttack(opponentSlabId, pvpTestContext.GetCurrentPlayer().InstanceId),

                       player => player.CardPlay(playerZepticId2, ItemPosition.Start),
                       opponent => {}
                   };

                Action validateEndState = () =>
                {
                    Assert.AreEqual(0, pvpTestContext.GetCurrentPlayer().Defense);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }


        [UnityTest]
        [Timeout(150 * 1000 * TestHelper.TestTimeScale)]
        public IEnumerator Zeptic()
        {
            return AsyncTest(async () =>
            {
                Deck opponentDeck = new Deck(
                    0,
                    0,
                    "test deck",
                    new List<DeckCardData>
                    {
                        new DeckCardData("Slab", 30)
                    },
                    Enumerators.OverlordSkill.NONE,
                    Enumerators.OverlordSkill.NONE
                );

                Deck playerDeck = new Deck(
                    0,
                    0,
                    "test deck2",
                    new List<DeckCardData>
                    {
                        new DeckCardData("Zeptic", 30)
                    },
                    Enumerators.OverlordSkill.NONE,
                    Enumerators.OverlordSkill.NONE
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck) {
                    Player1HasFirstTurn = true
                };

                InstanceId playerZepticId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Zeptic", 1);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                   {
                       player => player.CardPlay(playerZepticId, ItemPosition.Start),
                       opponent => {}
                   };

                Action validateEndState = () =>
                {
                    Assert.NotNull(TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerZepticId));
                    Assert.AreEqual(1, ((BoardUnitModel) TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerZepticId)).CurrentHp);
                    Assert.AreEqual(18, pvpTestContext.GetCurrentPlayer().Defense);
                    Assert.AreEqual(3, pvpTestContext.GetCurrentPlayer().CurrentGoo);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }


        [UnityTest]
        [Timeout(150 * 1000 * TestHelper.TestTimeScale)]
        public IEnumerator CorrectCardDraw()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = new Deck(
                    0,
                    0,
                    "test deck",
                    new List<DeckCardData>
                    {
                        new DeckCardData("Slab", 30)
                    },
                    Enumerators.OverlordSkill.NONE,
                    Enumerators.OverlordSkill.NONE
                );

                Deck opponentDeck = new Deck(
                    0,
                    0,
                    "test deck2",
                    new List<DeckCardData>
                    {
                        new DeckCardData("Slab", 30)
                    },
                    Enumerators.OverlordSkill.NONE,
                    Enumerators.OverlordSkill.NONE
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck) {
                    Player1HasFirstTurn = true
                };

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                {
                    player =>
                    {
                        Assert.AreEqual(4, pvpTestContext.GetCurrentPlayer().CardsInHand.Count);
                        Assert.AreEqual(3, pvpTestContext.GetOpponentPlayer().CardsInHand.Count);
                    },
                    opponent =>
                    {
                        Assert.AreEqual(4, pvpTestContext.GetCurrentPlayer().CardsInHand.Count);
                        Assert.AreEqual(5, pvpTestContext.GetOpponentPlayer().CardsInHand.Count);
                    },
                    player =>
                    {
                        Assert.AreEqual(5, pvpTestContext.GetCurrentPlayer().CardsInHand.Count);
                        Assert.AreEqual(5, pvpTestContext.GetOpponentPlayer().CardsInHand.Count);
                    },
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, null);
            });
        }
    }
}
