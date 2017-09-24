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

using System.Collections.Generic;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.Custom;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Use event/fundamental calendar information (DailyFx) to design event based forex algorithms.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="custom data" />
    /// <meta name="tag" content="forex" />
    /// <meta name="tag" content="dailyfx" />
    public class DailyFxAlgorithm : QCAlgorithm
    {
        private int _sliceCount;
        private int _eventCount;
        private readonly Dictionary<string, DailyFx> _uniqueConfirmation = new Dictionary<string, DailyFx>();

        /// <summary>
        /// Add the Daily FX type to our algorithm and use its events.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2016, 05, 26);  //Set Start Date
            SetEndDate(2016, 05, 27);    //Set End Date
            SetCash(100000);             //Set Strategy Cash
            AddData<DailyFx>("DFX", Resolution.Second, DateTimeZone.Utc);
        }

        public override void OnData(Slice slice)
        {
            var result = slice.Get<DailyFx>();
            Debug(string.Format("SLICE >> {0} : {1}", _sliceCount++, result));
        }

        /// <summary>
        /// Trigger an event on a complete calendar event which has an actual value.
        /// </summary>
        public void OnData(DailyFx calendar)
        {
            // Used to validate the data is unique.
            _uniqueConfirmation.Add(calendar.ToString(), calendar);
            Debug(string.Format("ONDATA >> {0}: {1}", _eventCount++, calendar));
        }
    }
}