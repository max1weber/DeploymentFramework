// Deployment Framework for BizTalk
// Copyright (C) 2008-14 Thomas F. Abraham, 2004-08 Scott Colestock
// This source file is subject to the Microsoft Public License (Ms-PL).
// See http://www.opensource.org/licenses/ms-pl.html.
// All other rights reserved.

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Extensibility;
using Microsoft.VisualStudio.CommandBars;
using Microsoft.Win32;
using System.Diagnostics;

namespace DeploymentFramework.VisualStudioAddIn
{
    /// <summary>The object for implementing an Add-in.</summary>
    /// <seealso class='IDTExtensibility2' />
    public class Connect : IDTExtensibility2, IDTCommandTarget
    {
        private string _registryKeyPath;
        private string _msbuildPath;
        //private string _msbuildVerbositySwitch;
        private string _gacUtilPath;
        private VSVersion _ideVersion;

        private CommandManager _commandManager;
        private CommandRunner _commandRunner;

        private object _missingType = Type.Missing;
        private DTE2 _applicationObject;
        private AddIn _addInInstance;

        private CommandBar _toolbar;
        private CommandBarPopup _commandBarPopup = null;

        /// <summary>Implements the constructor for the Add-in object. Place your initialization code within this method.</summary>
        public Connect()
        {
        }

        /// <summary>Implements the OnConnection method of the IDTExtensibility2 interface. Receives notification that the
        /// Add-in is being loaded.</summary>
        /// <param term='application'>Root object of the host application.</param>
        /// <param term='connectMode'>Describes how the Add-in is being loaded.</param>
        /// <param term='addInInst'>Object representing this Add-in.</param>
        /// <seealso class='IDTExtensibility2' />
        public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
        {
            _applicationObject = (DTE2)application;
            _addInInstance = (AddIn)addInInst;

            this._commandManager = new CommandManager(_applicationObject, _addInInstance);
            this._commandRunner = new CommandRunner(_applicationObject);


            _registryKeyPath = @"HKEY_CURRENT_USER\Software\DeploymentFrameworkForBizTalk\5.0\VSAddin\";

            string ideVersion = _applicationObject.DTE.Application.Version;
            if (string.Compare(ideVersion, "10.0") == 0)
            {
                _ideVersion = VSVersion.Vs2010;
                _registryKeyPath += "VS2010\\";
            }
            else if (string.Compare(ideVersion, "11.0") == 0)
            {
                _ideVersion = VSVersion.Vs2012;
                _registryKeyPath += "VS2012\\";
            }
            else if (string.Compare(ideVersion, "12.0") == 0)
            {
                _ideVersion = VSVersion.Vs2012;
                _registryKeyPath += "VS2013\\";
            }
            else
            {
                _ideVersion = VSVersion.Vs2010;
                _registryKeyPath += "VS2010\\";
            }

            // Ensure that the registry key exists.
            if (Registry.GetValue(_registryKeyPath, null, null) == null)
            {
                try
                {
                    Registry.SetValue(_registryKeyPath, null, "Default");
                }
                catch (Exception)
                {
                }
            }

            if (connectMode == ext_ConnectMode.ext_cm_UISetup)
            {
                this._commandManager.CreateCommands();
            }
            else if (connectMode == ext_ConnectMode.ext_cm_AfterStartup)
            {
                AddTemporaryUI();
            }
        }

        /// <summary>Implements the OnDisconnection method of the IDTExtensibility2 interface. Receives notification that the
        /// Add-in is being unloaded.</summary>
        /// <param term='disconnectMode'>Describes how the Add-in is being unloaded.</param>
        /// <param term='custom'>Array of parameters that are host application specific.</param>
        /// <seealso class='IDTExtensibility2' />
        public void OnDisconnection(ext_DisconnectMode disconnectMode, ref Array custom)
        {
            switch (disconnectMode)
            {
                case ext_DisconnectMode.ext_dm_HostShutdown:
                case ext_DisconnectMode.ext_dm_UserClosed:
                    {
                        try
                        {
                            if (_toolbar != null)
                            {
                                Registry.SetValue(_registryKeyPath, "ToolbarVisible", _toolbar.Visible);

                                _toolbar.Visible = false;
                                _toolbar.Delete();
                            }

                            if (_commandBarPopup != null)
                            {
                                _commandBarPopup.Delete(true);
                            }

                            _commandManager.RecreateCommands();
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine("Exception disconnecting Deployment Framework for BizTalk addin: " + ex.Message, "DeploymentFrameworkForBizTalk");
                        }

                        break;
                    }
            }
        }

        /// <summary>Implements the OnStartupComplete method of the IDTExtensibility2 interface. Receives notification that the
        /// host application has completed loading.</summary>
        /// <param term='custom'>Array of parameters that are host application specific.</param>
        /// <seealso class='IDTExtensibility2' />
        public void OnStartupComplete(ref Array custom)
        {
            AddTemporaryUI();
        }

        /// <summary>Implements the OnAddInsUpdate method of the IDTExtensibility2 interface. Receives notification when the collection
        /// of Add-ins has changed.</summary>
        /// <param term='custom'>Array of parameters that are host application specific.</param>
        /// <seealso class='IDTExtensibility2' />		
        public void OnAddInsUpdate(ref Array custom)
        {
        }

        /// <summary>Implements the OnBeginShutdown method of the IDTExtensibility2 interface. Receives notification that the
        /// host application is being unloaded.</summary>
        /// <param term='custom'>Array of parameters that are host application specific.</param>
        /// <seealso class='IDTExtensibility2' />
        public void OnBeginShutdown(ref Array custom)
        {
        }

        /// <summary>Implements the QueryStatus method of the IDTCommandTarget interface. This is called when the command's availability
        /// is updated</summary>
        /// <param term='commandName'>The name of the command to determine state for.</param>
        /// <param term='neededText'>Text that is needed for the command.</param>
        /// <param term='status'>The state of the command in the user interface.</param>
        /// <param term='commandText'>Text requested by the neededText parameter.</param>
        /// <seealso class='Exec' />
        public void QueryStatus(string commandName, vsCommandStatusTextWanted neededText, ref vsCommandStatus status, ref object commandText)
        {
            if (neededText == vsCommandStatusTextWanted.vsCommandStatusTextWantedNone)
            {
                if (commandName == _commandManager.GetFullCommandName(CommandManager.DeployCommandName))
                {
                    status = GetStatusForStandardCommand();
                    return;
                }
                else if (commandName == _commandManager.GetFullCommandName(CommandManager.UndeployCommandName))
                {
                    status = GetStatusForStandardCommand();
                    return;
                }
                else if (commandName == _commandManager.GetFullCommandName(CommandManager.DeployRulesCommandName))
                {
                    status = GetStatusForStandardCommand();
                    return;
                }
                else if (commandName == _commandManager.GetFullCommandName(CommandManager.UndeployRulesCommandName))
                {
                    status = GetStatusForStandardCommand();
                    return;
                }
                else if (commandName == _commandManager.GetFullCommandName(CommandManager.ExportSettingsCommandName))
                {
                    status = GetStatusForStandardCommand();
                    return;
                }
                else if (commandName == _commandManager.GetFullCommandName(CommandManager.ImportBindingsCommandName))
                {
                    status = GetStatusForStandardCommand();
                    return;
                }
                else if (commandName == _commandManager.GetFullCommandName(CommandManager.BounceCommandName))
                {
                    status = GetStatusForStandardCommand();
                    return;
                }
                else if (commandName == _commandManager.GetFullCommandName(CommandManager.PreprocessBindingsCommandName))
                {
                    status = GetStatusForStandardCommand();
                    return;
                }
                else if (commandName == _commandManager.GetFullCommandName(CommandManager.DecodeBindingsCommandName))
                {
                    status = GetStatusForStandardCommand();
                    return;
                }
                else if (commandName == _commandManager.GetFullCommandName(CommandManager.UpdateSSOCommandName))
                {
                    status = GetStatusForStandardCommand();
                    return;
                }
                else if (commandName == _commandManager.GetFullCommandName(CommandManager.UpdateOrchsCommandName))
                {
                    status = GetStatusForStandardCommand();
                    return;
                }
                else if (commandName == _commandManager.GetFullCommandName(CommandManager.BuildMSICommandName))
                {
                    status = GetStatusForStandardCommand();
                    return;
                }
                else if (commandName == _commandManager.GetFullCommandName(CommandManager.GACProjectOutputCommandName))
                {
                    status = GetStatusForStandardCommand();
                    return;
                }
                else if (commandName == _commandManager.GetFullCommandName(CommandManager.TerminateInstancesCommandName))
                {
                    status = GetStatusForStandardCommand();
                    return;
                }
            }
        }

        /// <summary>Implements the Exec method of the IDTCommandTarget interface. This is called when the command is invoked.</summary>
        /// <param term='commandName'>The name of the command to execute.</param>
        /// <param term='executeOption'>Describes how the command should be run.</param>
        /// <param term='varIn'>Parameters passed from the caller to the command handler.</param>
        /// <param term='varOut'>Parameters passed from the command handler to the caller.</param>
        /// <param term='handled'>Informs the caller if the command was handled or not.</param>
        /// <seealso class='Exec' />
        public void Exec(string commandName, vsCommandExecOption executeOption, ref object varIn, ref object varOut, ref bool handled)
        {
            handled = false;
            if (executeOption == vsCommandExecOption.vsCommandExecOptionDoDefault)
            {
                if (commandName == _commandManager.GetFullCommandName(CommandManager.DeployCommandName))
                {
                    ExecuteDeploy();
                    handled = true;
                    return;
                }
                else if (commandName == _commandManager.GetFullCommandName(CommandManager.UndeployCommandName))
                {
                    ExecuteUndeploy();
                    handled = true;
                    return;
                }
                else if (commandName == _commandManager.GetFullCommandName(CommandManager.DeployRulesCommandName))
                {
                    ExecuteDeployRules();
                    handled = true;
                    return;
                }
                else if (commandName == _commandManager.GetFullCommandName(CommandManager.UndeployRulesCommandName))
                {
                    ExecuteUndeployRules();
                    handled = true;
                    return;
                }
                else if (commandName == _commandManager.GetFullCommandName(CommandManager.ExportSettingsCommandName))
                {
                    ExecuteExportSettings();
                    handled = true;
                    return;
                }
                else if (commandName == _commandManager.GetFullCommandName(CommandManager.ImportBindingsCommandName))
                {
                    ExecuteImportBindings();
                    handled = true;
                    return;
                }
                else if (commandName == _commandManager.GetFullCommandName(CommandManager.BounceCommandName))
                {
                    ExecuteBounce();
                    handled = true;
                    return;
                }
                else if (commandName == _commandManager.GetFullCommandName(CommandManager.PreprocessBindingsCommandName))
                {
                    ExecutePreprocessBindings();
                    handled = true;
                    return;
                }
                else if (commandName == _commandManager.GetFullCommandName(CommandManager.DecodeBindingsCommandName))
                {
                    ExecuteDecodeBindings();
                    handled = true;
                    return;
                }
                else if (commandName == _commandManager.GetFullCommandName(CommandManager.UpdateSSOCommandName))
                {
                    ExecuteUpdateSso();
                    handled = true;
                    return;
                }
                else if (commandName == _commandManager.GetFullCommandName(CommandManager.UpdateOrchsCommandName))
                {
                    ExecuteQuickDeploy();
                    handled = true;
                    return;
                }
                else if (commandName == _commandManager.GetFullCommandName(CommandManager.BuildMSICommandName))
                {
                    ExecuteBuildMsi();
                    handled = true;
                    return;
                }
                else if (commandName == _commandManager.GetFullCommandName(CommandManager.GACProjectOutputCommandName))
                {
                    ExecuteGacProjectOutput();
                    handled = true;
                    return;
                }
                else if (commandName == _commandManager.GetFullCommandName(CommandManager.TerminateInstancesCommandName))
                {
                    ExecuteTerminateInstances();
                    handled = true;
                    return;
                }
            }
        }

        private void AddTemporaryUI()
        {
            _msbuildPath = Util.GetMsBuildPath(_ideVersion);
            //_msbuildVerbositySwitch = GetMSBuildOutputVerbositySwitch();
            _gacUtilPath = Util.GetGacUtilPath();

            CommandBars commandBars = (CommandBars)_applicationObject.CommandBars;

            //Find the MenuBar command bar, which is the top-level command bar holding all the main menu items
            CommandBar menuCommandBar = commandBars["MenuBar"];

            //Find the Tools command bar on the MenuBar command bar:
            string toolsMenuName = GetToolsMenuName();
            CommandBar toolsCommandBar = commandBars[toolsMenuName];

            try
            {
                _commandBarPopup = (CommandBarPopup)toolsCommandBar.Controls["DeploymentFrameworkforBizTalkCommandBar"];
            }
            catch (Exception)
            {
            }

            if (_commandBarPopup == null)
            {
                try
                {
                    _commandBarPopup =
                        (CommandBarPopup)toolsCommandBar.Controls.Add(MsoControlType.msoControlPopup, _missingType, _missingType, 1, true);
                }
                catch (ArgumentException)
                {
                }

                _commandBarPopup.CommandBar.Name = "DeploymentFrameworkforBizTalkCommandBar";
                _commandBarPopup.Caption = "Deployment Framework for BizTalk";
            }

            // Create a new toolbar
            _toolbar = commandBars.Add("Deployment Framework for BizTalk", MsoBarPosition.msoBarTop, System.Type.Missing, true);

            //Add a control for the command to the tools menu:
            Command command = null;
            CommandBarButton commandBarPopupButton = null;
            CommandBarButton toolBarButton = null;

            command = _applicationObject.Commands.Item(_commandManager.GetFullCommandName(CommandManager.DeployCommandName), -1);
            commandBarPopupButton = (CommandBarButton)command.AddControl(_commandBarPopup.CommandBar, _commandBarPopup.Controls.Count + 1);

            command = _applicationObject.Commands.Item(_commandManager.GetFullCommandName(CommandManager.UndeployCommandName), -1);
            commandBarPopupButton = (CommandBarButton)command.AddControl(_commandBarPopup.CommandBar, _commandBarPopup.Controls.Count + 1);

            command = _applicationObject.Commands.Item(_commandManager.GetFullCommandName(CommandManager.UpdateOrchsCommandName), -1);
            commandBarPopupButton = (CommandBarButton)command.AddControl(_commandBarPopup.CommandBar, _commandBarPopup.Controls.Count + 1);

            command = _applicationObject.Commands.Item(_commandManager.GetFullCommandName(CommandManager.BuildMSICommandName), -1);
            commandBarPopupButton = (CommandBarButton)command.AddControl(_commandBarPopup.CommandBar, _commandBarPopup.Controls.Count + 1);

            command = _applicationObject.Commands.Item(_commandManager.GetFullCommandName(CommandManager.DeployRulesCommandName), -1);
            commandBarPopupButton = (CommandBarButton)command.AddControl(_commandBarPopup.CommandBar, _commandBarPopup.Controls.Count + 1);
            commandBarPopupButton.BeginGroup = true;

            command = _applicationObject.Commands.Item(_commandManager.GetFullCommandName(CommandManager.UndeployRulesCommandName), -1);
            commandBarPopupButton = (CommandBarButton)command.AddControl(_commandBarPopup.CommandBar, _commandBarPopup.Controls.Count + 1);

            command = _applicationObject.Commands.Item(_commandManager.GetFullCommandName(CommandManager.BounceCommandName), -1);
            commandBarPopupButton = (CommandBarButton)command.AddControl(_commandBarPopup.CommandBar, _commandBarPopup.Controls.Count + 1);
            commandBarPopupButton.BeginGroup = true;

            command = _applicationObject.Commands.Item(_commandManager.GetFullCommandName(CommandManager.TerminateInstancesCommandName), -1);
            commandBarPopupButton = (CommandBarButton)command.AddControl(_commandBarPopup.CommandBar, _commandBarPopup.Controls.Count + 1);

            command = _applicationObject.Commands.Item(_commandManager.GetFullCommandName(CommandManager.ExportSettingsCommandName), -1);
            commandBarPopupButton = (CommandBarButton)command.AddControl(_commandBarPopup.CommandBar, _commandBarPopup.Controls.Count + 1);
            commandBarPopupButton.BeginGroup = true;

            command = _applicationObject.Commands.Item(_commandManager.GetFullCommandName(CommandManager.GACProjectOutputCommandName), -1);
            commandBarPopupButton = (CommandBarButton)command.AddControl(_commandBarPopup.CommandBar, _commandBarPopup.Controls.Count + 1);

            command = _applicationObject.Commands.Item(_commandManager.GetFullCommandName(CommandManager.DecodeBindingsCommandName), -1);
            commandBarPopupButton = (CommandBarButton)command.AddControl(_commandBarPopup.CommandBar, _commandBarPopup.Controls.Count + 1);

            command = _applicationObject.Commands.Item(_commandManager.GetFullCommandName(CommandManager.ImportBindingsCommandName), -1);
            commandBarPopupButton = (CommandBarButton)command.AddControl(_commandBarPopup.CommandBar, _commandBarPopup.Controls.Count + 1);

            command = _applicationObject.Commands.Item(_commandManager.GetFullCommandName(CommandManager.PreprocessBindingsCommandName), -1);
            commandBarPopupButton = (CommandBarButton)command.AddControl(_commandBarPopup.CommandBar, _commandBarPopup.Controls.Count + 1);

            command = _applicationObject.Commands.Item(_commandManager.GetFullCommandName(CommandManager.UpdateSSOCommandName), -1);
            commandBarPopupButton = (CommandBarButton)command.AddControl(_commandBarPopup.CommandBar, _commandBarPopup.Controls.Count + 1);


            command = _applicationObject.Commands.Item(_commandManager.GetFullCommandName(CommandManager.UpdateOrchsCommandName), -1);
            toolBarButton = (CommandBarButton)command.AddControl(_toolbar, 1);
            toolBarButton.Style = MsoButtonStyle.msoButtonAutomatic;

            command = _applicationObject.Commands.Item(_commandManager.GetFullCommandName(CommandManager.DeployCommandName), -1);
            toolBarButton = (CommandBarButton)command.AddControl(_toolbar, 2);
            toolBarButton.Style = MsoButtonStyle.msoButtonAutomatic;

            command = _applicationObject.Commands.Item(_commandManager.GetFullCommandName(CommandManager.UndeployCommandName), -1);
            toolBarButton = (CommandBarButton)command.AddControl(_toolbar, 3);
            toolBarButton.Style = MsoButtonStyle.msoButtonAutomatic;

            command = _applicationObject.Commands.Item(_commandManager.GetFullCommandName(CommandManager.BounceCommandName), -1);
            toolBarButton = (CommandBarButton)command.AddControl(_toolbar, 4);
            toolBarButton.Style = MsoButtonStyle.msoButtonAutomatic;

            command = _applicationObject.Commands.Item(_commandManager.GetFullCommandName(CommandManager.BuildMSICommandName), -1);
            toolBarButton = (CommandBarButton)command.AddControl(_toolbar, 5);
            toolBarButton.Style = MsoButtonStyle.msoButtonAutomatic;

            command = _applicationObject.Commands.Item(_commandManager.GetFullCommandName(CommandManager.UpdateSSOCommandName), -1);
            toolBarButton = (CommandBarButton)command.AddControl(_toolbar, 6);
            toolBarButton.Style = MsoButtonStyle.msoButtonAutomatic;

            command = _applicationObject.Commands.Item(_commandManager.GetFullCommandName(CommandManager.TerminateInstancesCommandName), -1);
            toolBarButton = (CommandBarButton)command.AddControl(_toolbar, 7);
            toolBarButton.Style = MsoButtonStyle.msoButtonAutomatic;

            command = _applicationObject.Commands.Item(_commandManager.GetFullCommandName(CommandManager.GACProjectOutputCommandName), -1);
            toolBarButton = (CommandBarButton)command.AddControl(_toolbar, 8);
            toolBarButton.Style = MsoButtonStyle.msoButtonAutomatic;

            bool toolbarIsVisible = bool.Parse(Registry.GetValue(_registryKeyPath, "ToolbarVisible", true).ToString());
            _toolbar.Visible = toolbarIsVisible;
        }

        private string GetToolsMenuName()
        {
            string toolsMenuName;

            try
            {
                //If you would like to move the command to a different menu, change the word "Tools" to the 
                //  English version of the menu. This code will take the culture, append on the name of the menu
                //  then add the command to that menu. You can find a list of all the top-level menus in the file
                //  CommandBar.resx.
                ResourceManager resourceManager =
                    new ResourceManager("DeploymentFramework.VisualStudioAddIn.CommandBar", Assembly.GetExecutingAssembly());
                CultureInfo cultureInfo = new System.Globalization.CultureInfo(_applicationObject.LocaleID);
                string resourceName = String.Concat(cultureInfo.TwoLetterISOLanguageName, "Tools");
                toolsMenuName = resourceManager.GetString(resourceName);
            }
            catch
            {
                //We tried to find a localized version of the word Tools, but one was not found.
                //  Default to the en-US word, which may work for the current culture.
                toolsMenuName = "Tools";
            }

            return toolsMenuName;
        }

        private vsCommandStatus GetStatusForStandardCommand()
        {
            bool isSolutionOpen = _applicationObject.Solution.IsOpen;

            if (_commandRunner.IsBusy == 1)
            {
                return vsCommandStatus.vsCommandStatusSupported;
            }
            else if (isSolutionOpen)
            {
                return vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
            }
            else
            {
                return vsCommandStatus.vsCommandStatusSupported;
            }
        }

        private void ExecuteGacProjectOutput()
        {
            Array projects = (Array)_applicationObject.ActiveSolutionProjects;

            if (projects.Length > 0)
            {
                if (projects.Length > 1)
                {
                    MessageBox.Show("Please select only one project.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }

                Project proj = projects.GetValue(0) as Project;

                if (proj.ConfigurationManager == null)
                {
                    return;
                }

                OutputGroup primaryOutputGroup = proj.ConfigurationManager.ActiveConfiguration.OutputGroups.Item("Built");
                object[] primaryOutputs = primaryOutputGroup.FileURLs as object[];

                Uri path = new Uri(primaryOutputs[0].ToString());

                string arguments = string.Format("/i \"{0}\" /f", Path.GetFullPath(path.LocalPath));
                _commandRunner.ExecuteBuild(_gacUtilPath, arguments);
            }
        }

        private void ExecuteBuildMsi()
        {
            string projectPath = Util.GetDeploymentProjectPath(GetActiveSolutionFileName());
            string arguments = string.Format("\"{0}\" /nologo /t:Installer /clp:DisableMPLogging /p:Configuration={1}", projectPath, GetActiveSolutionConfiguration());
            _commandRunner.ExecuteBuild(_msbuildPath, arguments);
        }

        private void ExecuteQuickDeploy()
        {
            string projectPath = Util.GetDeploymentProjectPath(GetActiveSolutionFileName());
            string arguments = string.Format("\"{0}\" /nologo /t:UpdateOrchestration /clp:DisableMPLogging /p:Configuration={1}", projectPath, GetActiveSolutionConfiguration());
            _commandRunner.ExecuteBuild(_msbuildPath, arguments);
        }

        private void ExecuteUpdateSso()
        {
            string projectPath = Util.GetDeploymentProjectPath(GetActiveSolutionFileName());
            string arguments = string.Format("\"{0}\" /nologo /t:DeploySSO /clp:DisableMPLogging /p:Configuration={1}", projectPath, GetActiveSolutionConfiguration());
            _commandRunner.ExecuteBuild(_msbuildPath, arguments);
        }

        private void ExecutePreprocessBindings()
        {
            string projectPath = Util.GetDeploymentProjectPath(GetActiveSolutionFileName());
            string arguments = string.Format("\"{0}\" /nologo /t:PreprocessBindings /clp:DisableMPLogging /p:Configuration={1}", projectPath, GetActiveSolutionConfiguration());
            _commandRunner.ExecuteBuild(_msbuildPath, arguments);
        }

        private void ExecuteDecodeBindings()
        {
            string projectPath = Util.GetDeploymentProjectPath(GetActiveSolutionFileName());
            string arguments = string.Format("\"{0}\" /nologo /t:DecodeBindings /clp:DisableMPLogging /p:Configuration={1}", projectPath, GetActiveSolutionConfiguration());
            _commandRunner.ExecuteBuild(_msbuildPath, arguments);
        }

        private void ExecuteBounce()
        {
            string projectPath = Util.GetDeploymentProjectPath(GetActiveSolutionFileName());
            string arguments = string.Format("\"{0}\" /nologo /t:BounceBizTalk /clp:DisableMPLogging", projectPath);
            _commandRunner.ExecuteBuild(_msbuildPath, arguments);
        }

        private void ExecuteImportBindings()
        {
            string projectPath = Util.GetDeploymentProjectPath(GetActiveSolutionFileName());
            string arguments = string.Format("\"{0}\" /nologo /t:ImportBindings /clp:DisableMPLogging /p:Configuration={1}", projectPath, GetActiveSolutionConfiguration());
            _commandRunner.ExecuteBuild(_msbuildPath, arguments);
        }

        private void ExecuteExportSettings()
        {
            string projectPath = Util.GetDeploymentProjectPath(GetActiveSolutionFileName());
            string arguments = string.Format("\"{0}\" /nologo /t:ExportSettings /clp:DisableMPLogging /p:Configuration={1}", projectPath, GetActiveSolutionConfiguration());
            _commandRunner.ExecuteBuild(_msbuildPath, arguments);
        }

        private void ExecuteUndeploy()
        {
            string projectPath = Util.GetDeploymentProjectPath(GetActiveSolutionFileName());
            string arguments = string.Format("\"{0}\" /nologo /t:Undeploy /clp:DisableMPLogging /p:Configuration={1}", projectPath, GetActiveSolutionConfiguration());
            _commandRunner.ExecuteBuild(_msbuildPath, arguments);
        }

        private void ExecuteDeploy()
        {
            string projectPath = Util.GetDeploymentProjectPath(GetActiveSolutionFileName());
            string arguments = string.Format("\"{0}\" /nologo /t:Deploy /clp:DisableMPLogging /p:Configuration={1}", projectPath, GetActiveSolutionConfiguration());
            _commandRunner.ExecuteBuild(_msbuildPath, arguments);
        }

        private void ExecuteUndeployRules()
        {
            string projectPath = Util.GetDeploymentProjectPath(GetActiveSolutionFileName());
            string arguments =
                string.Format("\"{0}\" /nologo /t:UndeployVocabAndRules /clp:DisableMPLogging /p:Configuration={1};RemoveRulePoliciesFromAppOnUndeploy=true", projectPath, GetActiveSolutionConfiguration());
            _commandRunner.ExecuteBuild(_msbuildPath, arguments);
        }

        private void ExecuteDeployRules()
        {
            string projectPath = Util.GetDeploymentProjectPath(GetActiveSolutionFileName());
            string arguments =
                string.Format("\"{0}\" /nologo /t:DeployVocabAndRules /clp:DisableMPLogging /p:Configuration={1};ExplicitlyDeployRulePoliciesOnDeploy=true", projectPath, GetActiveSolutionConfiguration());
            _commandRunner.ExecuteBuild(_msbuildPath, arguments);
        }

        private void ExecuteTerminateInstances()
        {
            string projectPath = Util.GetDeploymentProjectPath(GetActiveSolutionFileName());
            string arguments =
                string.Format("\"{0}\" /nologo /t:TerminateServiceInstances /clp:DisableMPLogging /p:Configuration={1}", projectPath, GetActiveSolutionConfiguration());
            _commandRunner.ExecuteBuild(_msbuildPath, arguments);
        }

        private string GetActiveSolutionConfiguration()
        {
            return _applicationObject.Solution.SolutionBuild.ActiveConfiguration.Name;
        }

        private string GetActiveSolutionFileName()
        {
            return _applicationObject.Solution.FileName;
        }

        //private string GetMSBuildOutputVerbositySwitch()
        //{
        //    EnvDTE.Properties p = _applicationObject.DTE.get_Properties("Environment", "ProjectsAndSolution");

        //    int mSBuildOutputVerbosity = (int)p.Item("MSBuildOutputVerbosity").Value;

        //    switch (mSBuildOutputVerbosity)
        //    {
        //        case 0:
        //            return "/v:q";
        //        case 1:
        //            return "/v:m";
        //        case 2:
        //            return "/v:n";
        //        case 3:
        //            return "/v:d";
        //        case 4:
        //            return "/v:diag";
        //        default:
        //            return "/v:n";
        //    }
        //}
    }
}
