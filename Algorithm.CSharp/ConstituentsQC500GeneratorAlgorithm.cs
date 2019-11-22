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

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration of how to estimate constituents of QC500 index based on the company fundamentals
    /// The algorithm creates a default tradable and liquid universe containing 500 US equities
    /// which are chosen at the first trading day of each month.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="universes" />
    /// <meta name="tag" content="coarse universes" />
    /// <meta name="tag" content="fine universes" />
    public class ConstituentsQC500GeneratorAlgorithm : QCAlgorithm
    {
        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Daily;

            SetStartDate(2018, 1, 1);   // Set Start Date
            SetEndDate(2019, 1, 1);     // Set End Date
            SetCash(100000);            // Set Strategy Cash

            // Add QC500 Universe
            AddUniverse(Universe.Index.QC500);
        }
    }
}