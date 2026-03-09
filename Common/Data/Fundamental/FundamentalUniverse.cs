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
using Python.Runtime;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Data.Fundamental
{
    /// <summary>
    /// Lean fundamentals universe data class
    /// </summary>
    [Obsolete("'Fundamentals' was renamed to 'FundamentalUniverse'")]
    public class Fundamentals : FundamentalUniverse { }

    /// <summary>
    /// Lean fundamentals universe data class
    /// </summary>
    public class FundamentalUniverse : BaseDataCollection
    {
        private static readonly Fundamental _factory = new();

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public FundamentalUniverse()
        {
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="time">The current time</param>
        /// <param name="symbol">The associated symbol</param>
        public FundamentalUniverse(DateTime time, Symbol symbol) : base(time, symbol)
        {
        }

        /// <summary>
        /// Return the URL string source of the file. This will be converted to a stream
        /// </summary>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            var path = _factory.GetSource(config, date, isLiveMode).Source;
            return new SubscriptionDataSource(path, SubscriptionTransportMedium.LocalFile, FileFormat.FoldingCollection);
        }

        /// <summary>
        /// Will read a new instance from the given line
        /// </summary>
        /// <param name="config">The associated requested configuration</param>
        /// <param name="line">The line to parse</param>
        /// <param name="date">The current time</param>
        /// <param name="isLiveMode">True if live mode</param>
        /// <returns>A new instance or null</returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            try
            {
                var csv = line.Split(',');
                var symbol = new Symbol(SecurityIdentifier.Parse(csv[0]), csv[1]);
                return new Fundamental(date, symbol);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Will clone the current instance
        /// </summary>
        /// <returns>The cloned instance</returns>
        public override BaseData Clone()
        {
            return new FundamentalUniverse(Time, Symbol) { Data = Data, EndTime = EndTime };
        }

        /// <summary>
        /// Gets the default resolution for this data and security type
        /// </summary>
        /// <remarks>This is a method and not a property so that python
        /// custom data types can override it</remarks>
        public override Resolution DefaultResolution()
        {
            return Resolution.Daily;
        }

        /// <summary>
        /// Creates the universe symbol for the target market
        /// </summary>
        /// <returns>The universe symbol to use</returns>
        public override Symbol UniverseSymbol(string market = null)
        {
            market ??= QuantConnect.Market.USA;
            var ticker = $"{GetType().Name}-{market}-{Guid.NewGuid()}";
            return Symbol.Create(ticker, SecurityType.Equity, market, baseDataType: GetType());
        }

        /// <summary>
        /// Creates a new fundamental universe for the USA market
        /// </summary>
        /// <param name="selector">The selector function</param>
        /// <param name="universeSettings">The universe settings to use, will default to algorithms if not provided</param>
        /// <returns>A configured new universe instance</returns>
        public static FundamentalUniverseFactory USA(Func<IEnumerable<Fundamental>, IEnumerable<Symbol>> selector, UniverseSettings universeSettings = null)
        {
            return new FundamentalUniverseFactory(QuantConnect.Market.USA, universeSettings, selector);
        }

        /// <summary>
        /// Creates a new fundamental universe for the USA market
        /// </summary>
        /// <param name="selector">The selector function</param>
        /// <param name="universeSettings">The universe settings to use, will default to algorithms if not provided</param>
        /// <returns>A configured new universe instance</returns>
        public static FundamentalUniverseFactory USA(PyObject selector, UniverseSettings universeSettings = null)
        {
            return new FundamentalUniverseFactory(QuantConnect.Market.USA, universeSettings, selector);
        }

        /// <summary>
        /// Creates a new fundamental universe for the USA market
        /// </summary>
        /// <param name="selector">The selector function</param>
        /// <param name="universeSettings">The universe settings to use, will default to algorithms if not provided</param>
        /// <returns>A configured new universe instance</returns>
        public static FundamentalUniverseFactory USA(Func<IEnumerable<Fundamental>, object> selector, UniverseSettings universeSettings = null)
        {
            return new FundamentalUniverseFactory(QuantConnect.Market.USA, universeSettings, selector);
        }
    }
}
