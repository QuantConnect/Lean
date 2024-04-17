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
using QuantConnect.Indicators;
using System;

namespace QuantConnect.Tests.Indicators;

[TestFixture]
public class DerivativeOscillatorIndicatorTest : CommonIndicatorTests<IndicatorDataPoint>
{
    protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
    {
        return new DerivativeOscillator("D014", 14, 5, 3, 9);
    }

    protected override string TestFileName => "spy_do.csv";

    protected override string TestColumnName => "DO";

    [Test]
    public override void ResetsProperly()
    {
        var derivativeOscillator = new DerivativeOscillator("D014", 14, 5, 3, 9);
        var period = derivativeOscillator.WarmUpPeriod;

        // Generate a stream of random data and calculate the derivative oscillator
        var seed = 14;
        Random rand = new Random(seed);
        var reference = DateTime.Today;
        for(int i = 0; i < period; i++) 
        {
            var data = new IndicatorDataPoint(reference.AddSeconds(i), rand.Next());
            derivativeOscillator.Update(data);
        }

        var expected = derivativeOscillator.Current.Value;
        
        Assert.IsTrue(derivativeOscillator.IsReady);
        Assert.AreNotEqual(0m, derivativeOscillator.Current.Value);
        Assert.AreNotEqual(0, derivativeOscillator.Samples);

        // Now do some partial updates
        for(int i = 0; i < 5; i++)
        {
            var data = new IndicatorDataPoint(reference.AddSeconds(i), rand.Next());
            derivativeOscillator.Update(data);
        }
        
        // Check if reset functionality works
        derivativeOscillator.Reset();
        TestHelper.AssertIndicatorIsInDefaultState(derivativeOscillator);

        // Update using the exact same random data stream again to verify if internals were reset properly
        // But now insert data until it is ready.
        rand = new Random(seed);
        reference = DateTime.Today;
        int j = 0;
        while (!derivativeOscillator.IsReady)
        {
            var data = new IndicatorDataPoint(reference.AddSeconds(j), rand.Next());
            derivativeOscillator.Update(data);
            j++;
        }

        Assert.IsTrue(derivativeOscillator.IsReady);
        Assert.AreNotEqual(0m, derivativeOscillator.Current.Value);
        Assert.AreNotEqual(0, derivativeOscillator.Samples);

        // If they are not equal, the internal indicators were not reset properly
        Assert.AreEqual(expected, derivativeOscillator.Current.Value);
    }

    [Test]
    public void ComparesWithExternalData()
    {
        TestHelper.TestIndicator(
            CreateIndicator(),
            TestFileName,
            TestColumnName,
            (ind, expected) => Assert.AreEqual(expected, (double)((DerivativeOscillator)ind).Current.Value, 1e-9)
        );
    }
}
