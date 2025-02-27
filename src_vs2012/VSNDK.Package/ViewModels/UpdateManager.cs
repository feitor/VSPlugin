﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using RIM.VSNDK_Package.Diagnostics;
using RIM.VSNDK_Package.Tools;

namespace RIM.VSNDK_Package.ViewModels
{
    internal sealed class UpdateManager : IDisposable
    {
        #region Internal Classes

        /// <summary>
        /// Description of actions scheduled to execution by UpdateManager.
        /// </summary>
        public sealed class ActionData
        {
            private readonly string _description;
            private ApiLevelUpdateRunner _runner;

            #region Properties

            internal ActionData(UpdateManager manager, ApiLevelAction action, ApiLevelTarget target, string name, Version version)
            {
                if (manager == null)
                    throw new ArgumentNullException("manager");
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentNullException("name");
                if (version == null)
                    throw new ArgumentNullException("version");

                UpdateManager = manager;
                Action = action;
                Target = target;
                Name = name;
                Version = version;

                _description = Name.Contains(Version.ToString())
                                    ? string.Concat(GetActionName(Action), " ", Name)
                                    : string.Concat(GetActionName(Action), " ", Name, " (", Version, ")");
            }

            private static string GetActionName(ApiLevelAction action)
            {
                switch (action)
                {
                    case ApiLevelAction.Install:
                        return "Install";
                    case ApiLevelAction.Uninstall:
                        return "Remove";
                    default:
                        throw new ArgumentOutOfRangeException("action");
                }
            }

            private UpdateManager UpdateManager
            {
                get;
                set;
            }

            public bool IsRunning
            {
                get { return _runner != null; }
            }

            public bool CanAbort
            {
                get;
                private set;
            }

            public ApiLevelAction Action
            {
                get;
                private set;
            }

            public ApiLevelTarget Target
            {
                get;
                private set;
            }

            public string Name
            {
                get;
                private set;
            }

            public Version Version
            {
                get;
                private set;
            }

            #endregion

            public override string ToString()
            {
                return _description;
            }

            internal void Abort()
            {
                bool aborted = false;

                lock (GetType())
                {
                    if (_runner != null && CanAbort)
                    {
                        aborted = _runner.Abort();
                    }
                }

                if (aborted)
                {
                    UpdateManager.Finished(this);
                }
            }

            public void Start()
            {
                lock (GetType())
                {
                    // do nothing if already running
                    if (IsRunning)
                        return;

                    _runner = new ApiLevelUpdateRunner(RunnerDefaults.NdkDirectory, Action, Target, Version);
                    _runner.Finished += OnFinished;
                    _runner.Log += OnLog;
                    _runner.ExecuteAsync();
                }
            }

            private void OnLog(object sender, ApiLevelUpdateLogEventArgs e)
            {
                if (e != null)
                {
                    CanAbort = e.CanAbort;
                }

                UpdateManager.NotifyLog(e);
            }

            private void OnFinished(object sender, ToolRunnerEventArgs e)
            {
                lock (GetType())
                {
                    _runner.Finished -= OnFinished;
                    _runner.Log -= OnLog;
                    _runner = null;
                }

                UpdateManager.Finished(this);
            }

            public void Delete()
            {
                // don't allow deletion, if already running:
                if (IsRunning)
                    return;

                UpdateManager.Remove(this);
            }
        }

        #endregion

        private event EventHandler<ApiLevelUpdateLogEventArgs> HiddenLog;

        public event EventHandler<ApiLevelUpdateLogEventArgs> Log
        {
            add
            {
                HiddenLog += value;

                // send again the last log message...
                if (value != null && _lastLog != null)
                {
                    value(this, _lastLog);
                }
            }
            remove { HiddenLog -= value; }
        }

        private const int DelayInterval = 30000; // in milisec

        public event EventHandler Started;
        public event EventHandler Completed;

        private readonly PackageViewModel _vm;
        private ApiLevelUpdateLogEventArgs _lastLog;
        private readonly List<ActionData> _actions;
        private Timer _timer;

        public UpdateManager(PackageViewModel vm, string folder)
        {
            if (vm == null)
                throw new ArgumentNullException("vm");
            if (string.IsNullOrEmpty(folder))
                throw new ArgumentNullException("folder");

            _vm = vm;
            Folder = folder;
            SyncFilePath = Path.Combine(Folder, "vsplugin.lock");
            _actions = new List<ActionData>();
            Actions = new ActionData[0];
        }

        ~UpdateManager()
        {
            Dispose(false);
        }

        #region Properties

        /// <summary>
        /// Gets the currently running action.
        /// </summary>
        public ActionData CurrentAction
        {
            get;
            private set;
        }

        /// <summary>
        /// Thread-safe read-only collection of performed actions.
        /// </summary>
        public ActionData[] Actions
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the playground folder for sync between different instances of Visual Studio.
        /// </summary>
        private string Folder
        {
            get;
            set;
        }

        private StreamWriter SyncFile
        {
            get;
            set;
        }

        private string SyncFilePath
        {
            get;
            set;
        }

        #endregion

        /// <summary>
        /// Requests installation or removal over specified item and version.
        /// </summary>
        public void Request(ApiLevelAction action, ApiLevelTarget target, string name, Version version)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");
            if (version == null)
                throw new ArgumentNullException("version");

            Schedule(new ActionData(this, action, target, name, version));
        }

        private void Schedule(ActionData action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            lock (GetType())
            {
                TraceLog.WriteLine("Scheduled: {0}", action);

                // add action to execution:
                _actions.Add(action);
                Actions = _actions.ToArray();
            }

            // and try to start it:
            Start();
        }

        private void Remove(ActionData action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            lock (GetType())
            {
                TraceLog.WarnLine("Removed: {0}", action);

                // remove from execution:
                action.Abort();
                if (_actions.Remove(action))
                {
                    Actions = _actions.ToArray();
                }
            }
        }

        /// <summary>
        /// Checks, if there exists scheduled action for specified item.
        /// </summary>
        public bool IsProcessing(ApiLevelTarget target, Version version)
        {
            if (version == null)
                return false;

            var current = CurrentAction;
            if (current != null && current.Target == target && current.Version == version)
                return true;

            foreach (var action in Actions)
            {
                if (action.Target == target && action.Version == version)
                    return true;
            }

            return false;
        }

        private void Start()
        {
            // already started...
            if (SyncFile != null)
                return;
            if (CurrentAction != null)
                return;

            lock (GetType())
            {
                if (_actions.Count == 0)
                    return;
                if (SyncFile != null)
                    return;

                try
                {
                    SyncFile = new StreamWriter(File.Open(SyncFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None));
                    SyncFile.Write(System.Diagnostics.Process.GetCurrentProcess().Id);
                }
                catch (Exception)
                {
                    // ok, probably occupied by another instance of Visual Studio... retry in some time...
                    StartLater();
                    return;
                }

                CurrentAction = _actions[0];
                _actions.RemoveAt(0);
                Actions = _actions.ToArray();

                // go-go-go:
                NotifyLog(new ApiLevelUpdateLogEventArgs("Starting..."));
                CurrentAction.Start();
            }

            NotifyStarted();
        }

        private void StartLater()
        {
            if (_timer == null)
            {
                NotifyLog(new ApiLevelUpdateLogEventArgs("Another instance of Visual Studio is already performing an update. Waiting...", 0, true));
                TraceLog.WriteLine("Another instance of Visual Studio occupies the UpdateManager. Waiting for own time slot...");

                _timer = new Timer();
                _timer.Interval = DelayInterval; // in milisec
                _timer.Tick += TimerOnTick;
                _timer.Start();
            }
        }

        private void TimerOnTick(object sender, EventArgs e)
        {
            _timer.Dispose();
            _timer = null;

            Start();
        }

        private void Finished(ActionData action)
        {
            if (SyncFile == null)
                return;

            bool completed = false;

            lock (GetType())
            {
                if (CurrentAction == action)
                {
                    CurrentAction = null;
                    _lastLog = null;

                    // and now reload underlying stored lists:
                    _vm.Reset(action.Target);
                    completed = true;

                    SyncFile.Close();
                    SyncFile = null;
                }
            }

            if (completed)
            {
                NotifyCompleted();
            }

            // and start the next action from the queue:
            Start();
        }

        private void UpdateLastLog(ApiLevelUpdateLogEventArgs e)
        {
            if (e == null)
                return;

            // this method is a bit complex, as logger sends partial data,
            // and here we want to maintain the 'full picture':
            if (string.IsNullOrEmpty(e.Message) || e.Progress < 0)
            {
                _lastLog = new ApiLevelUpdateLogEventArgs(string.IsNullOrEmpty(e.Message) ? (_lastLog != null ? _lastLog.Message : null)
                                                                                          : e.Message,
                                                          e.Progress < 0 ? (_lastLog != null ? _lastLog.Progress : 0)
                                                                        : e.Progress, e.CanAbort);
            }
            else
            {
                _lastLog = e;
            }
        }

        private void NotifyLog(ApiLevelUpdateLogEventArgs e)
        {
            var handler = HiddenLog;
            UpdateLastLog(e);

            if (handler != null)
                handler(this, e);
        }

        private void NotifyStarted()
        {
            var handler = Started;

            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        private void NotifyCompleted()
        {
            var handler = Completed;

            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        #region IDisposable Implementation

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (GetType())
                {
                    if (SyncFile != null)
                    {
                        SyncFile.Dispose();
                        SyncFile = null;
                    }

                    ActionData[] copiedActions = Actions;
                    Actions = new ActionData[0];
                    _actions.Clear();

                    // and abort all actions:
                    foreach (var action in copiedActions)
                    {
                        action.Abort();
                    }
                }

                if (_timer != null)
                {
                    _timer.Dispose();
                    _timer = null;
                }
            }
        }

        #endregion
    }
}
