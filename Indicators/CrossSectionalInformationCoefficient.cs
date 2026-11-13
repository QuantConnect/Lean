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
    /// an update. The signals can't be built from other indicators, as <see cref="TimeSeriesInformationCoefficient"/>
    /// explains. An asset's price is its input's value.
    ///
    /// Inputs whose end times fall in the same UTC period, at the inputs' resolution, form a period, so assets whose bars
    /// end at different local times, such as futures of different exchanges, share their daily periods. The indicator
    /// correlates a period as soon as every asset it tracks has an input for it. Otherwise, such as when an asset was
    /// delisted, it correlates the assets that have one when the next period starts, and that value has the next
    /// period's first input time. A period needs at least three assets with a signal value and a return. An asset with
    /// no input for a period pairs its last signal value with its return since then. Use <see cref="Add"/> and
    /// <see cref="Remove"/> to follow a changing cross-section, such as a universe's constituents.
    /// </summary>
    public class CrossSectionalInformationCoefficient : MultiSymbolIndicator<IBaseData>
    {
        private readonly Func<Symbol, IIndicator> _signalFactory;
        private readonly Dictionary<Symbol, AssetData> _assets = new();
        private readonly Dictionary<Symbol, IIndicator> _signals = new();
        private readonly HashSet<Symbol> _assetsAddedAfterFirstInput = new();
        private readonly HashSet<Symbol> _periodAssets = new();
        private DateTime _periodEndTimeUtc;
        private bool _isPeriodCorrelated;
        private bool _isResolutionSet;
        private bool _isReady;
        private bool _hasNewValue;
        private int _signalWarmUpPeriod = 1;

        /// <summary>
        /// The signal of each asset whose information coefficient this indicator measures
        /// </summary>
        public IReadOnlyDictionary<Symbol, IIndicator> Signals => _signals;

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _isReady;

        /// <summary>
        /// Creates a new cross-sectional information coefficient indicator
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="symbols">The assets to correlate across. A period needs at least three</param>
        /// <param name="signalFactory">Creates the signal of each asset. This indicator updates them</param>
        public CrossSectionalInformationCoefficient(string name, IEnumerable<Symbol> symbols, Func<Symbol, IIndicator> signalFactory)
            : base(name, symbols, 2)
        {
            _signalFactory = signalFactory;
            foreach (var symbol in DataBySymbol.Keys.ToList())
            {
                Add(symbol);
            }
        }

        /// <summary>
        /// Creates a new cross-sectional information coefficient indicator
        /// </summary>
        /// <param name="symbols">The assets to correlate across. A period needs at least three</param>
        /// <param name="signalFactory">Creates the signal of each asset. This indicator updates them</param>
        public CrossSectionalInformationCoefficient(IEnumerable<Symbol> symbols, Func<Symbol, IIndicator> signalFactory)
            : this("CSIC", symbols, signalFactory)
        {
        }

        /// <summary>
        /// Adds an asset to the cross-section, with a new signal from the signal factory. The indicator only gets the
        /// asset's data it is given: for an indicator an algorithm helper created, also register it for the asset.
        /// </summary>
        /// <param name="symbol">The asset to add</param>
        public void Add(Symbol symbol)
        {
            if (_assets.ContainsKey(symbol))
            {
                return;
            }
            var signal = _signalFactory(symbol);
            _signalWarmUpPeriod = Math.Max(_signalWarmUpPeriod, TimeSeriesInformationCoefficient.ValidateOwnedSignal(signal, Name));
            WarmUpPeriod = _signalWarmUpPeriod + 1;
            _assets[symbol] = new AssetData(signal);
            _signals[symbol] = signal;
            if (!DataBySymbol.ContainsKey(symbol))
            {
                DataBySymbol[symbol] = new SymbolData(symbol, 2);
            }
            if (_isResolutionSet)
            {
                _assetsAddedAfterFirstInput.Add(symbol);
            }
        }

        /// <summary>
        /// Removes an asset from the cross-section, such as one that left the universe. Its later inputs are ignored.
        /// When every remaining asset already has an input for the current period, that period is correlated when the
        /// next one starts.
        /// </summary>
        /// <param name="symbol">The asset to remove</param>
        public void Remove(Symbol symbol)
        {
            _assets.Remove(symbol);
            _signals.Remove(symbol);
            DataBySymbol.Remove(symbol);
            _assetsAddedAfterFirstInput.Remove(symbol);
            _periodAssets.Remove(symbol);
        }

        /// <summary>
        /// Publishes a value only when a period is correlated, so indicators chained to this one get one value per period
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>The correlation of the period the input completes or ends, if any</returns>
        protected override IndicatorResult ValidateAndComputeNextValue(IBaseData input)
        {
            _hasNewValue = false;
            var value = ComputeNextValue(input);
            return new IndicatorResult(value, _hasNewValue ? IndicatorStatus.Success : IndicatorStatus.ValueNotReady);
        }

        /// <summary>
        /// Updates the input's asset, and correlates the period the input completes or ends
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>The correlation of the period the input completes or ends, or else the last one</returns>
        protected override decimal ComputeNextValue(IBaseData input)
        {
            if (!_assets.TryGetValue(input.Symbol, out var asset))
            {
                return IndicatorValue;
            }

            // The inherited symbol data aligns the input's end time: in UTC, truncated to the inputs' resolution
            var symbolData = DataBySymbol[input.Symbol];
            if (!_isResolutionSet || _assetsAddedAfterFirstInput.Remove(input.Symbol))
            {
                SetResolution(input);
                _isResolutionSet = true;
            }
            else
            {
                symbolData.CurrentInput = input;
            }

            var inputEndTimeUtc = symbolData.CurrentInputEndTimeUtc;
            if (inputEndTimeUtc > _periodEndTimeUtc)
            {
                if (!_isPeriodCorrelated)
                {
                    IndicatorValue = ComputeIndicator();
                }
                _periodEndTimeUtc = inputEndTimeUtc;
                _periodAssets.Clear();
                _isPeriodCorrelated = false;
            }

            asset.Update(input, Name);
            if (inputEndTimeUtc == _periodEndTimeUtc)
            {
                _periodAssets.Add(input.Symbol);
                // With at least three assets, the input that starts a period can't also complete it
                if (_periodAssets.Count == _assets.Count)
                {
                    _isPeriodCorrelated = true;
                    IndicatorValue = ComputeIndicator();
                }
            }
            return IndicatorValue;
        }

        /// <summary>
        /// Correlates the signal values and returns of the period's assets that have both, when at least three do
        /// </summary>
        /// <returns>The period's correlation, or else the last value</returns>
        protected override decimal ComputeIndicator()
        {
            var signalValues = new List<double>(_periodAssets.Count);
            var returns = new List<double>(_periodAssets.Count);
            foreach (var symbol in _periodAssets)
            {
                var asset = _assets[symbol];
                if (asset.Return.HasValue)
                {
                    signalValues.Add((double)asset.PreviousSignalValue.Value);
                    returns.Add((double)asset.Return.Value);
                }
            }

            if (signalValues.Count < 3)
            {
                return IndicatorValue;
            }

            _isReady = true;
            _hasNewValue = true;
            var correlation = MathNet.Numerics.Statistics.Correlation.Spearman(signalValues, returns);
            return correlation.IsNaNOrZero() ? 0m : Extensions.SafeDecimalCast(correlation);
        }

        /// <summary>
        /// Resets this indicator and its signals to their initial state. It keeps tracking the same assets
        /// </summary>
        public override void Reset()
        {
            foreach (var asset in _assets.Values)
            {
                asset.Reset();
            }
            _assetsAddedAfterFirstInput.Clear();
            _periodAssets.Clear();
            _periodEndTimeUtc = default;
            _isPeriodCorrelated = false;
            _isResolutionSet = false;
            _isReady = false;
            base.Reset();
        }

        private class AssetData
        {
            private readonly IIndicator _signal;
            private long _signalSamples;
            private DateTime _endTime;
            private decimal _price;
            private decimal? _signalValue;
            private decimal _previousPrice;

            /// <summary>
            /// The signal value at the asset's input before its last one
            /// </summary>
            public decimal? PreviousSignalValue { get; private set; }

            /// <summary>
            /// The asset's return since the input before its last one, when it has a previous signal value and price
            /// </summary>
            public decimal? Return => PreviousSignalValue.HasValue && _previousPrice != 0 ? _price / _previousPrice - 1 : null;

            public AssetData(IIndicator signal)
            {
                _signal = signal;
                _signalSamples = signal.Samples;
            }

            public void Update(IBaseData input, string ownerName)
            {
                // A second input for the same time replaces the first one, so its return is still since the previous time
                if (input.EndTime != _endTime)
                {
                    PreviousSignalValue = _signalValue;
                    _previousPrice = _price;
                    _endTime = input.EndTime;
                }
                _signalSamples = TimeSeriesInformationCoefficient.UpdateOwnedSignal(_signal, input, _signalSamples, ownerName);
                _price = input.Value;
                _signalValue = _signal.IsReady ? _signal.Current.Value : null;
            }

            public void Reset()
            {
                _signal.Reset();
                _signalSamples = 0;
                _endTime = default;
                _price = 0;
                _signalValue = null;
                _previousPrice = 0;
                PreviousSignalValue = null;
            }
        }
    }
}
