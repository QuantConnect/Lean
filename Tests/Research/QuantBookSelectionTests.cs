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
using System.Linq;
using Python.Runtime;
using NUnit.Framework;
using QuantConnect.Research;
using System.Collections.Generic;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Tests.Research
{
    [TestFixture]
    public class QuantBookSelectionTests
    {
        private QuantBook _qb;
        private DateTime _end;
        private DateTime _start;

        [SetUp]
        public void Setup()
        {
            _qb = new QuantBook();
            _end = new DateTime(2014, 4, 7);
            _start = new DateTime(2014, 3, 24);
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void UniverseSelectionData(Language language)
        {
            if (language == Language.CSharp)
            {
                var history = _qb.Select<Fundamentals, Fundamental>(_start, _end).ToList();

                // we asked for 2 weeks, 5 work days for each week expected
                Assert.AreEqual(10, history.Count);
                Assert.IsTrue(history.All(x => x.Count() > 7000));
                Assert.IsTrue(history.All(x => x.All(fundamental => fundamental.GetType() == typeof(Fundamental))));
            }
            else
            {
                using (Py.GIL())
                {
                    var testModule = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def getSelect(qb, start, end):
    return qb.Select(Fundamentals, start, end)
                    ");

                    dynamic getSelect = testModule.GetAttr("getSelect");
                    var pyHistory = getSelect(_qb, _start, _end);

                    Assert.AreEqual(10, pyHistory.__len__().AsManagedObject(typeof(int)));

                    for (var i = 0; i < 10; i++)
                    {
                        var index = pyHistory.index[i];
                        var type = typeof(List<Fundamental>);
                        var fundamental = (List<Fundamental>)pyHistory.loc[index].data.AsManagedObject(type);

                        Assert.GreaterOrEqual(fundamental.Count, 7000);
                    }
                }
            }
        }

        [TestCase(Language.CSharp, false)]
        [TestCase(Language.Python, false)]
        [TestCase(Language.CSharp, true)]
        [TestCase(Language.Python, true)]
        public void UniverseSelectionSelection(Language language, bool useUniverseUnchanged)
        {
            var selectionState = false;
            if (language == Language.CSharp)
            {
                var history = _qb.Select<Fundamentals, Fundamental>(_start, _end, (fundamental) =>
                {
                    if(!useUniverseUnchanged || !selectionState)
                    {
                        selectionState = true;
                        return new[] { Symbols.AAPL };
                    }
                    // after the first call we will return 'unchanged' if 'useUniverseUnchanged' is true
                    return Universe.Unchanged;
                }).ToList();

                // we asked for 2 weeks, 5 work days for each week expected
                Assert.AreEqual(10, history.Count);
                Assert.IsTrue(history.All(x => x.Count() == 1));
                Assert.IsTrue(history.All(x => x.Single().Symbol == Symbols.AAPL));
                Assert.IsTrue(history.All(x => x.All(fundamental => fundamental.GetType() == typeof(Fundamental))));
            }
            else
            {
                using (Py.GIL())
                {
                    dynamic testModule = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

class Test():
    def __init__(self, useUniverseUnchanged):
        self.useUniverseUnchanged = useUniverseUnchanged
        self.state = False

    def selection(self, fundamentals):
        if not self.useUniverseUnchanged or not self.state:
            self.state = True
            return [ x.Symbol for x in fundamentals if x.Symbol.Value == ""AAPL"" ]
        return Universe.Unchanged

    def getSelect(self, qb, start, end):
        return qb.Select(Fundamentals, start, end, self.selection)
").GetAttr("Test");

                    var instance = testModule(useUniverseUnchanged);
                    var pyHistory = instance.getSelect(_qb, _start, _end);

                    Assert.AreEqual(10, pyHistory.__len__().AsManagedObject(typeof(int)));

                    for (var i = 0; i < 10; i++)
                    {
                        var index = pyHistory.index[i];
                        var series = pyHistory.loc[index].data;
                        var type = typeof(Fundamental[]);
                        var fundamental = (Fundamental[])series.AsManagedObject(type);

                        Assert.GreaterOrEqual(fundamental.Length, 1);
                        Assert.AreEqual(Symbols.AAPL, fundamental[0].Symbol);
                    }
                }
            }
        }
    }
}
