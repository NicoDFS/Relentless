// Copyright (c) 2018 - Loom Network. All rights reserved.
// https://loomx.io/



using LoomNetwork.CZB.Common;
using System;
using System.Collections.Generic;

namespace LoomNetwork.CZB
{
    public class ActionsQueueController : IController
    {
        public event Action<GameActionReport> GotNewActionReportEvent;
        public event Action<List<GameActionReport>> ActionsReportsUpdatedEvent;

        private Queue<GameAction<object>> _actionsToDo;
        private List<GameActionReport> _actionsReports;

        private GameAction<object> _actionInProgress;

        public List<GameActionReport> ActionsReports
        {
            get
            {
                return _actionsReports;
            }
        }

        public void Init()
        {
            _actionsToDo = new Queue<GameAction<object>>();
            _actionsReports = new List<GameActionReport>();
            _actionInProgress = null;
        }

        public void Dispose()
        {
        }

        public void Update()
        {
        }

        /// <summary>
        /// AddNewActionInToQueue
        /// </summary>
        /// <param name="actionToDo">action to do, parameter + callback action</param>
        /// <param name="parameter">parameters for action if ot needs</param>
        /// <param name="report">report that will be added into reports list</param>
        public void AddNewActionInToQueue(Action<object, Action> actionToDo, object parameter = null, GameActionReport report = null)
        {
            GameAction<object> gameAction = new GameAction<object>(actionToDo, parameter, report);
            gameAction.OnActionDoneEvent += OnActionDoneEvent;
            _actionsToDo.Enqueue(gameAction);

            if (_actionInProgress == null && _actionsToDo.Count < 2)
                TryCallNewActionFromQueue();
        }

        public void StopAllActions()
        {
            _actionsToDo.Clear();
            _actionInProgress = null;
        }

        //todo improve I guess
        public GameActionReport FormatGameActionReport(Enumerators.ActionType actionType, object[] parameters)
        {
            GameActionReport actionReport = new GameActionReport(actionType, parameters);

            return actionReport;
        }

        public void PostGameActionReport(GameActionReport report)
        {
            if (report != null)
            {
                _actionsReports.Add(report);
                GotNewActionReportEvent?.Invoke(report);
                ActionsReportsUpdatedEvent?.Invoke(_actionsReports);
            }
        }

        private void OnActionDoneEvent(GameAction<object> previousAction)
        {
            PostGameActionReport(previousAction.report);

            TryCallNewActionFromQueue();
        }

        private void TryCallNewActionFromQueue()
        {
            if (_actionsToDo.Count > 0)
            {
                _actionInProgress = _actionsToDo.Dequeue();
                _actionInProgress.DoAction();
            }
            else _actionInProgress = null;
        }
    }

    public class GameAction<T>
    {
        public event Action<GameAction<T>> OnActionDoneEvent;

        private ITimerManager _timerManager;

        public Action<T, Action> action;
        public T parameter;
        public GameActionReport report;

        public GameAction(Action<T, Action> action, T parameter, GameActionReport report)
        {
            _timerManager = GameClient.Get<ITimerManager>();

            this.action = action;
            this.parameter = parameter;
            this.report = report;
        }

        public void DoAction()
        {
            action?.Invoke(parameter, ActionDoneCallback);
        }

        private void ActionDoneCallback()
        {
            //small delay between actions
            _timerManager.AddTimer((x) =>
            {
                OnActionDoneEvent?.Invoke(this);
            }, null, Constants.DELAY_BETWEEN_GAMEPLAY_ACTIONS);
        }
    }

    public class GameActionReport
    {
        public Enumerators.ActionType actionType;
        public object[] parameters;

        public GameActionReport(Enumerators.ActionType actionType, object[] parameters)
        {
            this.actionType = actionType;
            this.parameters = parameters;
        }
    }
}
