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
using System.Globalization;
using Newtonsoft.Json;
using QuantConnect.Brokerages;
using QuantConnect.Data;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Live Trading Functionality Demonstration algorithm including SMS, Email and Web hook notifications.
    /// </summary>
    /// <meta name="tag" content="live trading" />
    /// <meta name="tag" content="alerts" />
    /// <meta name="tag" content="sms alerts" />
    /// <meta name="tag" content="web hooks" />
    /// <meta name="tag" content="email alerts" />
    /// <meta name="tag" content="runtime statistics" />
    public class LiveTradingFeaturesAlgorithm : QCAlgorithm
    {
        private bool _isConnected;

        /// <summary>
        /// Initialise the Algorithm and Prepare Required Data.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 11);
            SetCash(25000);

            //Equity Data for US Markets:
            AddSecurity(SecurityType.Equity, "IBM", Resolution.Second);

            //FOREX Data for Weekends: 24/6
            AddSecurity(SecurityType.Forex, "EURUSD", Resolution.Minute);

            //Custom/Bitcoin Live Data: 24/7
            AddData<Bitcoin>("BTC", Resolution.Second, TimeZones.Utc);

            //if the algorithm is connected to the brokerage
            _isConnected = true;
        }

        /// <summary>
        /// Raises the data event.
        /// </summary>
        /// <param name="data">Data.</param>
        public override void OnData(Slice data)
        {
            if (!Portfolio["IBM"].HoldStock && data.ContainsKey("IBM"))
            {
                int quantity = (int)Math.Floor(Portfolio.MarginRemaining / data["IBM"].Close);
                Order("IBM", quantity);
                Debug("Purchased IBM on " + Time.ToShortDateString());
                Notify.Email("myemail@gmail.com", "Test", "Test Body", "test attachment");
            }

            if (data.TryGet<Bitcoin>("BTC", out var bitcoinData))
            {
                if (LiveMode) //Live Mode Property
                {
                    //Configurable title header statistics numbers
                    SetRuntimeStatistic("BTC", bitcoinData.Close.ToStringInvariant("C"));
                }

                if (!Portfolio.HoldStock)
                {
                    Order("BTC", 100);

                    //Send a notification email/SMS/web request on events:
                    Notify.Email("myemail@gmail.com", "Test", "Test Body", "test attachment");
                    Notify.Sms("+11233456789", Time.ToStringInvariant("u") + ">> Test message from live BTC server.");
                    Notify.Web("http://api.quantconnect.com", Time.ToStringInvariant("u") + ">> Test data packet posted from live BTC server.");
                    Notify.Telegram("id", Time.ToStringInvariant("u") + ">> Test message from live BTC server.");
                    Notify.Ftp("ftp.quantconnect.com", "username", "password", "path/to/file.txt",
                        Time.ToStringInvariant("u") + ">> Test file from live BTC server.");
                    Notify.Sftp("ftp.quantconnect.com", "username", "password", "path/to/file.txt",
                        Time.ToStringInvariant("u") + ">> Test file from live BTC server.");
                    Notify.Sftp("ftp.quantconnect.com", "username", "privatekey", "optionalprivatekeypassphrase", "path/to/file.txt",
                        Time.ToStringInvariant("u") + ">> Test file from live BTC server.");
                }
            }
        }

        /// <summary>
        /// Brokerage message event handler. This method is called for all types of brokerage messages.
        /// </summary>
        public override void OnBrokerageMessage(BrokerageMessageEvent messageEvent)
        {
            Debug($"Brokerage meesage received - {messageEvent.ToString()}");
        }

        /// <summary>
        /// Brokerage disconnected event handler. This method is called when the brokerage connection is lost.
        /// </summary>
        public override void OnBrokerageDisconnect()
        {
            _isConnected = false;
            Debug($"Brokerage disconnected!");
        }

        /// <summary>
        /// Brokerage reconnected event handler. This method is called when the brokerage connection is restored after a disconnection.
        /// </summary>
        public override void OnBrokerageReconnect()
        {
            _isConnected = true;
            Debug($"Brokerage reconnected!");
        }

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
            public decimal VolumeUSD { get; set; }

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
                return new SubscriptionDataSource("https://www.quandl.com/api/v3/datasets/BCHARTS/BITSTAMPUSD.csv?order=asc", SubscriptionTransportMedium.RemoteFile);
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
                //Date      Open   High    Low     Close   Volume (BTC)    Volume (Currency)   Weighted Price
                //2011-09-13 5.8    6.0     5.65    5.97    58.37138238,    346.0973893944      5.929230648356
                try
                {
                    string[] data = line.Split(',');
                    coin.Time = DateTime.Parse(data[0], CultureInfo.InvariantCulture);
                    coin.Open = Convert.ToDecimal(data[1], CultureInfo.InvariantCulture);
                    coin.High = Convert.ToDecimal(data[2], CultureInfo.InvariantCulture);
                    coin.Low = Convert.ToDecimal(data[3], CultureInfo.InvariantCulture);
                    coin.Close = Convert.ToDecimal(data[4], CultureInfo.InvariantCulture);
                    coin.VolumeBTC = Convert.ToDecimal(data[5], CultureInfo.InvariantCulture);
                    coin.VolumeUSD = Convert.ToDecimal(data[6], CultureInfo.InvariantCulture);
                    coin.WeightedPrice = Convert.ToDecimal(data[7], CultureInfo.InvariantCulture);
                    coin.Value = coin.Close;
                }
                catch { /* Do nothing, skip first title row */ }

                return coin;
            }
        }
    }
}
