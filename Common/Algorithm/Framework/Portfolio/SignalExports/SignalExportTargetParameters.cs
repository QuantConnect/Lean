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

using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.Framework.Portfolio.SignalExports
{
    /// <summary>
    /// Class to wrap objects needed to send signals to the different 3rd party API's
    /// </summary>
    public class SignalExportTargetParameters
    {
        /// <summary>
        /// List of portfolio targets to be sent to some 3rd party API
        /// </summary>
        public List<PortfolioTarget> Targets { get; set; }

        /// <summary>
        /// Algorithm being ran
        /// </summary>
        public IAlgorithm Algorithm { get; set; }
    }
}
