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

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    /// <summary>
    /// Event arguments class for the <see cref="InteractiveBrokersClient.TickOptionComputation"/> event
    /// </summary>
    public sealed class TickOptionComputationEventArgs : TickEventArgs
    {
        /// <summary>
        /// The implied volatility calculated by the TWS option modeler, using the specified tick type value.
        /// </summary>
        public double ImpliedVolatility { get; private set; }

        /// <summary>
        /// The option delta value.
        /// </summary>
        public double Delta { get; private set; }

        /// <summary>
        /// The option price.
        /// </summary>
        public double OptionPrice { get; private set; }

        /// <summary>
        /// The present value of dividends expected on the option's underlying.
        /// </summary>
        public double PvDividend { get; private set; }

        /// <summary>
        /// The option gamma value.
        /// </summary>
        public double Gamma { get; private set; }

        /// <summary>
        /// The option vega value.
        /// </summary>
        public double Vega { get; private set; }

        /// <summary>
        /// The option theta value.
        /// </summary>
        public double Theta { get; private set; }

        /// <summary>
        /// The price of the underlying.
        /// </summary>
        public double UnderlyingPrice { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TickOptionComputationEventArgs"/> class
        /// </summary>
        public TickOptionComputationEventArgs(int tickerId, int field, double impliedVolatility, double delta, double optionPrice, double pvDividend, double gamma, double vega, double theta, double underlyingPrice)
            : base(tickerId, field)
        {
            ImpliedVolatility = impliedVolatility;
            Delta = delta;
            OptionPrice = optionPrice;
            PvDividend = pvDividend;
            Gamma = gamma;
            Vega = vega;
            Theta = theta;
            UnderlyingPrice = underlyingPrice;
        }
    }
}