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
using System.Text;

namespace QuantConnect.Data.Custom.Intrinio
{
    /// <summary>
    ///     TRanformation available for the Economic data.
    /// </summary>
    public enum IntrinioDataTransformation
    {
        /// <summary>
        ///     The rate of change
        /// </summary>
        Roc,

        /// <summary>
        ///     Rate of change from Year Ago
        /// </summary>
        AnnualyRoc,

        /// <summary>
        ///     The compounded annual rate of change
        /// </summary>
        CompoundedAnnualRoc,

        /// <summary>
        ///     The continuously compounded annual rate of change
        /// </summary>
        AnnualyCCRoc,

        /// <summary>
        ///     The continuously compounded rateof change
        /// </summary>
        CCRoc,

        /// <summary>
        ///     The level, no transformation.
        /// </summary>
        Level,

        /// <summary>
        ///     The natural log
        /// </summary>
        Ln,

        /// <summary>
        ///     The percent change
        /// </summary>
        Pc,

        /// <summary>
        ///     The percent change from year ago
        /// </summary>
        AnnualyPc
    }

    /// <summary>
    ///     Access the massive repository of economic data from the Federal Reserve Economic Data system via the Intrinio API.
    /// </summary>
    /// <seealso cref="QuantConnect.Data.BaseData" />
    public class IntrinioEconomicData : BaseData
    {
        private static DateTime _lastApiCall = DateTime.MinValue;
        private static TimeSpan _msSinceLastCall = TimeSpan.MaxValue;

        private readonly string _baseUrl = @"https://api.intrinio.com/historical_data.csv?";

        private readonly IntrinioDataTransformation _dataTransformation;


        private bool _backtestingFirstTimeCallOrLiveMode = true;

        /// <summary>
        ///     Initializes a new instance of the <see cref="IntrinioEconomicData" /> class.
        /// </summary>
        public IntrinioEconomicData() : this(IntrinioDataTransformation.Level)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="IntrinioEconomicData" /> class.
        /// </summary>
        /// <param name="dataTransformation">The item.</param>
        public IntrinioEconomicData(IntrinioDataTransformation dataTransformation)
        {
            _dataTransformation = dataTransformation;

            // If the user and the password is not set then then throw error.
            if (!IntrinioConfig.IsInitialized)
            {
                throw new
                    InvalidOperationException("Please set a valid Intrinio user and password using the 'IntrinioEconomicData.SetUserAndPassword' static method. " +
                                              "For local backtesting, the user and password can be set in the 'parameters' fields from the 'config.json' file.");
            }
        }


        /// <summary>
        ///     Return the URL string source of the file. This will be converted to a stream
        /// </summary>
        /// <param name="config">Configuration object</param>
        /// <param name="date">Date of this source file</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>
        ///     String URL of source file.
        /// </returns>
        /// <remarks>
        ///     Given Intrinio's API limits, we cannot make more than one CSV request per second. That's why in backtesting mode
        ///     we make sure we make just one call to retrieve all the data needed. Also, to avoid the problem of many sources
        ///     asking the data at the beginning of the algorithm, a pause of a second is added.
        /// </remarks>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            SubscriptionDataSource subscription;

            // We want to make just one call with all the data in backtesting mode.
            // Also we want to make one call per second becasue of the API limit.
            if (_backtestingFirstTimeCallOrLiveMode)
            {
                // Force the engine to wait at least 1000 ms between API calls.
                IntrinioConfig.RateGate.WaitToProceed();

                // In backtesting mode, there is only one call at the beggining with all the data 
                _backtestingFirstTimeCallOrLiveMode = false || isLiveMode;
                subscription = GetIntrinioSubscription(config, isLiveMode);
            }
            else
            {
                subscription = new SubscriptionDataSource("", SubscriptionTransportMedium.LocalFile);
            }
            return subscription;
        }

        /// <summary>
        ///     Reader converts each line of the data source into BaseData objects. Each data type creates its own factory method,
        ///     and returns a new instance of the object
        ///     each time it is called. The returned object is assumed to be time stamped in the config.ExchangeTimeZone.
        /// </summary>
        /// <param name="config">Subscription data config setup object</param>
        /// <param name="line">Line of the source document</param>
        /// <param name="date">Date of the requested data</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>
        ///     Instance of the T:BaseData object generated by this line of the CSV
        /// </returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            var obs = line.Split(',');
            var time = DateTime.MinValue;
            if (!DateTime.TryParseExact(obs[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None,
                                        out time)) return null;
            var value = obs[1].ToDecimal();
            return new IntrinioEconomicData
            {
                Symbol = config.Symbol,
                Time = time,
                EndTime = time + QuantConnect.Time.OneDay,
                Value = value
            };
        }

        private static string GetStringForDataTransformation(IntrinioDataTransformation dataTransformation)
        {
            var item = "level";
            switch (dataTransformation)
            {
                case IntrinioDataTransformation.Roc:
                    item = "change";
                    break;
                case IntrinioDataTransformation.AnnualyRoc:
                    item = "yr_change";
                    break;
                case IntrinioDataTransformation.CompoundedAnnualRoc:
                    item = "c_annual_roc";
                    break;
                case IntrinioDataTransformation.AnnualyCCRoc:
                    item = "cc_annual_roc";
                    break;
                case IntrinioDataTransformation.CCRoc:
                    item = "cc_roc";
                    break;
                case IntrinioDataTransformation.Level:
                    item = "level";
                    break;
                case IntrinioDataTransformation.Ln:
                    item = "log";
                    break;
                case IntrinioDataTransformation.Pc:
                    item = "percent_change";
                    break;
                case IntrinioDataTransformation.AnnualyPc:
                    item = "yr_percent_change";
                    break;
            }
            return item;
        }

        private SubscriptionDataSource GetIntrinioSubscription(SubscriptionDataConfig config, bool isLiveMode)
        {
            // In Live mode, we only want the last observation, in backtesitng we need the data in ascending order.
            var order = isLiveMode ? "desc" : "asc";
            var item = GetStringForDataTransformation(_dataTransformation);
            var url = $"{_baseUrl}identifier={config.Symbol.Value}&item={item}&sort_order={order}";
            var byteKey = Encoding.ASCII.GetBytes($"{IntrinioConfig.User}:{IntrinioConfig.Password}");
            var authorizationHeaders = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Authorization",
                                                 $"Basic ({Convert.ToBase64String(byteKey)})")
            };

            return new SubscriptionDataSource(url, SubscriptionTransportMedium.RemoteFile, FileFormat.Csv,
                                              authorizationHeaders);
        }
    }
}