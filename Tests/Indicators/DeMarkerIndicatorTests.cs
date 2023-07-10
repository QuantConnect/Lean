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

using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class DeMarkerIndicatorTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            RenkoBarSize = 0.001m;
            VolumeRenkoBarSize = 1000m;
            return new DeMarkerIndicator("DEM", 14);
        }
        
        protected override string TestFileName => "eurusd60_dem.txt";

        protected override string TestColumnName => "dem";
        
        [Test]
        public void TestDivByZero()
        {
            var dem = new DeMarkerIndicator("DEM", 3);
            foreach (var data in TestHelper.GetDataStream(4))
            {
                // Should handle High = Low case by returning 0m.
                var tradeBar = new TradeBar
                {
                    Open = data.Value,
                    Close = data.Value,
                    High = 1,
                    Low = 1,
                    Volume = 1
                };
                dem.Update(tradeBar); 
            }
            Assert.AreEqual(dem.Current.Value, 0m);
        }
    }
}
      
