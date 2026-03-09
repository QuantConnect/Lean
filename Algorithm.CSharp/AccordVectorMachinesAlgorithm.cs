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

using Accord.MachineLearning.VectorMachines.Learning;
using QuantConnect.Indicators;
using System;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Machine Learning example using Accord VectorMachines Learning
    /// In this example, the algorithm forecasts the direction based on the last 5 days of rate of return
    /// </summary>
    public class AccordVectorMachinesAlgorithm : QCAlgorithm
    {
        // Define the size of the data used to train the model
        // It will use _lookback sets with _inputSize members
        // Those members are rate of return
        private const int _lookback = 30;
        private const int _inputSize = 5;
        private RollingWindow<double> _window = new RollingWindow<double>(_inputSize * _lookback + 2);

        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(100000);

            var symbol = AddEquity("SPY").Symbol;

            ROC(symbol, 1, Resolution.Daily).Updated += (s, e) => _window.Add((double)e.Value);

            Schedule.On(DateRules.Every(DayOfWeek.Monday),
                TimeRules.AfterMarketOpen(symbol, 10),
                TrainAndTrade);

            SetWarmUp(_window.Size, Resolution.Daily);
        }

        private void TrainAndTrade()
        {
            if (!_window.IsReady) return;

            // Convert the rolling window of rate of change into the Learn method
            var returns = new double[_inputSize];
            var targets = new double[_lookback];
            var inputs = new double[_lookback][];

            // Use the sign of the returns to predict the direction
            for (var i = 0; i < _lookback; i++)
            {
                for (var j = 0; j < _inputSize; j++)
                {
                    returns[j] = Math.Sign(_window[i + j + 1]);
                }

                targets[i] = Math.Sign(_window[i]);
                inputs[i] = returns;
            }

            // Train SupportVectorMachine using SetHoldings("SPY", percentage);
            var teacher = new LinearCoordinateDescent();
            teacher.Learn(inputs, targets);

            var svm = teacher.Model;

            // Compute the value for the last rate of change
            var last = (double) Math.Sign(_window[0]);
            var value = svm.Compute(new[] {last});
            if (value.IsNaNOrZero()) return;

            SetHoldings("SPY", Math.Sign(value));
        }
    }
}