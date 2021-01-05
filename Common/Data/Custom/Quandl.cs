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
using System.Net;

namespace QuantConnect.Data.Custom
{
    /// <summary>
    /// Quandl Data Type - Import generic data from quandl, without needing to define Reader methods.
    /// This reads the headers of the data imported, and dynamically creates properties for the imported data.
    /// </summary>
    public class Quandl : DynamicData
    {
        private bool _isInitialized;
        private readonly List<string> _propertyNames = new List<string>();
        private readonly string _valueColumn;
        private static string _authCode = "";

        /// <summary>
        /// Static constructor for the <see cref="Quandl"/> class
        /// </summary>
        static Quandl()
        {
            // The Quandl API now requires TLS 1.2 for API requests (since 9/18/2018).
            // NET 4.5.2 and below does not enable this more secure protocol by default, so we add it in here
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        /// <summary>
        /// Flag indicating whether or not the Quanl auth code has been set yet
        /// </summary>
        public static bool IsAuthCodeSet
        {
            get;
            private set;
        }

        /// <summary>
        /// The end time of this data. Some data covers spans (trade bars) and as such we want
        /// to know the entire time span covered
        /// </summary>
        public override DateTime EndTime
        {
            get { return Time + Period; }
            set { Time = value - Period; }
        }

        /// <summary>
        /// Gets a time span of one day
        /// </summary>
        public TimeSpan Period
        {
            get { return QuantConnect.Time.OneDay; }
        }

        /// <summary>
        /// Default quandl constructor uses Close as its value column
        /// </summary>
        public Quandl() : this("Close")
        {
        }

        /// <summary>
        /// Constructor for creating customized quandl instance which doesn't use "Close" as its value item.
        /// </summary>
        /// <param name="valueColumnName"></param>
        protected Quandl(string valueColumnName)
        {
            _valueColumn = valueColumnName;
        }

        /// <summary>
        /// Generic Reader Implementation for Quandl Data.
        /// </summary>
        /// <param name="config">Subscription configuration</param>
        /// <param name="line">CSV line of data from the souce</param>
        /// <param name="date">Date of the requested line</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns></returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            // be sure to instantiate the correct type
            var data = (Quandl) Activator.CreateInstance(GetType());
            data.Symbol = config.Symbol;
            var csv = line.Split(',');

            if (!_isInitialized)
            {
                _isInitialized = true;
                foreach (var propertyName in csv)
                {
                    var property = propertyName.Trim();
                    // should we remove property names like Time?
                    // do we need to alias the Time??
                    data.SetProperty(property, 0m);
                    _propertyNames.Add(property);
                }
                // Returns null at this point where we are only reading the properties names
                return null;
            }

            data.Time = DateTime.ParseExact(csv[0], "yyyy-MM-dd", CultureInfo.InvariantCulture);

            for (var i = 1; i < csv.Length; i++)
            {
                var value = csv[i].ToDecimal();
                data.SetProperty(_propertyNames[i], value);
            }

            // we know that there is a close property, we want to set that to 'Value'
            data.Value = (decimal)data.GetProperty(_valueColumn);

            return data;
        }

        /// <summary>
        /// Quandl Source Locator: Using the Quandl V1 API automatically set the URL for the dataset.
        /// </summary>
        /// <param name="config">Subscription configuration object</param>
        /// <param name="date">Date of the data file we're looking for</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>STRING API Url for Quandl.</returns>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            var source = $"https://www.quandl.com/api/v3/datasets/{config.Symbol.Value}.csv?order=asc&api_key={_authCode}";
            return new SubscriptionDataSource(source, SubscriptionTransportMedium.RemoteFile);
        }

        /// <summary>
        /// Set the auth code for the quandl set to the QuantConnect auth code.
        /// </summary>
        /// <param name="authCode"></param>
        public static void SetAuthCode(string authCode)
        {
            if (string.IsNullOrWhiteSpace(authCode)) return;

            _authCode = authCode;
            IsAuthCodeSet = true;
        }
    }
}
