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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Algorithm.CSharp.AltData
{
    public class CBOEAlgorithm : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2003, 1, 1);
            SetEndDate(2019, 10, 11);
            SetCash(100000);

            AddData<CBOE>("VIX");
        }

        public override void OnData(Slice data)
        {
            foreach (var point in data.Get<CBOE>())
            {
                var value = point.Value;
                Log($"{Time} - {value.Time}, {value.Open}, {value.High}, {value.Low}, {value.Close}");
            }
        }
    }
}
