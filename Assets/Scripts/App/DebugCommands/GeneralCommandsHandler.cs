using Loom.Client;
using Loom.ZombieBattleground.BackendCommunication;
using Opencoding.CommandHandlerSystem;

namespace Loom.ZombieBattleground
{
    public static class GeneralCommandsHandler
    {
        private static BackendDataControlMediator _backendDataControlMediator;

        public static void Initialize()
        {
            CommandHandlers.RegisterCommandHandlers(typeof(GeneralCommandsHandler));

            _backendDataControlMediator = GameClient.Get<BackendDataControlMediator>();
        }

        [CommandHandler(Description = "Logs in into the game with a fake random user")]
        private static async void FakeLogin()
        {
            UserDataModel userDataModel = new UserDataModel(
                "ZombieSlayer_Fake_" + UnityEngine.Random.Range(int.MinValue, int.MaxValue).ToString().Replace("-", "0"),
                CryptoUtils.GeneratePrivateKey()
            );

            _backendDataControlMediator.SetUserDataModel(userDataModel);
            await _backendDataControlMediator.LoginAndLoadData();

            userDataModel.IsValid = true;
            _backendDataControlMediator.SetUserDataModel(userDataModel);

            GameClient.Get<IUIManager>().GetPopup<LoginPopup>().Hide();

        }

        [CommandHandler(Description = "Skips tutorial if you inside it")]
        public static void SkipTutorialFlow()
        {
            if (!GameClient.Get<ITutorialManager>().IsTutorial)
                return;

            if (GameClient.Get<IAppStateManager>().AppState == Common.Enumerators.AppState.GAMEPLAY)
            {
                GameClient.Get<IGameplayManager>().EndGame(Common.Enumerators.EndGameType.CANCEL);
                GameClient.Get<IMatchManager>().FinishMatch(Common.Enumerators.AppState.MAIN_MENU);
            }
            else
            {
                GameClient.Get<IAppStateManager>().ChangeAppState(Common.Enumerators.AppState.MAIN_MENU);
            }

            GameClient.Get<IDataManager>().CachedUserLocalData.Tutorial = false;
            GameClient.Get<ITutorialManager>().StopTutorial();
            GameClient.Get<IGameplayManager>().IsTutorial = false;
        }
    }
}
