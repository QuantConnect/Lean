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

namespace QuantConnect.VisualStudioPlugin
{
    public partial class ProjectNameDialog : DialogWindow
    {
        private bool _projectNameProvided = false;
        private string _selectedProjectName = null;

        public ProjectNameDialog(List<string> projectNames, string suggestedProjectName)
        {
            InitializeComponent();
            projectNames.ForEach(p => projectNameBox.Items.Add(p));
            projectNameBox.Text = suggestedProjectName;
        }

        private void SelectButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var projectName = projectNameBox.Text;
            if (projectName.Length == 0)
            {
                projectNameBox.BorderBrush = System.Windows.Media.Brushes.Red;
                projectNameBox.ToolTip = "Error occurred with the data of the control.";
            }
            else
            {
                _projectNameProvided = true;
                _selectedProjectName = projectName;
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
    }
}
