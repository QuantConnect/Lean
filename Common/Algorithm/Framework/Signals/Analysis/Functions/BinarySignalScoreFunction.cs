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

namespace QuantConnect.Algorithm.Framework.Signals.Analysis.Functions
{
    /// <summary>
    /// Defines a scoring function that always returns 1 or 0.
    /// You're either right or you're wrong with this one :)
    /// </summary>
    public class BinarySignalScoreFunction : ISignalScoreFunction
    {
        /// <inheritdoc />
        public double Evaluate(SignalAnalysisContext context, SignalScoreType scoreType)
        {
            var signal = context.Signal;

            var startingValue = context.InitialValues.Get(signal.Type);
            var currentValue = context.CurrentValues.Get(signal.Type);

            switch (signal.Direction)
            {
                case SignalDirection.Down:
                    return currentValue < startingValue ? 1 : 0;

                case SignalDirection.Flat:
                    // can't really do percent changes with zero
                    if (startingValue == 0) return currentValue == startingValue ? 1 : 0;

                    // TODO : Re-evaluate flat predictions, potentially adding ISignal.Tolerance to say 'how flat'
                    var deltaPercent = Math.Abs(currentValue - startingValue)/startingValue;
                    if (signal.PercentChange.HasValue)
                    {
                        return Math.Abs(deltaPercent) < (decimal) Math.Abs(signal.PercentChange.Value) ? 1 : 0;
                    }

                    // this is pretty much impossible, I suppose unless the ticks are large and/or volumes are small
                    return currentValue == startingValue ? 1 : 0;

                case SignalDirection.Up:
                    return currentValue > startingValue ? 1 : 0;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}