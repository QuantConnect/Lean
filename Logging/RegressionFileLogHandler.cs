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
    /// Provides an implementation of <see cref="ILogHandler"/> that writes all log messages to a file on disk
    /// without timestamps.
    /// </summary>
    /// <remarks>
    /// This type is provided for convenience/setting from configuration
    /// </remarks>
    public class RegressionFileLogHandler : FileLogHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RegressionFileLogHandler"/> class
        /// that will write to a 'regression.log' file in the executing directory
        /// </summary>
        public RegressionFileLogHandler()
            : base("regression.log", false)
        {
        }
    }
}