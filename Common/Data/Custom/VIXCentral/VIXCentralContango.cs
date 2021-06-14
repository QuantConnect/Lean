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
using System.IO;
using System.Linq;

namespace QuantConnect.Data.Custom.VIXCentral
{
    /// <summary>
    /// VIXCentral Contango
    /// </summary>
    public class VIXCentralContango : BaseData
    {
        /// <summary>
        /// The month of the front month contract (possible values: 1 - 12)
        /// </summary>
        public int FrontMonth { get; set; }

        /// <summary>
        /// Front month contract
        /// </summary>
        public decimal F1 { get; set; }

        /// <summary>
        /// Contract 1 month away from the front month contract
        /// </summary>
        public decimal F2 { get; set; }

        /// <summary>
        /// Contract 2 months away from the front month contract
        /// </summary>
        public decimal F3 { get; set; }

        /// <summary>
        /// Contract 3 months away from the front month contract
        /// </summary>
        public decimal F4 { get; set; }

        /// <summary>
        /// Contract 4 months away from the front month contract
        /// </summary>
        public decimal F5 { get; set; }

        /// <summary>
        /// Contract 5 months away from the front month contract
        /// </summary>
        public decimal F6 { get; set; }

        /// <summary>
        /// Contract 6 months away from the front month contract
        /// </summary>
        public decimal F7 { get; set; }

        /// <summary>
        /// Contract 7 months away from the front month contract
        /// </summary>
        public decimal F8 { get; set; }

        /// <summary>
        /// Contract 8 months away from the front month contract
        /// </summary>
        public decimal? F9 { get; set; }

        /// <summary>
        /// Contract 9 months away from the front month contract
        /// </summary>
        public decimal? F10 { get; set; }

        /// <summary>
        /// Contract 10 months away from the front month contract
        /// </summary>
        public decimal? F11 { get; set; }

        /// <summary>
        /// Contract 11 months away from the front month contract
        /// </summary>
        public decimal? F12 { get; set; }

        /// <summary>
        /// Percentage change between contract F2 and F1, calculated as: (F2 - F1) / F1
        /// </summary>
        public decimal Contango_F2_Minus_F1 { get; set; }

        /// <summary>
        /// Percentage change between contract F7 and F4, calculated as: (F7 - F4) / F4
        /// </summary>
        public decimal Contango_F7_Minus_F4 { get; set; }

        /// <summary>
        /// Percentage change between contract F7 and F4 divided by 3, calculated as: ((F7 - F4) / F4) / 3
        /// </summary>
        public decimal Contango_F7_Minus_F4_Div_3 { get; set; }

        /// <summary>
        /// The timespan that each data point covers
        /// </summary>
        public TimeSpan Period { get; set; }

        /// <summary>
        /// The ending time of the data point
        /// </summary>
        public override DateTime EndTime
        {
            get => Time + Period;
            set => Time = value - Period;
        }

        /// <summary>
        /// Creates a new instance of the object
        /// </summary>
        public VIXCentralContango()
        {
            Period = TimeSpan.FromDays(1);
            DataType = MarketDataType.Base;
        }

        /// <summary>
        /// Gets the source location of the VIXCentral data
        /// </summary>
        /// <param name="config"></param>
        /// <param name="date"></param>
        /// <param name="isLiveMode"></param>
        /// <returns></returns>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            var localFilePath = Path.Combine(
                Globals.DataFolder,
                "alternative",
                "vixcentral",
                "vix_contango.csv");

            return new SubscriptionDataSource(localFilePath, SubscriptionTransportMedium.LocalFile);
        }

        /// <summary>
        /// Reads the data from the source and creates a BaseData instance
        /// </summary>
        /// <param name="config">Configuration</param>
        /// <param name="line">Line of data</param>
        /// <param name="date">Date we're requesting data for</param>
        /// <param name="isLiveMode">Is live mode</param>
        /// <returns>New BaseData instance to be used in the algorithm</returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            // Return null if we don't have a valid date for the first entry
            if (!char.IsNumber(line.FirstOrDefault()))
            {
                return null;
            }

            try
            {
                var csv = line.Split(',');
                var contangoValues = csv.Skip(1)
                    .Select(x => Parse.TryParse(x, NumberStyles.Any, out decimal y) ? y : (decimal?) null)
                    .ToList();

                if (contangoValues.All(x => x == null || x.Value == 0))
                {
                    return null;
                }

                var dataDate = Parse.DateTimeExact(csv[0], "yyyy-MM-dd");

                return new VIXCentralContango
                {
                    // A one day delay is added to the end time automatically
                    Time = dataDate,
                    Symbol = config.Symbol,

                    FrontMonth = (int)contangoValues[0],
                    F1 = contangoValues[1].Value,
                    F2 = contangoValues[2].Value,
                    F3 = contangoValues[3].Value,
                    F4 = contangoValues[4].Value,
                    F5 = contangoValues[5].Value,
                    F6 = contangoValues[6].Value,
                    F7 = contangoValues[7].Value,
                    F8 = contangoValues[8].Value,
                    F9 = contangoValues[9],
                    F10 = contangoValues[10],
                    F11 = contangoValues[11],
                    F12 = contangoValues[12],

                    Contango_F2_Minus_F1 = contangoValues[13].Value,
                    Contango_F7_Minus_F4 = contangoValues[14].Value,
                    Contango_F7_Minus_F4_Div_3 = contangoValues[15].Value,
                };
            }
            catch (Exception err)
            {
                return null;
            }
        }

        /// <summary>
        /// Determines whether the data source requires mapping
        /// </summary>
        /// <returns>false</returns>
        public override bool RequiresMapping()
        {
            return false;
        }

        /// <summary>
        /// Determines if data source is sparse
        /// </summary>
        /// <returns>false</returns>
        public override bool IsSparseData()
        {
            return false;
        }

        /// <summary>
        /// Converts the instance to a string
        /// </summary>
        /// <returns>String containing open, high, low, close</returns>
        public override string ToString()
        {
            return $"{Time:yyyy-MM-dd} - {Symbol} :: Contango F2/F1: {Contango_F2_Minus_F1}, Contango F7/F3: {Contango_F7_Minus_F4}, Contango F7/F3 div 3: {Contango_F7_Minus_F4_Div_3}";
        }

        /// <summary>
        /// Gets the default resolution for this data and security type
        /// </summary>
        public override Resolution DefaultResolution()
        {
            return Resolution.Daily;
        }

        /// <summary>
        /// Gets the supported resolution for this data and security type
        /// </summary>
        public override List<Resolution> SupportedResolutions()
        {
            return DailyResolution;
        }
    }
}
