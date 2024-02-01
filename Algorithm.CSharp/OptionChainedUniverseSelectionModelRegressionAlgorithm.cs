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

using QuantConnect.Algorithm.Selection;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;
using System;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm to test the OptionChainedUniverseSelectionModel class
    /// </summary>
    public class OptionChainedUniverseSelectionModelRegressionAlgorithm: QCAlgorithm
    {
        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Daily;
            SetStartDate(2014, 03, 22);
            SetEndDate(2014, 04, 07);
            SetCash(100000);

            AddSecurity(SecurityType.Equity, "GOOG", Resolution.Daily);

            var universe = AddUniverse(coarse =>
            {
                // select the various google symbols over the period
                return from c in coarse
                       let sym = c.Symbol.Value
                       where sym == "GOOG" || sym == "GOOCV" || sym == "GOOAV"
                       select c.Symbol;

                // Before March 28th 2014:
                // - Only GOOG  T1AZ164W5VTX existed
                // On March 28th 2014
                // - GOOAV VP83T1ZUHROL and GOOCV VP83T1ZUHROL are listed
                // On April 02nd 2014
                // - GOOAV VP83T1ZUHROL is delisted
                // - GOOG  T1AZ164W5VTX becomes GOOGL
                // - GOOCV VP83T1ZUHROL becomes GOOG
            });

            AddUniverseSelection(new OptionChainedUniverseSelectionModel(universe,
                option_filter_universe => option_filter_universe));
        }

        public override void OnEndOfAlgorithm()
        {
            if (!UniverseManager.ContainsKey("?GOOCV"))
            {
                throw new Exception("Option chain {?GOOCV} should have been in the universe but it was not");
            }

            if (!UniverseManager.ContainsKey("?GOOG"))
            {
                throw new Exception("Option chain {?GOOG} should have been in the universe but it was not");
            }

            if (!UniverseManager.ContainsKey("?GOOAV"))
            {
                throw new Exception("Option chain {?GOOAV} should have been in the universe but it was not");
            }
        }
    }
}
