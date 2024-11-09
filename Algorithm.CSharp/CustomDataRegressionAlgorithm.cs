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
using System.Globalization;
using Newtonsoft.Json;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression test to demonstrate importing and trading on custom data.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="custom data" />
    /// <meta name="tag" content="crypto" />
    /// <meta name="tag" content="regression test" />
    public class CustomDataRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool _warmedUpChecked;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2020, 01, 05);
            SetEndDate(2020, 01, 10);

            //Set the cash for the strategy:
            SetCash(100000);

            //Define the symbol and "type" of our generic data:
            var resolution = LiveMode ? Resolution.Second : Resolution.Daily;
            AddData<Bitcoin>("BTC", resolution);

            var seeder = new FuncSecuritySeeder(GetLastKnownPrices);
            SetSecurityInitializer(security => seeder.SeedSecurity(security));
        }

        /// <summary>
        /// Event Handler for Bitcoin Data Events: These Bitcoin objects are created from our
        /// "Bitcoin" type below and fired into this event handler.
        /// </summary>
        /// <param name="data">One(1) Bitcoin Object, streamed into our algorithm synchronised in time with our other data streams</param>
        public void OnData(Bitcoin data)
        {
            //If we don't have any bitcoin "SHARES" -- invest"
            if (!Portfolio.Invested)
            {
                //Bitcoin used as a tradable asset, like stocks, futures etc.
                if (data.Close != 0)
                {
                    //Access custom data symbols using <ticker>.<custom-type>
                    Order("BTC.Bitcoin", Portfolio.MarginRemaining / Math.Abs(data.Close + 1));
                }
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            changes.FilterCustomSecurities = false;
            foreach (var addedSecurity in changes.AddedSecurities)
            {
                if (addedSecurity.Symbol.Value == "BTC")
                {
                    _warmedUpChecked = true;
                }
                if (!addedSecurity.HasData)
                {
                    throw new RegressionTestException($"Security {addedSecurity.Symbol} was not warmed up!");
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_warmedUpChecked)
            {
                throw new RegressionTestException($"Security was not warmed up!");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 50;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "27587.925%"},
            {"Drawdown", "4.200%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "109685"},
            {"Net Profit", "9.685%"},
            {"Sharpe Ratio", "238.834"},
            {"Sortino Ratio", "945.079"},
            {"Probabilistic Sharpe Ratio", "81.660%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "149.482"},
            {"Beta", "-6.002"},
            {"Annual Standard Deviation", "0.61"},
            {"Annual Variance", "0.371"},
            {"Information Ratio", "218.36"},
            {"Tracking Error", "0.664"},
            {"Treynor Ratio", "-24.253"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "BTC.Bitcoin 2S"},
            {"Portfolio Turnover", "16.03%"},
            {"OrderListHash", "dde8821614d33c89e6e75c536447b7da"}
        };

        /// <summary>
        /// Custom Data Type: Bitcoin data from Quandl - http://www.quandl.com/help/api-for-bitcoin-data
        /// </summary>
        public class Bitcoin : BaseData
        {
            [JsonProperty("timestamp")]
            public int Timestamp { get; set; }
            [JsonProperty("open")]
            public decimal Open { get; set; }
            [JsonProperty("high")]
            public decimal High { get; set; }
            [JsonProperty("low")]
            public decimal Low { get; set; }
            public decimal Mid { get; set; }

            [JsonProperty("last")]
            public decimal Close { get; set; }
            [JsonProperty("bid")]
            public decimal Bid { get; set; }
            [JsonProperty("ask")]
            public decimal Ask { get; set; }
            [JsonProperty("vwap")]
            public decimal WeightedPrice { get; set; }
            [JsonProperty("volume")]
            public decimal VolumeBTC { get; set; }

            /// <summary>
            /// The end time of this data. Some data covers spans (trade bars)
            /// and as such we want to know the entire time span covered
            /// </summary>
            /// <remarks>
            /// This property is overriden to allow different values for Time and EndTime
            /// if they are set in the Reader. In the base implementation EndTime equals Time
            /// </remarks>
            public override DateTime EndTime { get; set; }

            /// <summary>
            /// 1. DEFAULT CONSTRUCTOR: Custom data types need a default constructor.
            /// We search for a default constructor so please provide one here. It won't be used for data, just to generate the "Factory".
            /// </summary>
            public Bitcoin()
            {
                Symbol = "BTC";
            }

            /// <summary>
            /// 2. RETURN THE STRING URL SOURCE LOCATION FOR YOUR DATA:
            /// This is a powerful and dynamic select source file method. If you have a large dataset, 10+mb we recommend you break it into smaller files. E.g. One zip per year.
            /// We can accept raw text or ZIP files. We read the file extension to determine if it is a zip file.
            /// </summary>
            /// <param name="config">Configuration object</param>
            /// <param name="date">Date of this source file</param>
            /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
            /// <returns>String URL of source file.</returns>
            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                if (isLiveMode)
                {
                    return new SubscriptionDataSource("https://www.bitstamp.net/api/ticker/", SubscriptionTransportMedium.Rest);
                }

                //return "http://my-ftp-server.com/futures-data-" + date.ToString("Ymd") + ".zip";
                // OR simply return a fixed small data file. Large files will slow down your backtest
                return new SubscriptionDataSource("https://www.quantconnect.com/api/v2/proxy/nasdaq/api/v3/datatables/QDL/BITFINEX.csv?code=BTCUSD&api_key=qAWKpUfmSVFnU3bRQwKy")
                {
                    Sort = true
                };
            }

            /// <summary>
            /// 3. READER METHOD: Read 1 line from data source and convert it into Object.
            /// Each line of the CSV File is presented in here. The backend downloads your file, loads it into memory and then line by line
            /// feeds it into your algorithm
            /// </summary>
            /// <param name="line">string line from the data source file submitted above</param>
            /// <param name="config">Subscription data, symbol name, data type</param>
            /// <param name="date">Current date we're requesting. This allows you to break up the data source into daily files.</param>
            /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
            /// <returns>New Bitcoin Object which extends BaseData.</returns>
            public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
            {
                var coin = new Bitcoin();
                if (isLiveMode)
                {
                    //Example Line Format:
                    //{"high": "441.00", "last": "421.86", "timestamp": "1411606877", "bid": "421.96", "vwap": "428.58", "volume": "14120.40683975", "low": "418.83", "ask": "421.99"}
                    try
                    {
                        coin = JsonConvert.DeserializeObject<Bitcoin>(line);
                        coin.EndTime = DateTime.UtcNow.ConvertFromUtc(config.ExchangeTimeZone);
                        coin.Value = coin.Close;
                    }
                    catch { /* Do nothing, possible error in json decoding */ }
                    return coin;
                }

                //Example Line Format:
                // code    date        high     low      mid      last     bid      ask      volume
                // BTCUSD  2024-10-08  63248.0  61940.0  62246.5  62245.0  62246.0  62247.0  477.91102114
                try
                {
                    string[] data = line.Split(',');
                    coin.Time = DateTime.Parse(data[1], CultureInfo.InvariantCulture);
                    coin.EndTime = coin.Time.AddDays(1);
                    coin.High = Convert.ToDecimal(data[2], CultureInfo.InvariantCulture);
                    coin.Low = Convert.ToDecimal(data[3], CultureInfo.InvariantCulture);
                    coin.Mid = Convert.ToDecimal(data[4], CultureInfo.InvariantCulture);
                    coin.Close = Convert.ToDecimal(data[5], CultureInfo.InvariantCulture);
                    coin.Bid = Convert.ToDecimal(data[6], CultureInfo.InvariantCulture);
                    coin.Ask = Convert.ToDecimal(data[7], CultureInfo.InvariantCulture);
                    coin.VolumeBTC = Convert.ToDecimal(data[8], CultureInfo.InvariantCulture);
                    coin.Value = coin.Close;
                }
                catch { /* Do nothing, skip first title row */ }

                return coin;
            }
        }
    }
}
