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
using System;

namespace QuantConnect.Tests.Indicators 
{
	[TestFixture]
	public class RollingSharpeRatioTests : CommonIndicatorTests<IndicatorDataPoint>
    {   
    	protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
    	{
            return new RollingSharpeRatio("RSR", 5, 5);
    	}

        protected override string TestFileName => "spy_rsr.txt";

        protected override string TestColumnName => "RSR_10_10";

        [Test]
        public void TestTradeBarsWithSameValue() 
        {
    		// With the value not changing, the indicator should return default value 0m.
    		var rsr = new RollingSharpeRatio("RSR", 10, 10);
    		
    		// push the value 100000 into the indicator 20 times (sharpeRatioPeriod + movingAveragePeriod)
    		for(int i = 0; i < 20; i++) {
    			IndicatorDataPoint point = new IndicatorDataPoint(new DateTime(), 100000m);
    			rsr.Update(point);
    		}
    		
    		Assert.AreEqual(rsr.Current.Value, 0m);
        }
        
        [Test]
        public void TestTradeBarsWithDifferingValue() 
        {
        	// With the value changing, the indicator should return a value that is not the default 0m.
        	var rsr = new RollingSharpeRatio("RSR", 10, 10);
    		
    		// push the value 100000 into the indicator 20 times (sharpeRatioPeriod + movingAveragePeriod)
    		for(int i = 0; i < 20; i++) {
    			IndicatorDataPoint point = new IndicatorDataPoint(new DateTime(), 100000m + i);
    			rsr.Update(point);
    		}
    		
    		Assert.AreNotEqual(rsr.Current.Value, 0m);
        }
        
        [Test]
        public void TestDivByZero()
        {
        	// With the value changing, the indicator should return a value that is not the default 0m.
        	var rsr = new RollingSharpeRatio("RSR", 10, 10);
    		
    		// push the value 100000 into the indicator 20 times (sharpeRatioPeriod + movingAveragePeriod)
    		for(int i = 0; i < 20; i++) {
    			IndicatorDataPoint point = new IndicatorDataPoint(new DateTime(), 0);
    			rsr.Update(point);
    		}
    		
    		Assert.AreEqual(rsr.Current.Value, 0m);
        }
    }
}