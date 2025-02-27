﻿//* Copyright 2010-2011 Research In Motion Limited.
//*
//* Licensed under the Apache License, Version 2.0 (the "License");
//* you may not use this file except in compliance with the License.
//* You may obtain a copy of the License at
//*
//* http://www.apache.org/licenses/LICENSE-2.0
//*
//* Unless required by applicable law or agreed to in writing, software
//* distributed under the License is distributed on an "AS IS" BASIS,
//* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//* See the License for the specific language governing permissions and
//* limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using RIM.VSNDK_Package.Signing.Models;
using System.IO;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.Win32;
using System.Xml;

namespace RIM.VSNDK_Package.Signing
{
    /// <summary>
    /// Interaction logic for SigningDialog.xaml
    /// </summary>
    public partial class SigningDialog : Window
    {
        private SigningData signingData = null;

        public SigningDialog()
        {
            InitializeComponent();

            signingData = new SigningData();
            gridMain.DataContext = signingData; 
        }

        /// <summary>
        /// Open BlackBerry Signing in the default browser and start a thread that will move the downloaded 
        /// bbidtoken.csk file to the right folder. Then, it is presented the Regisration Dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            Browser wb = new Browser(this);
            wb.ShowDialog();

            if ((!signingData.Registered) && (File.Exists(signingData.bbidtokenPath)))
            {
                RegistrationWindow win = new RegistrationWindow();
                win.ResizeMode = System.Windows.ResizeMode.NoResize;
                bool? res = win.ShowDialog();
            }

            signingData.RefreshScreen();
        }

        /// <summary>
        /// Unregister your signing keys.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnUnregister_Click(object sender, RoutedEventArgs e)
        {
            DeRegisterWindow win = new DeRegisterWindow();
            bool? res = win.ShowDialog();

            signingData.RefreshScreen();    
        }

        /// <summary>
        /// Backup your signing keys.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnBackup_Click(object sender, RoutedEventArgs e)
        {
            string zipfile = string.Empty;
            
            ///Create Dialog
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "signingkey";
            dlg.DefaultExt = ".zip"; // Default file extension
            dlg.Filter = "zip files (.zip)|*.zip"; // Filter files by extension
            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                zipfile = dlg.FileName;
                signingData.Backup(zipfile);
            }
        }

        /// <summary>
        /// Restore your signing keys.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRestore_Click(object sender, RoutedEventArgs e)
        {
            string zipfile = string.Empty;
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".zip"; // Default file extension
            dlg.Filter = "zip files (.zip)|*.zip"; // Filter files by extension
            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                zipfile = dlg.FileName;
                signingData.Restore(zipfile);
                signingData.RefreshScreen(); 
            }
        }
    }
}
