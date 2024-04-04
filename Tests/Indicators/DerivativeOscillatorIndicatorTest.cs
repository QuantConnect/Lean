using NUnit.Framework;
using QuantConnect.Indicators;
using System;
using System.Linq;

namespace QuantConnect.Tests.Indicators;

public class DerivativeOscillatorIndicatorTest : CommonIndicatorTests<IndicatorDataPoint>
{
    protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
    {
        return new IndicatorDerivativeOscillator("Derivative Oscillator", 14, 5, 3, 9);
    }

    protected override string TestFileName => "spy_do.csv";

    protected override string TestColumnName => "DO";

    [Test]
    public void DoComputesCorrectly()
    {
        var derivativeOscillator = new IndicatorDerivativeOscillator(TestColumnName, 14, 5, 3, 9);

        // List of random prices for testing
        var prices = new [] { 100m, 105m, 110m, 115m, 120m, 125m, 130m, 135m, 140m, 145m, 150m, 155m, 160m, 165m, 170m };

        // Expected derivative oscillator value
        var expectedValue = 0;

        foreach (var price in prices)
        {
            derivativeOscillator.Update(new IndicatorDataPoint(DateTime.UtcNow, price));
        }

        // Get the computed value
        var computedValue = derivativeOscillator.Current.Value;

        // Assert
        Assert.AreEqual(expectedValue, computedValue, "Derivative oscillator value does not match expected value");
    }

    public override void ResetsProperly()
    {
        // derivativeOscillator reset is just setting the value and samples back to 0
        var derivativeOscillator = new IndicatorDerivativeOscillator("D014", 14, 5, 3, 9);

        foreach (var data in TestHelper.GetDataStream(5))
        {
            derivativeOscillator.Update(data);
        }

        Assert.IsTrue(derivativeOscillator.IsReady);
        Assert.AreNotEqual(0m, derivativeOscillator.Current.Value);
        Assert.AreNotEqual(0, derivativeOscillator.Samples);

        derivativeOscillator.Reset();

        TestHelper.AssertIndicatorIsInDefaultState(derivativeOscillator);
    }

    [Test]
    public void ComparesWithExternalData()
    {
        TestHelper.TestIndicator(
            CreateIndicator(),
            TestFileName,
            TestColumnName,
            (ind, expected) => Assert.AreEqual(expected, (double)((IndicatorDerivativeOscillator)ind).Current.Value)
        );
    }
}
