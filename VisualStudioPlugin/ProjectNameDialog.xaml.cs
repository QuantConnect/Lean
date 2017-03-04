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

using Microsoft.VisualStudio.PlatformUI;
using System.Collections.Generic;
using System;
using System.Linq;

namespace QuantConnect.VisualStudioPlugin
{
    /// <summary>
    /// Dialog window to select a project in QuantConnect that will be
    /// used to save selected files to
    /// </summary>
    public partial class ProjectNameDialog : DialogWindow
    {
        private bool _projectNameProvided = false;
        private string _selectedProjectName = null;
        private int _selectedProjectId = 0;

        /// <summary>
        /// Create ProjectNameDialog
        /// </summary>
        /// <param name="projects">List of projects for a user to select from, a project is represented as a tuple with project id and name.</param>
        /// <param name="suggestedProjectName"></param>
        public ProjectNameDialog(List<Tuple<int, string>> projects, string suggestedProjectName)
        {
            InitializeComponent();
            SetProjectNames(projects);
            SetSuggestedProjectName(suggestedProjectName);
        }

        private void SetProjectNames(List<Tuple<int, string>> projects)
        {
            projects.ForEach(p => projectNameBox.Items.Add(new ComboboxItem(p.Item1.ToString(), p.Item2)));
        }

        private void SetSuggestedProjectName(string suggestedProjectName)
        {
            projectNameBox.Text = suggestedProjectName;
        }

        private void SelectButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ComboboxItem selectedItem = projectNameBox.SelectedItem as ComboboxItem;
            int projectId = Int32.Parse(selectedItem.Value);
            var projectName = selectedItem.DisplayText;

            if (projectName.Length == 0)
            {
                DisplayProjectNameError();
            }
            else
            {
                SaveSelectedProjectName(projectId, projectName);
                Close();
            }
        }

        private void DisplayProjectNameError()
        {
            projectNameBox.BorderBrush = System.Windows.Media.Brushes.Red;
            projectNameBox.ToolTip = "Error occurred with the data of the control.";
        }

        private void SaveSelectedProjectName(int projectId, string projectName)
        {
            _projectNameProvided = true;
            _selectedProjectName = projectName;
            _selectedProjectId = projectId;
        }

        private void CancelButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        public bool ProjectNameProvided()
        {
            return _projectNameProvided;
        }

        public string GetSelectedProjectName()
        {
            return _selectedProjectName;
        }

        public int GetSelectedProjectId()
        {
            return _selectedProjectId;
        }

        private class ComboboxItem {
            public string Value { get; }
            public string DisplayText { get; }

            public ComboboxItem(string Value, string DisplayText)
            {
                this.Value = Value;
                this.DisplayText = DisplayText;
            }

            public override string ToString()
            {
                return this.DisplayText;
            }
        }
    }
}
