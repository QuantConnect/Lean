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
using NUnit.Framework;
using QuantConnect.Commands;
using QuantConnect.Interfaces;

namespace QuantConnect.Tests.Common.Commands
{
    [TestFixture]
    public class BaseCommandTests
    {
        [Test]
        public void GetsSymbolWhenSymbolIsPresent()
        {
            var command = new TestBaseCommand();
            Assert.DoesNotThrow(
                () => command.PublicGetSymbol(null, SecurityType.Base, null, Symbols.AAPL)
            );
        }

        [Test]
        public void GetsSymbolWhenTickerSecurityMarketIsPresent()
        {
            var command = new TestBaseCommand();
            Assert.DoesNotThrow(
                () =>
                    command.PublicGetSymbol(Symbols.AAPL.ID.Symbol, SecurityType.Equity, Market.USA)
            );
        }

        [Test]
        public void ReturnSymbolWhenOtherValuesArePresentToo()
        {
            var command = new TestBaseCommand();
            var symbol = command.PublicGetSymbol(
                Symbols.GOOG.ID.Symbol,
                SecurityType.Equity,
                Market.USA,
                Symbols.AAPL
            );
            Assert.AreEqual(Symbols.AAPL, symbol);
        }

        [Test]
        public void GetsSymbolThrowsWhenTickerMissing()
        {
            var command = new TestBaseCommand();
            Assert.Throws<ArgumentException>(
                () => command.PublicGetSymbol(null, SecurityType.Equity, Market.USA)
            );
        }

        [Test]
        public void GetsSymbolThrowsWhenSecurityTypeMissing()
        {
            var command = new TestBaseCommand();
            Assert.Throws<ArgumentException>(
                () => command.PublicGetSymbol(Symbols.AAPL.ID.Symbol, SecurityType.Base, Market.USA)
            );
        }

        [Test]
        public void GetsSymbolThrowsWhenMarketMissing()
        {
            var command = new TestBaseCommand();
            Assert.Throws<ArgumentException>(
                () => command.PublicGetSymbol(Symbols.AAPL.ID.Symbol, SecurityType.Equity, null)
            );
        }

        private class TestBaseCommand : BaseCommand
        {
            public Symbol PublicGetSymbol(
                string ticker,
                SecurityType securityType,
                string market,
                Symbol symbol = null
            )
            {
                return GetSymbol(ticker, securityType, market, symbol);
            }

            public override CommandResultPacket Run(IAlgorithm algorithm)
            {
                throw new NotImplementedException();
            }
        }
    }
}
