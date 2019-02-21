using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Loom.ZombieBattleground.BackendCommunication;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using NUnit.Framework;
using UnityEngine;

namespace Loom.ZombieBattleground.Test
{
    public static class PvPTestUtility
    {
        private static TestHelper TestHelper => TestHelper.Instance;

        public static async Task GenericPvPTest(
            PvpTestContext pvpTestContext,
            IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns,
            Action validateEndStateAction,
            bool enableReverseMatch = true,
            bool enableBackendGameLogicMatch = false,
            bool enableClientGameLogicMatch = true,
            bool onlyReverseMatch = false
            )
        {
            void LogTestMode()
            {
                Debug.Log($"= RUNNING INTEGRATION TEST [{TestContext.CurrentTestExecutionContext.CurrentTest.Name}] Reverse: {pvpTestContext.IsReversed}, UseBackendLogic: {pvpTestContext.UseBackendLogic}");
            }

            async Task ExecuteTest()
            {
                LogTestMode();
                await GenericPvPTest(
                    turns,
                    pvpTestContext.Player1Deck,
                    () =>
                    {
                        bool player1HasFirstTurn = pvpTestContext.IsReversed ?
                            !pvpTestContext.Player1HasFirstTurn :
                            pvpTestContext.Player1HasFirstTurn;
                        TestHelper.DebugCheats.ForceFirstTurnUserId = player1HasFirstTurn ?
                            TestHelper.BackendDataControlMediator.UserDataModel.UserId :
                            TestHelper.GetOpponentDebugClient().UserDataModel.UserId;
                        TestHelper.DebugCheats.UseCustomDeck = true;
                        TestHelper.DebugCheats.CustomDeck = pvpTestContext.IsReversed ? pvpTestContext.Player2Deck : pvpTestContext.Player1Deck;
                        TestHelper.DebugCheats.DisableDeckShuffle = true;
                        TestHelper.DebugCheats.IgnoreGooRequirements = true;
                        TestHelper.DebugCheats.UseCustomDeck = true;
                        TestHelper.DebugCheats.CustomRandomSeed = 1337;

                        GameClient.Get<IPvPManager>().UseBackendGameLogic = pvpTestContext.UseBackendLogic;
                    },
                    cheats =>
                    {
                        cheats.UseCustomDeck = true;
                        cheats.CustomDeck = pvpTestContext.IsReversed ? pvpTestContext.Player1Deck : pvpTestContext.Player2Deck;
                        },
                    validateEndStateAction);
            }

            async Task ExecuteTestWithReverse()
            {
                if (!onlyReverseMatch)
                {
                    pvpTestContext.IsReversed = false;
                    await ExecuteTest();
                }

                if (enableReverseMatch)
                {
                    pvpTestContext.IsReversed = true;
                    await ExecuteTest();
                }
            }

            //enableBackendGameLogicMatch = false;
            if (!enableClientGameLogicMatch && !enableBackendGameLogicMatch)
                throw new Exception("At least one tests must be run");

            if (!enableReverseMatch && onlyReverseMatch)
                throw new Exception("!enableReverseMatch && onlyReverseMatch");

            if (enableClientGameLogicMatch)
            {
                pvpTestContext.UseBackendLogic = false;
                await ExecuteTestWithReverse();
            }

            if (enableBackendGameLogicMatch)
            {
                pvpTestContext.UseBackendLogic = true;
                await ExecuteTestWithReverse();
            }
        }

        private static async Task GenericPvPTest(
            IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns,
            Deck deck,
            Action setupAction,
            Action<DebugCheatsConfiguration> modifyOpponentDebugCheats,
            Action validateEndStateAction)
        {
            MatchScenarioPlayer matchScenarioPlayer = null;

            bool canceled = false;
            await TestHelper.CreateAndConnectOpponentDebugClient(
                async exception =>
                {
                    await GameClient.Get<IPvPManager>().StopMatchmaking();
                    matchScenarioPlayer?.AbortNextMoves();
                    canceled = true;
                }
                );
            setupAction?.Invoke();

            List<string> tags = new List<string>
            {
                "pvpTest",
                TestHelper.GetTestName(),
                Guid.NewGuid().ToString()
            };

            await StartOnlineMatch(tags, selectedHordeIndex: -1, createOpponent: false);

            GameClient.Get<IUIManager>().GetPage<GameplayPage>().CurrentDeckId = (int) deck.Id;
            GameClient.Get<IGameplayManager>().CurrentPlayerDeck = deck;
            await GameClient.Get<IMatchManager>().FindMatch();
            GameClient.Get<IPvPManager>().MatchMakingFlowController.ActionWaitingTime = 1;

            await TestHelper.MatchmakeOpponentDebugClient(modifyOpponentDebugCheats);

            Assert.IsFalse(canceled, "canceled");
            await TestHelper.WaitUntilPlayerOrderIsDecided();
            Assert.IsFalse(canceled, "canceled");

            using (matchScenarioPlayer = new MatchScenarioPlayer(TestHelper, turns))
            {
                await matchScenarioPlayer.Play();
            }

            validateEndStateAction?.Invoke();
            await TestHelper.GoBackToMainScreen();
        }

        public static async Task StartOnlineMatch(IReadOnlyList<string> tags = null, int selectedHordeIndex = 0, bool createOpponent = true)
        {

            await TestHelper.MainMenuTransition("Button_Play");

            await TestHelper.AssertCurrentPageName(Enumerators.AppState.PlaySelection);
            await TestHelper.MainMenuTransition("Button_PvPMode");
            await TestHelper.AssertCurrentPageName(Enumerators.AppState.PvPSelection);
            await TestHelper.MainMenuTransition("Button_CasualType");
            await TestHelper.AssertCurrentPageName(Enumerators.AppState.HordeSelection);

            if (selectedHordeIndex > 0)
            {
                await TestHelper.SelectAHordeByIndex(selectedHordeIndex);
            }

            if (tags == null)
            {
                tags = new List<string>
                {
                    "onlineTest",
                    TestHelper.GetTestName()
                };
            }
            TestHelper.SetPvPTags(tags);
            TestHelper.DebugCheats.Enabled = true;
            TestHelper.DebugCheats.CustomRandomSeed = 0;

            await TestHelper.LetsThink();

            if (selectedHordeIndex > 0)
            {
                await TestHelper.MainMenuTransition("Button_Battle");
            }

            if (createOpponent)
            {
                await TestHelper.CreateAndConnectOpponentDebugClient();
            }
        }

        public static WorkingCard GetCardOnBoard(Player player, string name)
        {
            WorkingCard workingCard =
                player
                .BoardCards
                .Select(boardCard => boardCard.Model.Card)
                .Concat(player.CardsOnBoard)
                .FirstOrDefault(card => CardNameEqual(name, card));

            if (workingCard == null)
            {
                throw new Exception($"No '{name}' cards found on board for player {player}");
            }

            return workingCard;
        }

        public static WorkingCard GetCardInHand(Player player, string name)
        {
            WorkingCard workingCard =
                player
                    .CardsInHand
                    .FirstOrDefault(card => CardNameEqual(name, card));

            if (workingCard == null)
            {
                throw new Exception($"No '{name}' cards found in hand of player {player}");
            }

            return workingCard;
        }

        public static bool CardNameEqual(string name1, string name2)
        {
            return String.Equals(name1, name2, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool CardNameEqual(string name, WorkingCard card)
        {
            return CardNameEqual(name, card.LibraryCard.Name);
        }

        public static Deck GetDeckWithCards(string name, int heroId = 0, params DeckCardData[] cards)
        {
            Deck deck = new Deck(
                 0,
                 heroId,
                 name,
                 cards.ToList(),
                 Enumerators.OverlordSkill.NONE,
                 Enumerators.OverlordSkill.NONE
             );

            return deck;
        }

        public static Deck GetDeckWithCards(string name,
                                    int heroId = 0,
                                    Enumerators.OverlordSkill primaryskill = Enumerators.OverlordSkill.NONE,
                                    Enumerators.OverlordSkill secondarySkill = Enumerators.OverlordSkill.NONE,
                                    params DeckCardData[] cards)
        {
            Deck deck = GetDeckWithCards(name, heroId, cards);
            deck.PrimarySkill = primaryskill;
            deck.SecondarySkill = secondarySkill;

            return deck;
        }
    }
}
