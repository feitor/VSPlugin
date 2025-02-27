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
using System.ComponentModel;
using Microsoft.Win32;
using System.Xml;
using System.IO;
using System.Windows.Data;

namespace RIM.VSNDK_Package.Settings.Models
{
    /// <summary>
    /// Class the story a new NDK configuration entry.
    /// </summary>
    public class NDKEntryClass
    {
        public string NDKName { get; set; }
        public string HostPath { get; set; }
        public string TargetPath { get; set; }
        public NDKEntryClass(string name, string host, string target)
        {
            NDKName = name;
            HostPath = host;
            TargetPath = target;
        }
    }

    /// <summary>
    /// Data Model Class for the Settings Dialog
    /// </summary>
    public class SettingsData : INotifyPropertyChanged
    {
        #region Member Variables and Constants
        private string _deviceIP;
        private string _devicePassword;
        private string _simulatorIP;
        private string _simulatorPassword;
        private CollectionView _ndkEntries;
        private NDKEntryClass _ndkEntry = new NDKEntryClass("", "", "");

        private string _targetPath;
        private string _hostPath;

        private const string ColDeviceIp = "DeviceIP";
        private const string ColDevicePw = "DevicePassword";
        private const string ColSimulatorIp = "SimulatorIP";
        private const string ColSimulatorPw = "SimulatorPassword";
        private const string ColNdkEntry = "NDKEntry";
        #endregion

        /// <summary>
        /// SettingsData Constructor
        /// </summary>
        public SettingsData()
        {
            GetDeviceInfo();
            GetSimulatorInfo();
            RefreshScreen();
        }

        /// <summary>
        /// Refresh the screen
        /// </summary>
        public void RefreshScreen()
        {
            // Get the NDK Path data
            GetNDKPath();

            // Refresh the NDK List
            string[] dirPaths = new string[2];
            dirPaths[0] = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)) + @"bbndk_vs\..\qconfig\";
            dirPaths[1] = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Research In Motion\BlackBerry Native SDK\qconfig\";
            IList<NDKEntryClass> NDKList = new List<NDKEntryClass>();

            for (int i = 0; i < 2; i++)
            {
                if (!Directory.Exists(dirPaths[i]))
                    continue;

                string[] filePaths = Directory.GetFiles(dirPaths[i], "*.xml");

                foreach (string file in filePaths)
                {
                    try
                    {
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.Load(file);
                        string name = xmlDoc.GetElementsByTagName("name")[0].InnerText;
                        string hostpath = xmlDoc.GetElementsByTagName("host")[0].InnerText;
                        string targetpath = xmlDoc.GetElementsByTagName("target")[0].InnerText;
                        NDKEntryClass NDKEntry = new NDKEntryClass(name, hostpath, targetpath);
                        NDKList.Add(NDKEntry);

                        if (NDKEntry.HostPath == HostPath)
                        {
                            NDKEntryClass = NDKEntry;
                        }

                    }
                    catch
                    {
                        break;
                    }
                }
            }

            NDKEntries = new CollectionView(NDKList);
        }

        /// <summary>
        /// return API Name
        /// </summary>
        /// <param name="version">version to match</param>
        /// <returns></returns>
        public string GetAPIName(string version)
        {
            APITargetListSingleton ap = APITargetListSingleton.Instance;
           
            foreach (APITargetClass target in ap._tempAPITargetList)
            {
                if (target.TargetVersion  == version)
                {
                    return target.TargetName;
                }
            }

            return string.Empty;
        }

        #region Properties

        /// <summary>
        /// Getter/Setter for the DeviceIP property
        /// </summary>
        public string DeviceIP
        {
            get { return _deviceIP; }
            set { _deviceIP = value; OnPropertyChanged(ColDeviceIp); }
        }

        /// <summary>
        /// Getter Setter for the TargetPath
        /// </summary>
        public string TargetPath
        {
            get { return _targetPath; }
            set { _targetPath = value; }
        }

        /// <summary>
        /// Getter Setter for the HostPath
        /// </summary>
        public string HostPath
        {
            get { return _hostPath; }
            set { _hostPath = value; }
        }

        /// <summary>
        /// Getter/Setter for the DevicePassword property
        /// </summary>
        public string DevicePassword
        {
            get { return _devicePassword; }
            set { _devicePassword = value; OnPropertyChanged(ColDevicePw); }
        }

        /// <summary>
        /// Getter/Setter for the SimulatorIP property
        /// </summary>
        public string SimulatorIP
        {
            get { return _simulatorIP; }
            set { _simulatorIP = value; OnPropertyChanged(ColSimulatorIp); }
        }

        /// <summary>
        /// Getter/Setter for the SimulatorPassword property
        /// </summary>
        public string SimulatorPassword
        {
            get { return _simulatorPassword; }
            set { _simulatorPassword = value; OnPropertyChanged(ColSimulatorPw); }
        }

        /// <summary>
        /// Getter for the NDKEntries property
        /// </summary>
        public CollectionView NDKEntries
        {
            get { return _ndkEntries; }
            set { _ndkEntries = value; }
        }

        /// <summary>
        /// Getter/Setter for the NDKEntryClass property
        /// </summary>
        public NDKEntryClass NDKEntryClass
        {
            get { return _ndkEntry; }
            set
            {
                if (_ndkEntry == value) return;
                _ndkEntry = value;
                OnPropertyChanged(ColNdkEntry);
            }
        }

        #endregion

         /// <summary>
        /// Set Device Password and IP
        /// </summary>
        /// <returns></returns>
        public void SetDeviceInfo()
        {
            registerTargetInfo(DevicePassword, DeviceIP, "device");
        }

        /// <summary>
        /// Set Simulator Password and IP
        /// </summary>
        /// <returns></returns>
        public void SetSimulatorInfo()
        {
            registerTargetInfo(SimulatorPassword, SimulatorIP, "simulator");
        }

        /// <summary>
        /// Function to retrieve device info from the registry
        /// </summary>
        /// <returns></returns>
        public void GetDeviceInfo()
        {
            RegistryKey rkHKCU = Registry.CurrentUser;
            RegistryKey rkSettingsPath = null;

            try
            {
                rkSettingsPath = rkHKCU.CreateSubKey("Software\\BlackBerry\\BlackBerryVSPlugin");

                object pwd = rkSettingsPath.GetValue("device_password");
                if (pwd != null)
                    DevicePassword = GlobalFunctions.Decrypt(pwd.ToString());

            }
            catch
            {
            }

            try
            {
                object ip = rkSettingsPath.GetValue("device_IP");
                if (ip != null)
                    DeviceIP = ip.ToString();
            }
            catch
            {
            }

            rkSettingsPath.Close();
            rkHKCU.Close();
        }

        /// <summary>
        /// Function to retrieve simulator info from the registry
        /// </summary>
        /// <returns></returns>
        public void GetSimulatorInfo()
        {
            RegistryKey rkHKCU = Registry.CurrentUser;
            RegistryKey rkSettingsPath = null;

            try
            {
                rkSettingsPath = rkHKCU.CreateSubKey("Software\\BlackBerry\\BlackBerryVSPlugin");

                object pwd = rkSettingsPath.GetValue("simulator_password");
                if (pwd != null)
                    SimulatorPassword = GlobalFunctions.Decrypt(pwd.ToString());
            }
            catch
            {
            }

            try
            {
                object ip = rkSettingsPath.GetValue("simulator_IP");
                if (ip != null)
                    SimulatorIP = ip.ToString();
            }
            catch
            {
            }

            rkSettingsPath.Close();
            rkHKCU.Close();
        }


        /// <summary>
        /// Set the password and IP address into the correct registry keys for both simulator and device
        /// </summary>
        /// <param name="password">The password to encrypt and store.</param>
        /// <param name="IP">The IP Address to store.</param>
        /// <param name="type">The key location device or simulator.</param>
        private void registerTargetInfo(string password, string IP, string type)
        {
            RegistryKey rkHKCU = Registry.CurrentUser;
            RegistryKey rkTargetInfo = null;

            try
            {
                rkTargetInfo = rkHKCU.CreateSubKey("Software\\BlackBerry\\BlackBerryVSPlugin");
                if (password == null)
                    password = "";

                if (IP == null)
                    IP = "";

                rkTargetInfo.SetValue(type + "_password", GlobalFunctions.Encrypt(password));
                rkTargetInfo.SetValue(type + "_IP", IP);
            }
            catch
            {

            }
            finally
            {
                rkTargetInfo.Close();
                rkHKCU.Close();
            }
        }

        /// <summary>
        /// Set the NDK Path into the register for future reference by the MSBUILD
        /// </summary>
        public void SetNDKPaths()
        {
            RegistryKey rkHKCU = Registry.CurrentUser;
            RegistryKey rkNDKPath = null;

            try
            {
                rkNDKPath = rkHKCU.CreateSubKey("Software\\BlackBerry\\BlackBerryVSPlugin");
                rkNDKPath.SetValue("NDKHostPath", _ndkEntry.HostPath);
                rkNDKPath.SetValue("NDKTargetPath", _ndkEntry.TargetPath);

                string qnx_config = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Research In Motion\BlackBerry Native SDK";

                Environment.SetEnvironmentVariable("QNX_TARGET", _ndkEntry.TargetPath);
                Environment.SetEnvironmentVariable("QNX_HOST", _ndkEntry.HostPath);
                Environment.SetEnvironmentVariable("QNX_CONFIGURATION", qnx_config);

                string ndkpath = string.Format(@"{0}/usr/bin;{1}\bin;{0}/usr/qde/eclipse/jre/bin;", _ndkEntry.HostPath, qnx_config) + Environment.GetEnvironmentVariable("PATH");
                Environment.SetEnvironmentVariable("PATH", ndkpath);
            }
            catch
            {

            }
            rkNDKPath.Close();
            rkHKCU.Close();

        }

        /// <summary>
        /// Return the NDK Path from the registry
        /// </summary>
        /// <returns></returns>
        public bool GetNDKPath()
        {
            bool success;

            RegistryKey rkHKCU = Registry.CurrentUser;
            RegistryKey rkNDKPath = null;

            try
            {
                rkNDKPath = rkHKCU.CreateSubKey("Software\\BlackBerry\\BlackBerryVSPlugin");
                HostPath = rkNDKPath.GetValue("NDKHostPath").ToString();
                TargetPath = rkNDKPath.GetValue("NDKTargetPath").ToString();
                rkNDKPath.Close();
                rkHKCU.Close();
                success = true;
            }
            catch
            {
                if (rkNDKPath != null)
                    rkNDKPath.Close();
                rkHKCU.Close();
                success = false;
            }

            return success;
        }

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Fire the PropertyChnaged event handler on change of property
        /// </summary>
        /// <param name="propName"></param>
        protected void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        #endregion

    }


}
