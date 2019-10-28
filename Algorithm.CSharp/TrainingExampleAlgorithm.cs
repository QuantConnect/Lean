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

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Example algorithm showing how to use QCAlgorithm.Train method
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="training" />
    /// </summary>
    public class TrainingExampleAlgorithm : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 14);

            AddEquity("SPY", Resolution.Daily);

            // Set TrainingMethod to be executed immediately
            Train(TrainingMethod);

            // Set TrainingMethod to be executed at 8:00 am every Sunday
            Train(DateRules.Every(DayOfWeek.Sunday), TimeRules.At(8, 0), TrainingMethod);
        }

        private void TrainingMethod()
        {
            Log($"Start training at {Time}");
            // Use the historical data to train the machine learning model
            var history = History("SPY", 200, Resolution.Daily);

            // ML code:

        }
    }
}