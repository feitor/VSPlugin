﻿using System;
using System.Windows.Forms;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace RIM.VSNDK_Package
{
    public static class MessageBoxHelper
    {
        private const int IDABORT = 3;
        private const int IDCANCEL = 2;
        private const int IDIGNORE = 5;
        private const int IDNO = 7;
        private const int IDOK = 1;
        private const int IDRETRY = 4;
        private const int IDYES = 6;


        private static IVsUIShell _uiShell;

        /// <summary>
        /// Initialize internal structures.
        /// </summary>
        public static void Initialise(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException("serviceProvider");

            _uiShell = serviceProvider.GetService(typeof(IVsUIShell)) as IVsUIShell;
            if (_uiShell == null)
                throw new InvalidOperationException("Unable to initialize UiShell");
        }

        /// <summary>
        /// Shows message box.
        /// </summary>
        public static DialogResult Show(string text, string title, MessageBoxButtons buttons, MessageBoxIcon icon,
                            MessageBoxDefaultButton defaultButton)
        {
            // check the state:
            if (_uiShell == null)
                throw new InvalidOperationException("MessageBox ecosystem has not been initialized properly yet");

            var guid = Guid.Empty;
            int result;
            var xButton = OLEMSGBUTTON.OLEMSGBUTTON_OKCANCEL;
            var xIcon = OLEMSGICON.OLEMSGICON_INFO;
            var xDefaultButton = OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST;

            switch (buttons)
            {
                case MessageBoxButtons.OK:
                    xButton = OLEMSGBUTTON.OLEMSGBUTTON_OK;
                    break;
                case MessageBoxButtons.OKCancel:
                    xButton = OLEMSGBUTTON.OLEMSGBUTTON_OKCANCEL;
                    break;
                case MessageBoxButtons.RetryCancel:
                    xButton = OLEMSGBUTTON.OLEMSGBUTTON_RETRYCANCEL;
                    break;
                case MessageBoxButtons.AbortRetryIgnore:
                    xButton = OLEMSGBUTTON.OLEMSGBUTTON_ABORTRETRYIGNORE;
                    break;
                case MessageBoxButtons.YesNo:
                    xButton = OLEMSGBUTTON.OLEMSGBUTTON_YESNO;
                    break;
                case MessageBoxButtons.YesNoCancel:
                    xButton = OLEMSGBUTTON.OLEMSGBUTTON_YESNOCANCEL;
                    break;
            }

            switch (icon)
            {
                case MessageBoxIcon.Question:
                    xIcon = OLEMSGICON.OLEMSGICON_QUERY;
                    break;
                case MessageBoxIcon.Information:
                    xIcon = OLEMSGICON.OLEMSGICON_INFO;
                    break;
                case MessageBoxIcon.Error:
                    xIcon = OLEMSGICON.OLEMSGICON_CRITICAL;
                    break;
                case MessageBoxIcon.Exclamation:
                    xIcon = OLEMSGICON.OLEMSGICON_WARNING;
                    break;
            }

            switch (defaultButton)
            {
                case MessageBoxDefaultButton.Button1:
                    xDefaultButton = OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST;
                    break;
                case MessageBoxDefaultButton.Button2:
                    xDefaultButton = OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_SECOND;
                    break;
                case MessageBoxDefaultButton.Button3:
                    xDefaultButton = OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_THIRD;
                    break;
            }

            // show proper message box:
            ErrorHandler.ThrowOnFailure(_uiShell.ShowMessageBox(0, ref guid, title, text, null, 0,
                                                               xButton,
                                                               xDefaultButton,
                                                               xIcon, 0, out result));

            // interpret the result:
            switch (result)
            {
                case IDOK:
                    return DialogResult.OK;
                case IDCANCEL:
                    return DialogResult.Cancel;
                case IDABORT:
                    return DialogResult.Abort;
                case IDIGNORE:
                    return DialogResult.Ignore;
                case IDNO:
                    return DialogResult.No;
                case IDYES:
                    return DialogResult.Yes;
                case IDRETRY:
                    return DialogResult.Retry;
                default:
                    return DialogResult.None;
            }
        }

        /// <summary>
        /// Shows message box.
        /// </summary>
        public static DialogResult Show(string text, string title, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            return Show(text, title, buttons, icon, MessageBoxDefaultButton.Button1);
        }

        /// <summary>
        /// Shows message box.
        /// </summary>
        public static DialogResult Show(string text, string title, MessageBoxButtons buttons)
        {
            return Show(text, title, buttons, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
        }

        /// <summary>
        /// Shows message box.
        /// </summary>
        public static DialogResult Show(string text, string title, MessageBoxIcon icon)
        {
            return Show(text, title, MessageBoxButtons.OK, icon, MessageBoxDefaultButton.Button1);
        }

        /// <summary>
        /// Shows message box.
        /// </summary>
        public static DialogResult Show(string text, string title)
        {
            return Show(text, title, MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
        }
    }
}
