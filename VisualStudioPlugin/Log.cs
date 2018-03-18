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
using System;

namespace QuantConnect.VisualStudioPlugin
{
    /// <summary>
    /// Log helper to write messages into VisualStudio ActivityLog
    /// </summary>
    public class Log
    {
        private const string QUANT_CONNECT = "QuantConnect.";

        private readonly string _logSource;

        public Log(Type type)
        {
            _logSource = QUANT_CONNECT + type.Name;
        }

        public void Info(string message)
        {
            ActivityLog.LogInformation(_logSource, message);
        }
        public void Error(string message)
        {
            ActivityLog.LogError(_logSource, message);
        }
    }
}
