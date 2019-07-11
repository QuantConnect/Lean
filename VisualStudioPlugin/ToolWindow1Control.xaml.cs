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

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;

namespace QuantConnect.VisualStudioPlugin
{
    using System.Linq;
    using System.Windows.Controls;
    using System;
    using System.Windows;
    using Microsoft.VisualStudio.Shell;

    /// <summary>
    /// Interaction logic for ToolWindow1Control.
    /// </summary>
    internal partial class ToolWindow1Control : UserControl, IBacktestObserver
    {
        private readonly ObservableCollection<DataGridItem> _dataGridCollection;

        private readonly AuthenticationCommand _authenticationCommand;
        // Limit to the amount of backtest to add to the table
        private const int _maximumBacktestToShow = 50;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolWindow1Control"/> class.
        /// </summary>
        public ToolWindow1Control()
        {
            _dataGridCollection = new ObservableCollection<DataGridItem>();
            InitializeComponent();
            dataGrid.ItemsSource = _dataGridCollection;
            _authenticationCommand = new AuthenticationCommand();
            // sending false to avoid blocking UI when VS is starting and toolwindow is open
            UpdateAvailableProjects(false);
        }

        /// <summary>
        /// Callback for project combo box. Updates available backtest for the selected project.
        /// </summary>
        private void ProjectNameBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            var selectedItem = projectNameBox.SelectedItem as ComboboxProjectItem;
            if (selectedItem != null)
            {
                var projectId = selectedItem.Id;
                UpdateAvailableBacktests(projectId);
            }
        }

        /// <summary>
        /// Updates available backtests in the backtest data grid for a specific project
        /// </summary>
        /// <param name="projectId">Target project id</param>
        private async void UpdateAvailableBacktests(int projectId)
        {
            VsUtils.DisplayInStatusBar(ServiceProvider.GlobalProvider, "Loading project backtests...");
            if (await _authenticationCommand.Login(ServiceProvider.GlobalProvider, false))
            {
                try
                {
                    var backtestsList = await System.Threading.Tasks.Task.Run(() =>
                    {
                        var api = AuthorizationManager.GetInstance().GetApi();
                        var result = api.ListBacktests(projectId);
                        return result.Backtests;
                    });
                    var selectedItem = projectNameBox.SelectedItem as ComboboxProjectItem;
                    // Verify the backtest are from the selected project
                    if (selectedItem?.Id == projectId)
                    {
                        lock (_dataGridCollection)
                        {
                            _dataGridCollection.Clear();
                            // Setting a limit of _maximumBacktestToShow backtests in the table...
                            for (var i = 0; i < backtestsList.Count && i < _maximumBacktestToShow; i++)
                            {
                                _dataGridCollection.Add(new DataGridItem
                                {
                                    Name = backtestsList[i].Name,
                                    Progress = backtestsList[i].Progress,
                                    ProjectId = projectId,
                                    Date = backtestsList[i].Created,
                                    BacktestId = backtestsList[i].BacktestId,
                                    Status = string.IsNullOrEmpty(backtestsList[i].Error) ?
                                        DataGridItem.BacktestSucceeded : DataGridItem.BacktestFailed,
                                    Note = backtestsList[i].Note
                                });
                            }
                        }
                        VsUtils.DisplayInStatusBar(ServiceProvider.GlobalProvider, "Successfully loaded backtests");
                    }
                }
                catch (Exception exception)
                {
                    VsUtils.ShowErrorMessageBox(ServiceProvider.GlobalProvider,
                        "QuantConnect Exception", exception.ToString());
                }
            }
        }

        /// <summary>
        /// Updates available projects in the combo box
        /// </summary>
        public async void UpdateAvailableProjects(bool showLoginDialog = true)
        {
            VsUtils.DisplayInStatusBar(ServiceProvider.GlobalProvider, "Loading available projects...");
            if (await _authenticationCommand.Login(ServiceProvider.GlobalProvider, false, showLoginDialog))
            {
                try
                {
                    var projectNames = await System.Threading.Tasks.Task.Run(() =>
                    {
                        var api = AuthorizationManager.GetInstance().GetApi();
                        var projects = api.ListProjects().Projects;
                        return projects.Select(p => Tuple.Create(p.ProjectId, p.Name, p.Language)).ToList();
                    });
                    // Clear available projects
                    projectNameBox.Items.Clear();
                    lock (_dataGridCollection)
                    {
                        // Clear rows in the backtest data grid
                        _dataGridCollection.Clear();
                    }
                    if (projectNames.Count > 0)
                    {
                        projectNames.ForEach(p => projectNameBox.Items.Add(new ComboboxProjectItem(p.Item1, p.Item2, p.Item3)));
                        VsUtils.DisplayInStatusBar(ServiceProvider.GlobalProvider, "Successfully loaded projects");
                        // Select first project and load available backtest
                        var project = projectNameBox.Items[0] as ComboboxProjectItem;
                        projectNameBox.SelectedItem = project;
                    }
                    else
                    {
                        VsUtils.DisplayInStatusBar(ServiceProvider.GlobalProvider, "No projects available");
                    }
                }
                catch (Exception exception)
                {
                    VsUtils.ShowErrorMessageBox(ServiceProvider.GlobalProvider,
                        "QuantConnect Exception", exception.ToString());
                }
            }
        }

        /// <summary>
        /// Callback for the open button in the backtest data grid, shared by all rows. Opens backtest.
        /// </summary>
        private void Open_OnClick(object sender, RoutedEventArgs e)
        {
            var obj = ((FrameworkElement)sender).DataContext as DataGridItem;
            if (obj != null)
            {
                VsUtils.DisplayInStatusBar(ServiceProvider.GlobalProvider, "Opening backtest");
                var projectUrl = string.Format(
                    CultureInfo.CurrentCulture,
                    "https://www.quantconnect.com/terminal/#open/{0}/{1}",
                    obj.ProjectId,
                    obj.BacktestId
                );
                Process.Start(projectUrl);
            }
        }

        /// <summary>
        /// Callback for the delete button in the backtest data grid, shared by all rows. Deletes backtest.
        /// </summary>
        private async void Delete_OnClick(object sender, RoutedEventArgs e)
        {
            var obj = ((FrameworkElement)sender).DataContext as DataGridItem;
            if (obj != null)
            {
                var projectId = obj.ProjectId;
                var backtestId = obj.BacktestId;
                VsUtils.DisplayInStatusBar(ServiceProvider.GlobalProvider, "Deleting backtest...");
                var deleteResult = false;
                if (await _authenticationCommand.Login(ServiceProvider.GlobalProvider, false))
                {
                    try
                    {
                        deleteResult = await System.Threading.Tasks.Task.Run(() =>
                        {
                            var api = AuthorizationManager.GetInstance().GetApi();
                            return api.DeleteBacktest(projectId, backtestId).Success;
                        });
                    }
                    catch (Exception exception)
                    {
                        VsUtils.ShowErrorMessageBox(ServiceProvider.GlobalProvider,
                            "QuantConnect Exception", exception.ToString());
                    }
                }
                if (deleteResult)
                {
                    var selectedItem = projectNameBox.SelectedItem as ComboboxProjectItem;
                    // Verify the backtest is from the selected project
                    if (selectedItem?.Id == projectId)
                    {
                        lock (_dataGridCollection)
                        {
                            foreach (DataGridItem item in _dataGridCollection)
                            {
                                if (item.BacktestId == backtestId)
                                {
                                    _dataGridCollection.Remove(item);
                                    break;
                                }
                            }
                        }
                    }
                    VsUtils.DisplayInStatusBar(ServiceProvider.GlobalProvider, "Successfully deleted backtest");
                }
                else
                {
                    VsUtils.DisplayInStatusBar(ServiceProvider.GlobalProvider, "Error when deleting backtest");
                }
            }
        }

        /// <summary>
        /// Creates new backtest, row, in the backtest data grid (UI)
        /// </summary>
        /// <param name="projectId">Target project id</param>
        /// <param name="backtestStatus">Backtest current status</param>
        public void BacktestCreated(int projectId, Api.Backtest backtestStatus)
        {
            var selectedItem = projectNameBox.SelectedItem as ComboboxProjectItem;
            // Verify the backtest is from the selected project
            if (selectedItem?.Id == projectId)
            {
                lock (_dataGridCollection)
                {
                    _dataGridCollection.Insert(0, new DataGridItem
                    {
                        Name = backtestStatus.Name,
                        Progress = 0,
                        ProjectId = projectId,
                        BacktestId = backtestStatus.BacktestId,
                        Date = backtestStatus.Created,
                        Status = DataGridItem.BacktestInProgress
                    });
                }
            }
        }

        /// <summary>
        /// Updates backtest Progress in the backtest data grid (UI)
        /// </summary>
        /// <param name="projectId">Target project id</param>
        /// <param name="backtestStatus">Backtest current status</param>
        public void BacktestStatusUpdated(int projectId, Api.Backtest backtestStatus)
        {
            var selectedItem = projectNameBox.SelectedItem as ComboboxProjectItem;
            // Verify the backtest is from the selected project
            if (selectedItem?.Id == projectId)
            {
                lock (_dataGridCollection)
                {
                    foreach (DataGridItem item in _dataGridCollection)
                    {
                        if (item.BacktestId == backtestStatus.BacktestId)
                        {
                            item.Progress = backtestStatus.Progress;
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updates backtest Status and Progress in the backtest data grid (UI)
        /// </summary>
        /// <param name="projectId">Target project id</param>
        /// <param name="backtestStatus">Backtest current status</param>
        public void BacktestFinished(int projectId, Api.Backtest backtestStatus)
        {
            var selectedItem = projectNameBox.SelectedItem as ComboboxProjectItem;
            // Verify the backtest is from the selected project
            if (selectedItem?.Id == projectId)
            {
                lock (_dataGridCollection)
                {
                    foreach (DataGridItem item in _dataGridCollection)
                    {
                        if (item.BacktestId == backtestStatus.BacktestId)
                        {
                            var success = string.IsNullOrEmpty(backtestStatus.Error) &&
                                          string.IsNullOrEmpty(backtestStatus.StackTrace);
                            item.Status = success ? DataGridItem.BacktestSucceeded : DataGridItem.BacktestFailed;
                            item.Progress = backtestStatus.Progress;
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Callback for the refresh button. Will reload available projects and empty backtest data grid
        /// </summary>
        private void RefreshButton_OnClick(object sender, RoutedEventArgs e)
        {
            UpdateAvailableProjects();
        }

        /// <summary>
        /// Callback for the new project button. Will show a new popup so the user can select name and language
        /// Will create project calling the server through API.
        /// </summary>
        private async void NewProjectButton_OnClick(object sender, RoutedEventArgs e)
        {
            var window = new NewProjectDialog();
            VsUtils.DisplayDialogWindow(window);
            if (window.CreateNewProject &&
                await _authenticationCommand.Login(ServiceProvider.GlobalProvider, false))
            {
                var result = false;
                var projectId = 0;
                try
                {
                    var apiResponse = await System.Threading.Tasks.Task.Run(() =>
                    {
                        var api = AuthorizationManager.GetInstance().GetApi();
                        return api.CreateProject(window.NewProjectName, window.NewProjectLanguage);
                    });
                    if (apiResponse.Success && apiResponse.Projects.Count > 0)
                    {
                        result = true;
                        projectId = apiResponse.Projects.First().ProjectId;
                    }
                }
                catch (Exception exception)
                {
                    VsUtils.ShowErrorMessageBox(ServiceProvider.GlobalProvider,
                        "QuantConnect Exception", exception.ToString());
                }
                if (result)
                {
                    // lets update the combo box without having to go to the server again
                    var newProject = new ComboboxProjectItem(projectId, window.NewProjectName, window.NewProjectLanguage);
                    projectNameBox.Items.Add(newProject);
                    projectNameBox.SelectedItem = newProject;
                    VsUtils.DisplayInStatusBar(ServiceProvider.GlobalProvider, "Successfully created a new project");
                }
                else
                {
                    VsUtils.DisplayInStatusBar(ServiceProvider.GlobalProvider, "Failed to create a new project");
                }
            }
        }

        /// <summary>
        /// Callback for the edit button. Will show a new popup so the user can edit backtest name and note.
        /// Will update the server through API.
        /// </summary>
        private async void Edit_OnClick(object sender, RoutedEventArgs e)
        {
            var obj = ((FrameworkElement)sender).DataContext as DataGridItem;
            if (obj != null)
            {
                var projectId = obj.ProjectId;
                var backtestId = obj.BacktestId;
                var backtestNote = obj.Note;
                var window = new EditBacktestDialog(obj.Name, backtestNote);
                VsUtils.DisplayDialogWindow(window);
                if (window.BacktestNameProvided &&
                    !string.IsNullOrEmpty(window.BacktestName) &&
                    await _authenticationCommand.Login(ServiceProvider.GlobalProvider, false))
                {
                    var result = false;
                    try
                    {
                        result = await System.Threading.Tasks.Task.Run(() =>
                        {
                            var api = AuthorizationManager.GetInstance().GetApi();
                            return api.UpdateBacktest(projectId, backtestId, window.BacktestName, window.BacktestNote).Success;
                        });
                    }
                    catch (Exception exception)
                    {
                        VsUtils.ShowErrorMessageBox(ServiceProvider.GlobalProvider,
                            "QuantConnect Exception", exception.ToString());
                    }
                    if (result)
                    {
                        // lets update the data grid without having to go to the server again
                        obj.Name = window.BacktestName;
                        obj.Note = window.BacktestNote;
                        VsUtils.DisplayInStatusBar(ServiceProvider.GlobalProvider, "Successfully edited backtest");
                    }
                    else
                    {
                        VsUtils.DisplayInStatusBar(ServiceProvider.GlobalProvider, "Failed to edit backtest");
                    }
                }
            }
        }
    }
}