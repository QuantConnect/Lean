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

using Microsoft.VisualStudio.Shell;

namespace QuantConnect.VisualStudioPlugin
{
    /// <summary>
    /// Log helper to write messages into VisualStudio ActivityLog
    /// </summary>
    internal static class VSActivityLog
    {
        private const string _logSource = "QuantConnect.";

        public static void Info(string message, string source = "")
        {
            ActivityLog.LogInformation(_logSource + source, message);
        }
        public static void Error(string message, string source = "")
        {
            ActivityLog.LogError(_logSource + source, message);
        }
    }
}
