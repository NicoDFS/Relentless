using Loom.ZombieBattleground.BackendCommunication;
using Loom.ZombieBattleground.Protobuf;
using Loom.Google.Protobuf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Loom.ZombieBattleground.Common;
using UnityEngine;

namespace Loom.ZombieBattleground
{
    public class QueueManager : IService, IQueueManager
    {
        private Queue<Func<Task>> _tasks;
        private BackendFacade _backendFacade;

        public bool Active { get; set; }

        public void Init()
        {
            _backendFacade = GameClient.Get<BackendFacade>();
            _tasks = new Queue<Func<Task>>();
        }

        public void Clear()
        {
            _tasks.Clear();
        }

        public async void Update()
        {
            if (!Active)
                return;

            while (_tasks.Count > 0)
            {
                await _tasks.Dequeue()();
            }
        }

        public void AddTask(Func<Task> taskFunc)
        {
            _tasks.Enqueue(taskFunc);
        }

        public void AddAction(IMessage request)
        {
            AddTask(async () =>
            {
                switch (request)
                {
                    case PlayerActionRequest playerActionMessage:
                        try
                        {
                            await _backendFacade.SendPlayerAction(playerActionMessage);
                        }
                        catch (TimeoutException exception)
                        {
                            Helpers.ExceptionReporter.LogException(exception);
                            Debug.LogWarning(" Time out == " + exception);
                            ShowConnectionPopup();
                        }
                        catch (Exception exception)
                        {
                            Helpers.ExceptionReporter.LogException(exception);
                            Debug.LogWarning(" other == " + exception);
                            ShowConnectionPopup();
                        }
                        break;

                    case EndMatchRequest endMatchMessage:
                        try
                        {
                            await _backendFacade.SendEndMatchRequest(endMatchMessage);
                        }
                        catch (TimeoutException exception)
                        {
                            Helpers.ExceptionReporter.LogException(exception);
                            Debug.LogWarning(" Time out == " + exception);
                            ShowConnectionPopup();
                        }
                        catch (Exception exception)
                        {
                            Helpers.ExceptionReporter.LogException(exception);
                            Debug.LogWarning(" other == " + exception);
                            ShowConnectionPopup();
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException($"Unknown action type: {request.GetType()}");
                }
            });
        }

        public void Dispose()
        {
            Clear();
        }

        private void ShowConnectionPopup()
        {
            IUIManager uiManager = GameClient.Get<IUIManager>();
            IGameplayManager gameplayManager = GameClient.Get<IGameplayManager>();
            ConnectionPopup connectionPopup = uiManager.GetPopup<ConnectionPopup>();

            if (gameplayManager.CurrentPlayer == null)
            {
                return;
            }

            if (connectionPopup.Self == null)
            {
                Func<Task> connectFuncInGame = async () =>
                {
                    GameClient.Get<IQueueManager>().Clear();
                    gameplayManager.CurrentPlayer.ThrowLeaveMatch();
                    gameplayManager.EndGame(Enumerators.EndGameType.CANCEL);
                    GameClient.Get<IMatchManager>().FinishMatch(Enumerators.AppState.MAIN_MENU);
                    connectionPopup.Hide();
                };

                connectionPopup.ConnectFuncInGameplay = connectFuncInGame;
                connectionPopup.Show();
                connectionPopup.ShowFailedInGamePlay();
            }
        }
    }
}
