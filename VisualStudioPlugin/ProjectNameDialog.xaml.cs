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
    public partial class ProjectNameDialog : DialogWindow
    {
        private bool _projectNameProvided = false;
        private string _selectedProjectName = null;
        private int _selectedProjectId = 0;

        public ProjectNameDialog(List<Tuple<int, string>> projectNames, string suggestedProjectName)
        {
            InitializeComponent();
            projectNames.ForEach(p => projectNameBox.Items.Add(new ComboboxItem(p.Item1.ToString(), p.Item2)));
            projectNameBox.Text = suggestedProjectName;
        }

        private void SelectButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ComboboxItem selectedItem = projectNameBox.SelectedItem as ComboboxItem;
            int projectId = Int32.Parse(selectedItem.Value);
            var projectName = selectedItem.DisplayText;

            if (projectName.Length == 0)
            {
                projectNameBox.BorderBrush = System.Windows.Media.Brushes.Red;
                projectNameBox.ToolTip = "Error occurred with the data of the control.";
            }
            else
            {
                _projectNameProvided = true;
                _selectedProjectName = projectName;
                _selectedProjectId = projectId;
                this.Close();
            }
        }

        private void CancelButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Close();
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
