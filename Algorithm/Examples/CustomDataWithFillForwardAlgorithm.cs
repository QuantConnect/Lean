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
using QuantConnect.Data.Test;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Forex;

namespace QuantConnect.Algorithm.Examples
{
    /// <summary>
    /// Custom Data Example Algorithm using Fillforward to
    /// </summary>
    public class CustomDataWithFillForwardAlgorithm : QCAlgorithm
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2014, 05, 01);
            SetEndDate(2014, 05, 30);

            // create 'custom' data that just looks in the normal place for data

            AddData<FakeForexTradeBarCustom>("EURUSD", Resolution.Minute, true);
            Securities["EURUSD"].Exchange = new ForexExchange();

            AddData<FakeForexTradeBarCustom>("NZDUSD", Resolution.Minute, true);
            Securities["NZDUSD"].Exchange = new ForexExchange();

            AddData<FakeEquityTradeBarCustom>("MSFT", Resolution.Minute, true);
            Securities["MSFT"].Exchange = new EquityExchange();

            AddData<FakeEquityTradeBarCustom>("SPY", Resolution.Minute, true);
            Securities["SPY"].Exchange = new EquityExchange();
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="custom">TradeBars IDictionary object with your stock data</param>
        public void OnData(FakeTradeBarCustom custom)
        {
            Console.WriteLine(custom.Time.ToString("o") + " FF " + (custom.IsFillForward ? "1" : "0") + " " + custom.Symbol);
        }
    }
}
