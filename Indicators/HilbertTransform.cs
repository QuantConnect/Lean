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

using System.Diagnostics;

namespace QuantConnect.Indicators;

/// <summary>
/// This indicator computes the Hilbert Transform Indicator by John Ehlers.
/// By using present and prior price differences, and some feedback, price values are split into their complex number components
/// of real (inPhase) and imaginary (quadrature) parts.
/// <remark>Source: http://www.technicalanalysis.org.uk/moving-averages/Ehle.pdf</remark>
/// </summary>
public class HilbertTransform : Indicator, IIndicatorWarmUpPeriodProvider
{
    private readonly int _length;
    private readonly IndicatorBase<IndicatorDataPoint> _input;
    private readonly IndicatorBase<IndicatorDataPoint> _prev;
    private readonly IndicatorBase<IndicatorDataPoint> _detrendPrice;
    private readonly IndicatorBase<IndicatorDataPoint> _detrendPriceDelay2;
    private readonly IndicatorBase<IndicatorDataPoint> _detrendPriceDelay4;
    private readonly IndicatorBase<IndicatorDataPoint> _inPhaseDelay3;
    private readonly IndicatorBase<IndicatorDataPoint> _quadratureDelay2;

    /// <summary>
    /// Real (inPhase) part of complex number component of price values
    /// </summary>
    public IndicatorBase<IndicatorDataPoint> InPhase { get; }

    /// <summary>
    /// Imaginary (quadrature) part of complex number component of price values
    /// </summary>
    public IndicatorBase<IndicatorDataPoint> Quadrature { get; }

    /// <summary>
    /// Required period, in data points, for the indicator to be ready and fully initialized.
    /// </summary>
    public int WarmUpPeriod => _length + 2;

    /// <summary>
    /// Gets a flag indicating when this indicator is ready and fully initialized
    /// </summary>
    public override bool IsReady => Samples >= WarmUpPeriod;

    /// <summary>
    /// Creates a new Hilbert Transform indicator
    /// </summary>
    /// <param name="name">The name of this indicator</param>
    /// <param name="length">The length of the FIR filter used in the calculation of the Hilbert Transform.
    /// This parameter determines the number of filter coefficients in the FIR filter.</param>
    /// <param name="inPhaseMultiplicationFactor">The multiplication factor used in the calculation of the in-phase component of the Hilbert Transform. 
    /// This parameter adjusts the sensitivity and responsiveness of the transform to changes in the input signal.</param>
    /// <param name="quadratureMultiplicationFactor">The multiplication factor used in the calculation of the quadrature component of the Hilbert Transform. 
    /// This parameter also adjusts the sensitivity and responsiveness of the transform to changes in the input signal.</param>
    public HilbertTransform(string name, int length, decimal inPhaseMultiplicationFactor, decimal quadratureMultiplicationFactor)
        : base(name)
    {
        _length = length;

        _input = new Identity(name + "_input");
        _prev = new Delay(name + "_prev", length);
        _detrendPrice = _input.Minus(_prev);
        // 2nd and 4th order difference in detrended price, thus being the 1st and 3rd delay
        _detrendPriceDelay2 = new Delay(name + "_detrendPriceDelay2", 1);
        _detrendPriceDelay4 = new Delay(name + "_detrendPriceDelay4", 3);
        // Update after InPhase & Quadrature property, so delay length -1
        _inPhaseDelay3 = new Delay(name + "_inPhaseDelay3", 2);
        _quadratureDelay2 = new Delay(name + "_quadratureDelay2", 1);

        InPhase = new FunctionalIndicator<IndicatorDataPoint>(name + "_inPhase",
            _ =>
            {
                if (!InPhase!.IsReady)
                {
                    return 0m;
                }

                var v2Value = _detrendPriceDelay2.IsReady ? _detrendPriceDelay2.Current.Value : 0m;
                var v4Value = _detrendPriceDelay4.IsReady ? _detrendPriceDelay4.Current.Value : 0m;
                var inPhase3Value = _inPhaseDelay3.IsReady ? _inPhaseDelay3.Current.Value : 0m;
                return 1.25m * (v4Value - v2Value * inPhaseMultiplicationFactor) + inPhase3Value * inPhaseMultiplicationFactor;
            },
            _ => Samples > length + 2,
            () =>
            {
                _detrendPrice.Reset();
                _detrendPriceDelay2.Reset();
                _detrendPriceDelay4.Reset();
                _inPhaseDelay3.Reset();
            });

        Quadrature = new FunctionalIndicator<IndicatorDataPoint>(name + "_quad",
            _ =>
            {
                if (!Quadrature!.IsReady)
                {
                    return 0m;
                }

                var v2Value = _detrendPriceDelay2.IsReady ? _detrendPriceDelay2.Current.Value : 0m;
                var v1Value = _detrendPrice.IsReady ? _detrendPrice.Current.Value : 0m;
                var quadrature2Value = _quadratureDelay2.IsReady ? _quadratureDelay2.Current.Value : 0m;
                return v2Value - v1Value * quadratureMultiplicationFactor + quadrature2Value * quadratureMultiplicationFactor;
            },
            _ => Samples > length,
            () =>
            {
                _detrendPrice.Reset();
                _detrendPriceDelay2.Reset();
                _quadratureDelay2.Reset();
            });
    }

    /// <summary>
    /// Creates a new Hilbert Transform indicator with default name and default params
    /// </summary>
    /// <param name="length">The length of the FIR filter used in the calculation of the Hilbert Transform.
    /// This parameter determines the number of filter coefficients in the FIR filter.</param>
    /// <param name="inPhaseMultiplicationFactor">The multiplication factor used in the calculation of the in-phase component
    /// of the Hilbert Transform. This parameter adjusts the sensitivity and responsiveness of
    /// the transform to changes in the input signal.</param>
    /// <param name="quadratureMultiplicationFactor">The multiplication factor used in the calculation of the quadrature component of
    /// the Hilbert Transform. This parameter also adjusts the sensitivity and responsiveness of the
    /// transform to changes in the input signal.</param>
    public HilbertTransform(int length = 7, decimal inPhaseMultiplicationFactor = 0.635m, decimal quadratureMultiplicationFactor = 0.338m)
        : this($"Hilbert({length}, {inPhaseMultiplicationFactor}, {quadratureMultiplicationFactor})", length, inPhaseMultiplicationFactor, quadratureMultiplicationFactor)
    {
    }

    /// <summary>
    /// Computes the next value of this indicator from the given state
    /// </summary>
    /// <param name="input">The input given to the indicator</param>
    /// <returns>A new value for this indicator</returns>
    protected override decimal ComputeNextValue(IndicatorDataPoint input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");

        _input.Update(input);
        _prev.Update(input);

        if (_prev.IsReady)
        {
            _detrendPrice.Update(input);
        }

        if (_detrendPrice.IsReady)
        {
            _detrendPriceDelay2.Update(_detrendPrice.Current);
            _detrendPriceDelay4.Update(_detrendPrice.Current);
        }

        InPhase.Update(input);
        if (InPhase.IsReady)
        {
            _inPhaseDelay3.Update(InPhase.Current);
        }

        Quadrature.Update(input);
        if (Quadrature.IsReady)
        {
            _quadratureDelay2.Update(Quadrature.Current);
        }

        return input.Value;
    }

    /// <summary>
    /// Resets this indicator to its initial state
    /// </summary>
    public override void Reset()
    {
        base.Reset();

        _input.Reset();
        _prev.Reset();
        _detrendPrice.Reset();
        _detrendPriceDelay2.Reset();
        _detrendPriceDelay4.Reset();
        _inPhaseDelay3.Reset();
        _quadratureDelay2.Reset();

        InPhase.Reset();
        Quadrature.Reset();
    }
}
