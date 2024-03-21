using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators;

public class DerivativeIndicatorOscillatorTest : CommonIndicatorTests<IndicatorDataPoint>
{
    protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
    {
        return new IndicatorDerivativeOscillator("DO14", 14,5,3,9);
    }

    protected override string TestFileName => "spy_derivative_oscillator_daily_14_5_3_9.txt";

    protected override string TestColumnName => "DO14";
    
    
}
