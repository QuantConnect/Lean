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

namespace QuantConnect.Report
{
    /// <summary>
    /// Crisis events utility class
    /// </summary>
    public class Crisis
    {
        /// <summary>
        /// Crisis events and pre-defined values
        /// </summary>
        public static readonly Dictionary<CrisisEvent, Crisis> Events = new Dictionary<CrisisEvent, Crisis>
        {
            {CrisisEvent.DotCom, new Crisis("DotCom Bubble 2000", new DateTime(2000, 2, 26), new DateTime(2000, 9, 10)) },
            {CrisisEvent.SeptemberEleventh, new Crisis("September 11, 2001", new DateTime(2001, 9, 5), new DateTime(2001, 10, 10)) },
            {CrisisEvent.USHousingBubble2003, new Crisis("U.S. Housing Bubble 2003", new DateTime(2003, 1, 1), new DateTime(2003, 2, 20)) },
            {CrisisEvent.GlobalFinancialCrisis, new Crisis("Global Financial Crisis 2007", new DateTime(2007, 10, 1), new DateTime(2011, 12, 1))},
            {CrisisEvent.FlashCrash, new Crisis("Flash Crash 2010", new DateTime(2010, 5, 1), new DateTime(2010, 5, 22))},
            {CrisisEvent.FukushimaMeltdown, new Crisis("Fukushima Meltdown 2011", new DateTime(2011, 3, 1), new DateTime(2011, 4, 22)) },
            {CrisisEvent.USDowngradeEuropeanDebt, new Crisis("U.S. Credit Downgrade 2011", new DateTime(2011, 8, 5), new DateTime(2011, 9, 1))},
            {CrisisEvent.EurozoneSeptember2012, new Crisis("ECB IR Event 2012", new DateTime(2012, 9, 5), new DateTime(2012, 10, 12))},
            {CrisisEvent.EurozoneOctober2014, new Crisis("European Debt Crisis 2014", new DateTime(2014, 10, 1), new DateTime(2014, 10, 29))},
            {CrisisEvent.MarketSellOff2015, new Crisis("Market Sell-Off 2015", new DateTime(2015, 8, 10), new DateTime(2015, 10, 10))},
            {CrisisEvent.Recovery, new Crisis("Recovery 2010-2012", new DateTime(2010, 1, 1), new DateTime(2012, 10, 1))},
            {CrisisEvent.NewNormal, new Crisis("New Normal 2014-2019", new DateTime(2014, 1, 1), new DateTime(2019, 1, 1))},
            {CrisisEvent.COVID19, new Crisis("COVID-19 Pandemic 2020", new DateTime(2020, 2, 10), new DateTime(2020, 9, 20))},
            {CrisisEvent.PostCOVIDRunUp, new Crisis("Post-COVID Run-up 2020-2021", new DateTime(2020, 4, 1), new DateTime(2022, 1, 1))},
            {CrisisEvent.MemeSeason, new Crisis("Meme Season 2021", new DateTime(2021, 1, 1), new DateTime(2021, 5, 15))},
            {CrisisEvent.RussiaInvadesUkraine, new Crisis("Russia Invades Ukraine 2022-2023", new DateTime(2022, 2, 1), new DateTime(2024, 1, 1))},
            {CrisisEvent.AIBoom, new Crisis("AI Boom 2022-Present", new DateTime(2022, 11, 30), DateTime.Now)},
        };

        /// <summary>
        /// Start of the crisis event
        /// </summary>
        public DateTime Start { get; }

        /// <summary>
        /// End of the crisis event
        /// </summary>
        public DateTime End { get; }

        /// <summary>
        /// Name of the crisis
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Creates a new crisis instance with the given name and start/end date.
        /// </summary>
        /// <param name="name">Name of the crisis</param>
        /// <param name="start">Start date of the crisis</param>
        /// <param name="end">End date of the crisis</param>
        public Crisis(string name, DateTime start, DateTime end)
        {
            Name = name;
            Start = start;
            End = end;
        }

        /// <summary>
        /// Returns a pre-defined crisis event
        /// </summary>
        /// <param name="crisisEvent">Crisis Event</param>
        /// <returns>Pre-defined crisis event</returns>
        public static Crisis FromCrisis(CrisisEvent crisisEvent)
        {
            return Events[crisisEvent];
        }

        /// <summary>
        /// Converts instance to string using the dates in the instance as start/end dates
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ToString(Start, End);
        }

        /// <summary>
        /// Converts instance to string using the provided dates
        /// </summary>
        /// <param name="start">Start date</param>
        /// <param name="end">End date</param>
        /// <returns></returns>
        public string ToString(DateTime start, DateTime end)
        {
            return $"{Name}";
        }
    }
}
