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

using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression test algorithm simply fetch history on boarder of Daylight Saving Time shift
    /// </summary>
    public class DaylightSavingTimeHistoryRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol[] _symbols = new[]
        {
            QuantConnect.Symbol.Create("EURUSD", SecurityType.Forex, Market.FXCM),
            QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA)
        };
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2011, 11, 10);  //Set Start Date
            SetEndDate(2011, 11, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash

            for (int i = 0; i < _symbols.Length; i++)
            {
                var symbol = _symbols[i];
                IEnumerable<BaseData> history;
                if (symbol.SecurityType == SecurityType.Equity)
                {
                    try
                    {

                        history = History<QuoteBar>(symbol, 10, Resolution.Daily).Select(bar => bar as BaseData);
                        throw new Exception("We were expecting an argument exception to be thrown. Equity does not have daily QuoteBars!");
                    }
                    catch (ArgumentException)
                    {
                        // expected
                    }
                    history = History<TradeBar>(symbol, 10, Resolution.Daily).Select(bar => bar as BaseData);
                }
                else
                {
                    history = History<QuoteBar>(symbol, 10, Resolution.Daily)
                        .Select(bar => bar as BaseData);
                }

                var duplications = history
                    .GroupBy(k => k.Time)
                    .Where(g => g.Count() > 1);
                if (duplications.Any())
                {
                    var time = duplications.First().Key;
                    throw new Exception($"Duplicated bars were issued for time {time}");
                }
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
