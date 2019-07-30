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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Custom.USTreasury;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration algorithm showing how to use and access U.S. Treasury yield curve data
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="custom data" />
    /// <meta name="tag" content="yield curve" />
    public class USTreasuryYieldCurveDataAlgorithm : QCAlgorithm
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2017, 1, 1);
            SetEndDate(2019, 6, 30);
            SetCash(100000);

            // Since yield data isn't associated with any ticker, we must put a placeholder ticker
            AddData<USTreasuryYieldCurveRate>("USTYC");
        }

        public override void OnData(Slice slice)
        {
            var data = slice.Get<USTreasuryYieldCurveRate>();

            foreach (var curve in data.Values)
            {
                Log($"{curve.Time} - 1M: {curve.OneMonth}, 2M: {curve.TwoMonth}, 3M: {curve.ThreeMonth}, 6M: {curve.SixMonth}, 1Y: {curve.OneYear}, 2Y: {curve.TwoYear}, 3Y: {curve.ThreeYear}, 5Y: {curve.FiveYear}, 10Y: {curve.TenYear}, 20Y: {curve.TwentyYear}, 30Y: {curve.ThirtyYear}");
            }
        }
    }
}
