using System;
using System.IO;
using Loom.Client;
using Loom.ZombieBattleground.BackendCommunication;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using Loom.ZombieBattleground.Gameplay;
using Newtonsoft.Json;
using UnityEngine;

namespace Loom.ZombieBattleground
{
    public class GameClient : ServiceLocatorBase
    {
        public event Action ServicesInitialized;
        public bool UpdateServices { get; set; } = true;

        private static readonly object Sync = new object();

        private static GameClient _instance;

        /// <summary>
        ///     Initializes a new instance of the <see cref="GameClient" /> class.
        /// </summary>
        internal GameClient()
        {
            Debug.Log("Starting game, version " + BuildMetaInfo.Instance.FullVersionName);

            LoadObjectsManager loadObjectsManager = new LoadObjectsManager();
            loadObjectsManager.LoadAssetBundleFromFile(Constants.AssetBundleMain);

            BackendEndpoint backendEndpoint = GetDefaultBackendEndpoint();

            string configDataFilePath = Path.Combine(Application.persistentDataPath, Constants.LocalConfigDataFileName);
            ConfigData configData = new ConfigData();
            if (File.Exists(configDataFilePath))
            {
                configData = JsonConvert.DeserializeObject<ConfigData>(File.ReadAllText(configDataFilePath));
                if (configData.Backend != null)
                {
                    Debug.Log("Backend overriden by config file.");
                    backendEndpoint = configData.Backend;
                }
            }

            Func<Contract, IContractCallProxy> contractCallProxyFactory =
                contract => new ThreadedContractCallProxyWrapper(new TimeMetricsContractCallProxy(contract, false, true));

            AddService<IApplicationSettingsManager>(new ApplicationSettingsManager());
            AddService<ILoadObjectsManager>(loadObjectsManager);
            AddService<ITimerManager>(new TimerManager());
            AddService<IInputManager>(new InputManager());
            AddService<ILocalizationManager>(new LocalizationManager());
            AddService<IScenesManager>(new ScenesManager());
            AddService<IAppStateManager>(new AppStateManager());
            AddService<ICameraManager>(new CameraManager());
            AddService<IPlayerManager>(new PlayerManager());
            AddService<ISoundManager>(new SoundManager());
            AddService<INavigationManager>(new NavigationManager());
            AddService<IGameplayManager>(new GameplayManager());
            AddService<IOverlordExperienceManager>(new OverlordExperienceManager());
            AddService<ITutorialManager>(new TutorialManager());
            AddService<IMatchManager>(new MatchManager());
            AddService<IUIManager>(new UIManager());
            AddService<IDataManager>(new DataManager(configData));
            AddService<BackendFacade>(new BackendFacade(backendEndpoint, contractCallProxyFactory));
            AddService<ActionCollectorUploader>(new ActionCollectorUploader());
            AddService<BackendDataControlMediator>(new BackendDataControlMediator());
            AddService<IFacebookManager>(new FacebookManager());
            AddService<IAnalyticsManager>(new AnalyticsManager());
            AddService<IPvPManager>(new PvPManager());
            AddService<IQueueManager>(new QueueManager());
            AddService<DebugCommandsManager>(new DebugCommandsManager());
            AddService<PushNotificationManager>(new PushNotificationManager());
            AddService<FiatBackendManager>(new FiatBackendManager());
            AddService<FiatPlasmaManager>(new FiatPlasmaManager());
            AddService<OpenPackPlasmaManager>(new OpenPackPlasmaManager());
            AddService<IInAppPurchaseManager>(new InAppPurchaseManager());
            AddService<TutorialRewardManager>(new TutorialRewardManager());
        }

        public override void InitServices() {
            base.InitServices();

            ServicesInitialized?.Invoke();
        }

        public override void Update()
        {
            if (!UpdateServices)
                return;

            base.Update();
        }

        public static BackendEndpoint GetDefaultBackendEndpoint()
        {
#if (UNITY_EDITOR || USE_LOCAL_BACKEND) && !USE_PRODUCTION_BACKEND && !USE_STAGING_BACKEND && !USE_BRANCH_TESTING_BACKEND && !USE_REBALANCE_BACKEND
            const BackendPurpose backend = BackendPurpose.Local;
#elif USE_PRODUCTION_BACKEND
            const BackendPurpose backend = BackendPurpose.Production;
#elif USE_BRANCH_TESTING_BACKEND
            const BackendPurpose backend = BackendPurpose.BranchTesting;
#elif USE_REBALANCE_BACKEND
            const BackendPurpose backend = BackendPurpose.Rebalance;
#else
            const BackendPurpose backend = BackendPurpose.Staging;
#endif

            BackendEndpoint backendEndpoint = BackendEndpointsContainer.Endpoints[backend];
            return backendEndpoint;
        }

        public static GameClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (Sync)
                    {
                        _instance = new GameClient();
                    }
                }

                return _instance;
            }
        }

        public static T Get<T>()
        {
            return Instance.GetService<T>();
        }
    }
}
