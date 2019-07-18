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

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm is a regression test for issue #2018 and PR #2038.
    /// </summary>
    public class OptionDataNullReferenceRegressionAlgorithm : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2016, 12, 1);
            SetEndDate(2017, 1, 1);
            SetCash(500000);

            AddEquity("DUST");

            var option = AddOption("DUST");

            option.SetFilter(u => u.IncludeWeeklys()
                                   .Strikes(-1, +1)
                                   .Expiration(TimeSpan.FromDays(25), TimeSpan.FromDays(100)));
        }
    }
}
