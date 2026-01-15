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
using QuantConnect.Data.Market;

namespace QuantConnect.Algorithm.CSharp
{
    public class SecuritySessionDailyNoPreciseEndTimeRegressionAlgorithm : SecuritySessionRegressionAlgorithm
    {
        protected override Resolution Resolution => Resolution.Daily;
        protected override bool DailyPreciseEndTime => false;

        protected override void ConfigureSchedule()
        {
            Schedule.On(DateRules.EveryDay(), TimeRules.At(0, 0, 1), ValidateSessionBars);
        }

        protected override void ValidateSessionBars()
        {
            if (ProcessedDataCount == 0)
            {
                return;
            }
            var session = Security.Session;
            PreviousSessionBar = new TradeBar(CurrentDate, Security.Symbol, Open, High, Low, Close, Volume);

            // At this point the data was consolidated so we can check the previous session bar (index 1)
            if (session[1].Open != Open
                || session[1].High != High
                || session[1].Low != Low
                || session[1].Close != Close
                || session[1].Volume != Volume)
            {
                throw new RegressionTestException("Mismatch in current session bar (OHLCV)");
            }
        }


        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public override List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 48;
    }
}
