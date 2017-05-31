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
using System.Linq;
using QuantConnect.Data.Market;

namespace QuantConnect.Data.Utils
{
    public class Aggregate
    {
        /// <summary>
        /// Aggregates a list of ticks into tradebars at the requested resolution
        /// </summary>
        /// <param name="symbol">The symbol whose data the ticks represent</param>
        /// <param name="bars">The data formatted as bars</param>
        /// <param name="resolution">The desired resolution</param>
        /// <returns><see cref="IEnumerable{QuoteBar}"/></returns>
        public static IEnumerable<QuoteBar> BarsIntoQuoteBars(Symbol symbol, IEnumerable<QuoteBar> bars, TimeSpan resolution)
        {
            return
                (from t in bars.OrderBy(x => x.Time)
                 group t by t.Time.RoundDown(resolution)
                     into g
                 select new QuoteBar
                 {
                     Symbol = symbol,
                     Time = g.Key,
                     Ask = new Bar()
                     {
                         Open = g.First().Ask.Open,
                         High = g.Max(t => t.Ask.High),
                         Low = g.Min(t => t.Ask.Low),
                         Close = g.Last().Ask.Close
                     },
                     Bid = new Bar()
                     {
                         Open = g.First().Bid.Open,
                         High = g.Max(t => t.Bid.High),
                         Low = g.Min(t => t.Bid.Low),
                         Close = g.Last().Bid.Close
                     }
                 });
        }

        /// <summary>
        /// Aggregates a list of tradebars into tradebars at the requested
        /// resolution while filtering out any bars outside of the normal U.S.
        /// equity market hours
        /// </summary>
        /// <param name="symbol">The symbol whose data the ticks represent</param>
        /// <param name="bars">The data formatted as bars</param>
        /// <param name="timeSpan">The desired timespan</param>
        /// <returns><see cref="IEnumerable{TradeBar}"/></returns>
        public static IEnumerable<TradeBar> EquityHourDailyBarsIntoTradeBars(Symbol symbol, IEnumerable<TradeBar> bars, TimeSpan timeSpan)
        {
            return
               (from t in bars
                   where t.Time.TimeOfDay.TotalHours >= 9.5 && t.Time.TimeOfDay.TotalHours < 16
                group t by t.Time.RoundDown(timeSpan)
                    into g
                select new TradeBar
                {
                    Symbol = symbol,
                    Time = g.Key,
                    Open = g.First().Open,
                    High = g.Max(t => t.High),
                    Low = g.Min(t => t.Low),
                    Close = g.Last().Close,
                    Volume = g.Sum(x => x.Volume)
                });
        }
    }
}
