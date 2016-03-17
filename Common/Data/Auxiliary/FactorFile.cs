﻿/*
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuantConnect.Logging;

namespace QuantConnect.Data.Auxiliary
{
    /// <summary>
    /// Represents an entire factor file for a specified symbol
    /// </summary>
    public class FactorFile
    {
        private readonly SortedList<DateTime, FactorFileRow> _data;

        /// <summary>
        /// Gets the symbol this factor file represents
        /// </summary>
        public string Permtick { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FactorFile"/> class.
        /// </summary>
        public FactorFile(string permtick, IEnumerable<FactorFileRow> data)
        {
            Permtick = permtick.ToUpper();
            _data = new SortedList<DateTime, FactorFileRow>(data.ToDictionary(x => x.Date));
        }

        /// <summary>
        /// Reads a FactorFile in from the <see cref="Globals.DataFolder"/>.
        /// </summary>
        public static FactorFile Read(string permtick, string market)
        {
            return new FactorFile(permtick, FactorFileRow.Read(permtick, market));
        }

        /// <summary>
        /// Gets the price scale factor that includes dividend and split adjustments for the specified search date
        /// </summary>
        public decimal GetPriceScaleFactor(DateTime searchDate)
        {
            decimal factor = 1;
            //Iterate backwards to find the most recent factor:
            foreach (var splitDate in _data.Keys.Reverse())
            {
                if (splitDate.Date < searchDate.Date) break;
                factor = _data[splitDate].PriceScaleFactor;
            }
            return factor;
        }

        /// <summary>
        /// Gets the split factor to be applied at the specified date
        /// </summary>
        public decimal GetSplitFactor(DateTime searchDate)
        {
            decimal factor = 1;
            //Iterate backwards to find the most recent factor:
            foreach (var splitDate in _data.Keys.Reverse())
            {
                if (splitDate.Date < searchDate.Date) break;
                factor = _data[splitDate].SplitFactor;
            }
            return factor;
        }

        /// <summary>
        /// Checks whether or not a symbol has scaling factors
        /// </summary>
        public static bool HasScalingFactors(string permtick, string market)
        {
            // check for factor files
            var path = Path.Combine(Globals.DataFolder, "equity", market, "factor_files", permtick.ToLower() + ".csv");
            if (File.Exists(path))
            {
                return true;
            }
            Log.Trace("FactorFile.HasScalingFactors(): Factor file not found: " + permtick);
            return false;
        }

        /// <summary>
        /// Returns true if the specified date is the last trading day before a dividend event
        /// is to be fired
        /// </summary>
        /// <remarks>
        /// NOTE: The dividend event in the algorithm should be fired at the end or AFTER
        /// this date. This is the date in the file that a factor is applied, so for example,
        /// MSFT has a 31 cent dividend on 2015.02.17, but in the factor file the factor is applied
        /// to 2015.02.13, which is the first trading day BEFORE the actual effective date.
        /// </remarks>
        /// <param name="date">The date to check the factor file for a dividend event</param>
        /// <param name="priceFactorRatio">When this function returns true, this value will be populated
        /// with the price factor ratio required to scale the closing value (pf_i/pf_i+1)</param>
        public bool HasDividendEventOnNextTradingDay(DateTime date, out decimal priceFactorRatio)
        {
            priceFactorRatio = 0;
            var index = _data.IndexOfKey(date);
            if (index > -1 && index < _data.Count - 1)
            {
                // grab the next key to ensure it's a dividend event
                var thisRow = _data.Values[index];
                var nextRow = _data.Values[index + 1];

                // if the price factors have changed then it's a dividend event
                if (thisRow.PriceFactor != nextRow.PriceFactor)
                {
                    priceFactorRatio = thisRow.PriceFactor/nextRow.PriceFactor;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if the specified date is the last trading day before a split event
        /// is to be fired
        /// </summary>
        /// <remarks>
        /// NOTE: The split event in the algorithm should be fired at the end or AFTER this
        /// date. This is the date in the file that a factor is applied, so for example MSFT
        /// has a split on 1999.03.29, but in the factor file the split factor is applied on
        /// 1999.03.26, which is the first trading day BEFORE the actual split date.
        /// </remarks>
        public bool HasSplitEventOnNextTradingDay(DateTime date, out decimal splitFactor)
        {
            splitFactor = 1;
            var index = _data.IndexOfKey(date);
            if (index > -1 && index < _data.Count - 1)
            {
                // grab the next key to ensure it's a split event
                var thisRow = _data.Values[index];
                var nextRow = _data.Values[index + 1];

                // if the split factors have changed then it's a split event
                if (thisRow.SplitFactor != nextRow.SplitFactor)
                {
                    splitFactor = thisRow.SplitFactor/nextRow.SplitFactor;
                    return true;
                }
            }
            return false;
        }
    }
}