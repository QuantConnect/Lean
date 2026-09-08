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

using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Exceptions;
using System;
using System.Collections.Generic;

namespace QuantConnect.Tests.Common.Exceptions
{
    [TestFixture]
    public class AttributeErrorPythonExceptionInterpreterTests
    {
        private PythonException _quoteBarVolumeException;
        private PythonException _tradeBarAskPriceException;
        private PythonException _genericAttributeErrorException;

        [SetUp]
        public void Setup()
        {
            using (Py.GIL())
            {
                var module = Py.Import("Test_PythonExceptionInterpreter");
                dynamic algorithm = module.GetAttr("Test_PythonExceptionInterpreter").Invoke();

                _quoteBarVolumeException = CatchPythonException(() => algorithm.attribute_error_quote_bar_volume());
                _tradeBarAskPriceException = CatchPythonException(() => algorithm.attribute_error_trade_bar_ask_price());
                _genericAttributeErrorException = CatchPythonException(() => algorithm.attribute_error_generic());
            }
        }

        [Test]
        [TestCase(typeof(Exception), ExpectedResult = false)]
        [TestCase(typeof(KeyNotFoundException), ExpectedResult = false)]
        [TestCase(typeof(MissingMemberException), ExpectedResult = false)]
        [TestCase(typeof(InvalidOperationException), ExpectedResult = false)]
        [TestCase(typeof(PythonException), ExpectedResult = true)]
        public bool CanInterpretReturnsTrueForOnlyWrongBarTypeAttributeErrors(Type exceptionType)
        {
            var exception = exceptionType == typeof(PythonException)
                ? _quoteBarVolumeException
                : (Exception)Activator.CreateInstance(exceptionType);
            return new AttributeErrorPythonExceptionInterpreter().CanInterpret(exception);
        }

        [Test]
        public void DoesNotInterpretAttributeErrorsWithoutAHint()
        {
            // a plain AttributeError ('QuoteBar' object has no attribute 'not_an_attribute') keeps
            // the default interpretation
            Assert.IsFalse(new AttributeErrorPythonExceptionInterpreter().CanInterpret(_genericAttributeErrorException));
        }

        [Test]
        public void QuoteBarVolumeAccessExplainsTradeBarHoldsVolume()
        {
            var interpreter = new AttributeErrorPythonExceptionInterpreter();
            Assert.IsTrue(interpreter.CanInterpret(_quoteBarVolumeException));

            var interpreted = interpreter.Interpret(_quoteBarVolumeException, NullExceptionInterpreter.Instance);

            StringAssert.Contains("'QuoteBar' object has no attribute 'volume'", interpreted.Message);
            StringAssert.Contains("trade data like volume comes with", interpreted.Message);
            StringAssert.Contains("data.bars.get(symbol)", interpreted.Message);
        }

        [Test]
        public void TradeBarQuoteAttributeAccessExplainsQuoteBarHoldsQuotes()
        {
            var interpreter = new AttributeErrorPythonExceptionInterpreter();
            Assert.IsTrue(interpreter.CanInterpret(_tradeBarAskPriceException));

            var interpreted = interpreter.Interpret(_tradeBarAskPriceException, NullExceptionInterpreter.Instance);

            StringAssert.Contains("'TradeBar' object has no attribute 'ask_price'", interpreted.Message);
            StringAssert.Contains("bid/ask quotes come with", interpreted.Message);
            StringAssert.Contains("data.quote_bars.get(symbol)", interpreted.Message);
        }

        private static PythonException CatchPythonException(Action action)
        {
            try
            {
                action();
            }
            catch (PythonException pythonException)
            {
                return pythonException;
            }
            throw new InvalidOperationException("Expected a PythonException to be thrown");
        }
    }
}
