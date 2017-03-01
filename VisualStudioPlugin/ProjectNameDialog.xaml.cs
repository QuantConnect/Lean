using Microsoft.VisualStudio.PlatformUI;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Controls;

namespace QuantConnect.VisualStudioPlugin
{
    public partial class ProjectNameDialog : DialogWindow
    {
        private bool _projectNameProvided = false;
        private string _selectedProjectName = null;

        public ProjectNameDialog(List<string> projectNames)
        {
            InitializeComponent();
            projectNames.ForEach(p => projectNameBox.Items.Add(p));
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
