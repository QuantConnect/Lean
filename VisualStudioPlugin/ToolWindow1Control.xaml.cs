//------------------------------------------------------------------------------
// <copyright file="ToolWindow1Control.xaml.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

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
            var selectedItem = projectNameBox.SelectedItem as ProjectNameDialog.ComboboxItem;
            if (selectedItem != null)
            {
                var projectId = selectedItem.ProjectId;
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
                    var selectedItem = projectNameBox.SelectedItem as ProjectNameDialog.ComboboxItem;
                    // Verify the backtest are from the selected project
                    if (selectedItem?.ProjectId == projectId)
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
                                    DataGridItem.BacktestSucceeded : DataGridItem.BacktestFailed
                            });
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
                        return projects.Select(p => Tuple.Create(p.ProjectId, p.Name)).ToList();
                    });
                    // Clear available projects
                    projectNameBox.Items.Clear();
                    // Clear rows in the backtest data grid
                    _dataGridCollection.Clear();
                    if (projectNames.Count > 0)
                    {
                        projectNames.ForEach(p => projectNameBox.Items.Add(new ProjectNameDialog.ComboboxItem(p.Item1, p.Item2)));
                        VsUtils.DisplayInStatusBar(ServiceProvider.GlobalProvider, "Successfully loaded projects");
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
                    var selectedItem = projectNameBox.SelectedItem as ProjectNameDialog.ComboboxItem;
                    // Verify the backtest is from the selected project
                    if (selectedItem?.ProjectId == projectId)
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
            var selectedItem = projectNameBox.SelectedItem as ProjectNameDialog.ComboboxItem;
            // Verify the backtest is from the selected project
            if (selectedItem?.ProjectId == projectId)
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

        /// <summary>
        /// Updates backtest Progress in the backtest data grid (UI)
        /// </summary>
        /// <param name="projectId">Target project id</param>
        /// <param name="backtestStatus">Backtest current status</param>
        public void BacktestStatusUpdated(int projectId, Api.Backtest backtestStatus)
        {
            var selectedItem = projectNameBox.SelectedItem as ProjectNameDialog.ComboboxItem;
            // Verify the backtest is from the selected project
            if (selectedItem?.ProjectId == projectId)
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

        /// <summary>
        /// Updates backtest Status and Progress in the backtest data grid (UI)
        /// </summary>
        /// <param name="projectId">Target project id</param>
        /// <param name="backtestStatus">Backtest current status</param>
        public void BacktestFinished(int projectId, Api.Backtest backtestStatus)
        {
            var selectedItem = projectNameBox.SelectedItem as ProjectNameDialog.ComboboxItem;
            // Verify the backtest is from the selected project
            if (selectedItem?.ProjectId == projectId)
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

        /// <summary>
        /// Callback for the refresh button. Will reload available projects and empty backtest data grid
        /// </summary>
        private void RefreshButton_OnClick(object sender, RoutedEventArgs e)
        {
            UpdateAvailableProjects();
        }
    }
}