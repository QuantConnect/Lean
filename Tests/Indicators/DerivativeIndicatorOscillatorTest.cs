using NUnit.Framework;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators;

public class DerivativeIndicatorOscillatorTest : CommonIndicatorTests<IndicatorDataPoint>
{
    protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
    {
        return new IndicatorDerivativeOscillator("DO14", 14, 5, 3, 9);
    }

    protected override string TestFileName => "spy_derivative_oscillator_daily_14_5_3_9.txt";

    protected override string TestColumnName => "DO14";

    [Test]
    public void DoComputesCorrectly()
    {
        
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
}
