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
using System.IO;
using System.Linq;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Data.Custom
{
    /// <summary>
    ///     FXCM Real FOREX Volume and Transaction data from its clients base, available for the following pairs:
    ///     - EURUSD, USDJPY, GBPUSD, USDCHF, EURCHF, AUDUSD, USDCAD,
    ///       NZDUSD, EURGBP, EURJPY, GBPJPY, EURAUD, EURCAD, AUDJPY
    ///     FXCM only provides support for FX symbols which produced over 110 million average daily volume (ADV) during 2013.
    ///     This limit is imposed to ensure we do not highlight low volume/low ticket symbols in addition to other financial
    ///     reporting concerns.
    /// </summary>
    /// <seealso cref="QuantConnect.Data.BaseData" />
    public class FxcmVolume : BaseData
    {
        /// <summary>
        /// Auxiliary enum used to map the pair symbol into FXCM request code.
        /// </summary>
        private enum FxcmSymbolId
        {
            EURUSD = 1,
            USDJPY = 2,
            GBPUSD = 3,
            USDCHF = 4,
            EURCHF = 5,
            AUDUSD = 6,
            USDCAD = 7,
            NZDUSD = 8,
            EURGBP = 9,
            EURJPY = 10,
            GBPJPY = 11,
            EURAUD = 14,
            EURCAD = 15,
            AUDJPY = 17
        }

        /// <summary>
        ///     The request base URL.
        /// </summary>
        private readonly string _baseUrl = " http://marketsummary2.fxcorporate.com/ssisa/servlet?RT=SSI";

        /// <summary>
        ///     FXCM session id.
        /// </summary>
        private readonly string _sid = "quantconnect";

        /// <summary>
        ///     The columns index which should be added to obtain the transactions.
        /// </summary>
        private readonly long[] _transactionsIdx = { 27, 29, 31, 33 };

        /// <summary>
        ///     Integer representing client version.
        /// </summary>
        private readonly int _ver = 1;

        /// <summary>
        ///     The columns index which should be added to obtain the volume.
        /// </summary>
        private readonly int[] _volumeIdx = { 26, 28, 30, 32 };

        /// <summary>
        ///     Sum of opening and closing Transactions for the entire time interval.
        /// </summary>
        /// <value>
        ///     The transactions.
        /// </value>
        public int Transactions { get; set; }

        /// <summary>
        ///     Sum of opening and closing Volume for the entire time interval.
        ///     The volume measured in the QUOTE CURRENCY.
        /// </summary>
        /// <remarks>Please remember to convert this data to a common currency before making comparison between different pairs.</remarks>
        public long Volume { get; set; }

        /// <summary>
        ///     Return the URL string source of the file. This will be converted to a stream
        /// </summary>
        /// <param name="config">Configuration object</param>
        /// <param name="date">Date of this source file</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>
        ///     String URL of source file.
        /// </returns>
        /// <exception cref="System.NotImplementedException">FOREX Volume data is not available in live mode, yet.</exception>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            var interval = GetIntervalFromResolution(config.Resolution);
            var symbolId = GetFxcmIDFromSymbol(config.Symbol.Value.Split('_').First());

            if (isLiveMode)
            {
                var source = Invariant($"{_baseUrl}&ver={_ver}&sid={_sid}&interval={interval}&offerID={symbolId}");
                return new SubscriptionDataSource(source, SubscriptionTransportMedium.Rest, FileFormat.Csv);
            }
            else
            {
                var source = GenerateZipFilePath(config, date);
                return new SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile);
            }
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
            var fxcmVolume = new FxcmVolume { DataType = MarketDataType.Base, Symbol = config.Symbol };
            if (isLiveMode)
            {
                try
                {
                    var obs = line.Split('\n')[2].Split(';');
                    var stringDate = obs[0].Substring(startIndex: 3);
                    fxcmVolume.Time = DateTime.ParseExact(stringDate, "yyyyMMddHHmm", DateTimeFormatInfo.InvariantInfo);
                    fxcmVolume.Volume = _volumeIdx.Select(x => Parse.Long(obs[x])).Sum();
                    fxcmVolume.Transactions = _transactionsIdx.Select(x => Parse.Int(obs[x])).Sum();
                    fxcmVolume.Value = fxcmVolume.Volume;
                }
                catch (Exception exception)
                {
                    Logging.Log.Error($"Invalid data. Line: {line}. Exception: {exception.Message}");
                    return null;
                }
            }
            else
            {
                var obs = line.Split(',');
                if (config.Resolution == Resolution.Minute)
                {
                    fxcmVolume.Time = date.Date.AddMilliseconds(Parse.Int(obs[0]));
                }
                else
                {
                    fxcmVolume.Time = DateTime.ParseExact(obs[0], "yyyyMMdd HH:mm", CultureInfo.InvariantCulture);
                }
                fxcmVolume.Volume = Parse.Long(obs[1]);
                fxcmVolume.Transactions = obs[2].ConvertInvariant<int>();
                fxcmVolume.Value = fxcmVolume.Volume;
            }
            return fxcmVolume;
        }

        private static string GenerateZipFilePath(SubscriptionDataConfig config, DateTime date)
        {
            var source = Path.Combine(new[] { Globals.DataFolder, "forex", "fxcm", config.Resolution.ToLower() });
            string filename;

            var symbol = config.Symbol.Value.Split('_').First().ToLowerInvariant();
            if (config.Resolution == Resolution.Minute)
            {
                filename = Invariant($"{date:yyyyMMdd}_volume.zip");
                source = Path.Combine(source, symbol, filename);
            }
            else
            {
                filename = $"{symbol}_volume.zip";
                source = Path.Combine(source, filename);
            }
            return source;
        }

        /// <summary>
        ///     Gets the FXCM identifier from a FOREX pair ticker.
        /// </summary>
        /// <param name="ticker">The pair ticker.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Volume data is not available for the selected ticker. - ticker</exception>
        private int GetFxcmIDFromSymbol(string ticker)
        {
            int symbolId;
            try
            {
                symbolId = (int)Enum.Parse(typeof(FxcmSymbolId), ticker);
            }
            catch (ArgumentException)
            {
                throw new ArgumentOutOfRangeException(nameof(ticker), ticker,
                                                      "Volume data is not available for the selected ticker.");
            }
            return symbolId;
        }

        /// <summary>
        ///     Gets the string interval representation from the resolution.
        /// </summary>
        /// <param name="resolution">The requested resolution.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentOutOfRangeException">
        ///     resolution - tick or second resolution are not supported for Forex
        ///     Volume.
        /// </exception>
        private string GetIntervalFromResolution(Resolution resolution)
        {
            string interval;
            switch (resolution)
            {
                case Resolution.Minute:
                    interval = "M1";
                    break;

                case Resolution.Hour:
                    interval = "H1";
                    break;

                case Resolution.Daily:
                    interval = "D1";
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(resolution), resolution,
                                                          "Tick or second resolution are not supported for Forex Volume. Available resolutions are Minute, Hour and Daily.");
            }
            return interval;
        }
    }
}