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

using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Algorithm;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture]
    public class AlgorithmPlottingTests
    {
        [Test]
        public void TestGetChartUpdatesWhileAdding()
        {
            var algorithm = new QCAlgorithm();

            var task1 = Task.Factory.StartNew(() =>
            {
                for (var i = 0; i < 1000; i++)
                {
                    algorithm.AddChart(new Chart($"Test_{i}"));
                    Thread.Sleep(1);
                }
            });

            var task2 = Task.Factory.StartNew(() =>
            {
                for (var i = 0; i < 1000; i++)
                {
                    algorithm.GetChartUpdates(true);
                    Thread.Sleep(1);
                }
            });

            Task.WaitAll(task1, task2);
        }
    }
}
