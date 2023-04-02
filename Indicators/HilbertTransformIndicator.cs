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
using System.Diagnostics;

namespace QuantConnect.Indicators;

/// <summary>
/// This indicator computes the Hilbert Transform Indicator by John Ehlers.
/// By using present and prior price differences, and some feedback, price values are split into their complex number components
/// of real (inPhase) and imaginary (quadrature) parts.
/// http://www.technicalanalysis.org.uk/moving-averages/Ehle.pdf
/// </summary>
public class HilbertTransformIndicator : Indicator, IIndicatorWarmUpPeriodProvider
{
    private const int Quadrature2Length = 2;
    private const int InPhase3Length = 3;

    private readonly IndicatorBase<IndicatorDataPoint> _input;
    private readonly IndicatorBase<IndicatorDataPoint> _prev;
    private readonly IndicatorBase<IndicatorDataPoint> _v1;
    private readonly IndicatorBase<IndicatorDataPoint> _v2;
    private readonly IndicatorBase<IndicatorDataPoint> _v4;
    private readonly Queue<IndicatorDataPoint> _inPhase3 = new();
    private readonly Queue<IndicatorDataPoint> _quadrature2 = new();

    private int updatesCount = 0;
    private readonly int _inPhase3WarmUpPeriod;
    private readonly int _quad2WarmUpPeriod;
    private readonly int _length;

    /// <summary>
    /// Real (inPhase) part of complex number component of price values
    /// </summary>
    public IndicatorBase<IndicatorDataPoint> InPhase { get; }

    /// <summary>
    /// Imaginary (quadrature) part of complex number component of price values
    /// </summary>
    public IndicatorBase<IndicatorDataPoint> Quadrature { get; }

    /// <summary>
    /// Creates a new Hilbert Transform indicator
    /// </summary>
    /// <param name="name">The name of this indicator</param>
    /// <param name="length">The length of the FIR filter used in the calculation of the Hilbert Transform.
    /// This parameter determines the number of filter coefficients in the FIR filter.</param>
    /// <param name="inPhaseMultiplicationFactor">The multiplication factor used in the calculation of the in-phase component
    /// of the Hilbert Transform. This parameter adjusts the sensitivity and responsiveness of
    /// the transform to changes in the input signal.</param>
    /// <param name="quadratureMultiplicationFactor">The multiplication factor used in the calculation of the quadrature component of
    /// the Hilbert Transform. This parameter also adjusts the sensitivity and responsiveness of the
    /// transform to changes in the input signal.</param>
    public HilbertTransformIndicator(string name, int length, decimal inPhaseMultiplicationFactor, decimal quadratureMultiplicationFactor)
        : base(name)
    {
        _length = length;
        _quad2WarmUpPeriod = _length + 2;
        _inPhase3WarmUpPeriod = _quad2WarmUpPeriod + InPhase3Length;

        _input = new Identity(name + "_input");
        _prev = new Delay(name + "_prev", length);
        _v1 = _input.Minus(_prev);
        _v2 = new Delay(name + "_v2", 2);
        _v4 = new Delay(name + "_v4", 4);
        InPhase = new FunctionalIndicator<IndicatorDataPoint>(name + "_inPhase",
            _ =>
            {
                if (!InPhase!.IsReady)
                {
                    return decimal.Zero;
                }

                var v2Value = _v2.IsReady ? _v2.Current.Value : decimal.Zero;
                var v4Value = _v4.IsReady ? _v4.Current.Value : decimal.Zero;
                var inPhase3Value = _inPhase3.Count == InPhase3Length ? _inPhase3.Peek().Value : decimal.Zero;
                return (v4Value - v2Value * inPhaseMultiplicationFactor) * 1.25M + inPhase3Value * inPhaseMultiplicationFactor;
            },
            _ => Samples > _length + 2,
            () =>
            {
                _v1.Reset();
                _v2.Reset();
                _v4.Reset();
                _inPhase3.Clear();
            });
        Quadrature = new FunctionalIndicator<IndicatorDataPoint>(name + "_quad",
            _ =>
            {
                if (!Quadrature!.IsReady)
                {
                    return decimal.Zero;
                }

                var v2Value = _v2.IsReady ? _v2.Current.Value : decimal.Zero;
                var v1Value = _v1.IsReady ? _v1.Current.Value : decimal.Zero;
                var quadrature2Value = _quadrature2.Count == Quadrature2Length ? _quadrature2.Peek().Value : decimal.Zero;
                return v2Value - v1Value * quadratureMultiplicationFactor + quadrature2Value * quadratureMultiplicationFactor;
            },
            _ => Samples > _length,
            () =>
            {
                _v1.Reset();
                _v2.Reset();
                _quadrature2.Clear();
            });
    }

    /// <summary>
    /// Creates a new Hilbert Transform indicator with default name and default params
    /// </summary>
    /// <param name="length">The length of the FIR filter used in the calculation of the Hilbert Transform.
    /// This parameter determines the number of filter coefficients in the FIR filter.</param>
    /// <param name="iMult">The multiplication factor used in the calculation of the in-phase component
    /// of the Hilbert Transform. This parameter adjusts the sensitivity and responsiveness of
    /// the transform to changes in the input signal.</param>
    /// <param name="qMult">The multiplication factor used in the calculation of the quadrature component of
    /// the Hilbert Transform. This parameter also adjusts the sensitivity and responsiveness of the
    /// transform to changes in the input signal.</param>
    public HilbertTransformIndicator(int length = 7, decimal iMult = 0.635M, decimal qMult = 0.338M)
        : this($"Hilbert({length}, {iMult}, {qMult})", length, iMult, qMult)
    {
    }

    /// <summary>
    /// Gets a flag indicating when this indicator is ready and fully initialized
    /// </summary>
    public override bool IsReady => updatesCount >= WarmUpPeriod;

    /// <summary>
    /// Computes the next value of this indicator from the given state
    /// </summary>
    /// <param name="input">The input given to the indicator</param>
    /// <returns>A new value for this indicator</returns>
    protected override decimal ComputeNextValue(IndicatorDataPoint input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");

        updatesCount += 1;

        _input.Update(input);
        _prev.Update(input);

        if (_prev.IsReady)
        {
            _v1.Update(input);
        }

        if (_v1.IsReady)
        {
            _v2.Update(_v1.Current);
            _v4.Update(_v1.Current);
        }

        InPhase.Update(input);
        if (InPhase.IsReady)
        {
            _inPhase3.Enqueue(InPhase.Current);
        }

        while (_inPhase3.Count > InPhase3Length)
        {
            _inPhase3.Dequeue();
        }

        Quadrature.Update(input);
        if (Quadrature.IsReady)
        {
            _quadrature2.Enqueue(Quadrature.Current);
        }

        while (_quadrature2.Count > Quadrature2Length)
        {
            _quadrature2.Dequeue();
        }

        return input.Value;
    }

    /// <summary>
    /// Required period, in data points, for the indicator to be ready and fully initialized.
    /// </summary>
    public int WarmUpPeriod => Math.Max(_inPhase3WarmUpPeriod, _quad2WarmUpPeriod);

    /// <summary>
    /// Resets this indicator to its initial state
    /// </summary>
    public override void Reset()
    {
        base.Reset();
        InPhase.Reset();
        Quadrature.Reset();
        updatesCount = 0;
    }
}
