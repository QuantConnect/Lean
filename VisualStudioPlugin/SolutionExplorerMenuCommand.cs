/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE80;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.VisualStudioPlugin
{
    /// <summary>
    /// Command handler for QuantConnect Solution Explorer menu buttons
    /// </summary>
    internal sealed class SolutionExplorerMenuCommand
    {
        /// <summary>
        /// Command IDs for Solution Explorer menu buttons
        /// </summary>
        public const int SendForBacktestingCommandId = 0x0100;
        public const int SaveToQuantConnectCommandId = 0x0110;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("00ce2ccb-74c7-42f4-bf63-52c573fc1532");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly QuantConnectPackage _package;

        private DTE2 _dte2;

        private ProjectFinder _projectFinder;

        private LogInCommand _logInCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="SolutionExplorerMenuCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private SolutionExplorerMenuCommand(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            _package = package as QuantConnectPackage;
            _dte2 = ServiceProvider.GetService(typeof(SDTE)) as DTE2;
            _logInCommand = CreateLogInCommand();

            var commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                RegisterSendForBacktesting(commandService);
                RegisterSaveToQuantConnect(commandService);
            }
        }

        private ProjectFinder CreateProjectFinder()
        {
            return new ProjectFinder(PathUtils.GetSolutionFolder(_dte2));
        }

        private LogInCommand CreateLogInCommand()
        {
            return new LogInCommand(_package.DataPath);
        }

        private void RegisterSendForBacktesting(OleMenuCommandService commandService)
        {
            var menuCommandID = new CommandID(CommandSet, SendForBacktestingCommandId);
            var oleMenuItem = new OleMenuCommand(new EventHandler(SendForBacktestingCallback), menuCommandID);
            commandService.AddCommand(oleMenuItem);
        }

        private void RegisterSaveToQuantConnect(OleMenuCommandService commandService)
        {
            var menuCommandID = new CommandID(CommandSet, SaveToQuantConnectCommandId);
            var oleMenuItem = new OleMenuCommand(new EventHandler(SaveToQuantConnectCallback), menuCommandID);
            commandService.AddCommand(oleMenuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static SolutionExplorerMenuCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return _package;
            }
        }

        // Lazily create ProjectFinder only when we have an opened solution
        private ProjectFinder ProjectFinder
        {
            get {
                if (_projectFinder == null)
                {
                    _projectFinder = CreateProjectFinder();
                }
                return _projectFinder;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new SolutionExplorerMenuCommand(package);
        }

        private void SendForBacktestingCallback(object sender, EventArgs e)
        {
            ExecuteOnProject(sender, (selectedProjectId, selectedProjectName, files) =>
            {
                uloadFilesToServer(selectedProjectId, files);
                VsUtils.DisplayInStatusBar(this.ServiceProvider, "Compiling project ...");
                var compileStatus = compileProjectOnServer(selectedProjectId);
                VsUtils.DisplayInStatusBar(this.ServiceProvider, "Backtesting project ...");
                backtestProjectOnServer(selectedProjectId, compileStatus.CompileId);
                VsUtils.DisplayInStatusBar(this.ServiceProvider, "Backtest complete.");

                var fileNames = files.Select(f => f.Item1).ToList();

                var message = string.Format(CultureInfo.CurrentCulture, "Send for backtesting to project {0}, files: {1}", selectedProjectId + " : " + selectedProjectName, string.Join(" ", fileNames));
                var title = "SendToBacktesting";

                // Show a message box to prove we were here
                VsShellUtilities.ShowMessageBox(
                    this.ServiceProvider,
                    message,
                    title,
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            });
        }

        private void SaveToQuantConnectCallback(object sender, EventArgs e)
        {
            ExecuteOnProject(sender, (selectedProjectId, selectedProjectName, files) =>
            {
                uloadFilesToServer(selectedProjectId, files);
       
                var fileNames = files.Select(f => f.Item1).ToList();      

                var message = string.Format(CultureInfo.CurrentCulture, "Save to project {0}, files {1}", selectedProjectId + " : " + selectedProjectName , string.Join(" ", fileNames));
                var title = "SaveToQuantConnect";

                // Show a message box to prove we were here
                VsShellUtilities.ShowMessageBox(
                    this.ServiceProvider,
                    message,
                    title,
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            });
        }

        private void uloadFilesToServer(int selectedProjectId, List<Tuple<string, string>> files)
        {
            var api = AuthorizationManager.GetInstance().GetApi();
            foreach (Tuple<string, string> file in files)
            {
                api.DeleteProjectFile(selectedProjectId, file.Item1);
                string fileContent = System.IO.File.ReadAllText(file.Item2);
                api.AddProjectFile(selectedProjectId, file.Item1, fileContent);
            }
        }

        private Api.Compile compileProjectOnServer(int projectId)
        {
            var api = AuthorizationManager.GetInstance().GetApi();
            Api.Compile compileStatus = api.CreateCompile(projectId);
            var compileId = compileStatus.CompileId;
            while (compileStatus.State == Api.CompileState.InQueue)
            {
                System.Threading.Thread.Sleep(5000);
                compileStatus = api.ReadCompile(projectId, compileId);
            }
            return compileStatus;
        }

        private Api.Backtest backtestProjectOnServer(int projectId, string compileId)
        {
            var api = AuthorizationManager.GetInstance().GetApi();
            Api.Backtest backtestStatus = api.CreateBacktest(projectId, compileId, "My New Backtest");
            var backtestId = backtestStatus.BacktestId;
            while (!backtestStatus.Completed)
            {
                System.Threading.Thread.Sleep(5000);
                backtestStatus = api.ReadBacktest(projectId, backtestId);
            }
            return backtestStatus;
        }

        private void ExecuteOnProject(object sender, Action<int, string, List<Tuple<string, string>>> onProject)
        {
            if (_logInCommand.DoLogIn(this.ServiceProvider, explicitLogin: false))
            {
                var api = AuthorizationManager.GetInstance().GetApi();
                var projects = api.ListProjects().Projects;
                var projectNames = projects.Select(p => Tuple.Create(p.ProjectId, p.Name)).ToList();

                var files = GetSelectedFiles(sender);
                var fileNames = files.Select(tuple => tuple.Item1).ToList();
                var suggestedProjectName = ProjectFinder.ProjectNameForFiles(fileNames);
                var projectNameDialog = new ProjectNameDialog(projectNames, suggestedProjectName);
                VsUtils.DisplayDialogWindow(projectNameDialog);

                if (projectNameDialog.ProjectNameProvided())
                {
                    var selectedProjectName = projectNameDialog.GetSelectedProjectName();
                    var selectedProjectId = projectNameDialog.GetSelectedProjectId();
                    ProjectFinder.AssociateProjectWith(selectedProjectName, fileNames);

                    onProject.Invoke(selectedProjectId, selectedProjectName, files);
                }
            }
        }

        private List<Tuple<string, string>> GetSelectedFiles(object sender)
        {
            var myCommand = sender as OleMenuCommand;

            var selectedFiles = new List<Tuple<string, string>>();
            var selectedItems = (object[])_dte2.ToolWindows.SolutionExplorer.SelectedItems;
            foreach (EnvDTE.UIHierarchyItem selectedUIHierarchyItem in selectedItems)
            {
                if (selectedUIHierarchyItem.Object is EnvDTE.ProjectItem)
                {
                    var item = selectedUIHierarchyItem.Object as EnvDTE.ProjectItem;
                    var filePath = item.Properties.Item("FullPath").Value.ToString();
                    var fileAndItsPath = new Tuple<string, string>(item.Name, filePath);
                    selectedFiles.Add(fileAndItsPath);
                }
            }
            return selectedFiles;
        }
    }
}
