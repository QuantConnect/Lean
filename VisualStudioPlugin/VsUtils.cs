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


using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace QuantConnect.VisualStudioPlugin
{
    /// <summary>
    /// Collection of commonly used methods to work with VisualStudio UI
    /// </summary>
    class VsUtils
    {
        /// <summary>
        /// Display message in VisualStudio status bar
        /// </summary>
        /// <param name="serviceProvider">Visual Studio services provider</param>
        /// <param name="msg">Message to display</param>
        public static void DisplayInStatusBar(IServiceProvider serviceProvider, string msg)
        {
            int frozen;
            var statusBar = GetStatusBar(serviceProvider);
            statusBar.IsFrozen(out frozen);
            if (frozen == 0)
            {
                statusBar.SetText(msg);
            }
        }

        private static IVsStatusbar GetStatusBar(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService(typeof(SVsStatusbar)) as IVsStatusbar;
        }
    }
}
