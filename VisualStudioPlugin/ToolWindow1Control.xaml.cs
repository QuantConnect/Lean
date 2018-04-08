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
            UpdateAvailableProjects();
        }

        /// <summary>
        /// Callback for project combo box. Updates available backtest for the selected project.
        /// </summary>
        private void projectNameBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            VsUtils.DisplayInStatusBar(ServiceProvider.GlobalProvider, "Loading project backtests...");
            var selectedItem = projectNameBox.SelectedItem as ProjectNameDialog.ComboboxItem;
            if (selectedItem != null)
            {
                var projectId = selectedItem.ProjectId;
                UpdateAvailableBacktests(projectId);
                VsUtils.DisplayInStatusBar(ServiceProvider.GlobalProvider, "Successfully loaded backtests");
            }
        }

        /// <summary>
        /// Updates available backtests in the backtest data grid for a specific project
        /// </summary>
        /// <param name="projectId">Target project id</param>
        private async void UpdateAvailableBacktests(int projectId)
        {
            _dataGridCollection.Clear();
            if (_authenticationCommand.Login(ServiceProvider.GlobalProvider, false))
            {
                var backtestsList = await System.Threading.Tasks.Task.Run(() =>
                {
                    var api = AuthorizationManager.GetInstance().GetApi();
                    var result = api.ListBacktests(projectId);
                    return result.Backtests;
                });
                // Setting a limit of _maximumBacktestToShow backtests in the table...
                for (var i = 0; i < backtestsList.Count && i < _maximumBacktestToShow; i++)
                {
                    _dataGridCollection.Add(new DataGridItem
                    {
                        Name = backtestsList[i].Name,
                        Progress = backtestsList[i].Progress,
                        ProjectId = projectId,
                        BacktestId = backtestsList[i].BacktestId,
                        Status = string.IsNullOrEmpty(backtestsList[i].Error) ? DataGridItem.BacktestSucceeded : DataGridItem.BacktestFailed
                    });
                }
            }
        }

        /// <summary>
        /// Updates available projects in the combo box
        /// </summary>
        public async void UpdateAvailableProjects()
        {
            // Clear available projects
            projectNameBox.Items.Clear();
            // Clear rows in the backtest data grid
            _dataGridCollection.Clear();
            VsUtils.DisplayInStatusBar(ServiceProvider.GlobalProvider, "Loading available projects...");
            if (_authenticationCommand.Login(ServiceProvider.GlobalProvider, false))
            {
                var projectNames = await System.Threading.Tasks.Task.Run(() =>
                {
                    var api = AuthorizationManager.GetInstance().GetApi();
                    var projects = api.ListProjects().Projects;
                    return projects.Select(p => Tuple.Create(p.ProjectId, p.Name)).ToList();
                });
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
                if (_authenticationCommand.Login(ServiceProvider.GlobalProvider, false))
                {
                    deleteResult = await System.Threading.Tasks.Task.Run(() =>
                    {
                        var api = AuthorizationManager.GetInstance().GetApi();
                        return api.DeleteBacktest(projectId, backtestId).Success;
                    });
                }
                if (deleteResult)
                {
                    foreach (DataGridItem item in _dataGridCollection)
                    {
                        if (item.BacktestId == backtestId && item.ProjectId == projectId)
                        {
                            _dataGridCollection.Remove(item);
                            break;
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
        /// <param name="backtestId">Target backtest id</param>
        /// <param name="backtestName">Backtest name</param>
        /// <param name="creationDateTime">Date and time of creation</param>
        public void NewBacktest(int projectId, string backtestId, string backtestName, DateTime creationDateTime)
        {
            var selectedItem = projectNameBox.SelectedItem as ProjectNameDialog.ComboboxItem;
            // Verify the backtest is from the selected project
            if (selectedItem?.ProjectId == projectId)
            {
                _dataGridCollection.Insert(0, new DataGridItem
                {
                    Name = backtestName,
                    Progress = 0,
                    ProjectId = projectId,
                    BacktestId = backtestId,
                    Date = creationDateTime,
                    Status = DataGridItem.BacktestInProgress
                });
            }
        }

        /// <summary>
        /// Updates backtest Progress in the backtest data grid (UI)
        /// </summary>
        /// <param name="projectId">Target project id</param>
        /// <param name="backtestId">Target backtest id</param>
        /// <param name="progress">Current backtest progress</param>
        public void UpdateBacktest(int projectId, string backtestId, decimal progress)
        {
            var selectedItem = projectNameBox.SelectedItem as ProjectNameDialog.ComboboxItem;
            // Verify the backtest is from the selected project
            if (selectedItem?.ProjectId == projectId)
            {
                foreach (DataGridItem item in _dataGridCollection)
                {
                    if (item.BacktestId == backtestId)
                    {
                        item.Progress = progress;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Updates backtest Status in the backtest data grid (UI)
        /// </summary>
        /// <param name="projectId">Target project id</param>
        /// <param name="backtestId">Target backtest id</param>
        /// <param name="succeeded">Result of the backtest</param>
        public void BacktestFinished(int projectId, string backtestId, bool succeeded)
        {
            var selectedItem = projectNameBox.SelectedItem as ProjectNameDialog.ComboboxItem;
            // Verify the backtest is from the selected project
            if (selectedItem?.ProjectId == projectId)
            {
                foreach (DataGridItem item in _dataGridCollection)
                {
                    if (item.BacktestId == backtestId)
                    {
                        item.Status = succeeded ? DataGridItem.BacktestSucceeded : DataGridItem.BacktestFailed;
                        if (succeeded)
                        {
                            item.Progress = 1;
                        }
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