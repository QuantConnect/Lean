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

namespace QuantConnect.VisualStudioPlugin
{
    /// <summary>
    /// Dialog window to select a project in QuantConnect that will be
    /// used to save selected files to
    /// </summary>
    internal partial class ProjectNameDialog : DialogWindow
    {
        /// <summary>
        /// True if user selected a valid project name
        /// </summary>
        public bool ProjectNameProvided { get; private set; }

        /// <summary>
        /// Selected project name
        /// </summary>
        public string SelectedProjectName { get; private set; }

        /// <summary>
        /// Id of a selected projected
        /// </summary>
        public int? SelectedProjectId { get; private set; } = 0;

        /// <summary>
        /// Create ProjectNameDialog
        /// </summary>
        /// <param name="projects">List of projects for a user to select from, a project is represented as a tuple with project id and name.</param>
        /// <param name="suggestedProjectName"></param>
        public ProjectNameDialog(List<Tuple<int, string, Language>> projects, string suggestedProjectName)
        {
            InitializeComponent();
            SetProjectNames(projects);
            SetSuggestedProjectName(suggestedProjectName);
        }

        private void SetProjectNames(List<Tuple<int, string, Language>> projects)
        {
            projects.ForEach(p => projectNameBox.Items.Add(new ComboboxProjectItem(p.Item1, p.Item2, p.Item3)));
        }

        private void SetSuggestedProjectName(string suggestedProjectName)
        {
            foreach (var item in projectNameBox.Items)
            {
                var project = item as ComboboxProjectItem;
                if (project.Name == suggestedProjectName)
                {
                    projectNameBox.SelectedItem = project;
                    break;
                }
            }
        }

        private void SelectButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var selectedItem = projectNameBox.SelectedItem as ComboboxProjectItem;

            if (selectedItem != null)
            {
                var projectId = selectedItem.Id;
                var projectName = selectedItem.Name;
                SaveSelectedProjectName(projectId, projectName);
                Close();
            }
            else if (projectNameBox.Text.Length != 0)
            {
                SaveSelectedProjectName(null, projectNameBox.Text);
                Close();
            }
            else
            {
                DisplayProjectNameError();
            }
        }

        private void DisplayProjectNameError()
        {
            projectNameBox.BorderBrush = System.Windows.Media.Brushes.Red;
            projectNameBox.ToolTip = "Error occurred with the data of the control.";
        }

        private void SaveSelectedProjectName(int? projectId, string projectName)
        {
            ProjectNameProvided = true;
            SelectedProjectName = projectName;
            SelectedProjectId = projectId;
        }

        private void CancelButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }
    }
}
