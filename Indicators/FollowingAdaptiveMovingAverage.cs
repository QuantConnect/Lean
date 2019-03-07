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

namespace QuantConnect.Indicators
{
    /// <summary>
    /// The Following Adaptive Moving Average (FAMA) by John Ehlers
    /// </summary>
    public class FollowingAdaptiveMovingAverage : WindowIndicator<IndicatorDataPoint>
    {
        private readonly decimal _fastLimit;
        private readonly decimal _slowLimit;
        private readonly RollingWindow<decimal> _smooth;
        private readonly RollingWindow<decimal> _detrender;
        private readonly RollingWindow<decimal> _q1;
        private readonly RollingWindow<decimal> _i1;
        private readonly RollingWindow<decimal> _q2;
        private readonly RollingWindow<decimal> _i2;
        private readonly RollingWindow<decimal> _re;
        private readonly RollingWindow<decimal> _im;
        private readonly RollingWindow<decimal> _period;
        private readonly RollingWindow<decimal> _smoothPeriod;
        private readonly RollingWindow<decimal> _phase;
        private decimal _prevMama;
        private decimal _prevFama;

        /// <summary>
        /// Initializes a new instance of the FollowingAdaptiveMovingAverage class with the specified name and limits
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="fastLimit">The maximum allowed value to alpha of the indicator</param>
        /// <param name="slowLimit">The minimum allowed value to alpha of the indicator</param>
        public FollowingAdaptiveMovingAverage(string name, decimal fastLimit = .5m, decimal slowLimit = .05m)
            : base(name, 6)
        {
            _fastLimit = fastLimit;
            _slowLimit = slowLimit;
            
            _smooth = new RollingWindow<decimal>(7);
            _detrender = new RollingWindow<decimal>(7);
            _q1 = new RollingWindow<decimal>(7);
            _i1 = new RollingWindow<decimal>(7);
            _q2 = new RollingWindow<decimal>(2);
            _i2 = new RollingWindow<decimal>(2);
            _re = new RollingWindow<decimal>(2);
            _im = new RollingWindow<decimal>(2);
            _period = new RollingWindow<decimal>(2);
            _smoothPeriod = new RollingWindow<decimal>(2);
            _phase = new RollingWindow<decimal>(2);
        }

        /// <summary>
        /// Initializes a new instance of the FollowingAdaptiveMovingAverage class with the default name and limits
        /// </summary>
        /// <param name="fastLimit">The maximum allowed value to alpha of the indicator</param>
        /// <param name="slowLimit">The minimum allowed value to alpha of the indicator</param>
        public FollowingAdaptiveMovingAverage(decimal fastLimit = .5m, decimal slowLimit = .05m)
            : this($"FAMA({fastLimit},{slowLimit})", fastLimit, slowLimit)
        {
        }
        
        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => Samples > 4;

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <param name="window">The window for the input history</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IReadOnlyWindow<IndicatorDataPoint> window, IndicatorDataPoint input)
        {
            if (Samples <= 4)
                return input.Price;

            var smooth = (4m * input.Price + 3m * window[1].Price + 2m * window[2].Price + window[3].Price) / 10m; 
            _smooth.Add(smooth);
            
            var detrender = (.0962m * _smooth[0] 
                             + .5769m * ReplaceEmpty(_smooth, 2) 
                             - .5769m * ReplaceEmpty(_smooth,4) 
                             - .0962m * ReplaceEmpty(_smooth,6)) 
                            * (.075m * ReplaceEmpty(_period, 1) + .54m);
            _detrender.Add(detrender);

            // Compute InPhase and Quadrature components
            var q1 = (.0962m * _detrender[0]
                      + .5769m * ReplaceEmpty(_detrender,2) 
                      - .5769m * ReplaceEmpty(_detrender, 4) 
                      - .0962m * ReplaceEmpty(_detrender, 6))
                     * (.075m * ReplaceEmpty(_period, 1) + .54m);
            _q1.Add(q1);
            _i1.Add(ReplaceEmpty(_detrender,3));
            
            // Advance the phase of InPhase1 and Quadrature1 by 90 degrees
            var jI = (.0962m * _i1[0] 
                      + .5769m * ReplaceEmpty(_i1, 2) 
                      - .5769m * ReplaceEmpty(_i1, 4) 
                      - .0962m * ReplaceEmpty(_i1, 6)) 
                     * (.075m * ReplaceEmpty(_period, 1) + .54m);
            
            var jQ = (.0962m * _q1[0] 
                      + .5769m * ReplaceEmpty(_q1,2) 
                      - .5769m * ReplaceEmpty(_q1, 4) 
                      - .0962m * ReplaceEmpty(_q1, 6)) 
                     * (.075m * ReplaceEmpty(_period, 1) + .54m);
             
            // Phasor addition for 3 bar averaging
            _i2.Add(_i1[0] - jQ);
            _q2.Add(_q1[0] + jI);
            
            // Smooth the I and Q components before applying the discriminator
            _i2.Add(.2m * _i2[0] + .8m * ReplaceEmpty(_i2, 1));
            _q2.Add(.2m * _q2[0] + .8m * ReplaceEmpty(_q2, 1));
            
            // Homodyne Discriminator
            _re.Add(_i2[0] * ReplaceEmpty(_i2,1) + _q2[0] * ReplaceEmpty(_q2,1));
            _im.Add(_i2[0] * ReplaceEmpty(_q2,1) - _q2[0] * ReplaceEmpty(_i2,1));
            
            _re.Add(.2m * _re[0] + .8m * ReplaceEmpty(_re,1));
            _im.Add(.2m * _im[0] + .8m * ReplaceEmpty(_im,1));

            if (_im[0] != 0 && _re[0] != 0)
            {
                double period = 360 / Math.Atan((double)(_im[0] / _re[0]));
                _period.Add((decimal)period);
            }
               
            if (ReplaceEmpty(_period, 0) > 1.5m * ReplaceEmpty(_period, 1)) 
                _period.Add(1.5m * ReplaceEmpty(_period, 1));
            if (ReplaceEmpty(_period, 0) < .67m * ReplaceEmpty(_period, 1)) 
                _period.Add(.67m * ReplaceEmpty(_period, 1));
            if (ReplaceEmpty(_period, 0) < 6) 
                _period.Add(6);
            if (ReplaceEmpty(_period, 0) > 50) 
                _period.Add(50);
            
            _period.Add(.2m * ReplaceEmpty(_period, 0) + .8m * ReplaceEmpty(_period, 1));
            
            _smoothPeriod.Add(.33m * _period[0] + .67m * ReplaceEmpty(_smoothPeriod, 1));
            
            if (_i1[0] != 0m) 
                _phase.Add((decimal)Math.Atan((double)(_q1[0] / _i1[0])));
            
            var deltaPhase = ReplaceEmpty(_phase, 1) - ReplaceEmpty(_phase,0);
            if (deltaPhase < 1) deltaPhase = 1;
            
            var alpha = _fastLimit / deltaPhase;
            if (alpha < _slowLimit) alpha = _slowLimit;

            _prevMama  = alpha * input.Value + (1 - alpha) * _prevMama;
            _prevFama  = .5m * alpha * _prevMama + (1 - .5m * alpha) * _prevFama;
            
            return _prevFama;
        }

        /// <summary>
        /// Replace the value that doesn't exists with zero
        /// </summary>
        /// <param name="window">The window for extract value with index of n</param>
        /// <param name="n">The index of value that must be extracted</param>
        /// <returns>The ith most recent entry or zero</returns>
        private decimal ReplaceEmpty(RollingWindow<decimal> window, int n)
        {
            try
            {
                return window[n];
            }
            catch (ArgumentOutOfRangeException)
            {
                return 0m;
            }
        }
        
        /// <summary>
        /// Resets the average to its initial state
        /// </summary>
        public override void Reset()
        {
            _prevMama = 0m;
            _prevFama = 0m;
            _smooth.Reset();
            _detrender.Reset();
            _q1.Reset();
            _i1.Reset();
            _q2.Reset();
            _i2.Reset();
            _re.Reset();
            _im.Reset();
            _period.Reset();
            _smoothPeriod.Reset();
            _phase.Reset();
            base.Reset();
        }
    }
}