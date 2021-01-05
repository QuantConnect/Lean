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
using System.Globalization;
using System.IO;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class PythonIndicatorNoinheritanceTests : PythonIndicatorTests
    {
        /// <summary>
        /// In this Custom Indicator, Update returns a boolean
        /// </summary>
        protected override IndicatorBase<IBaseData> CreateIndicator()
        {
            using (Py.GIL())
            {
                var module = PythonEngine.ModuleFromString(
                    Guid.NewGuid().ToString(),
                    @"
from collections import deque
from datetime import datetime, timedelta
from numpy import sum

class CustomSimpleMovingAverage():
    def __init__(self, name, period):
        self.Name = name
        self.Value = 0
        self.IsReady = False
        self.queue = deque(maxlen=period)

    # Update method is mandatory
    def Update(self, input):
        self.queue.appendleft(input.Value)
        count = len(self.queue)
        self.Value = sum(self.queue) / count
        self.IsReady = count == self.queue.maxlen
        return self.IsReady
"
                );
                var indicator = module.GetAttr("CustomSimpleMovingAverage")
                    .Invoke("custom".ToPython(), 14.ToPython());

                return new PythonIndicator(indicator);
            }
        }

        protected override void RunTestIndicator(IndicatorBase<IBaseData> indicator)
        {
            var first = true;
            var closeIndex = -1;
            var targetIndex = -1;
            foreach (var line in File.ReadLines(Path.Combine("TestData", TestFileName)))
            {
                var parts = line.Split(new[] { ',' }, StringSplitOptions.None);

                if (first)
                {
                    first = false;
                    for (var i = 0; i < parts.Length; i++)
                    {
                        if (parts[i].Trim() == "Close")
                        {
                            closeIndex = i;
                        }
                        if (parts[i].Trim() == TestColumnName)
                        {
                            targetIndex = i;
                        }
                    }
                    if (closeIndex * targetIndex < 0)
                    {
                        Assert.Fail($"Didn't find one of 'Close' or '{line}' in the header: ", TestColumnName);
                    }

                    continue;
                }

                var close = decimal.Parse(parts[closeIndex], CultureInfo.InvariantCulture);
                var date = Time.ParseDate(parts[0]);

                var data = new IndicatorDataPoint(date, close);
                indicator.Update(data);

                if (!indicator.IsReady || parts[targetIndex].Trim() == string.Empty)
                {
                    continue;
                }

                var expected = double.Parse(parts[targetIndex], CultureInfo.InvariantCulture);
                Assertion.Invoke(indicator, expected);
            }
        }
    }
}