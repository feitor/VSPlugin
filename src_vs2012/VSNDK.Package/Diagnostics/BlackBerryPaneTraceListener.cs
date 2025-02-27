﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace RIM.VSNDK_Package.Diagnostics
{
    /// <summary>
    /// Pane panel, that listens for BlackBerry-only trace log messages and displays on UI.
    /// </summary>
    internal sealed class BlackBerryPaneTraceListener : TraceListener
    {
        public const string SName = "BlackBerry Pane Trace Listener";

        private readonly IVsOutputWindowPane _outputPane;
        private readonly TimeTracker _time;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public BlackBerryPaneTraceListener(string title, bool printTime, IVsOutputWindow outputWindow, Guid paneGuid)
            : base(SName)
        {
            if (outputWindow == null)
                throw new ArgumentNullException("outputWindow");

            _outputPane = FindOrCreatePane(title, outputWindow, paneGuid);
            if (printTime)
                _time = new TimeTracker();
        }

        private static IVsOutputWindowPane FindOrCreatePane(string title, IVsOutputWindow outputWindow, Guid paneGuid)
        {
            IVsOutputWindowPane pane;

            // try to retrieve existing pane:
            if (outputWindow.GetPane(ref paneGuid, out pane) == VSConstants.S_OK)
            {
                return pane;
            }

            // if there is no particular pane, create new instance:
            Marshal.ThrowExceptionForHR(outputWindow.CreatePane(ref paneGuid, title, 1, 0));
            return outputWindow.GetPane(ref paneGuid, out pane) == VSConstants.S_OK ? pane : null;
        }

        /// <summary>
        /// Brings this pane to front.
        /// </summary>
        public void Activate()
        {
            _outputPane.Activate();
        }

        #region TraceListener Overrides

        public override void Write(string message)
        {
            // do nothing, only want to capture filtered-by-category messages
        }

        public override void WriteLine(string message)
        {
            // do nothing, only want to capture filtered-by-category messages
        }

        public override void Write(string message, string category)
        {
            // print only messages of 'BlackBerry' category:
            if (string.CompareOrdinal(category, TraceLog.Category) != 0)
                return;

            var timeString = _time != null ? _time.GetCurrent() : null;
            if (!string.IsNullOrEmpty(timeString))
                ErrorHandler.ThrowOnFailure(_outputPane.OutputString(timeString));

            ErrorHandler.ThrowOnFailure(_outputPane.OutputString(message));
        }

        public override void WriteLine(string message, string category)
        {
            // print only messages of 'BlackBerry' category:
            if (string.CompareOrdinal(category, TraceLog.Category) != 0)
                return;

            var timeString = _time != null ? _time.GetCurrentAndReset() : null;
            if (!string.IsNullOrEmpty(timeString))
                ErrorHandler.ThrowOnFailure(_outputPane.OutputString(timeString));

            ErrorHandler.ThrowOnFailure(_outputPane.OutputString(message));
            ErrorHandler.ThrowOnFailure(_outputPane.OutputString(Environment.NewLine));
        }

        #endregion
    }
}
