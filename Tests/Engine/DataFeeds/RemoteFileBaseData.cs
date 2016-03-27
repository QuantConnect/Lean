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
    public class RemoteFileBaseData : BaseData
    {
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            var csv = line.Split(',');
            if (csv[1].ToLower() != config.Symbol.ToString().ToLower())
            {
                // this row isn't for me
                return null;
            }

            var time = QuantConnect.Time.UnixTimeStampToDateTime(double.Parse(csv[0])).ConvertFromUtc(config.DataTimeZone).Subtract(config.Increment);
            return new RemoteFileBaseData
            {
                Symbol = config.Symbol,
                Time = time,
                EndTime = time.Add(config.Increment),
                Value = decimal.Parse(csv[3])

            };
        }

        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            // this file is only a few seconds worth of data, so it's quick to download
            var remoteFileSource = @"http://www.quantconnect.com/live-test?type=file&symbols=" + config.Symbol.Value;
            remoteFileSource = @"http://beta.quantconnect.com/live-test?type=file&symbols=" + config.Symbol.Value;
            return new SubscriptionDataSource(remoteFileSource, SubscriptionTransportMedium.RemoteFile, FileFormat.Csv);
        }
    }
}