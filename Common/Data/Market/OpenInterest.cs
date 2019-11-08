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

using QuantConnect.Util;
using System;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Defines a data type that represents open interest for given security
    /// </summary>
    public class OpenInterest : Tick
    {
        /// <summary>
        /// Initializes a new instance of the OpenInterest class
        /// </summary>
        public OpenInterest()
        {
            DataType = MarketDataType.Tick;
            TickType = TickType.OpenInterest;
            Value = 0;
            Time = new DateTime();
            Symbol = Symbol.Empty;
        }

        /// <summary>
        /// Cloner constructor for fill forward engine implementation. Clone the original OI into this new one:
        /// </summary>
        /// <param name="original">Original OI we're cloning</param>
        public OpenInterest(OpenInterest original)
        {
            DataType = MarketDataType.Tick;
            TickType = TickType.OpenInterest;
            Value = original.Value;
            Time = original.Time;
            Symbol = original.Symbol;
        }

        /// <summary>
        /// Initializes a new instance of the OpenInterest class with data
        /// </summary>
        /// <param name="time">Full date and time</param>
        /// <param name="symbol">Underlying equity security symbol</param>
        /// <param name="openInterest">Open Interest value</param>
        public OpenInterest(DateTime time, Symbol symbol, decimal openInterest)
        {
            DataType = MarketDataType.Tick;
            TickType = TickType.OpenInterest;
            Time = time;
            Symbol = symbol;
            Value = openInterest;
        }

        /// <summary>
        /// Constructor for QuantConnect open interest data
        /// </summary>
        /// <param name="config">Subscription configuration</param>
        /// <param name="symbol">Symbol for underlying asset</param>
        /// <param name="line">CSV line of data from QC OI csv</param>
        /// <param name="baseDate">The base date of the OI</param>
        public OpenInterest(SubscriptionDataConfig config, Symbol symbol, string line, DateTime baseDate)
        {
            var csv = line.Split(',');
            DataType = MarketDataType.Tick;
            TickType = TickType.OpenInterest;
            Symbol = symbol;

            Time = (config.Resolution == Resolution.Daily || config.Resolution == Resolution.Hour) ?
                // hourly and daily have different time format, and can use slow, robust c# parser.
                DateTime.ParseExact(csv[0], DateFormat.TwelveCharacter,
                    System.Globalization.CultureInfo.InvariantCulture)
                    .ConvertTo(config.DataTimeZone, config.ExchangeTimeZone)
                :
                // Using custom "ToDecimal" conversion for speed on high resolution data.
                baseDate.Date.AddMilliseconds(csv[0].ToInt32()).ConvertTo(config.DataTimeZone, config.ExchangeTimeZone);

            Value = csv[1].ToDecimal();
        }

        /// <summary>
        /// Parse an open interest data line from quantconnect zip source files.
        /// </summary>
        /// <param name="line">CSV source line of the compressed source</param>
        /// <param name="date">Base date for the open interest (date is stored as int milliseconds since midnight)</param>
        /// <param name="config">Subscription configuration object</param>
        public OpenInterest(SubscriptionDataConfig config, string line, DateTime date):
            this(config, config.Symbol, line, date)
        {
        }

        /// <summary>
        /// Tick implementation of reader method: read a line of data from the source and convert it to an open interest object.
        /// </summary>
        /// <param name="config">Subscription configuration object for algorithm</param>
        /// <param name="line">Line from the datafeed source</param>
        /// <param name="date">Date of this reader request</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>New initialized open interest object</returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            if (isLiveMode)
            {
                // currently OIs don't come through the reader function
                return new OpenInterest();
            }

            return new OpenInterest(config, line, date);
        }

        /// <summary>
        /// Get source for OI data feed - not used with QuantConnect data sources implementation.
        /// </summary>
        /// <param name="config">Configuration object</param>
        /// <param name="date">Date of this source request if source spread across multiple files</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>String source location of the file to be opened with a stream</returns>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            if (isLiveMode)
            {
                // this data type is streamed in live mode
                return new SubscriptionDataSource(string.Empty, SubscriptionTransportMedium.Streaming);
            }

            var source = LeanData.GenerateZipFilePath(Globals.DataFolder, config.Symbol, date, config.Resolution, config.TickType);
            if (config.SecurityType == SecurityType.Option ||
                config.SecurityType == SecurityType.Future)
            {
                source += "#" + LeanData.GenerateZipEntryName(config.Symbol, date, config.Resolution, config.TickType);
            }
            return new SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile, FileFormat.Csv);
        }

        /// <summary>
        /// Clone implementation for open interest class:
        /// </summary>
        /// <returns>New tick object clone of the current class values.</returns>
        public override BaseData Clone()
        {
            return new OpenInterest(this);
        }
    }
}