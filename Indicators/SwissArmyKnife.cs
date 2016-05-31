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

using QuantConnect.Data.Market;
using System;

namespace QuantConnect.Indicators
{

    /// <summary>
    /// The tools of the Swiss Army Knife. Some of the tools lend well to chaining with the "Of" Method, others may be treated like moving averages
    /// </summary>
    public enum SwissArmyKnifeTool
    {
        EMA,
        Gauss,
        Butter,
        Smooth,
        HighPass,
        TwoPoleHighPass,
        BandPass,
        BandStop
    }

    /// <summary>
    /// Swiss Army Knife indicator by John Ehlers
    /// </summary>
    public class SwissArmyKnife : Indicator
    {

        RollingWindow<double> price;
        SwissArmyKnifeTool _tool;
        int _n = 4;
        double _period = 20;
        double _delta = .1;
        double c0 = 1;
        double c1;
        double b0 = 1;
        double b1;
        double b2;
        double a1;
        double a2;
        double alpha;
        double beta;
        double gamma;
        RollingWindow<double> filt;

        /// <summary>
        /// Swiss Army Knife indicator by John Ehlers
        /// </summary>
        /// <param name="period"></param>
        /// <param name="n">window</param>
        /// <param name="delta"></param>
        /// <param name="tool"></param>
        public SwissArmyKnife(int period, int n, double delta, SwissArmyKnifeTool tool)
            : this("Swiss" + period, period, n, delta, tool)
        {
        }

        /// <summary>
        /// Swiss Army Knife indicator by John Ehlers
        /// </summary>
        /// <param name="name"></param>
        /// <param name="period"></param>
        /// <param name="n">window</param>
        /// <param name="delta"></param>
        /// <param name="tool"></param>
        public SwissArmyKnife(string name, int period, int n, double delta, SwissArmyKnifeTool tool)
            : base(name)
        {
            _n = n;
            _period = period;
            _tool = tool;
            _delta = delta;
            filt = new RollingWindow<double>(n);
            price = new RollingWindow<double>(n);
        }


        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady
        {
            get { return Samples >= _period; }
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {

            price.Add((double)input.Price);

            if (price.Samples < 4 && _tool != SwissArmyKnifeTool.Smooth)
            {
                filt.Add((double)input.Price);
                return input.Price;
            }
            else if (_tool == SwissArmyKnifeTool.Smooth)
            {
                filt.Add((double)input.Price);
                if (price.Samples < 4)
                {
                    return input.Price;
                }
            }

            if (_tool == SwissArmyKnifeTool.EMA)
            {
                alpha = (Math.Cos(Math.PI / (int)_period) + Math.Sin(Math.PI / (int)_period) - 1) / Math.Cos(Math.PI / (int)_period);
                b0 = alpha;
                a1 = 1 - alpha;
            }

            if (_tool == SwissArmyKnifeTool.Gauss)
            {
                beta = 2.415 * (1 - Math.Cos(Math.PI / _period));
                alpha = -beta + Math.Sqrt(beta * beta + 2d * beta);
                c0 = alpha * alpha;
                a1 = 2d * (1 - alpha);
                a2 = -(1 - alpha) * (1 - alpha);
            }

            if (_tool == SwissArmyKnifeTool.Butter)
            {
                beta = 2.415 * (1 - Math.Cos(Math.PI / _period));
                alpha = -beta + Math.Sqrt(beta * beta + 2d * beta);
                c0 = alpha * alpha / 4d;
                b1 = 2;
                b2 = 1;
                a1 = 2d * (1 - alpha);
                a2 = -(1 - alpha) * (1 - alpha);
            }

            if (_tool == SwissArmyKnifeTool.Smooth)
            {
                c0 = 1d / 4d;
                b1 = 2;
                b2 = 1;
            }

            if (_tool == SwissArmyKnifeTool.HighPass)
            {
                alpha = (Math.Cos(Math.PI / _period) + Math.Sin(Math.PI / _period) - 1) / Math.Cos(Math.PI / _period);
                c0 = 1 - alpha / 2d;
                b1 = -1;
                a1 = 1 - alpha;
            }

            if (_tool == SwissArmyKnifeTool.TwoPoleHighPass)
            {
                beta = 2.415 * (1 - Math.Cos(Math.PI / _period));
                alpha = -beta + Math.Sqrt(beta * beta + 2d * beta);
                c0 = (1 - alpha / 2d) * (1 - alpha / 2d);
                b1 = -2;
                b2 = 1;
                a1 = 2d * (1 - alpha);
                a2 = -(1 - alpha) * (1 - alpha);
            }

            if (_tool == SwissArmyKnifeTool.BandPass)
            {
                beta = Math.Cos(Math.PI / _period);
                gamma = (1 / Math.Cos(2 * Math.PI * _delta / _period));
                alpha = gamma - Math.Sqrt(Math.Pow(gamma, 2) - 1);
                c0 = (1 - alpha) / 2d;
                b2 = -1d;
                a1 = beta * (1d + alpha);
                a2 = -alpha;
            }

            if (_tool == SwissArmyKnifeTool.BandStop)
            {
                beta = Math.Cos(Math.PI / _period);
                gamma = 1d / Math.Cos(2 * Math.PI * _delta / _period);
                alpha = gamma - Math.Sqrt(gamma * gamma - 1);
                c0 = (1 + alpha) / 2d;
                b1 = -2d * beta;
                b2 = 1;
                a1 = beta * (1 + alpha);
                a2 = -alpha;
            }

            double signal = c0 * (b0 * price[0] + b1 * price[1] + b2 * price[2]) + a1 * filt[0] + a2 * filt[1] - c1 * price[_n - 1];

            filt.Add(signal);

            return (decimal)signal;

        }

        /// <summary>
        /// Resets to the initial state
        /// </summary>
        public override void Reset()
        {
            _n = 4;
            _period = 20;
            _delta = 0.1;
            filt = new RollingWindow<double>(4);
            price = new RollingWindow<double>(4);
            base.Reset();
        }


    }
}