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

using CsvHelper.Configuration.Attributes;
using System;

namespace QuantConnect.ToolBox.AlphaVantageDownloader
{
    /// <summary>
    /// Alpha Vantage time series model
    /// </summary>
    internal class TimeSeries
    {
        [Name("time", "timestamp")]
        public DateTime Time { get; set; }

        [Name("open")]
        public decimal Open { get; set; }

        [Name("high")]
        public decimal High { get; set; }

        [Name("low")]
        public decimal Low { get; set; }

        [Name("close")]
        public decimal Close { get; set; }

        [Name("volume")]
        public decimal Volume { get; set; }
    }
}
