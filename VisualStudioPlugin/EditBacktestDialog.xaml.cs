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
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace QuantConnect.VisualStudioPlugin
{
    /// <summary>
    /// Interaction logic for EditBacktestDialog.xaml. Popup that allows the user to define a new backtest name or add a note
    /// </summary>
    internal partial class EditBacktestDialog : DialogWindow
    {
        /// <summary>
        /// True if user selected a valid backtest name
        /// </summary>
        public bool BacktestNameProvided { get; private set; }

        /// <summary>
        /// User provided backtest name
        /// </summary>
        public string BacktestName { get; private set; }

        /// <summary>
        /// User provided backtest name
        /// </summary>
        public string BacktestNote { get; private set; }

        private readonly Brush _backtestNameNormalBrush;

        public EditBacktestDialog(string currentBacktestName, string currentBacktestNote)
        {
            InitializeComponent();
            BacktestName = currentBacktestName;
            textBox.Text = currentBacktestNote;
            backtestNameBox.Text = BacktestName;
            _backtestNameNormalBrush = backtestNameBox.BorderBrush;
            BacktestNameProvided = false;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(backtestNameBox.Text))
            {
                backtestNameBox.BorderBrush = Brushes.Red;
            }
            else
            {
                BacktestName = backtestNameBox.Text;
                BacktestNote = textBox.Text;
                BacktestNameProvided = true;
                Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void InputField_KeyDown(object sender, KeyEventArgs e)
        {
            backtestNameBox.BorderBrush = _backtestNameNormalBrush;
            if (e.Key == Key.Return)
            {
                Ok_Click(sender, e);
            }
        }
    }
}
