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

using System;
using QuantConnect.Data;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    /// <summary>
    /// Custom data type that uses a remote file download
    /// </summary>
    public class CustomMockedFileBaseData : BaseData
    {
        private int _incrementsToAdd;
        public static DateTime StartDate;

        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            var start = StartDate.AddTicks(config.Increment.Ticks * _incrementsToAdd++).ConvertFromUtc(config.DataTimeZone);
            return new CustomMockedFileBaseData
            {
                Symbol = config.Symbol,
                Time = start,
                EndTime = start.Add(config.Increment),
                Value = 10
            };
        }

        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            // using an already existing file, the file it self and its contents don't really matter. The reader will mock the values
            return new SubscriptionDataSource("./TestData/spy_with_ichimoku.csv", SubscriptionTransportMedium.LocalFile, FileFormat.Csv);
        }
    }
}