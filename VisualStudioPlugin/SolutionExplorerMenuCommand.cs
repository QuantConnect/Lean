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
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

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
        private const int _sendForBacktestingCommandId = 0x0100;
        private const int _saveToQuantConnectCommandId = 0x0110;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        private static readonly Guid _commandSet = new Guid("00ce2ccb-74c7-42f4-bf63-52c573fc1532");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly QuantConnectPackage _package;

        private readonly DTE2 _dte2;

        private ProjectFinder _projectFinder;

        /// <summary>
        /// Observer for the status of backtests launched from the plugin
        /// </summary>
        private readonly IBacktestObserver _backtestObserver;

        private readonly AuthenticationCommand _authenticationCommand;

        /// <summary>
        /// Instance of the solution explorer menu command.
        /// </summary>
        public static SolutionExplorerMenuCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider _serviceProvider => _package;

        // Lazily create _projectFinder only when we have an opened solution
        private ProjectFinder _lazyProjectFinder => _projectFinder ?? (_projectFinder = CreateProjectFinder());

        private ProjectFinder CreateProjectFinder()
        {
            return new ProjectFinder(PathUtils.GetSolutionFolder(_dte2));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SolutionExplorerMenuCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private SolutionExplorerMenuCommand(Package package, IBacktestObserver backtestObserver)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            _backtestObserver = backtestObserver;
            _package = package as QuantConnectPackage;
            _dte2 = _serviceProvider.GetService(typeof(SDTE)) as DTE2;
            _authenticationCommand = new AuthenticationCommand();

            var commandService = _serviceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                RegisterSendForBacktesting(commandService);
                RegisterSaveToQuantConnect(commandService);
            }
        }

        private void RegisterSendForBacktesting(OleMenuCommandService commandService)
        {
            var menuCommandId = new CommandID(_commandSet, _sendForBacktestingCommandId);
            var oleMenuItem = new OleMenuCommand(SendForBacktestingCallback, menuCommandId);
            commandService.AddCommand(oleMenuItem);
        }

        private void RegisterSaveToQuantConnect(OleMenuCommandService commandService)
        {
            var menuCommandId = new CommandID(_commandSet, _saveToQuantConnectCommandId);
            var oleMenuItem = new OleMenuCommand(SaveToQuantConnectCallback, menuCommandId);
            commandService.AddCommand(oleMenuItem);
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package, IBacktestObserver backtestObserver)
        {
            Instance = new SolutionExplorerMenuCommand(package, backtestObserver);
        }

        private void SendForBacktestingCallback(object sender, EventArgs e)
        {
            try
            {
                ExecuteOnProjectAsync(sender, async (selectedProjectId, selectedProjectName, files) =>
                {
                    var uploadResult = await System.Threading.Tasks.Task.Run(() =>
                        UploadFilesToServer(selectedProjectId, files));
                    if (!uploadResult)
                    {
                        return;
                    }

                    var compilationResult = await CompileProjectOnServer(selectedProjectId);
                    if (!compilationResult.Item1)
                    {
                        var errorDialog = new ErrorDialog("Compilation Error", compilationResult.Item2);
                        VsUtils.DisplayDialogWindow(errorDialog);
                        return;
                    }

                    var backtestResult = await BacktestProjectOnServer(selectedProjectId, compilationResult.Item2);
                    if (!backtestResult.Item1)
                    {
                        var errorDialog = new ErrorDialog("Backtest Failed", backtestResult.Item2.Error);
                        VsUtils.DisplayDialogWindow(errorDialog);
                        return;
                    }

                    var projectUrl = string.Format(
                        CultureInfo.CurrentCulture,
                        "https://www.quantconnect.com/terminal/#open/{0}/{1}",
                        selectedProjectId,
                        backtestResult.Item2.BacktestId
                    );
                    Process.Start(projectUrl);
                });
            }
            catch (Exception exception)
            {
                VsUtils.ShowErrorMessageBox(_serviceProvider, "QuantConnect Exception", exception.ToString());
            }
        }

        private void SaveToQuantConnectCallback(object sender, EventArgs e)
        {
            try
            {
                ExecuteOnProjectAsync(sender, (selectedProjectId, selectedProjectName, files) =>
                {
                    System.Threading.Tasks.Task.Factory.StartNew(() =>
                    {
                        UploadFilesToServer(selectedProjectId, files);
                    }, CancellationToken.None, TaskCreationOptions.AttachedToParent, TaskScheduler.Default);
                });
            }
            catch (Exception exception)
            {
                VsUtils.ShowErrorMessageBox(_serviceProvider, "QuantConnect Exception", exception.ToString());
            }
        }

        /// <summary>
        /// Uploads a list of files to a specific project at QuantConnect
        /// </summary>
        /// <param name="selectedProjectId">Target project Id</param>
        /// <param name="files">List of files to upload</param>
        /// <returns>Returns false if any file failed to be uploaded</returns>
        private bool UploadFilesToServer(int selectedProjectId, IEnumerable<SelectedItem> files)
        {
            VsUtils.DisplayInStatusBar(_serviceProvider, "Uploading files to server...");
            var api = AuthorizationManager.GetInstance().GetApi();
            // Counters to keep track of files uploaded or not
            var filesUploaded = 0;
            var filesNotUploaded = 0;
            foreach (var file in files)
            {
                api.DeleteProjectFile(selectedProjectId, file.FileName);
                try
                {
                    var fileContent = File.ReadAllText(file.FilePath);
                    var response = api.AddProjectFile(selectedProjectId, file.FileName, fileContent);
                    if (response.Success)
                    {
                        filesUploaded++;
                    }
                    else
                    {
                        VSActivityLog.Error("Failed to add project file " + file.FileName);
                        filesNotUploaded++;
                    }
                }
                catch (Exception exception)
                {
                    VSActivityLog.Error("Exception adding project file " + file.FileName + ". Exception " + exception);
                    filesNotUploaded++;
                }
            }
            // Update Status bar accordingly based on counters
            var message = "Files upload complete";
            message += (filesUploaded != 0) ? ". Uploaded " + filesUploaded : "";
            message += (filesNotUploaded != 0) ? ". Failed to upload " + filesNotUploaded : "";
            VsUtils.DisplayInStatusBar(_serviceProvider, message);

            // Return false if any file failed to be uploaded
            var result = filesNotUploaded == 0;
            if (!result)
            {
                VsUtils.ShowErrorMessageBox(_serviceProvider, "Upload Files Failed", message);
            }
            return result;
        }

        /// <summary>
        /// Compiles specific projectId at QuantConnect
        /// </summary>
        /// <param name="projectId">Target project Id</param>
        /// <returns>Tuple&lt;bool, string&gt;. Item1 is true if compilation succeeded.
        /// Item2 is compile Id if compilation succeeded else error message.</returns>
        private async Task<Tuple<bool, string>> CompileProjectOnServer(int projectId)
        {
            VsUtils.DisplayInStatusBar(_serviceProvider, "Compiling project...");
            var api = AuthorizationManager.GetInstance().GetApi();
            var compileStatus = await System.Threading.Tasks.Task.Run(() => api.CreateCompile(projectId));
            var compileId = compileStatus.CompileId;

            while (compileStatus.State == Api.CompileState.InQueue)
            {
                compileStatus = await System.Threading.Tasks.Task.Delay(2000).
                    ContinueWith(_ => api.ReadCompile(projectId, compileId));
            }

            if (compileStatus.State == Api.CompileState.BuildError)
            {
                // Default to show Errors, now it is coming empty so use Logs. Will only show First Error || Log
                var error = compileStatus.Errors.Count == 0 ?
                    compileStatus.Logs.FirstOrDefault() : compileStatus.Errors.FirstOrDefault();
                VsUtils.DisplayInStatusBar(_serviceProvider, "Error when compiling project");
                return new Tuple<bool, string>(false, error);
            }
            VsUtils.DisplayInStatusBar(_serviceProvider, "Compilation completed successfully");
            return new Tuple<bool, string>(true, compileStatus.CompileId);
        }

        /// <summary>
        /// Backtests specific projectId and compileId at QuantConnect
        /// </summary>
        /// <param name="projectId">Target project Id</param>
        /// <param name="compileId">Target compile Id</param>
        /// <returns>Tuple&lt;bool, string&gt;. Item1 is true if backtest succeeded.
        /// Item2 is the corresponding Api.Backtest instance</returns>
        private async Task<Tuple<bool, Api.Backtest>> BacktestProjectOnServer(int projectId, string compileId)
        {
            VsUtils.DisplayInStatusBar(_serviceProvider, "Backtesting project...");
            var api = AuthorizationManager.GetInstance().GetApi();
            var backtestName = BacktestNameProvider.GetNewName();
            var backtestStatus = await System.Threading.Tasks.Task.Run(() => api.CreateBacktest(projectId, compileId, backtestName));
            var backtestId = backtestStatus.BacktestId;

            // Notify observer new backtest
            _backtestObserver.BacktestCreated(projectId, backtestStatus);

            var errorPresent = false;
            while (backtestStatus.Progress < 1 && !errorPresent)
            {
                backtestStatus = await System.Threading.Tasks.Task.Delay(4000).
                    ContinueWith(_ => api.ReadBacktest(projectId, backtestId));

                errorPresent = !string.IsNullOrEmpty(backtestStatus.Error) ||
                               !string.IsNullOrEmpty(backtestStatus.StackTrace);
                // Notify observer backtest status
                _backtestObserver.BacktestStatusUpdated(projectId, backtestStatus);
            }

            // Notify observer backtest finished
            _backtestObserver.BacktestFinished(projectId, backtestStatus);
            if (errorPresent)
            {
                VsUtils.DisplayInStatusBar(_serviceProvider, "Error when backtesting project");
                return new Tuple<bool, Api.Backtest>(false, backtestStatus);
            }
            var successMessage = "Backtest completed successfully";
            VsUtils.DisplayInStatusBar(_serviceProvider, successMessage);
            return new Tuple<bool, Api.Backtest>(true, backtestStatus);
        }

        private async void ExecuteOnProjectAsync(object sender, Action<int, string, List<SelectedItem>> onProject)
        {
            if (await _authenticationCommand.Login(_serviceProvider, false))
            {
                var projects = await System.Threading.Tasks.Task.Run(() =>
                {
                    var api = AuthorizationManager.GetInstance().GetApi();
                    return api.ListProjects().Projects;
                });

                var projectNames = projects.Select(p => Tuple.Create(p.ProjectId, p.Name, p.Language)).ToList();

                var files = GetSelectedFiles(sender);
                var fileNames = files.Select(tuple => tuple.FileName).ToList();
                var suggestedProjectName = _lazyProjectFinder.ProjectNameForFiles(fileNames);
                var projectNameDialog = new ProjectNameDialog(projectNames, suggestedProjectName);
                VsUtils.DisplayDialogWindow(projectNameDialog);

                if (projectNameDialog.ProjectNameProvided)
                {
                    var selectedProjectName = projectNameDialog.SelectedProjectName;
                    var selectedProjectId = projectNameDialog.SelectedProjectId;
                    _lazyProjectFinder.AssociateProjectWith(selectedProjectName, fileNames);

                    if (!selectedProjectId.HasValue)
                    {
                        var newProjectLanguage = PathUtils.DetermineProjectLanguage(files.Select(f => f.FilePath).ToList());
                        if (!newProjectLanguage.HasValue)
                        {
                            VsUtils.ShowMessageBox(_serviceProvider, "Failed to determine project language",
                                $"Failed to determine programming laguage for a project");
                            return;
                        }

                        selectedProjectId = CreateQuantConnectProject(selectedProjectName, newProjectLanguage.Value);
                        if (!selectedProjectId.HasValue)
                        {
                            VsUtils.ShowMessageBox(_serviceProvider, "Failed to create a project", $"Failed to create a project {selectedProjectName}");
                        }
                        onProject.Invoke(selectedProjectId.Value, selectedProjectName, files);
                    }
                    else
                    {
                        onProject.Invoke(selectedProjectId.Value, selectedProjectName, files);
                    }
                }
            }
        }

        private int? CreateQuantConnectProject(string projectName, Language projectLanguage)
        {
            var api = AuthorizationManager.GetInstance().GetApi();
            var projectResponse = api.CreateProject(projectName, projectLanguage);
            if (!projectResponse.Success)
            {
                return null;
            }
            return projectResponse.Projects[0].ProjectId;
        }

        private List<SelectedItem> GetSelectedFiles(object sender)
        {
            var selectedFiles = new List<SelectedItem>();
            var selectedItems = (object[])_dte2.ToolWindows.SolutionExplorer.SelectedItems;
            foreach (EnvDTE.UIHierarchyItem selectedUIHierarchyItem in selectedItems)
            {
                if (selectedUIHierarchyItem.Object is EnvDTE.ProjectItem)
                {
                    var item = selectedUIHierarchyItem.Object as EnvDTE.ProjectItem;
                    var filePath = item.Properties.Item("FullPath").Value.ToString();
                    var selectedItem = new SelectedItem
                    {
                        FileName = item.Name,
                        FilePath = filePath
                    };
                    selectedFiles.Add(selectedItem);
                }
            }

            // Check if the user selected a folder, and include files in directories otherwise language inference breaks.
            // Also to maintain child folder structure on webclient tweak the filename to contain folders expressed in URI format.
            if (selectedFiles.Count == 1 &&
                string.IsNullOrEmpty(Path.GetExtension(selectedFiles.First().FilePath)))
            {
                var basePath = selectedFiles.First().FilePath;
                var nonFolders = Directory.GetFiles(selectedFiles.First().FilePath, "*", SearchOption.AllDirectories);

                selectedFiles = nonFolders.Select(c => new SelectedItem
                {
                    FileName = "/" + c.Replace(basePath, string.Empty).Replace("\\", "/"),
                    FilePath = c
                }).ToList();
            }

            return selectedFiles;
        }

        private class SelectedItem
        {
            public string FileName
            {
                get; set;
            }

            public string FilePath
            {
                get; set;
            }
        }
    }
}
