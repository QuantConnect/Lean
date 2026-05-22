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

using System.Collections.Generic;

namespace QuantConnect.Optimizer
{
    /// <summary>
    /// A local maximum of the Sharpe surface on the parameter grid: a trial whose Sharpe is
    /// strictly greater than every face-neighbor's Sharpe (face-neighbors differ from this
    /// trial in exactly one parameter by one grid step). Multiple modes indicate a multimodal
    /// surface and suggest splitting the next optimization into narrower sweeps around each.
    /// </summary>
    public class Mode
    {
        /// <summary>
        /// The backtest id of the mode trial.
        /// </summary>
        public string BacktestId { get; set; }

        /// <summary>
        /// Parameter values for the mode trial (parameter name -> numeric value).
        /// </summary>
        public IReadOnlyDictionary<string, double> Parameters { get; set; }

        /// <summary>
        /// Sharpe ratio of the mode trial.
        /// </summary>
        public double SharpeRatio { get; set; }

        /// <summary>
        /// Number of face-neighbors this trial was compared against. A higher count means
        /// the mode is supported by more surrounding evidence (interior cells have more
        /// neighbors than edge or corner cells).
        /// </summary>
        public int NeighborCount { get; set; }
    }
}
