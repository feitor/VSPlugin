//* Copyright 2010-2011 Research In Motion Limited.
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
using EnvDTE80;
using EnvDTE;
using Microsoft.VisualStudio.VCProjectEngine;
using System.Diagnostics;
using Microsoft.VisualStudio.CommandBars;
using Microsoft.VisualStudio.Project;
using System.IO;

namespace VSNDK.AddIn
{
    
    public class configtableentry
    {
        public string config;
        public string platform;
        public bool deployable;
    }

    /// <summary>
    /// 
    /// </summary>
    public class VSNDKAddIn
    {
        private VSNDKCommandEvents _commandEvents;
        private DTE2 _applicationObject; 
        private EnvDTE.AddIn _addInInstance;
        private TokenProcessor _tokenProcessor;

        private List<configtableentry> _configTable;


        private const string BLACKBERRY = "BlackBerry";
        private const string BLACKBERRYSIMULATOR = "BlackBerrySimulator";
        private const string STANDARD_TOOL_BAR = "Standard";
        private const string SOLUTION_CONFIGURATIONS = "Solution Configurations";
        private const string SOLUTION_PLATFORMS = "Solution Platforms";
        private const string BAR_DESCRIPTOR = "bar-descriptor.xml";
        private const string BAR_DESCRIPTOR_PATH = @"\..\VCWizards\CodeWiz\BlackBerry\BarDescriptor\Templates\1033\";
        
        public static bool isDebugEngineRunning = false;

        /// <summary> 
        /// Run initialization code on first connection of the AddIn. 
        /// </summary>
        /// <param name="appObj"> Application Object. </param>
        /// <param name="addin"> Add In Object. </param>
        public void Connect(DTE2 appObj, EnvDTE.AddIn addin)
        {
            // Initialize External and Internal Variables.
            _applicationObject = appObj;
            _addInInstance = addin;

            _configTable = new List<configtableentry>();

            // Register Command Events
            _commandEvents = new VSNDKCommandEvents(appObj);
            _commandEvents.RegisterCommand(GuidList.guidVSStd2KString, CommandConstants.cmdidSolutionPlatform, cmdNewPlatform_afterExec, cmdNewPlatform_beforeExec);
            _commandEvents.RegisterCommand(GuidList.guidVSDebugGroup, CommandConstants.cmdidDebugBreakatFunction, cmdNewFunctionBreakpoint_afterExec, cmdNewFunctionBreakpoint_beforeExec);

            DisableIntelliSenseErrorReport(true);
            CheckSolutionPlatformCommand();
        }


        /// <summary> 
        /// Terminate the AddIn. 
        /// </summary>
        public void Disconnect()
        {
            DisableIntelliSenseErrorReport(false);
        }


        /// <summary> 
        /// Solution Platform command is shown in the Standard toolbar by default with Visual C++ settings. Add the 
        /// command if not in the Standard toolbar. 
        /// </summary>
        private void CheckSolutionPlatformCommand()
        {
            DTE dte = (DTE)_applicationObject;
            CommandBars commandBars = (CommandBars)dte.CommandBars;
            CommandBar standardCommandBar = commandBars[STANDARD_TOOL_BAR];
            int pos = 0;
            foreach (CommandBarControl cmd in standardCommandBar.Controls)
            {
                if (cmd.Caption == SOLUTION_CONFIGURATIONS)
                    pos = cmd.Index;
                if (cmd.Caption == SOLUTION_PLATFORMS)
                    return;
            }

            Command sp = null;
            foreach (Command c in dte.Commands)
            {
                if (c.Guid == GuidList.guidVSStd2KString && c.ID == CommandConstants.cmdidSolutionPlatform)
                {
                    sp = c;
                    break;
                }
            }
            if (sp != null)
                sp.AddControl(standardCommandBar, pos + 1);
        }


        /// <summary> 
        /// Set the DisableErrorReporting property value. 
        /// </summary>
        /// <param name="disable"> The property value to set. </param>
        private void DisableIntelliSenseErrorReport(bool disable)
        {
            DTE dte = _applicationObject as DTE;
            var txtEdCpp = dte.get_Properties("TextEditor", "C/C++ Specific");
            var prop = txtEdCpp.Item("DisableErrorReporting");
            if (prop != null)
                prop.Value = disable;
        }

        
        /// <summary> 
        /// New Platform Before Execution Event Handler. 
        /// </summary>
        /// <param name="Guid">Command GUID. </param>
        /// <param name="ID">Command ID. </param>
        /// <param name="CustomIn">Custom IN Object. </param>
        /// <param name="CustomOut">Custom OUT Object. </param>
        /// <param name="CancelDefault">Cancel the default execution of the command. </param>
        private void cmdNewPlatform_beforeExec(string Guid, int ID, object CustomIn, object CustomOut, ref bool CancelDefault)
        {
            GetSolutionPlarformConfig();
        }

        
        /// <summary> 
        /// New Platform After Execution Event Handler. 
        /// </summary>
        /// <param name="Guid">Command GUID. </param>
        /// <param name="ID">Command ID. </param>
        /// <param name="CustomIn">Custom IN Object. </param>
        /// <param name="CustomOut">Custom OUT Object. </param>
        private void cmdNewPlatform_afterExec(string Guid, int ID, object CustomIn, object CustomOut)
        {
            SolutionPlarformConfig();
            AddBarDescriptor();
        }


        /// <summary> 
        /// New Function Breakpoint Before Execution Event Handler. 
        /// </summary>
        /// <param name="Guid">Command GUID. </param>
        /// <param name="ID">Command ID. </param>
        /// <param name="CustomIn">Custom IN Object. </param>
        /// <param name="CustomOut">Custom OUT Object. </param>
        /// <param name="CancelDefault">Cancel the default execution of the command. </param>
        private void cmdNewFunctionBreakpoint_beforeExec(string Guid, int ID, object CustomIn, object CustomOut, ref bool CancelDefault)
        {
            // Add Code Here
        }


        /// <summary> 
        /// New Function Breakpoint After Execution Event Handler. 
        /// </summary>
        /// <param name="Guid">Command GUID. </param>
        /// <param name="ID">Command ID. </param>
        /// <param name="CustomIn">Custom IN Object. </param>
        /// <param name="CustomOut">Custom OUT Object. </param>
        private void cmdNewFunctionBreakpoint_afterExec(string Guid, int ID, object CustomIn, object CustomOut)
        {
            Breakpoint functionBP = _applicationObject.Debugger.Breakpoints.Item(_applicationObject.Debugger.Breakpoints.Count);

            if (functionBP != null)
            {
                if ((functionBP.FunctionColumnOffset != 1) || (functionBP.FunctionLineOffset != 1))
                {
                    System.Windows.Forms.MessageBox.Show("The breakpoint cannot be set.  Function breakpoints are only supported on the first line.", "Microsoft Visual Studio", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                    functionBP.Delete();
                }
            }
        }

        /// <summary>
        /// Set solution config after edit
        /// </summary>
        private void SolutionPlarformConfig()
        {
            DTE dte = _applicationObject as DTE;

            SolutionConfigurations SGS = dte.Solution.SolutionBuild.SolutionConfigurations;

            foreach (SolutionConfiguration SG in SGS)
            {
                string name = SG.Name;

                SolutionContexts SCS = SG.SolutionContexts;
                foreach (SolutionContext SC in SCS)
                {
                    string cname = SC.ConfigurationName;
                    string pname = SC.PlatformName;
                    string prname = SC.ProjectName;

                    configtableentry e = _configTable.Find(i => (i.config == cname) && (i.platform == pname));

                    if (e != null)
                    {
                        _configTable.Remove(e);
                    }
                    else
                    {
                        SC.ShouldDeploy = true;
                    }

                }
            }

        }

        /// <summary>
        /// Get solution configuration before edit
        /// </summary>
        private void GetSolutionPlarformConfig()
        {
            DTE dte = _applicationObject as DTE;

            SolutionConfigurations SGS = dte.Solution.SolutionBuild.SolutionConfigurations;

            foreach (SolutionConfiguration SG in SGS)
            {
                string name = SG.Name;

                SolutionContexts SCS = SG.SolutionContexts;
                foreach (SolutionContext SC in SCS)
                {
                    string cname = SC.ConfigurationName;
                    string pname = SC.PlatformName;
                    string prname = SC.ProjectName;

                    configtableentry c = new configtableentry();
                    c.platform = pname;
                    c.config = cname;
                    c.deployable = SC.ShouldDeploy;

                    _configTable.Add(c);
                }
            }

        }


        /// <summary> 
        /// Add Bar Descriptor to each project. 
        /// </summary>
        private void AddBarDescriptor()
        {
            try
            {
                DTE dte = _applicationObject as DTE;
                Projects projs = dte.Solution.Projects;


                List<Project> projList = new List<Project>();
                foreach (Project proj in projs)
                {
                    projList.Add(proj);
                }

                while (projList.Count > 0)
                {
                    Project proj = projList.ElementAt(0);
                    projList.RemoveAt(0);

                    Configuration config;
                    Property prop;
                    try
                    {
                        config = proj.ConfigurationManager.ActiveConfiguration;
                        prop = config.Properties.Item("ConfigurationType");
                    }
                    catch
                    {
                        config = null;
                        prop = null;
                    }

                    if (prop == null)
                    {
                        if (proj.ProjectItems != null)
                        {
                            foreach (ProjectItem projItem in proj.ProjectItems)
                            {
                                if (projItem.SubProject != null)
                                    projList.Add(projItem.SubProject);
                            }
                        }
                        continue;
                    }
                    
                    if (Convert.ToInt16(prop.Value) != Convert.ToInt16(ConfigurationTypes.typeApplication))
                        continue;

                    if (config.PlatformName != BLACKBERRY && config.PlatformName != BLACKBERRYSIMULATOR)
                        continue;
                    
                    ProjectItem baritem = proj.ProjectItems.Item(BAR_DESCRIPTOR);
                    string n = proj.Name;
                    if (baritem == null)
                    {
                        _tokenProcessor = new TokenProcessor();
                        Debug.WriteLine("Add bar descriptor file to the project");
                        string templatePath = dte.Solution.ProjectItemsTemplatePath(proj.Kind);
                        templatePath += BAR_DESCRIPTOR_PATH + BAR_DESCRIPTOR;
                        _tokenProcessor.AddReplace(@"[!output PROJECT_NAME]", proj.Name);
                        string destination = Path.GetFileName(templatePath);

                        // Remove directory used in previous versions of this plug-in.
                        string folder = Path.Combine(Path.GetDirectoryName(proj.FullName), proj.Name + "_barDescriptor");
                        if (Directory.Exists(folder))
                        {
                            try
                            {
                                Directory.Delete(folder);
                            }
                            catch (Exception)
                            {
                            }
                        }

                        folder = Path.Combine(Path.GetDirectoryName(proj.FullName), "BlackBerry-" + proj.Name);
                        Directory.CreateDirectory(folder);
                        destination = Path.Combine(folder, destination);
                        _tokenProcessor.UntokenFile(templatePath, destination);
                        ProjectItem projectitem = proj.ProjectItems.AddFromFile(destination);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

    }
}
