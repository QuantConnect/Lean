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
using QuantConnect.Data.Custom.CBOE;

namespace QuantConnect.Algorithm.CSharp.AltData
{
    public class CachedAlternativeDataAlgorithm : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2003, 1, 1);
            SetEndDate(2019, 10, 11);
            SetCash(100000);

            // QuantConnect caches a small subset of alternative data for easy consumption for the community.
            // You can use this in your algorithm as demonstrated below:

            // CBOE VIX: http://cache.quantconnect.com/alternative/cboe/vix.csv
            AddData<CBOE>("VIX");
        }

        public override void OnData(Slice data)
        {
            var vix = data.Get<CBOE>("VIX");
            Log($"VIX: {Time}, {vix.Open}, {vix.High}, {vix.Low}, {vix.Close}");
        }
    }
}
