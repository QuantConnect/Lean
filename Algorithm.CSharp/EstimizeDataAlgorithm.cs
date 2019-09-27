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

using QuantConnect.Data;
using QuantConnect.Data.Custom.Estimize;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This example algorithm shows how to import and use Estimize data types.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="custom data" />
    /// <meta name="tag" content="estimize" />
    public class EstimizeDataAlgorithm : QCAlgorithm
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2017, 1, 1);
            SetEndDate(2017, 12, 31);

            // be sure to add the underlying data source for our estimize data as it requires the mappings
            AddEquity("AAPL");

            AddData<EstimizeRelease>("AAPL");
            AddData<EstimizeEstimate>("AAPL");
            AddData<EstimizeConsensus>("AAPL");
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">EstimizeRelease object containing the stock release data</param>
        public void OnData(EstimizeRelease data)
        {
            Log($"{Time} - {data}");
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">EstimizeEstimate object containing the stock release data</param>
        public void OnData(EstimizeEstimate data)
        {
            Log($"{Time} - {data}");
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">EstimizeConsensus object containing the stock release data</param>
        public void OnData(EstimizeConsensus data)
        {
            Log($"{Time} - {data}");
        }
    }
}