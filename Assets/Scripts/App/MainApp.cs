// Copyright (c) 2018 - Loom Network. All rights reserved.
// https://loomx.io/



using LoomNetwork.CZB.Common;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace LoomNetwork.CZB
{
    public class MainApp : MonoBehaviour
    {
        public static int MainThreadId;

        public delegate void MainAppDelegate(object param);
        public event MainAppDelegate OnLevelWasLoadedEvent;

        public event Action LateUpdateEvent;
        public event Action FixedUpdateEvent;

        private static MainApp _Instance;
        public static MainApp Instance
        {
            get { return _Instance; }
            private set { _Instance = value; }
        }

        private void Awake()
        {
            if(Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);


            MainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        }

        private void Start()
        {
            if (Instance == this)
            {
                GameClient.Instance.InitServices();

                if (Constants.DEV_MODE)
                {
                    GameClient.Get<ISoundManager>().PlaySound(Common.Enumerators.SoundType.BACKGROUND, 128, Constants.BACKGROUND_SOUND_VOLUME, null, true, false, true);
                    GameClient.Get<IDataManager>().StartLoadCache();
                    GameClient.Get<IAppStateManager>().ChangeAppState(Common.Enumerators.AppState.DECK_SELECTION);
                }
                else
                    GameClient.Get<IAppStateManager>().ChangeAppState(Common.Enumerators.AppState.APP_INIT);

                SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            }
        }

        private void Update()
        {
            if (Instance == this)
                GameClient.Instance.Update();
        }

        private void LateUpdate()
        {
            if (Instance == this)
            {
                if (LateUpdateEvent != null)
                    LateUpdateEvent();
            }
        }

        private void FixedUpdate()
        {
            if (Instance == this)
            {
                if (FixedUpdateEvent != null)
                    FixedUpdateEvent();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                GameClient.Instance.Dispose();
        }


        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if (Instance == this)
            {
                if (OnLevelWasLoadedEvent != null)
                    OnLevelWasLoadedEvent(arg0.buildIndex);
            }
        }
    }
}