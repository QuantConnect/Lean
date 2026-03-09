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

namespace QuantConnect.Logging
{
    /// <summary>
    /// Subclass of ConsoleLogHandler that only logs error messages
    /// </summary>
    public class ConsoleErrorLogHandler : ConsoleLogHandler
    {
        /// <summary>
        /// Hide debug messages from log
        /// </summary>
        /// <param name="text">The debug text to log</param>
        public override void Debug(string text)
        {
        }

        /// <summary>
        /// Hide trace messages from log
        /// </summary>
        /// <param name="text">The trace text to log</param>
        public override void Trace(string text)
        {
        }
    }
}