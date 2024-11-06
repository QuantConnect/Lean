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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.ToolBox.RandomDataGenerator;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class PremierStochasticOscillatorTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            RenkoBarSize = 1m;
            VolumeRenkoBarSize = 0.5m;
            return new PremierStochasticOscillator("PSO", 8, 5);
        }

        protected override string TestFileName => "spy_pso.csv";

        protected override string TestColumnName => "pso";

        protected override Action<IndicatorBase<IBaseDataBar>, double> Assertion =>
            (indicator, expected) =>
                Assert.AreEqual(expected, (double)((PremierStochasticOscillator)indicator).Current.Value, 1e-3);

        [Test]
        public void IsReadyAfterPeriodUpdates()
        {
            int period = 3;
            int emaPeriod = 2;
            var pso = new PremierStochasticOscillator(period, emaPeriod);
            int minInputValues = period + 2 * (emaPeriod - 1);
            for (int i = 0; i < minInputValues; i++)
            {
                var data = new TradeBar
                {
                    Symbol = Symbol.Empty,
                    Time = DateTime.Now.AddSeconds(i),
                    Close = i
                };
                pso.Update(data);
            }
            Assert.IsTrue(pso.IsReady);
        }
    }
}
