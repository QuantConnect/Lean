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
using QuantConnect.Data;
using QuantConnect.Brokerages;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using System.IO;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm demonstrating and ensuring that Bybit crypto brokerage model works as expected with custom data types
    /// </summary>
    public class BybitCustomDataCryptoRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _btcUsdt;

        private ExponentialMovingAverage _fast;
        private ExponentialMovingAverage _slow;

        public override void Initialize()
        {
            SetStartDate(2022, 12, 13);
            SetEndDate(2022, 12, 13);

            SetAccountCurrency("USDT");
            SetCash(100000);

            SetBrokerageModel(BrokerageName.Bybit, AccountType.Cash);

            var symbol = AddCrypto("BTCUSDT").Symbol;
            _btcUsdt = AddData<CustomCryptoData>(symbol, Resolution.Minute).Symbol;

            // create two moving averages
            _fast = EMA(_btcUsdt, 30, Resolution.Minute);
            _slow = EMA(_btcUsdt, 60, Resolution.Minute);
        }

        public override void OnData(Slice data)
        {
            if (!_slow.IsReady)
            {
                return;
            }

            if (_fast > _slow)
            {
                if (Transactions.OrdersCount == 0)
                {
                    Buy(_btcUsdt, 1);
                }
            }
            else
            {
                if (Transactions.OrdersCount == 1)
                {
                    Liquidate(_btcUsdt);
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug(Time + " " + orderEvent);
        }

        public class CustomCryptoData : BaseData
        {
            public decimal Open;
            public decimal High;
            public decimal Low;
            public decimal Close;
            public decimal Volume;

            public override DateTime EndTime
            {
                get { return Time + Period; }
                set { Time = value - Period; }
            }

            public TimeSpan Period
            {
                get { return QuantConnect.Time.OneMinute; }
            }

            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                var tickTypeString = config.TickType.TickTypeToLower();
                var formattedDate = date.ToStringInvariant(DateFormat.EightCharacter);
                var source = Path.Combine(Globals.DataFolder, "crypto", "bybit", config.Resolution.ToString().ToLower(),
                    config.Symbol.Value.ToLower(), $"{formattedDate}_{tickTypeString}.zip");

                return new SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile, FileFormat.Csv);
            }

            public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
            {
                var csv = line.ToCsv(6);

                var data = new CustomCryptoData
                {
                    Symbol = config.Symbol,
                    Time = date.Date.AddMilliseconds(csv[0].ToInt32()).ConvertTo(config.DataTimeZone, config.ExchangeTimeZone),
                    Open = csv[1].ToDecimal(),
                    High = csv[2].ToDecimal(),
                    Low = csv[3].ToDecimal(),
                    Close = csv[4].ToDecimal(),
                    Volume = csv[5].ToDecimal(),
                    Value = csv[4].ToDecimal()
                };

                return data;
            }
        }

        /// <summary
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 4324;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 60;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000.00"},
            {"End Equity", "99981.72"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "₮0.00"},
            {"Estimated Strategy Capacity", "₮0"},
            {"Lowest Capacity Asset", "BTCUSDT.CustomCryptoData 2US"},
            {"Portfolio Turnover", "34.30%"},
            {"OrderListHash", "52ddb7dfcaaf1ea4f70cc614c49f0cd0"}
        };
    }
}
