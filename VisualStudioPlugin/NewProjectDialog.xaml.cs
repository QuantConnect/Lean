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
using Microsoft.VisualStudio.Shell;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace QuantConnect.VisualStudioPlugin
{
    /// <summary>
    /// Interaction logic for NewProjectDialog.xaml
    /// </summary>
    internal partial class NewProjectDialog : DialogWindow
    {
        /// <summary>
        /// True if user entered valid data
        /// </summary>
        public bool CreateNewProject { get; private set; }

        /// <summary>
        /// User provided Project name
        /// </summary>
        public string NewProjectName { get; private set; }

        /// <summary>
        /// User provided project programming laguange
        /// </summary>
        public Language NewProjectLanguage { get; private set; }

        /// <summary>
        /// Supported programming languages. This is to filter enum Language, which has unsupported languages.
        /// </summary>
        private static string[] _supportedLanguages = { "FSharp", "CSharp", "Python" };

        private readonly Brush _NewProjectNameNormalBrush;

        public NewProjectDialog()
        {
            InitializeComponent();
            CreateNewProject = false;
            _NewProjectNameNormalBrush = NewProjectNameBox.BorderBrush;
            foreach (Language language in Enum.GetValues(typeof(Language)))
            {
                var languageStr = Enum.GetName(typeof(Language), language);
                if (_supportedLanguages.Any(x => x.Equals(languageStr)))
                {
                    LanguageNameBox.Items.Add(new ComboboxLanguageItem(language, languageStr));
                }
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = LanguageNameBox.SelectedItem as ComboboxLanguageItem;
            if (string.IsNullOrEmpty(NewProjectNameBox.Text))
            {
                NewProjectNameBox.BorderBrush = Brushes.Red;
            }
            else if (selectedItem == null)
            {
                VsUtils.DisplayInStatusBar(ServiceProvider.GlobalProvider, "Please select a language for the new project");
            }
            else
            {
                NewProjectName = NewProjectNameBox.Text;
                NewProjectLanguage = selectedItem.LanguageId;
                CreateNewProject = true;
                Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void InputField_KeyDown(object sender, KeyEventArgs e)
        {
            NewProjectNameBox.BorderBrush = _NewProjectNameNormalBrush;
            if (e.Key == Key.Return)
            {
                Ok_Click(sender, e);
            }
        }

        /// <summary>
        /// Item that represents language name and language id in a combo box
        /// </summary>
        private class ComboboxLanguageItem
        {
            public Language LanguageId { get; }
            public string LanguageName { get; }

            public ComboboxLanguageItem(Language id, string name)
            {
                LanguageId = id;
                LanguageName = name;
            }

            public override string ToString()
            {
                return LanguageName;
            }
        }
    }
}
