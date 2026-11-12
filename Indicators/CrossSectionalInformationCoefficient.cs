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
using QuantConnect.Data;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// The cross-sectional information coefficient (IC) is the standard IC of factor research: each period, it is the
    /// Spearman correlation across many assets between their signal values at the end of the previous period and
    /// their returns over this period.
    ///
    /// The indicator owns one signal per asset: each input updates its asset's signal first, so the signals must not be
    /// updated elsewhere, such as by an algorithm helper that registered them. The indicator throws when it detects such
    /// an update. An asset's price is its input's value. The indicator updates when every asset has an input
    /// for the same time.
    /// </summary>
    public class CrossSectionalInformationCoefficient : MultiSymbolIndicator<IBaseData>
    {
        private readonly Dictionary<Symbol, decimal> _previousSignalValues = new();
        private readonly Dictionary<Symbol, long> _signalSamples;
        private bool _isReady;

        /// <summary>
        /// The signal of each asset whose information coefficient this indicator measures
        /// </summary>
        public IReadOnlyDictionary<Symbol, IIndicator> Signals { get; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _isReady;

        /// <summary>
        /// Creates a new cross-sectional information coefficient indicator
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="symbols">The assets to correlate across. At least three are needed</param>
        /// <param name="signalFactory">Creates the signal of each asset. This indicator updates them</param>
        public CrossSectionalInformationCoefficient(string name, IEnumerable<Symbol> symbols, Func<Symbol, IIndicator> signalFactory)
            : base(name, symbols, 2)
        {
            if (DataBySymbol.Count < 3)
            {
                throw new ArgumentException($"CrossSectionalInformationCoefficient indicator needs at least 3 symbols but got {DataBySymbol.Count}");
            }
            Signals = DataBySymbol.Keys.ToDictionary(symbol => symbol, signalFactory);
            _signalSamples = Signals.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Samples);
            var signalWarmUpPeriod = Signals.Values.Max(signal => (signal as IIndicatorWarmUpPeriodProvider)?.WarmUpPeriod ?? 1);
            WarmUpPeriod += signalWarmUpPeriod - 1;
        }

        /// <summary>
        /// Creates a new cross-sectional information coefficient indicator
        /// </summary>
        /// <param name="symbols">The assets to correlate across. At least three are needed</param>
        /// <param name="signalFactory">Creates the signal of each asset. This indicator updates them</param>
        public CrossSectionalInformationCoefficient(IEnumerable<Symbol> symbols, Func<Symbol, IIndicator> signalFactory)
            : this("CSIC", symbols, signalFactory)
        {
        }

        /// <summary>
        /// Updates the signal of the input's asset before aligning the input with the other assets' inputs
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>The most recently computed value of the indicator</returns>
        protected override decimal ComputeNextValue(IBaseData input)
        {
            if (Signals.TryGetValue(input.Symbol, out var signal))
            {
                _signalSamples[input.Symbol] = TimeSeriesInformationCoefficient.UpdateOwnedSignal(signal, input, _signalSamples[input.Symbol], Name);
            }
            return base.ComputeNextValue(input);
        }

        /// <summary>
        /// Correlates the assets' previous signal values with their returns since then, once every asset has them
        /// </summary>
        protected override decimal ComputeIndicator()
        {
            var hasEveryPreviousSignalValue = _previousSignalValues.Count == DataBySymbol.Count;
            var signalValues = new List<double>(DataBySymbol.Count);
            var returns = new List<double>(DataBySymbol.Count);
            foreach (var (symbol, data) in DataBySymbol)
            {
                if (hasEveryPreviousSignalValue && data.DataPoints.IsReady && data.DataPoints[1].Value != 0)
                {
                    signalValues.Add((double)_previousSignalValues[symbol]);
                    returns.Add((double)(data.DataPoints[0].Value / data.DataPoints[1].Value - 1));
                }

                var signal = Signals[symbol];
                if (signal.IsReady)
                {
                    _previousSignalValues[symbol] = signal.Current.Value;
                }
                else
                {
                    _previousSignalValues.Remove(symbol);
                }
            }

            if (signalValues.Count < DataBySymbol.Count)
            {
                return IndicatorValue;
            }

            _isReady = true;
            var correlation = MathNet.Numerics.Statistics.Correlation.Spearman(signalValues, returns);
            return correlation.IsNaNOrZero() ? 0m : Extensions.SafeDecimalCast(correlation);
        }

        /// <summary>
        /// Resets this indicator and its signals to their initial state
        /// </summary>
        public override void Reset()
        {
            foreach (var (symbol, signal) in Signals)
            {
                signal.Reset();
                _signalSamples[symbol] = 0;
            }
            _previousSignalValues.Clear();
            _isReady = false;
            base.Reset();
        }
    }
}
