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

using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Data.Custom.SEC;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Provides an example algorithm showcasing the <see cref="Security.Data"/> features
    /// </summary>
    public class DynamicSecurityDataAlgorithm : QCAlgorithm
    {
        private Security GOOGL;
        private const string Ticker = "GOOGL";

        public override void Initialize()
        {
            SetStartDate(2015, 10, 22);
            SetEndDate(2015, 10, 30);

            GOOGL = AddEquity(Ticker, Resolution.Daily);

            AddData<SECReport8K>(Ticker, Resolution.Daily);
            AddData<SECReport10K>(Ticker, Resolution.Daily);
            AddData<SECReport10Q>(Ticker, Resolution.Daily);
        }

        public override void OnData(Slice slice)
        {
            // The Security object's Data property provides convenient access
            // to the various types of data related to that security. You can
            // access not only the security's price data, but also any custom
            // data that is mapped to the security, such as our SEC reports.

            // 1. Get the most recent data point of a particular type:
            // 1.a Using the C# generic method, Get<T>:
            SECReport8K googlSec8kReport = GOOGL.Data.Get<SECReport8K>();
            SECReport10K googlSec10kReport = GOOGL.Data.Get<SECReport10K>();
            Log($"{Time:o}:  8K: {googlSec8kReport}");
            Log($"{Time:o}: 10K: {googlSec10kReport}");

            // 2. Get the list of data points of a particular type for the most recent time step:
            // 2.a Using the C# generic method, GetAll<T>:
            List<SECReport8K> googlSec8kReports = GOOGL.Data.GetAll<SECReport8K>();
            List<SECReport10K> googlSec10kReports = GOOGL.Data.GetAll<SECReport10K>();
            Log($"{Time:o}: List:  8K: {googlSec8kReports.Count}");
            Log($"{Time:o}: List: 10K: {googlSec10kReports.Count}");

            if (!Portfolio.Invested)
            {
                Buy(GOOGL.Symbol, 10);
            }
        }
    }
}
