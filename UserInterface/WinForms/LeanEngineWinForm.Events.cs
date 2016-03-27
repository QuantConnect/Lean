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
 *
*/

using System;
using System.Windows.Forms;

namespace QuantConnect.Views.WinForms
{
    /// <summary>
    /// Primary Form for use with LEAN:
    /// </summary>
    public partial class LeanEngineWinForm : Form
    {
        /// <summary>
        /// Trigger a terminate message to the Lean Engine.
        /// </summary>
        private void OnClosed(object sender, EventArgs eventArgs)
        {
            _engine.SystemHandlers.Dispose();
            _engine.AlgorithmHandlers.Dispose();
            Environment.Exit(0);
        }

        /// <summary>
        /// Binding to the Console Key Press. In the console there's virtually nothing for user input other than the end of the backtest.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="keyEventArgs"></param>
        private void ConsoleOnKeyUp(object sender, KeyEventArgs keyEventArgs)
        {
            if (!_resultsHandler.IsActive)
            {
                Environment.Exit(0);
            }
        }
    }
}
