using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class PythonUtilTests
    {
        [Test]
        public void ConvertToSymbolsTest()
        {
            var expected = new List<Symbol>
            {
                Symbol.Create("AIG", SecurityType.Equity, Market.USA), 
                Symbol.Create("BAC", SecurityType.Equity, Market.USA),
                Symbol.Create("IBM", SecurityType.Equity, Market.USA),
                Symbol.Create("GOOG", SecurityType.Equity, Market.USA)
            };

            using (Py.GIL())
            {
                // Test Python String
                var test1 = PythonUtil.ConvertToSymbols(new PyString("AIG"));
                Assert.IsTrue(typeof(List<Symbol>) == test1.GetType());
                Assert.AreEqual(expected.FirstOrDefault(), test1.FirstOrDefault());

                // Test Python List of Strings
                var list = (new List<string> {"AIG", "BAC", "IBM", "GOOG"}).ToPyList();
                var test2 = PythonUtil.ConvertToSymbols(list);
                Assert.IsTrue(typeof(List<Symbol>) == test2.GetType());
                Assert.IsTrue(test2.SequenceEqual(expected));

                // Test Python Symbol
                var test3 = PythonUtil.ConvertToSymbols(expected.FirstOrDefault().ToPython());
                Assert.IsTrue(typeof(List<Symbol>) == test3.GetType());
                Assert.AreEqual(expected.FirstOrDefault(), test3.FirstOrDefault());

                // Test Python List of Symbols
                var test4 = PythonUtil.ConvertToSymbols(expected.ToPyList());
                Assert.IsTrue(typeof(List<Symbol>) == test4.GetType());
                Assert.IsTrue(test4.SequenceEqual(expected));
            }
        }
    }
}
