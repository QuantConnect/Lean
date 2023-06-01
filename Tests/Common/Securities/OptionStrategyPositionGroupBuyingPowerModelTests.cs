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
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

using QuantConnect.Algorithm;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;
using QuantConnect.Securities.Option.StrategyMatcher;
using QuantConnect.Securities.Positions;
using QuantConnect.Logging;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class OptionStrategyPositionGroupBuyingPowerModelTests
    {
        private QCAlgorithm _algorithm;
        private SecurityPortfolioManager _portfolio;
        private QuantConnect.Securities.Equity.Equity _equity;
        private Option _callOption;
        private Option _putOption;

        [SetUp]
        public void Setup()
        {
            _algorithm = new AlgorithmStub();
            _algorithm.SetCash(1000000);
            _algorithm.SetSecurityInitializer(security => security.FeeModel = new ConstantFeeModel(0));
            _portfolio = _algorithm.Portfolio;

            _equity = _algorithm.AddEquity("SPY");

            var strike = 200m;
            var expiry = new DateTime(2016, 1, 15);

            var callOptionSymbol = Symbols.CreateOptionSymbol("SPY", OptionRight.Call, strike, expiry);
            _callOption = _algorithm.AddOptionContract(callOptionSymbol);

            var putOptionSymbol = Symbols.CreateOptionSymbol("SPY", OptionRight.Put, strike, expiry);
            _putOption = _algorithm.AddOptionContract(putOptionSymbol);
        }

        /// <summary>
        /// All these test cases are based on the assumption that the initial cash is 1,000,000.
        /// </summary>
        // option strategy definition, initial quantity, order quantity, expected result
        private static readonly TestCaseData[] HasSufficientBuyingPowerForOrderTestCases = new[]
        {
            // Initial margin requirement (with premium) for CoveredCall with quantities 1 and -1 are 19210 and 21450 respectively
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 0, 1000000 / (19210 + 0), true), // 0 to max long
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 0, 1000000 / (19210 + 0) + 1, false), // 0 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 0, -(1000000 / 21450), true), // 0 to max short
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 0, -(1000000 / 21450 + 1), false),    // 0 to max short + 1
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 20, (1000000 / 19210) - 20, true),    // 20 to max long
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 20, (1000000 / 19210 + 1) - 20, false), // 20 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 20, -20, true), // 20 to 0
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 20, -(1000000 / 21450) - 20, true), // 20 to max short
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 20, -(1000000 / 21450 + 1) - 20, false),  // 20 to max short + 1
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -20, (1000000 / 19210) - -20, true),   // -20 to max long
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -20, (1000000 / 19210 + 1) - -20, false),   // -20 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -20, 20, true), // -20 to 0
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -20, -(1000000 / 21450) - -20, true),    // -20 to max short
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -20, -(1000000 / 21450 + 1) - -20, false),  // -20 to max short + 1
            // Initial margin requirement (with premium) for ProtectiveCall with quantities 1 and -1 are 21450 and 19210 respectively
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, 0, 1000000 / 21450, true), // 0 to max long
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, 0, 1000000 / 21450 + 1, false), // 0 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, 0, -(1000000 / 19210), true), // 0 to max short
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, 0, -(1000000 / 19210 + 1), false),    // 0 to max short + 1
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, 20, (1000000 / 21450) - 20, true),    // 20 to max long
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, 20, (1000000 / 21450 + 1) - 20, false), // 20 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, 20, -20, true), // 20 to 0
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, 20, -(1000000 / 19210) - 20, true), // 20 to max short
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, 20, -(1000000 / 19210 + 1) - 20, false),  // 20 to max short + 1
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, -20, (1000000 / 21450) - -20, true),   // -20 to max long
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, -20, (1000000 / 21450 + 1) - -20, false),   // -20 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, -20, 20, true), // -20 to 0
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, -20, -(1000000 / 19210) - -20, true),    // -20 to max short
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, -20, -(1000000 / 19210 + 1) - -20, false),  // -20 to max short + 1
            // Initial margin requirement (with premium) for CoveredPut with quantities 1 and -1 are 10250 and 10252 respectively
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 0, 1000000 / 10250, true), // 0 to max long
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 0, 1000000 / 10250 + 1, false), // 0 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 0, -(1000000 / 10252), true), // 0 to max short
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 0, -(1000000 / 10252 + 1), false),    // 0 to max short + 1
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 20, (1000000 / 10250) - 20, true),    // 20 to max long
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 20, (1000000 / 10250 + 1) - 20, false), // 20 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 20, -20, true), // 20 to 0
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 20, -(1000000 / 10252) - 20, true), // 20 to max short
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 20, -(1000000 / 10252 + 1) - 20, false),  // 20 to max short + 1
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -20, (1000000 / 10250) - -20, true),   // -20 to max long
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -20, (1000000 / 10250 + 1) - -20, false),   // -20 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -20, 20, true), // -20 to 0
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -20, -(1000000 / 10252) - -20, true),    // -20 to max short
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -20, -(1000000 / 10252 + 1) - -20, false),  // -20 to max short + 1
            // Initial margin requirement (with premium) for ProtectivePut with quantities 1 and -1 are 10252 and 10250 respectively
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, 0, 1000000 / 10252, true), // 0 to max long
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, 0, 1000000 / 10252 + 1, false), // 0 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, 0, -(1000000 / 10250), true), // 0 to max short
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, 0, -(1000000 / 10250 + 1), false),    // 0 to max short + 1
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, 20, (1000000 / 10252) - 20, true),    // 20 to max long
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, 20, (1000000 / 10252 + 1) - 20, false), // 20 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, 20, -20, true), // 20 to 0
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, 20, -(1000000 / 10250) - 20, true), // 20 to max short
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, 20, -(1000000 / 10250 + 1) - 20, false),  // 20 to max short + 1
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, -20, (1000000 / 10252) - -20, true),   // -20 to max long
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, -20, (1000000 / 10252 + 1) - -20, false),   // -20 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, -20, 20, true), // -20 to 0
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, -20, -(1000000 / 10250) - -20, true),    // -20 to max short
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, -20, -(1000000 / 10250 + 1) - -20, false),  // -20 to max short + 1
            // Initial margin requirement (with premium) for BearCallSpread with quantities 1 and -1 are 1000 and 1200 respectively
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 0, 1000000 / 1000, true), // 0 to max long
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 0, 1000000 / 1000 + 1, false), // 0 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 0, -(1000000 / 1200), true), // 0 to short
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 0, -(1000000 / 1200 + 1), false),    // 0 to greater short
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 20, (1000000 / 1000) - 20, true),    // 20 to max long
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 20, (1000000 / 1000 + 1) - 20, false), // 20 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 20, -20, true), // 20 to 0
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 20, -(1000000 / 1200) - 20, true), // 20 to short
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 20, -(1000000 / 1200 + 1) - 20, false),  // 20 to greater short
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -20, (1000000 / 1000) - -20, true),   // -20 to max long
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -20, (1000000 / 1000 + 1) - -20, false),   // -20 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -20, 20, true), // -20 to 0
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -20, -(1000000 / 1200) - -20, true),    // -20 to short
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -20, -(1000000 / 1200 + 1) - -20, false),  // -20 to greater short
            // Initial margin requirement (with premium) for BearPutSpread with quantities 1 and -1 are 1 and 1000 respectively
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 0, 1000000 / 1, true), // 0 to long
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 0, 1000000 / 1 + 1, false), // 0 to greater long
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 0, -(1000000 / 1000), true), // 0 to max short
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 0, -(1000000 / 1000 + 1), false),    // 0 to max short + 1
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 20, (1000000 / 1) - 20, true),    // 20 to long
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 20, (1000000 / 1 + 1) - 20, false), // 20 to greater long
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 20, -20, true), // 20 to 0
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 20, -(1000000 / 1000) - 20, true), // 20 to max short
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 20, -(1000000 / 1000 + 1) - 20, false),  // 20 to max short + 1
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -20, (1000000 / 1) - -20, true),   // -20 to long
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -20, (1000000 / 1 + 1) - -20, false),   // -20 to greater long
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -20, 20, true), // -20 to 0
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -20, -(1000000 / 1000) - -20, true),    // -20 to max short
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -20, -(1000000 / 1000 + 1) - -20, false),  // -20 to max short + 1
            // Initial margin requirement (with premium) for BullCallSpread with quantities 1 and -1 are 1200 and 1000 respectively
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 0, 1000000 / 1200, true), // 0 to long
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 0, 1000000 / 1200 + 1, false), // 0 to greater long
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 0, -(1000000 / 1000), true), // 0 to max short
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 0, -(1000000 / 1000 + 1), false),    // 0 to max short + 1
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 20, (1000000 / 1200) - 20, true),    // 20 to long
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 20, (1000000 / 1200 + 1) - 20, false), // 20 to greater long
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 20, -20, true), // 20 to 0
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 20, -(1000000 / 1000) - 20, true), // 20 to max short
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 20, -(1000000 / 1000 + 1) - 20, false),  // 20 to max short + 1
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -20, (1000000 / 1200) - -20, true),   // -20 to long
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -20, (1000000 / 1200 + 1) - -20, false),   // -20 to greater long
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -20, 20, true), // -20 to 0
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -20, -(1000000 / 1000) - -20, true),    // -20 to max short
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -20, -(1000000 / 1000 + 1) - -20, false),  // -20 to max short + 1
            // Initial margin requirement (with premium) for BullPutSpread with quantities 1 and -1 are 1000 and 1 respectively
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 0, 1000000 / 1000, true), // 0 to max long
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 0, 1000000 / 1000 + 1, false), // 0 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 0, -(1000000 / 1), true), // 0 to short
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 0, -(1000000 / 1 + 1), false),    // 0 to greater short
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 20, (1000000 / 1000) - 20, true),    // 20 to max long
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 20, (1000000 / 1000 + 1) - 20, false), // 20 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 20, -20, true), // 20 to 0
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 20, -(1000000 / 1) - 20, true), // 20 to short
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 20, -(1000000 / 1 + 1) - 20, false),  // 20 to greater short
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -20, (1000000 / 1000) - -20, true),   // -20 to max long
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -20, (1000000 / 1000 + 1) - -20, false),   // -20 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -20, 20, true), // -20 to 0
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -20, -(1000000 / 1) - -20, true),    // -20 to short
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -20, -(1000000 / 1 + 1) - -20, false),  // -20 to greater short
            // Initial margin requirement (with premium) for Straddle with quantities 1 and -1 are 11202 and 19402 respectively
            new TestCaseData(OptionStrategyDefinitions.Straddle, 0, 1000000 / 11202, true), // 0 to long
            new TestCaseData(OptionStrategyDefinitions.Straddle, 0, 1000000 / 11202 + 1, false), // 0 to greater long
            new TestCaseData(OptionStrategyDefinitions.Straddle, 0, -(1000000 / 19402), true), // 0 to max short
            new TestCaseData(OptionStrategyDefinitions.Straddle, 0, -(1000000 / 19402 + 1), false),    // 0 to max short + 1
            new TestCaseData(OptionStrategyDefinitions.Straddle, 20, (1000000 / 11202) - 20, true),    // 20 to long
            new TestCaseData(OptionStrategyDefinitions.Straddle, 20, (1000000 / 11202 + 1) - 20, false), // 20 to greater long
            new TestCaseData(OptionStrategyDefinitions.Straddle, 20, -20, true), // 20 to 0
            new TestCaseData(OptionStrategyDefinitions.Straddle, 20, -(1000000 / 19402) - 20, true), // 20 to max short
            new TestCaseData(OptionStrategyDefinitions.Straddle, 20, -(1000000 / 19402 + 1) - 20, false),  // 20 to max short + 1
            new TestCaseData(OptionStrategyDefinitions.Straddle, -20, (1000000 / 11202) - -20, true),   // -20 to long
            new TestCaseData(OptionStrategyDefinitions.Straddle, -20, (1000000 / 11202 + 1) - -20, false),   // -20 to greater long
            new TestCaseData(OptionStrategyDefinitions.Straddle, -20, 20, true), // -20 to 0
            new TestCaseData(OptionStrategyDefinitions.Straddle, -20, -(1000000 / 19402) - -20, true),    // -20 to max short
            new TestCaseData(OptionStrategyDefinitions.Straddle, -20, -(1000000 / 19402 + 1) - -20, false),  // -20 to max short + 1
            // Initial margin requirement (with premium) for ShortStraddle with quantities 1 and -1 are 19402 and 11202 respectively
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, 0, 1000000 / 19402, true), // 0 to max long
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, 0, 1000000 / 19402 + 1, false), // 0 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, 0, -(1000000 / 11202), true), // 0 to short
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, 0, -(1000000 / 11202 + 1), false),    // 0 to greater short
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, 20, (1000000 / 19402) - 20, true),    // 20 to max long
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, 20, (1000000 / 19402 + 1) - 20, false), // 20 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, 20, -20, true), // 20 to 0
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, 20, -(1000000 / 11202) - 20, true), // 20 to short
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, 20, -(1000000 / 11202 + 1) - 20, false),  // 20 to greater short
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, -20, (1000000 / 19402) - -20, true),   // -20 to max long
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, -20, (1000000 / 19402 + 1) - -20, false),   // -20 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, -20, 20, true), // -20 to 0
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, -20, -(1000000 / 11202) - -20, true),    // -20 to short
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, -20, -(1000000 / 11202 + 1) - -20, false),  // -20 to greater short
            // Initial margin requirement (with premium) for Strangle with quantities 1 and -1 are 10002 and 18202 respectively
            new TestCaseData(OptionStrategyDefinitions.Strangle, 0, 1000000 / 10002, true), // 0 to long
            new TestCaseData(OptionStrategyDefinitions.Strangle, 0, 1000000 / 10002 + 1, false), // 0 to greater long
            new TestCaseData(OptionStrategyDefinitions.Strangle, 0, -(1000000 / 18202), true), // 0 to max short
            new TestCaseData(OptionStrategyDefinitions.Strangle, 0, -(1000000 / 18202 + 1), false),    // 0 to max short + 1
            new TestCaseData(OptionStrategyDefinitions.Strangle, 20, (1000000 / 10002) - 20, true),    // 20 to long
            new TestCaseData(OptionStrategyDefinitions.Strangle, 20, (1000000 / 10002 + 1) - 20, false), // 20 to greater long
            new TestCaseData(OptionStrategyDefinitions.Strangle, 20, -20, true), // 20 to 0
            new TestCaseData(OptionStrategyDefinitions.Strangle, 20, -(1000000 / 18202) - 20, true), // 20 to max short
            new TestCaseData(OptionStrategyDefinitions.Strangle, 20, -(1000000 / 18202 + 1) - 20, false),  // 20 to max short + 1
            new TestCaseData(OptionStrategyDefinitions.Strangle, -20, (1000000 / 10002) - -20, true),   // -20 to long
            new TestCaseData(OptionStrategyDefinitions.Strangle, -20, (1000000 / 10002 + 1) - -20, false),   // -20 to greater long
            new TestCaseData(OptionStrategyDefinitions.Strangle, -20, 20, true), // -20 to 0
            new TestCaseData(OptionStrategyDefinitions.Strangle, -20, -(1000000 / 18202) - -20, true),    // -20 to max short
            new TestCaseData(OptionStrategyDefinitions.Strangle, -20, -(1000000 / 18202 + 1) - -20, false),  // -20 to max short + 1
            // Initial margin requirement (with premium) for ShortStrangle with quantities 1 and -1 are 18202 and 10002 respectively
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, 0, 1000000 / 18202, true), // 0 to max long
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, 0, 1000000 / 18202 + 1, false), // 0 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, 0, -(1000000 / 11202), true), // 0 to short
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, 0, -(1000000 / 10002 + 1), false),    // 0 to greater short
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, 20, (1000000 / 18202) - 20, true),    // 20 to max long
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, 20, (1000000 / 18202 + 1) - 20, false), // 20 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, 20, -20, true), // 20 to 0
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, 20, -(1000000 / 10002) - 20, true), // 20 to short
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, 20, -(1000000 / 10002 + 1) - 20, false),  // 20 to greater short
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, -20, (1000000 / 18202) - -20, true),   // -20 to max long
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, -20, (1000000 / 18202 + 1) - -20, false),   // -20 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, -20, 20, true), // -20 to 0
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, -20, -(1000000 / 10002) - -20, true),    // -20 to short
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, -20, -(1000000 / 10002 + 1) - -20, false),  // -20 to greater short
            // Initial margin requirement (with premium) for ButterflyCall with quantities 1 and -1 are 400 and 1000 respectively
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 0, 1000000/ 400, true), // 0 to long
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 0, 1000000/ 400 + 1, false), // 0 to greater long
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 0, -(1000000 / 1000), true), // 0 to max short
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 0, -(1000000 / 1000 + 1), false),    // 0 to max short + 1
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 20, (1000000/ 400) - 20, true),    // 20 to long
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 20, (1000000/ 400 + 1) - 20, false), // 20 to greater long
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 20, -20, true), // 20 to 0
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 20, -(1000000 / 1000) - 20, true), // 20 to max short
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 20, -(1000000 / 1000 + 1) - 20, false),  // 20 to max short + 1
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -20, (1000000/ 400) - -20, true),   // -20 to long
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -20, (1000000/ 400 + 1) - -20, false),   // -20 to greater long
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -20, 20, true), // -20 to 0
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -20, -(1000000 / 1000) - -20, true),    // -20 to max short
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -20, -(1000000 / 1000 + 1) - -20, false),  // -20 to max short + 1
            // Initial margin requirement (with premium) for ShortButterflyCall with quantities 1 and -1 are 1000 and 0 respectively
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 0, 1000000 / 1000, true), // 0 to max long
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 0, 1000000 / 1000 + 1, false), // 0 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 0, -(1000000 / 10202), true), // 0 to short
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 0, -(1000000/ 400 + 1), false),    // 0 to greater short
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 20, (1000000 / 1000) - 20, true),    // 20 to max long
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 20, (1000000 / 1000 + 1) - 20, false), // 20 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 20, -20, true), // 20 to 0
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 20, -(1000000/ 400) - 20, true), // 20 to short
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 20, -(1000000/ 400 + 1) - 20, false),  // 20 to greater short
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -20, (1000000 / 1000) - -20, true),   // -20 to max long
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -20, (1000000 / 1000 + 1) - -20, false),   // -20 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -20, 20, true), // -20 to 0
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -20, -(1000000/ 400) - -20, true),    // -20 to short
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -20, -(1000000/ 400 + 1) - -20, false),  // -20 to greater short
            // Initial margin requirement (with premium) for ButterflyPut with quantities 1 and -1 are 1 and 1000 respectively
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 0, 1000000 / 1, true), // 0 to long
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 0, 1000000 / 1 + 1, false), // 0 to greater long
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 0, -(1000000 / 1000), true), // 0 to max short
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 0, -(1000000 / 1000 + 1), false),    // 0 to max short + 1
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 20, (1000000 / 1) - 20, true),    // 20 to long
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 20, (1000000 / 1 + 1) - 20, false), // 20 to greater long
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 20, -20, true), // 20 to 0
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 20, -(1000000 / 1000) - 20, true), // 20 to max short
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 20, -(1000000 / 1000 + 1) - 20, false),  // 20 to max short + 1
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -20, (1000000 / 1) - -20, true),   // -20 to long
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -20, (1000000 / 1 + 1) - -20, false),   // -20 to greater long
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -20, 20, true), // -20 to 0
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -20, -(1000000 / 1000) - -20, true),    // -20 to max short
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -20, -(1000000 / 1000 + 1) - -20, false),  // -20 to max short + 1
            // Initial margin requirement (with premium) for ShortButterflyPut with quantities 1 and -1 are 1000 and 1 respectively
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 0, 1000000 / 1000, true), // 0 to max long
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 0, 1000000 / 1000 + 1, false), // 0 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 0, -(1000000 / 1), true), // 0 to short
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 0, -(1000000 / 1 + 1), false),    // 0 to greater short
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 20, (1000000 / 1000) - 20, true),    // 20 to max long
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 20, (1000000 / 1000 + 1) - 20, false), // 20 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 20, -20, true), // 20 to 0
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 20, -(1000000 / 1) - 20, true), // 20 to short
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 20, -(1000000 / 1 + 1) - 20, false),  // 20 to greater short
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -20, (1000000 / 1000) - -20, true),   // -20 to max long
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -20, (1000000 / 1000 + 1) - -20, false),   // -20 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -20, 20, true), // -20 to 0
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -20, -(1000000 / 1) - -20, true),    // -20 to short
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -20, -(1000000 / 1 + 1) - -20, false),  // -20 to greater short
            // Initial margin requirement (with premium) for CallCalendarSpread with quantities 1 and -1 are 200 and 19400 respectively
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 0, 1000000 / 200, true), // 0 to long
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 0, 1000000 / 200 + 1, false), // 0 to greater long
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 0, -(1000000 / 19400), true), // 0 to max short
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 0, -(1000000 / 19400 + 1), false),    // 0 to max short + 1
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 20, (1000000 / 200) - 20, true),    // 20 to long
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 20, (1000000 / 200 + 1) - 20, false), // 20 to greater long
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 20, -20, true), // 20 to 0
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 20, -(1000000 / 19400) - 20, true), // 20 to max short
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 20, -(1000000 / 19400 + 1) - 20, false),  // 20 to max short + 1
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -20, (1000000 / 200) - -20, true),   // -20 to long
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -20, (1000000 / 200 + 1) - -20, false),   // -20 to greater long
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -20, 20, true), // -20 to 0
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -20, -(1000000 / 19400) - -20, true),    // -20 to max short
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -20, -(1000000 / 19400 + 1) - -20, false),  // -20 to max short + 1
            // Initial margin requirement (with premium) for ShortCallCalendarSpread with quantities 1 and -1 are 19400 and 400 respectively
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, 0, 1000000 / 19400, true), // 0 to max long
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, 0, 1000000 / 19400 + 1, false), // 0 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, 0, -(1000000 / 200), true), // 0 to short
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, 0, -(1000000 / 200 + 1), false),    // 0 to greater short
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, 20, (1000000 / 19400) - 20, true),    // 20 to max long
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, 20, (1000000 / 19400 + 1) - 20, false), // 20 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, 20, -20, true), // 20 to 0
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, 20, -(1000000 / 200) - 20, true), // 20 to short
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, 20, -(1000000 / 200 + 1) - 20, false),  // 20 to greater short
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, -20, (1000000 / 19400) - -20, true),   // -20 to max long
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, -20, (1000000 / 19400 + 1) - -20, false),   // -20 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, -20, 20, true), // -20 to 0
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, -20, -(1000000 / 200) - -20, true),    // -20 to short
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, -20, -(1000000 / 200 + 1) - -20, false),  // -20 to greater short
            // Initial margin requirement (with premium) for PutCalendarSpread with quantities 1 and -1 are 1 and 3002 respectively
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 0, 1000000 / 1, true), // 0 to long
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 0, 1000000 / 1 + 1, false), // 0 to greater long
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 0, -(1000000 / 3002), true), // 0 to max short
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 0, -(1000000 / 3002 + 1), false),    // 0 to max short + 1
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 20, (1000000 / 1) - 20, true),    // 20 to long
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 20, (1000000 / 1 + 1) - 20, false), // 20 to greater long
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 20, -20, true), // 20 to 0
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 20, -(1000000 / 3002) - 20, true), // 20 to max short
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 20, -(1000000 / 3002 + 1) - 20, false),  // 20 to max short + 1
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -20, (1000000 / 1) - -20, true),   // -20 to long
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -20, (1000000 / 1 + 1) - -20, false),   // -20 to greater long
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -20, 20, true), // -20 to 0
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -20, -(1000000 / 3002) - -20, true),    // -20 to max short
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -20, -(1000000 / 3002 + 1) - -20, false),  // -20 to max short + 1
            // Initial margin requirement (with premium) for ShortPutCalendarSpread with quantities 1 and -1 are 3002 and 1 respectively
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, 0, 1000000 / 3002, true), // 0 to max long
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, 0, 1000000 / 3002 + 1, false), // 0 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, 0, -(1000000 / 1), true), // 0 to short
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, 0, -(1000000 / 1 + 1), false),    // 0 to greater short
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, 20, (1000000 / 3002) - 20, true),    // 20 to max long
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, 20, (1000000 / 3002 + 1) - 20, false), // 20 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, 20, -20, true), // 20 to 0
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, 20, -(1000000 / 1) - 20, true), // 20 to short
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, 20, -(1000000 / 1 + 1) - 20, false),  // 20 to greater short
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, -20, (1000000 / 3002) - -20, true),   // -20 to max long
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, -20, (1000000 / 3002 + 1) - -20, false),   // -20 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, -20, 20, true), // -20 to 0
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, -20, -(1000000 / 1) - -20, true),    // -20 to short
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, -20, -(1000000 / 1 + 1) - -20, false),  // -20 to greater short
            // Initial margin requirement (with premium) for IronCondor with quantities 1 and -1 are 1000 and 1001 respectively
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 0, 1000000 / 1000, true), // 0 to max long
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 0, 1000000 / 1000 + 1, false), // 0 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 0, -(1000000 / 1001), true), // 0 to short
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 0, -(1000000 / 1001 + 1), false),    // 0 to greater short
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 20, (1000000 / 1000) - 20, true),    // 20 to max long
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 20, (1000000 / 1000 + 1) - 20, false), // 20 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 20, -20, true), // 20 to 0
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 20, -(1000000 / 1001) - 20, true), // 20 to short
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 20, -(1000000 / 1001 + 1) - 20, false),  // 20 to greater short
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -20, (1000000 / 1000) - -20, true),   // -20 to max long
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -20, (1000000 / 1000 + 1) - -20, false),   // -20 to max long + 1
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -20, 20, true), // -20 to 0
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -20, -(1000000 / 1001) - -20, true),    // -20 to short
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -20, -(1000000 / 1001 + 1) - -20, false),  // -20 to greater short
        };

        [TestCaseSource(nameof(HasSufficientBuyingPowerForOrderTestCases))]
        public void HasSufficientBuyingPowerForOrder(OptionStrategyDefinition optionStrategy, int initialPositionQuantity, int orderQuantity,
            bool expectedResult)
        {
            _algorithm.SetCash(1000000);

            var initialPositionGroup = SetUpOptionStrategy(optionStrategy, initialPositionQuantity);

            var orders = GetPositionGroupOrders(initialPositionGroup, initialPositionQuantity != 0 ? initialPositionQuantity : 1, orderQuantity);
            Assert.IsTrue(_portfolio.Positions.TryCreatePositionGroup(orders, out var ordersPositionGroup));

            var result = ordersPositionGroup.BuyingPowerModel.HasSufficientBuyingPowerForOrder(
                new HasSufficientPositionGroupBuyingPowerForOrderParameters(_portfolio, ordersPositionGroup, orders));

            Assert.AreEqual(expectedResult, result.IsSufficient, result.Reason);
        }

        [Test]
        public void HasSufficientBuyingPowerForStrategyOrder([Values] bool withInitialHoldings)
        {
            const decimal price = 1.2345m;
            const decimal underlyingPrice = 200m;

            _algorithm.SetCash(100000);
            var initialMargin = _portfolio.MarginRemaining;

            _equity.SetMarketPrice(new Tick { Value = underlyingPrice });
            _callOption.SetMarketPrice(new Tick { Value = price });
            _putOption.SetMarketPrice(new Tick { Value = price });

            var initialHoldingsQuantity = withInitialHoldings ? -10 : 0;
            _callOption.Holdings.SetHoldings(1.5m, initialHoldingsQuantity);
            _putOption.Holdings.SetHoldings(1m, initialHoldingsQuantity);

            var sufficientCaseConsidered = false;
            var insufficientCaseConsidered = false;

            // make sure these cases are considered:
            // 1. liquidating part of the position
            var partialLiquidationCaseConsidered = false;
            // 2. liquidating the whole position
            var fullLiquidationCaseConsidered = false;
            // 3. shorting more, but with margin left
            var furtherShortingWithMarginRemainingCaseConsidered = false;
            // 4. shorting even more to the point margin is no longer enough
            var furtherShortingWithNoMarginRemainingCaseConsidered = false;

            for (var strategyQuantity = Math.Abs(initialHoldingsQuantity); strategyQuantity > -30; strategyQuantity--)
            {
                var orders = GetStrategyOrders(strategyQuantity);
                Assert.IsTrue(_portfolio.Positions.TryCreatePositionGroup(orders, out var positionGroup));
                var buyingPowerModel = positionGroup.BuyingPowerModel;

                var maintenanceMargin = buyingPowerModel.GetMaintenanceMargin(
                    new PositionGroupMaintenanceMarginParameters(_portfolio, positionGroup));

                var hasSufficientBuyingPowerResult = buyingPowerModel.HasSufficientBuyingPowerForOrder(
                    new HasSufficientPositionGroupBuyingPowerForOrderParameters(_portfolio, positionGroup, orders));

                Assert.AreEqual(maintenanceMargin < initialMargin, hasSufficientBuyingPowerResult.IsSufficient);

                if (hasSufficientBuyingPowerResult.IsSufficient)
                {
                    sufficientCaseConsidered = true;
                }
                else
                {
                    Assert.IsTrue(sufficientCaseConsidered, "All 'sufficient buying power' case should have been before the 'insufficient' ones");

                    insufficientCaseConsidered = true;
                }

                var newPositionQuantity = positionGroup.Quantity;
                if (newPositionQuantity == 0)
                {
                    fullLiquidationCaseConsidered = true;
                }
                else if (initialHoldingsQuantity + strategyQuantity < 0)
                {
                    if (newPositionQuantity < Math.Abs(initialHoldingsQuantity))
                    {
                        partialLiquidationCaseConsidered = true;
                    }
                    else if (hasSufficientBuyingPowerResult.IsSufficient)
                    {
                        furtherShortingWithMarginRemainingCaseConsidered = true;
                    }
                    else
                    {
                        furtherShortingWithNoMarginRemainingCaseConsidered = true;
                    }
                }
            }

            Assert.IsTrue(sufficientCaseConsidered, "The 'sufficient buying power' case was not considered");
            Assert.IsTrue(insufficientCaseConsidered, "The 'insufficient buying power' case was not considered");

            if (withInitialHoldings)
            {
                Assert.IsTrue(partialLiquidationCaseConsidered, "The 'partial liquidation' case was not considered");
                Assert.IsTrue(fullLiquidationCaseConsidered, "The 'full liquidation' case was not considered");
            }

            Assert.IsTrue(furtherShortingWithMarginRemainingCaseConsidered, "The 'further shorting with margin remaining' case was not considered");
            Assert.IsTrue(furtherShortingWithNoMarginRemainingCaseConsidered, "The 'further shorting with no margin remaining' case was not considered");
        }

        [Test]
        public void HasSufficientBuyingPowerForReducingStrategyOrder()
        {
            const decimal price = 1m;
            const decimal underlyingPrice = 200m;

            _equity.SetMarketPrice(new Tick { Value = underlyingPrice });
            _callOption.SetMarketPrice(new Tick { Value = price });
            _putOption.SetMarketPrice(new Tick { Value = price });

            var initialHoldingsQuantity = -10;
            _callOption.Holdings.SetHoldings(1.5m, initialHoldingsQuantity);
            _putOption.Holdings.SetHoldings(1m, initialHoldingsQuantity);

            _algorithm.SetCash(_portfolio.TotalMarginUsed * 0.95m);

            var quantity = -initialHoldingsQuantity / 2;
            var orders = GetStrategyOrders(quantity);

            Assert.IsTrue(_portfolio.Positions.TryCreatePositionGroup(orders, out var positionGroup));

            var hasSufficientBuyingPowerResult = positionGroup.BuyingPowerModel.HasSufficientBuyingPowerForOrder(
                new HasSufficientPositionGroupBuyingPowerForOrderParameters(_portfolio, positionGroup, orders));

            Assert.IsTrue(hasSufficientBuyingPowerResult.IsSufficient);
        }

        // Increasing short position
        [TestCase(-10, -11)]
        // Decreasing short position
        [TestCase(-10, -9)]
        // Liquidating
        [TestCase(-10, 0)]
        public void PositionGroupOrderQuantityCalculationForDeltaBuyingPowerFromShortPosition(int initialHoldingsQuantity, int finalPositionQuantity)
        {
            _algorithm.SetCash(100000);
            // Just making sure we start from a short position
            var absQuantity = Math.Abs(initialHoldingsQuantity);
            initialHoldingsQuantity = -absQuantity;

            SetUpOptionStrategy(initialHoldingsQuantity);
            var positionGroup = _portfolio.Positions.Groups.Single();

            var expectedQuantity = initialHoldingsQuantity - finalPositionQuantity;
            var usedMargin = _portfolio.TotalMarginUsed;
            var marginPerNakedShortUnit = usedMargin / absQuantity;

            var deltaBuyingPower = expectedQuantity * marginPerNakedShortUnit;
            // Add a small buffer to avoid precision errors that occur when converting the delta buying power request into a target one
            deltaBuyingPower *= finalPositionQuantity < initialHoldingsQuantity ? 1.001m : 0.999m;

            ComputeAndAssertQuantityForDeltaBuyingPower(positionGroup, expectedQuantity, deltaBuyingPower);
        }

        // Increasing position
        [TestCase(10, 11)]
        // Decreasing position
        [TestCase(10, 9)]
        // Liquidating
        [TestCase(10, 0)]
        public void PositionGroupOrderQuantityCalculationForDeltaBuyingPowerFromLongPosition(int initialHoldingsQuantity, int finalPositionQuantity)
        {
            _algorithm.SetCash(100000);
            // Just making sure we start from a long position
            initialHoldingsQuantity = Math.Abs(initialHoldingsQuantity);

            var positionGroup = SetUpOptionStrategy(OptionStrategyDefinitions.CoveredCall, initialHoldingsQuantity);

            var expectedQuantity = finalPositionQuantity - initialHoldingsQuantity;
            var usedMargin = _portfolio.TotalMarginUsed;
            var marginPerLongUnit = Math.Abs(positionGroup.BuyingPowerModel.GetInitialMarginRequirement(_portfolio, positionGroup) / initialHoldingsQuantity);

            var shortUnitGroup = positionGroup.WithQuantity(-1, _portfolio.Positions);
            var marginPerNakedShortUnit = Math.Abs(shortUnitGroup.BuyingPowerModel.GetInitialMarginRequirement(
                new PositionGroupInitialMarginParameters(_portfolio, shortUnitGroup)).Value);

            var deltaBuyingPower = finalPositionQuantity >= 0
                //Going even longer / Going "less" long/ Liquidating
                ? expectedQuantity * marginPerLongUnit
                // Going short from long
                : -usedMargin + Math.Abs(finalPositionQuantity) * marginPerNakedShortUnit;

            var initialMarginRequirement = positionGroup.BuyingPowerModel.GetInitialMarginRequirement(_portfolio, positionGroup);
            var maintenanceMargin = positionGroup.BuyingPowerModel.GetMaintenanceMargin(_portfolio, positionGroup);
            // Adjust the delta buying power:
            // GetMaximumLotsForDeltaBuyingPower will add the delta buying power to the maintenance margin and used that as a target margin,
            // but then GetMaximumLotsForTargetBuyingPower will work with initial margin requirement so we make sure the resulting quantity
            // can be ordered. In order to match this, we need to adjust the delta buying power by the difference between the initial margin
            // requirement  and maintenance margin.
            deltaBuyingPower += initialMarginRequirement - maintenanceMargin;

            ComputeAndAssertQuantityForDeltaBuyingPower(positionGroup, expectedQuantity, deltaBuyingPower);
        }

        [TestCase(-10, 1, +1)]
        [TestCase(-10, 1, -1)]
        [TestCase(-10, 0.5, +1)]
        [TestCase(-10, 0.5, -1)]
        [TestCase(-10, 2, +1)]
        [TestCase(-10, 2, -1)]
        [TestCase(10, 1, +1)]
        [TestCase(10, 1, -1)]
        [TestCase(10, 0.5, +1)]
        [TestCase(10, 0.5, -1)]
        [TestCase(10, 2, +1)]
        [TestCase(10, 2, -1)]
        public void OrderQuantityCalculation(int initialHoldingsQuantity, decimal targetMarginPercent, int targetMarginDirection)
        {
            // The targetMarginDirection whether we want to go in the same direction as the holdings:
            //   +1: short will go shorter and long will go longer
            //   -1: short will go towards long and vice-versa

            var positionGroup = SetUpOptionStrategy(OptionStrategyDefinitions.CoveredCall, initialHoldingsQuantity);
            var currentUsedMargin = Math.Abs(positionGroup.BuyingPowerModel.GetInitialMarginRequirement(_portfolio, positionGroup));

            var longUnitGroup = positionGroup.CreateUnitGroup(_portfolio.Positions);
            var longUnitMargin = Math.Abs(longUnitGroup.BuyingPowerModel.GetInitialMarginRequirement(_portfolio, longUnitGroup));

            var expectedQuantity = 0m;
            var finalPositionQuantity = 0m;
            var targetFinalMargin = 0m;
            // Going to the opposite side
            if (targetMarginPercent > 1 && targetMarginDirection == -1)
            {
                var shortUnitGroup = positionGroup.WithQuantity(-1, _portfolio.Positions);
                var shortUnitMargin = Math.Abs(shortUnitGroup.BuyingPowerModel.GetInitialMarginRequirement(_portfolio, shortUnitGroup));
                finalPositionQuantity = Math.Floor(-currentUsedMargin / shortUnitMargin);
                expectedQuantity = -Math.Abs(initialHoldingsQuantity) + finalPositionQuantity;

                targetFinalMargin = finalPositionQuantity * shortUnitMargin;
            }
            else
            {
                expectedQuantity = Math.Abs(initialHoldingsQuantity * targetMarginPercent) * Math.Sign(targetMarginDirection);
                finalPositionQuantity = initialHoldingsQuantity + Math.Sign(initialHoldingsQuantity) * expectedQuantity;

                targetFinalMargin = Math.Abs(finalPositionQuantity * longUnitMargin);
            }

            var quantity = (positionGroup.BuyingPowerModel as OptionStrategyPositionGroupBuyingPowerModel).GetPositionGroupOrderQuantity(_portfolio,
                positionGroup, currentUsedMargin, targetFinalMargin, longUnitGroup, longUnitMargin, out _);

            // Liquidating
            if (targetFinalMargin == 0)
            {
                // Should always be negative when liquidating (or reducing)
                Assert.AreEqual(-Math.Abs(initialHoldingsQuantity), quantity);
            }
            // Reducing the position
            else if (targetFinalMargin < currentUsedMargin)
            {
                Assert.Less(quantity, 0);
            }
            // Increasing the position
            else
            {
                Assert.Greater(quantity, 0);
            }

            Assert.AreEqual(expectedQuantity, quantity);
        }

        /// <summary>
        /// Test cases for the <see cref="OptionStrategyPositionGroupBuyingPowerModel.GetInitialMarginRequirement"/> method.
        ///
        /// TODO: We should come back and revisit these test cases to make sure they are correct.
        /// The approximate values from IB for the prices used in the test are in the comments.
        /// For instance, see the test case for the CoveredCall strategy. The margin values do not match IB's values.
        ///
        /// Test cases marked as explicit will fail if ran, they have an approximate expected value based on IB's margin requirements.
        /// </summary>
        private static readonly TestCaseData[] InitialMarginRequirementsTestCases = new[]
        {
            // OptionStrategyDefinition, initialHoldingsQuantity, expectedInitialMarginRequirement
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 1, 19000m),                     // IB:  19325
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -1, 12000m),                    // IB:  12338.58
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, 1, 12000m),                  // IB:  inverted covered call
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, -1, 19000m),                 // IB:  covered call
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 1, 12000m),                      // IB:  12331.38
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -1, 10000m),                     // IB:  10276.15
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, 1, 10000m),                   // IB:  inverted covered put
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, -1, 12000m),                  // IB:  covered put
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 1, 1000m),                   // IB:  1000
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -1, 0m),                     // IB:  0
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 1, 0m),                       // IB:  0
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -1, 1000m),                   // IB:  1000
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 1, 0m),                      // IB:  0
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -1, 1000m),                  // IB:  1000
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 1, 1000m),                    // IB:  1000
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -1, 0m),                      // IB:  0
            new TestCaseData(OptionStrategyDefinitions.Straddle, 1, 0m),                            // IB:  0
            new TestCaseData(OptionStrategyDefinitions.Straddle, -1, 20000m),                       // IB:  20624
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, 1, 20000m),                   // IB:  20624
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, -1, 0m),                      // IB:  0
            new TestCaseData(OptionStrategyDefinitions.Strangle, 1, 0m),                            // IB:  0
            new TestCaseData(OptionStrategyDefinitions.Strangle, -1, 21000m),                       // IB:  20752
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, 1, 21000m),                   // IB:  20752
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, -1, 0m),                      // IB:  0
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 1, 0m),                       // IB:  0
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -1, 1000m),                   // IB:  1000
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 1, 1000m),               // IB:  1000
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -1, 0m),                 // IB:  0
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 1, 0m),                        // IB:  0
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -1, 1000m),                    // IB:  1000
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 1, 1000m),                // IB:  1000
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -1, 0m),                  // IB:  0
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 1, 0m),                  // IB:  0
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -1, 20000m),             // IB:  20537
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, 1, 20000m),         // IB:  20537
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, -1, 0m),            // IB:  0
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 1, 0m),                   // IB:  0
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -1, 3000m),               // IB:  3121
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, 1, 3000m),           // IB:  3121
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, -1, 0m),             // IB:  0
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 1, 1000m),                       // IB:  1001
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -1, 0m),                         // IB:  0
        };

        [TestCaseSource(nameof(InitialMarginRequirementsTestCases))]
        public void GetsInitialMarginRequirement(OptionStrategyDefinition optionStrategyDefinition, int quantity,
            decimal expectedInitialMarginRequirement)
        {
            var positionGroup = SetUpOptionStrategy(optionStrategyDefinition, quantity);

            var initialMarginRequirement = positionGroup.BuyingPowerModel.GetInitialMarginRequirement(
                new PositionGroupInitialMarginParameters(_portfolio, positionGroup));

            Console.WriteLine(initialMarginRequirement.Value);

            var premium = 0m;
            foreach (var position in positionGroup.Positions.Where(position => position.Symbol.SecurityType.IsOption()))
            {
                var option = (Option)_portfolio.Securities[position.Symbol];
                premium += option.Holdings.GetQuantityValue(position.Quantity).InAccountCurrency;
            }
            initialMarginRequirement -= Math.Max(0, premium);

            Assert.AreEqual((double)expectedInitialMarginRequirement, (double)initialMarginRequirement.Value, (double)(0.2m * expectedInitialMarginRequirement));
        }

        private static readonly TestCaseData[] CoveredCallInitialMarginRequirementsTestCases = new[]
        {
            // OptionStrategyDefinition, initialHoldingsQuantity, expectedInitialMarginRequirement, option strike
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 2, 53700m, 200),                     // IB: 53,714
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 2, 38000m, 300),                     // IB: 38,756
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 2, 23000m, 400),                     // IB: 23,752
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 2, 21000m, 500),                     // IB: 20,939
        };

        [TestCaseSource(nameof(CoveredCallInitialMarginRequirementsTestCases))]
        public void CoveredCallInitialMarginRequirement(OptionStrategyDefinition optionStrategyDefinition, int quantity,
            decimal expectedInitialMarginRequirement, int strike)
        {
            var positionGroup = SetUpOptionStrategy(optionStrategyDefinition, quantity, strike);

            var initialMarginRequirement = positionGroup.BuyingPowerModel.GetInitialMarginRequirement(
                new PositionGroupInitialMarginParameters(_portfolio, positionGroup));

            Assert.AreEqual((double)expectedInitialMarginRequirement, (double)initialMarginRequirement.Value, (double)(0.2m * expectedInitialMarginRequirement));
        }

        /// <summary>
        /// Test cases for the <see cref="OptionStrategyPositionGroupBuyingPowerModel.GetMaintenanceMargin"/> method.
        ///
        /// TODO: We should come back and revisit these test cases to make sure they are correct.
        /// The approximate values from IB for the prices used in the test are in the comments.
        /// For instance, see the test case for the CoveredCall strategy. The margin values do not match IB's values.
        ///
        /// Test cases marked as explicit will fail if ran, they have an approximate expected value based on IB's margin requirements.
        /// </summary>
        private static readonly TestCaseData[] MaintenanceMarginTestCases = new[]
        {
            // OptionStrategyDefinition, initialHoldingsQuantity, expectedMaintenanceMargin
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 1, 19000m),                     // IB:  19325
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -1, 3000m),                     // IB:  3000
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, 1, 3000m),                   // IB:  inverted covered call
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, -1, 19000m),                 // IB:  covered call
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 1, 10250m),                      // IB:  12000m
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -1, 10000m),                     // IB:  10276
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, 1, 10000m),                   // IB:  inverted covered Put
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, -1, 10250m),                  // IB:  covered Put
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 1, 1000m),                   // IB:  10000
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -1, 0m),                     // IB:  0
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 1, 0m),                       // IB:  0
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -1, 1000m),                   // IB:  1000
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 1, 0m),                      // IB:  0
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -1, 1000m),                  // IB:  1000
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 1, 1000m),                    // IB:  1000
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -1, 0m),                      // IB:  0
            new TestCaseData(OptionStrategyDefinitions.Straddle, 1, 0m),                            // IB:  0
            new TestCaseData(OptionStrategyDefinitions.Straddle, -1, 20000m),                       // IB:  20624
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, 1, 20000m),                   // IB:  20624
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, -1, 0m),                      // IB:  0
            new TestCaseData(OptionStrategyDefinitions.Strangle, 1, 0m),                            // IB:  0
            new TestCaseData(OptionStrategyDefinitions.Strangle, -1, 21000m),                       // IB:  20752
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, 1, 21000m),                   // IB:  20752
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, -1, 0m),                      // IB:  0
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 1, 0m),                       // IB:  0
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -1, 1000m),                   // IB:  1000
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 1, 1000m),               // IB:  1000
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -1, 0m),                 // IB:  0
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 1, 0m),                        // IB:  0
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -1, 1000m),                    // IB:  1000
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 1, 1000m),                // IB:  1000
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -1, 0m),                  // IB:  0
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 1, 0m),                  // IB:  0
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -1, 20000m),             // IB:  20537
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, 1, 20000m),         // IB:  20537
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, -1, 0m),            // IB:  0
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 1, 0m),                   // IB:  0
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -1, 3000m),               // IB:  3121
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, 1, 3000m),           // IB:  3121
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, -1, 0m),             // IB:  0
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 1, 1000m),                       // IB:  1017.62
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -1, 0m),                         // IB:  0
        };

        [TestCaseSource(nameof(MaintenanceMarginTestCases))]
        public void GetsMaintenanceMargin(OptionStrategyDefinition optionStrategyDefinition, int quantity, decimal expectedMaintenanceMargin)
        {
            var positionGroup = SetUpOptionStrategy(optionStrategyDefinition, quantity);

            var maintenanceMargin = positionGroup.BuyingPowerModel.GetMaintenanceMargin(
                new PositionGroupMaintenanceMarginParameters(_portfolio, positionGroup));

            Assert.AreEqual((double)expectedMaintenanceMargin, (double)maintenanceMargin.Value, (double)(0.2m * expectedMaintenanceMargin));
        }

        /// <remarks>
        /// TODO: Revisit the explicit test cases when we can take into account premium for strategies with zero margin.
        /// </remarks>
        // option strategy definition, initial position quantity, final position quantity
        private static readonly TestCaseData[] OrderQuantityForDeltaBuyingPowerTestCases = new[]
        {
            // Initial margin requirement for CoveredCall with quantity 10 and -10 is 192100 and 214500 respectively
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 10, 192100m / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 10, -192100m / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 10, -192100m, -10),
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 10, -192100m - 214500m, -20),    // Going from 10 to -10
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -10, 214500m / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -10, -214500m / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -10, -214500m, -10),
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -10, -214500m - 192100m, -20),   // Going from -10 to 10
            // Initial margin requirement for ProtectiveCall with quantity 10 and -10 is 214500 and 192100 respectively
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, 10, 214500m / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, 10, -214500m / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, 10, -214500m, -10),
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, 10, -214500m - 192100m, -20),
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, -10, 192100m / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, -10, -192100m / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, -10, -192100m, -10),
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, -10, -192100m - 214500m, -20),
            // Initial margin requirement for CoveredPut with quantity 10 and -10 is 102500 and 102520 respectively
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 10, 102500m / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 10, -102500m / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 10, -102500m, -10),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 10, -102500m - 102520m, -20),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -10, 102520m / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -10, -102520m / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -10, -102520m, -10),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -10, -102520m - 102500m, -20),
            // Initial margin requirement for ProtectivePut with quantity 10 and -10 is 102520 and 102500 respectively
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, 10, 102520m / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, 10, -102520m / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, 10, -102520m, -10),
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, 10, -102520m - 102500m, -20),
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, -10, 102500m / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, -10, -102500m / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, -10, -102500m, -10),
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, -10, -102500m - 102520m, -20),
            // Initial margin requirement for BearCallSpread with quantity 10 and -10 is 10000 and 12000 respectively
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 10, 10000m / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 10, -10000m / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 10, -10000m, -10),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 10, -10000m - 12000m, -20),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -10, 12000m / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -10, -12000m / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -10, -12000m, -10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -10, -12000m - 10000m, -20).Explicit(),
            // Initial margin requirement for BearPutSpread with quantity 10 and -10 is 10 and 10000 respectively
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 10, 10m / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 10, -10m / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 10, -10m, -10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 10, -10m - 10000m, -20).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -10, 10000m / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -10, -10000m / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -10, -10000m, -10),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -10, -10000m - 10m, -20),
            // Initial margin requirement for BullCallSpread with quantity 10 and -10 is 12000 and 10000 respectively
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 10, 12000m / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 10, -12000m / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 10, -12000m, -10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 10, -12000m - 10000m, -20).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -10, 10000m / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -10, -10000m / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -10, -10000m, -10),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -10, -10000m - 12000m, -20),
            // Initial margin requirement for BullPutSpread with quantity 10 and -10 is 10000 and 10 respectively
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 10, 10000m / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 10, -10000m / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 10, -10000m, -10),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 10, -10000m - 10m, -20),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -10, 10m / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -10, -10m / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -10, -10m, -10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -10, -10m - 10000m, -20).Explicit(),
            // Initial margin requirement for Straddle with quantity 10 and -10 is 112020 and 194020 respectively
            new TestCaseData(OptionStrategyDefinitions.Straddle, 10, 112020m / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.Straddle, 10, -112020m / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.Straddle, 10, -112020m, -10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.Straddle, 10, -112020m - 194020m, -20).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.Straddle, -10, 194020m / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.Straddle, -10, -194020m / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.Straddle, -10, -194020m, -10),
            new TestCaseData(OptionStrategyDefinitions.Straddle, -10, -194020m - 112020m, -20),
            // Initial margin requirement for ShortStraddle with quantity 10 and -10 is 194020 and 112020 respectively
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, 10, 194020m / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, 10, -194020m / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, 10, -194020m, -10),
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, 10, -194020m - 112020m, -20),
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, -10, 112020m / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, -10, -112020m / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, -10, -112020m, -10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, -10, -112020m - 194020m, -20).Explicit(),
            // Initial margin requirement for Strangle with quantity 10 and -10 is 100020 and 182020 respectively
            new TestCaseData(OptionStrategyDefinitions.Strangle, 10, 100020m / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.Strangle, 10, -100020m / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.Strangle, 10, -100020m, -10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.Strangle, 10, -100020m - 182020m, -20).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.Strangle, -10, 182020m / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.Strangle, -10, -182020m / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.Strangle, -10, -182020m, -10),
            new TestCaseData(OptionStrategyDefinitions.Strangle, -10, -182020m - 100020m, -20),
            // Initial margin requirement for ShortStrangle with quantity 10 and -10 is 182020 and 100020 respectively
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, 10, 182020m / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, 10, -182020m / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, 10, -182020m, -10),
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, 10, -182020m - 100020m, -20),
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, -10, 100020m / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, -10, -100020m / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, -10, -100020m, -10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, -10, -100020m - 182020m, -20).Explicit(),
            // Initial margin requirement for ButterflyCall with quantity 10 and -10 is 4000 and 10000 respectively
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 10, 4000m / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 10, -4000m / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 10, -4000m, -10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 10, -4000m - 10000m, -20).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -10, 10000m / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -10, -10000m / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -10, -10000m, -10),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -10, -10000m - 4000m, -20),
            // Initial margin requirement for ShortButterflyCall with quantity 10 and -10 is 10000 and 4000 respectively
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 10, 10000m / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 10, -10000m / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 10, -10000m, -10),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 10, -10000m - 4000m, -20),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -10, 4000m / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -10, -4000m / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -10, -4000m, -10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -10, -4000m - 10000m, -20).Explicit(),
            // Initial margin requirement for ButterflyPut with quantity 10 and -10 is 10 and 10000 respectively
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 10, 10m / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 10, -10m / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 10, -10m, -10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 10, -10m - 10000m, -20).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -10, 10000m / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -10, -10000m / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -10, -10000m, -10),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -10, -10000m - 10m, -20),
            // Initial margin requirement for ShortButterflyPut with quantity 10 and -10 is 10000 and 10 respectively
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 10, 10000m / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 10, -10000m / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 10, -10000m, -10),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 10, -10000m - 10m, -20),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -10, 10m / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -10, -10m / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -10, -10m, -10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -10, -10m - 10000m, -20).Explicit(),
            // Initial margin requirement for CallCalendarSpread with quantity 10 and -10 is 2000 and 194000 respectively
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 10, 2000m / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 10, -2000m / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 10, -2000m, -10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 10, -2000m - 194000m, -20).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -10, 194000m / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -10, -194000m / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -10, -194000m, -10),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -10, -194000m - 2000m, -20),
            // Initial margin requirement for ShortCallCalendarSpread with quantity 10 and -10 is 194000 and 2000 respectively
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, 10, 194000m / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, 10, -194000m / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, 10, -194000m, -10),
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, 10, -194000m - 2000m, -20),
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, -10, 2000m / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, -10, -2000m / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, -10, -2000m, -10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, -10, -2000m - 194000m, -20).Explicit(),
            // Initial margin requirement for PutCalendarSpread with quantity 10 and -10 is 10 and 30020 respectively
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 10, 10m / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 10, -10m / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 10, -10m, -10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 10, -10m - 30020m, -20).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -10, 30020m / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -10, -30020m / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -10, -30020m, -10),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -10, -30020m - 10m, -20),
            // Initial margin requirement for ShortPutCalendarSpread with quantity 10 and -10 is 30020 and 10 respectively
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, 10, 30020m / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, 10, -30020m / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, 10, -30020m, -10),
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, 10, -30020m - 10m, -20),
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, -10, 10m / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, -10, -10m / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, -10, -10m, -10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, -10, -10m - 30020m, -20).Explicit(),
            // Initial margin requirement for IronCondor with quantity 10 and -10 is 10000 and 10010 respectively
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 10, 10000m / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 10, -10000m / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 10, -10000m, -10),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 10, -10000m - 10010m, -20),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -10, 10010m / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -10, -10010m / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -10, -10010m, -10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -10, -10010m - 10000m, -20).Explicit(),
        };

        [TestCaseSource(nameof(OrderQuantityForDeltaBuyingPowerTestCases))]
        public void PositionGroupOrderQuantityCalculationForDeltaBuyingPower(OptionStrategyDefinition optionStrategyDefinition,
            int initialPositionQuantity, decimal deltaBuyingPower, int expectedQuantity)
        {
            var positionGroup = SetUpOptionStrategy(optionStrategyDefinition, initialPositionQuantity);

            var initialMarginRequirement = positionGroup.BuyingPowerModel.GetInitialMarginRequirement(_portfolio, positionGroup);
            var maintenanceMargin = positionGroup.BuyingPowerModel.GetMaintenanceMargin(_portfolio, positionGroup);
            // Adjust the delta buying power:
            // GetMaximumLotsForDeltaBuyingPower will add the delta buying power to the maintenance margin and used that as a target margin,
            // but then GetMaximumLotsForTargetBuyingPower will work with initial margin requirement so we make sure the resulting quantity
            // can be ordered. In order to match this, we need to adjust the delta buying power by the difference between the initial margin
            // requirement  and maintenance margin.
            deltaBuyingPower += initialMarginRequirement - maintenanceMargin;

            if (expectedQuantity != -Math.Abs(initialPositionQuantity)) // Not liquidating
            {
                // Add a small buffer to avoid rounding errors
                var usedMargin = _portfolio.TotalMarginUsed;
                deltaBuyingPower *= deltaBuyingPower > 0 || Math.Abs(deltaBuyingPower) > usedMargin ? 1.00001m : 0.99999m;
            }

            var result = positionGroup.BuyingPowerModel.GetMaximumLotsForDeltaBuyingPower(new GetMaximumLotsForDeltaBuyingPowerParameters(
                _portfolio, positionGroup, deltaBuyingPower, minimumOrderMarginPortfolioPercentage: 0));

            Assert.IsFalse(result.IsError);
            Assert.AreEqual(expectedQuantity, result.NumberOfLots);

            // Expected quantity is 0 for test cases where no buying power is used,
            // it should return 0 regardless of the delta, with the proper message
            if (expectedQuantity == 0)
            {
                Assert.AreEqual(Messages.PositionGroupBuyingPowerModel.DeltaCannotBeApplied, result.Reason);
            }
        }
        private static TestCaseData[] OrderQuantityForDeltaBuyingPowerWithCustomPositionGroupParameterTestCases()
        {
            return OrderQuantityForDeltaBuyingPowerTestCases
                .SelectMany(testCaseData =>
                {
                    var testCases = new List<TestCaseData>(2);
                    foreach (var referencePositionSideSign in new[] { +1, -1 })
                    {
                        var args = testCaseData.OriginalArguments.ToList();
                        args.Add(referencePositionSideSign);
                        var data = new TestCaseData(args.ToArray());

                        if (testCaseData.RunState == NUnit.Framework.Interfaces.RunState.Explicit)
                        {
                            data.Explicit();
                        }

                        testCases.Add(data);
                    }

                    return testCases;
                })
                .ToArray();
        }

        /// <summary>
        /// Tests <see cref="OptionStrategyPositionGroupBuyingPowerModel.GetMaximumLotsForDeltaBuyingPower"/> with reference position group in
        /// same and opposite side of the existing position group.
        ///
        /// The reference position group is not necessarily in the same side as the position group in the portfolio, it could be the inverted.
        /// So the consumer needs the result relative to that position group instead of the one being held.
        /// </summary>
        [TestCaseSource(nameof(OrderQuantityForDeltaBuyingPowerWithCustomPositionGroupParameterTestCases))]
        public void PositionGroupOrderQuantityCalculationForDeltaBuyingPowerWithCustomPositionGroupParameter(
            OptionStrategyDefinition optionStrategyDefinition, int initialPositionQuantity, decimal deltaBuyingPower, int expectedQuantity,
            int referencePositionSideSign)
        {
            var currentPositionGroup = SetUpOptionStrategy(optionStrategyDefinition, initialPositionQuantity);

            var initialMarginRequirement = currentPositionGroup.BuyingPowerModel.GetInitialMarginRequirement(_portfolio, currentPositionGroup);
            var maintenanceMargin = currentPositionGroup.BuyingPowerModel.GetMaintenanceMargin(_portfolio, currentPositionGroup);
            // Adjust the delta buying power:
            // GetMaximumLotsForDeltaBuyingPower will add the delta buying power to the maintenance margin and used that as a target margin,
            // but then GetMaximumLotsForTargetBuyingPower will work with initial margin requirement so we make sure the resulting quantity
            // can be ordered. In order to match this, we need to adjust the delta buying power by the difference between the initial margin
            // requirement  and maintenance margin.
            deltaBuyingPower += initialMarginRequirement - maintenanceMargin;

            if (expectedQuantity != -Math.Abs(initialPositionQuantity)) // Not liquidating
            {
                // Add a small buffer to avoid rounding errors
                var usedMargin = _portfolio.TotalMarginUsed;
                deltaBuyingPower *= deltaBuyingPower > 0 || Math.Abs(deltaBuyingPower) > usedMargin ? 1.00001m : 0.99999m;
            }

            // Using a reference position with in the same position side as the one in the portfolio
            var referencePositionGroup = new PositionGroup(currentPositionGroup.BuyingPowerModel, 1,
                currentPositionGroup.Select(position => new Position(position.Symbol,
                    referencePositionSideSign * Math.Sign(position.Quantity) * position.UnitQuantity, position.UnitQuantity)).ToArray());

            var result = currentPositionGroup.BuyingPowerModel.GetMaximumLotsForDeltaBuyingPower(new GetMaximumLotsForDeltaBuyingPowerParameters(
                _portfolio, referencePositionGroup, referencePositionSideSign * deltaBuyingPower, minimumOrderMarginPortfolioPercentage: 0));

            Assert.IsFalse(result.IsError);
            Assert.AreEqual(referencePositionSideSign * expectedQuantity, result.NumberOfLots);

            // Expected quantity is 0 for test cases where no buying power is used,
            // it should return 0 regardless of the delta, with the proper message
            if (expectedQuantity == 0)
            {
                Assert.AreEqual(Messages.PositionGroupBuyingPowerModel.DeltaCannotBeApplied, result.Reason);
            }
        }

        /// <remarks>
        /// TODO: Revisit the explicit test cases when we can take into account premium for strategies with zero margin.
        /// </remarks>
        // option strategy definition, initial position quantity, target buying power percent, expected quantity
        private static readonly TestCaseData[] OrderQuantityForTargetBuyingPowerTestCases = new[]
        {
            // Initial margin requirement for CoveredCall with quantity 10 and -10 is 192100m and 214500m respectively
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 10, 192100m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 10, 192100m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 10, -214500m, -20),  // Going from 10 to -10
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -10, 214500m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -10, 214500m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -10, -192100m, -20),    // Going from -10 to 10
            // Initial margin requirement for ProtectiveCall with quantity 10 and -10 is 214500m and 192100m respectively
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, 10, 214500m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, 10, 214500m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, 10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, 10, -192100m, -20),
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, -10, 192100m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, -10, 192100m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, -10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, -10, -214500m, -20),
            // Initial margin requirement for CoveredPut with quantity 10 and -10 is 102500m and 102520m respectively
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 10, 102500m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 10, 102500m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 10, -102520m, -20),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -10, 102520m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -10, 102520m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -10, -102500m, -20),
            // Initial margin requirement for ProtectivePut with quantity 10 and -10 is 102520m and 102500m respectively
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, 10, 102520m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, 10, 102520m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, 10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, 10, -102500m, -20),
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, -10, 102500m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, -10, 102500m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, -10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, -10, -102520m, -20),
            // Initial margin requirement for BearCallSpread with quantity 10 and -10 is 10000 and 12000 respectively
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 10, 10000m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 10, 10000m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 10, -12000m, -20),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -10, 12000m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -10, 12000m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -10, -10000m, -20),
            // Initial margin requirement for BearPutSpread with quantity 10 and -10 is 10 and 10000 respectively
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 10, 10m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 10, 10m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 10, -10000m, -20),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -10, 10000m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -10, 10000m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -10, -10m, -20),
            // Initial margin requirement for BullCallSpread with quantity 10 and -10 is 12000 and 10000 respectively
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 10, 12000m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 10, 12000m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 10, -10000m, -20),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -10, 10000m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -10, 10000m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -10, -12000m, -20),
            // Initial margin requirement for BullPutSpread with quantity 10 and -10 is 10000 and 10 respectively
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 10, 10000m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 10, 10000m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 10, -10m, -20),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -10, 10m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -10, 10m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -10, -10000m, -20),
            // Initial margin requirement for Straddle with quantity 10 and -10 is 112020 and 194020 respectively
            new TestCaseData(OptionStrategyDefinitions.Straddle, 10, 112020m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.Straddle, 10, 112020m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.Straddle, 10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.Straddle, 10, -194020m, -20),
            new TestCaseData(OptionStrategyDefinitions.Straddle, -10, 194020m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.Straddle, -10, 194020m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.Straddle, -10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.Straddle, -10, -112020m, -20),
            // Initial margin requirement for ShortStraddle with quantity 10 and -10 is 194020 and 112020 respectively
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, 10, 194020m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, 10, 194020m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, 10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, 10, -112020m, -20),
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, -10, 112020m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, -10, 112020m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, -10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, -10, -194020m, -20),
            // Initial margin requirement for Strangle with quantity 10 and -10 is 100020 and 182020 respectively
            new TestCaseData(OptionStrategyDefinitions.Strangle, 10, 100020m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.Strangle, 10, 100020m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.Strangle, 10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.Strangle, 10, -182020m, -20),
            new TestCaseData(OptionStrategyDefinitions.Strangle, -10, 182020m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.Strangle, -10, 182020m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.Strangle, -10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.Strangle, -10, -100020m, -20),
            // Initial margin requirement for ShortStrangle with quantity 10 and -10 is 182020 and 100020 respectively
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, 10, 182020m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, 10, 182020m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, 10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, 10, -100020m, -20),
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, -10, 100020m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, -10, 100020m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, -10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, -10, -182020m, -20),
            // Initial margin requirement for ButterflyCall with quantity 10 and -10 is 4000 and 10000 respectively
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 10, 4000m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 10, 4000m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 10, -10000m, -20),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -10, 10000m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -10, 10000m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -10, -4000m, -20),
            // Initial margin requirement for ShortButterflyCall with quantity 10 and -10 is 10000 and 4000 respectively
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 10, 10000m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 10, 10000m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 10, -4000m, -20),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -10, 4000m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -10, 4000m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -10, -10000m, -20),
            // Initial margin requirement for ButterflyPut with quantity 10 and -10 is 10 and 10000 respectively
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 10, 10m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 10, 10m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 10, -10000m, -20),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -10, 10000m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -10, 10000m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -10, -10m, -20),
            // Initial margin requirement for ShortButterflyPut with quantity 10 and -10 is 10000 and 10 respectively
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 10, 10000m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 10, 10000m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 10, -10m, -20),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -10, 10m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -10, 10m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -10, -10000m, -20),
            // Initial margin requirement for CallCalendarSpread with quantity 10 and -10 is 2000 and 194000 respectively
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 10, 2000m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 10, 2000m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 10, -194000m, -20),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -10, 194000m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -10, 194000m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -10, -2000m, -20),
            // Initial margin requirement for ShortCallCalendarSpread with quantity 10 and -10 is 194000 and 2000 respectively
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, 10, 194000m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, 10, 194000m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, 10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, 10, -2000m, -20),
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, -10, 2000m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, -10, 2000m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, -10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, -10, -194000m, -20),
            // Initial margin requirement for PutCalendarSpread with quantity 10 and -10 is 10 and 30020 respectively
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 10, 10m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 10, 10m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 10, -30020m, -20),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -10, 30020m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -10, 30020m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -10, -10m, -20),
            // Initial margin requirement for ShortPutCalendarSpread with quantity 10 and -10 is 30020 and 10 respectively
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, 10, 30020m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, 10, 30020m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, 10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, 10, -10m, -20),
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, -10, 10m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, -10, 10m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, -10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, -10, -30020m, -20),
            // Initial margin requirement for IronCondor with quantity 10 and -10 is 10000 and 10010 respectively
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 10, 10000m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 10, 10000m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 10, -10010m, -20),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -10, 10010m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -10, 10010m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -10, 0m, -10),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -10, -10000m, -20),
        };

        [TestCaseSource(nameof(OrderQuantityForTargetBuyingPowerTestCases))]
        public void PositionGroupOrderQuantityCalculationForTargetBuyingPower(OptionStrategyDefinition optionStrategyDefinition,
            int initialPositionQuantity, decimal targetBuyingPower, int expectedQuantity)
        {
            var positionGroup = SetUpOptionStrategy(optionStrategyDefinition, initialPositionQuantity);

            targetBuyingPower *= 1.0001m; // Add a small buffer to avoid rounding errors
            var targetBuyingPowerPercent = targetBuyingPower / _portfolio.TotalPortfolioValue;

            var quantity = positionGroup.BuyingPowerModel.GetMaximumLotsForTargetBuyingPower(new GetMaximumLotsForTargetBuyingPowerParameters(
                _portfolio, positionGroup, targetBuyingPowerPercent, minimumOrderMarginPortfolioPercentage: 0)).NumberOfLots;

            Assert.AreEqual(expectedQuantity, quantity);
        }

        private static TestCaseData[] OrderQuantityForTargetBuyingPowerWithCustomPositionGroupParameterTestCases()
        {
            return OrderQuantityForTargetBuyingPowerTestCases
                .SelectMany(testCaseData =>
                {
                    var testCases = new List<TestCaseData>(2);
                    foreach (var referencePositionSideSign in new[] { +1, -1 })
                    {
                        var args = testCaseData.OriginalArguments.ToList();
                        args.Add(referencePositionSideSign);
                        var data = new TestCaseData(args.ToArray());

                        if (testCaseData.RunState == NUnit.Framework.Interfaces.RunState.Explicit)
                        {
                            data.Explicit();
                        }

                        testCases.Add(data);
                    }

                    return testCases;
                })
                .ToArray();
        }

        /// <summary>
        /// Tests <see cref="OptionStrategyPositionGroupBuyingPowerModel.GetMaximumLotsForTargetBuyingPower"/> with reference position group in
        /// same and opposite side of the existing position group.
        ///
        /// The reference position group is not necessarily in the same side as the position group in the portfolio, it could be the inverted.
        /// So the consumer needs the result relative to that position group instead of the one being held.
        /// </summary>
        [TestCaseSource(nameof(OrderQuantityForTargetBuyingPowerWithCustomPositionGroupParameterTestCases))]
        public void PositionGroupOrderQuantityCalculationForTargetBuyingPowerWithCustomPositionGroupParameter(
            OptionStrategyDefinition optionStrategyDefinition, int initialPositionQuantity, decimal targetBuyingPower, int expectedQuantity,
            int referenceGroupSideSign)
        {
            var currentPositionGroup = SetUpOptionStrategy(optionStrategyDefinition, initialPositionQuantity);

            targetBuyingPower *= 1.0001m; // Add a small buffer to avoid rounding errors
            var targetBuyingPowerPercent = targetBuyingPower / _portfolio.TotalPortfolioValue;

            var referencePositionGroup = new PositionGroup(currentPositionGroup.BuyingPowerModel, 1,
                currentPositionGroup.Select(position => new Position(position.Symbol,
                    referenceGroupSideSign * Math.Sign(position.Quantity) * position.UnitQuantity, position.UnitQuantity)).ToArray());

            var quantity = currentPositionGroup.BuyingPowerModel.GetMaximumLotsForTargetBuyingPower(new GetMaximumLotsForTargetBuyingPowerParameters(
                _portfolio, referencePositionGroup, referenceGroupSideSign * targetBuyingPowerPercent,
                minimumOrderMarginPortfolioPercentage: 0)).NumberOfLots;
            Assert.AreEqual(referenceGroupSideSign * expectedQuantity, quantity);
        }

        /// <summary>
        /// Starting from the "initial position quantity", we want to get to a new position with "initial position quantity" + "order quantity".
        ///
        /// These test cases assume a starting margin/cash of 1M.
        /// The expected buying power test case parameter is left explicitly calculated as a constant in order to make it clearer.
        /// </summary>
        private static readonly TestCaseData[] PositionGroupBuyingPowerTestCases = new[]
        {
            // Going from 10 to 11.
            // Expected buying power for going longer:
            // (1000000 initial cash - 185000 used margin) margin remaining
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 10, 1, 1000000m - 185000m),
            // Going from 10 to 9.
            // Expected buying power for reducing, closing or going short:
            // (1000000 initial cash - 185000 used margin) margin remaining + 185000 used margin + 192100 initial margin requirement
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 10, -1, (1000000m - 185000m) + 185000m + 192100m),
            // Going from 10 to 0.
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 10, -10, (1000000m - 185000m) + 185000m + 192100m),
            // Going from 10 to -10.
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 10, -20, (1000000m - 185000m) + 185000m + 192100m),
            // Expected buying power for going shorter:
            // (1000000 initial cash - 30000 used margin) margin remaining
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -10, -1, 1000000m - 30000m),
            // Expected buying power for reducing, closing or going long:
            // (1000000 initial cash - 30000 used margin) margin remaining + 30000 used margin + 102500 initial margin requirement
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -10, 1, (1000000m - 30000m) + 30000m + 214500m),
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -10, 10, (1000000m - 30000m) + 30000m + 214500m),
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -10, 20, (1000000m - 30000m) + 30000m + 214500m),
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, 10, +1, 1000000m - 30000m),
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, 10, -1, (1000000m - 30000m) + 30000m + 214500m),
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, 10, -10, (1000000m - 30000m) + 30000m + 214500m),
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, 10, -20, (1000000m - 30000m) + 30000m + 214500m),
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, -10, -1, 1000000m - 185000m),
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, -10, 1, (1000000m - 185000m) + 185000m + 192100m),
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, -10, 10, (1000000m - 185000m) + 185000m + 192100m),
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, -10, 20, (1000000m - 185000m) + 185000m + 192100m),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 10, 1, 1000000m - 102500m),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 10, -1, (1000000m - 102500m) + 102500m + 102500m),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 10, -10, (1000000m - 102500m) + 102500m + 102500m),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 10, -20, (1000000m - 102500m) + 102500m + 102500m),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -10, -1, 1000000m - 102500m),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -10, 1, (1000000m - 102500m) + 102500m + 102520m),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -10, 10, (1000000m - 102500m) + 102500m + 102520m),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -10, 20, (1000000m - 102500m) + 102500m + 102520m),
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, 10, 1, 1000000m - 102500m),
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, 10, -1, (1000000m - 102500m) + 102500m + 102520m),
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, 10, -10, (1000000m - 102500m) + 102500m + 102520m),
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, 10, -20, (1000000m - 102500m) + 102500m + 102520m),
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, -10, -1, 1000000m - 102500m),
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, -10, 1, (1000000m - 102500m) + 102500m + 102500m),
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, -10, 10, (1000000m - 102500m) + 102500m + 102500m),
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, -10, 20, (1000000m - 102500m) + 102500m + 102500m),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 10, 1, 1000000m - 10000m),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 10, -1, (1000000m - 10000m) + 10000m + 10000m),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 10, -10, (1000000m - 10000m) + 10000m + 10000m),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 10, -20, (1000000m - 10000m) + 10000m + 10000m),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -10, -1, 1000000m - 0),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -10, 1, (1000000m - 0) + 0 + 12000m),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -10, 10, (1000000m - 0) + 0 + 12000m),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -10, 20, (1000000m - 0) + 0 + 12000m),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 10, 1, 1000000m - 0),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 10, -1, (1000000m - 0) + 0 + 10m),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 10, -10, (1000000m - 0) + 0 + 10m),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 10, -20, (1000000m - 0) + 0 + 10m),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -10, -1, 1000000m - 10000m),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -10, 1, (1000000m - 10000m) + 10000m + 10000m),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -10, 10, (1000000m - 10000m) + 10000m + 10000m),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -10, 20, (1000000m - 10000m) + 10000m + 10000m),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 10, 1, 1000000m - 0),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 10, -1, (1000000m - 0) + 0 + 12000m),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 10, -10, (1000000m - 0) + 0 + 12000m),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 10, -20, (1000000m - 0) + 0 + 12000m),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -10, -1, 1000000m - 10000m),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -10, 1, (1000000m - 10000m) + 10000m + 10000m),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -10, 10, (1000000m - 10000m) + 10000m + 10000m),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -10, 20, (1000000m - 10000m) + 10000m + 10000m),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 10, 1, 1000000m - 10000m),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 10, -1, (1000000m - 10000m) + 10000m + 10000m),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 10, -10, (1000000m - 10000m) + 10000m + 10000m),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 10, -20, (1000000m - 10000m) + 10000m + 10000m),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -10, -1, 1000000m - 0),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -10, 1, (1000000m - 0) + 0 + 10m),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -10, 10, (1000000m - 0) + 0 + 10m),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -10, 20, (1000000m - 0) + 0 + 10m),
            new TestCaseData(OptionStrategyDefinitions.Straddle, 10, 1, 1000000m - 0m),
            new TestCaseData(OptionStrategyDefinitions.Straddle, 10, -1, (1000000m - 0m) + 0m + 112020m),
            new TestCaseData(OptionStrategyDefinitions.Straddle, 10, -10, (1000000m - 0m) + 0m + 112020m),
            new TestCaseData(OptionStrategyDefinitions.Straddle, 10, -20, (1000000m - 0m) + 0m + 112020m),
            new TestCaseData(OptionStrategyDefinitions.Straddle, -10, -1, 1000000m - 194020m),
            new TestCaseData(OptionStrategyDefinitions.Straddle, -10, 1, (1000000m - 194020m) + 194020m + 194020m),
            new TestCaseData(OptionStrategyDefinitions.Straddle, -10, 10, (1000000m - 194020m) + 194020m + 194020m),
            new TestCaseData(OptionStrategyDefinitions.Straddle, -10, 20, (1000000m - 194020m) + 194020m + 194020m),
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, 10, +1, 1000000m - 194020m),
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, 10, -1, (1000000m - 194020m) + 194020m + 194020m),
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, 10, -10, (1000000m - 194020m) + 194020m + 194020m),
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, 10, -20, (1000000m - 194020m) + 194020m + 194020m),
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, -10, -1, 1000000m - 0m),
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, -10, 1, (1000000m - 0m) + 0m + 112020m),
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, -10, 10, (1000000m - 0m) + 0m + 112020m),
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, -10, 20, (1000000m - 0m) + 0m + 112020m),
            new TestCaseData(OptionStrategyDefinitions.Strangle, 10, 1, 1000000m - 0m),
            new TestCaseData(OptionStrategyDefinitions.Strangle, 10, -1, (1000000m - 0m) + 0m + 100020m),
            new TestCaseData(OptionStrategyDefinitions.Strangle, 10, -10, (1000000m - 0m) + 0m + 100020m),
            new TestCaseData(OptionStrategyDefinitions.Strangle, 10, -20, (1000000m - 0m) + 0m + 100020m),
            new TestCaseData(OptionStrategyDefinitions.Strangle, -10, -1, 1000000m - 182020m),
            new TestCaseData(OptionStrategyDefinitions.Strangle, -10, 1, (1000000m - 182020m) + 182020m + 182020m),
            new TestCaseData(OptionStrategyDefinitions.Strangle, -10, 10, (1000000m - 182020m) + 182020m + 182020m),
            new TestCaseData(OptionStrategyDefinitions.Strangle, -10, 20, (1000000m - 182020m) + 182020m + 182020m),
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, 10, 1, 1000000m - 182020m),
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, 10, -1, (1000000m - 182020m) + 182020m + 182020m),
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, 10, -10, (1000000m - 182020m) + 182020m + 182020m),
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, 10, -20, (1000000m - 182020m) + 182020m + 182020m),
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, -10, -1, 1000000m - 0m),
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, -10, 1, (1000000m - 0m) + 0m + 100020m),
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, -10, 10, (1000000m - 0m) + 0m + 100020m),
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, -10, 20, (1000000m - 0m) + 0m + 100020m),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 10, 1, 1000000m - 0m),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 10, -1, (1000000m - 0m) + 0m + 4000m),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 10, -10, (1000000m - 0m) + 0m + 4000m),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 10, -20, (1000000m - 0m) + 0m + 4000m),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -10, -1, 1000000m - 10000m),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -10, 1, (1000000m - 10000m) + 10000m + 10000m),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -10, 10, (1000000m - 10000m) + 10000m + 10000m),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -10, 20, (1000000m - 10000m) + 10000m + 10000m),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 10, 1, 1000000m - 10000m),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 10, -1, (1000000m - 10000m) + 10000m + 10000m),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 10, -10, (1000000m - 10000m) + 10000m + 10000m),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 10, -20, (1000000m - 10000m) + 10000m + 10000m),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -10, -1, 1000000m - 0),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -10, 1, (1000000m - 0) + 0 + 4000m),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -10, 10, (1000000m - 0) + 0 + 4000m),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -10, 20, (1000000m - 0) + 0 + 4000m),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 10, 1, 1000000m - 0),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 10, -1, (1000000m - 0) + 0 + 10m),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 10, -10, (1000000m - 0) + 0 + 10m),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 10, -20, (1000000m - 0) + 0 + 10m),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -10, -1, 1000000m - 10000m),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -10, 1, (1000000m - 10000m) + 10000m + 10000m),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -10, 10, (1000000m - 10000m) + 10000m + 10000m),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -10, 20, (1000000m - 10000m) + 10000m + 10000m),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 10, 1, 1000000m - 10000m),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 10, -1, (1000000m - 10000m) + 10000m + 10000m),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 10, -10, (1000000m - 10000m) + 10000m + 10000m),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 10, -20, (1000000m - 10000m) + 10000m + 10000m),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -10, -1, 1000000m - 0),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -10, 1, (1000000m - 0) + 0 + 10m),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -10, 10, (1000000m - 0) + 0 + 10m),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -10, 20, (1000000m - 0) + 0 + 10m),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 10, 1, 1000000m - 0),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 10, -1, (1000000m - 0) + 0 + 2000m),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 10, -10, (1000000m - 0) + 0 + 2000m),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 10, -20, (1000000m - 0) + 0 + 2000m),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -10, -1, 1000000m - 194000m),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -10, 1, (1000000m - 194000m) + 194000m + 194000m),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -10, 10, (1000000m - 194000m) + 194000m + 194000m),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -10, 20, (1000000m - 194000m) + 194000m + 194000m),
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, 10, 1, 1000000m - 194000m),
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, 10, -1, (1000000m - 194000m) + 194000m + 194000m),
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, 10, -10, (1000000m - 194000m) + 194000m + 194000m),
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, 10, -20, (1000000m - 194000m) + 194000m + 194000m),
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, -10, -1, 1000000m - 0),
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, -10, 1, (1000000m - 0) + 0 + 2000m),
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, -10, 10, (1000000m - 0) + 0 + 2000m),
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, -10, 20, (1000000m - 0) + 0 + 2000m),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 10, 1, 1000000m - 0),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 10, -1, (1000000m - 0) + 0 + 10m),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 10, -10, (1000000m - 0) + 0 + 10m),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 10, -20, (1000000m - 0) + 0 + 10m),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -10, -1, 1000000m - 30020m),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -10, 1, (1000000m - 30020m) + 30020m + 30020m),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -10, 10, (1000000m - 30020m) + 30020m + 30020m),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -10, 20, (1000000m - 30020m) + 30020m + 30020m),
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, 10, 1, 1000000m - 30020m),
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, 10, -1, (1000000m - 30020m) + 30020m + 30020m),
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, 10, -10, (1000000m - 30020m) + 30020m + 30020m),
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, 10, -20, (1000000m - 30020m) + 30020m + 30020m),
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, -10, -1, 1000000m - 0),
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, -10, 1, (1000000m - 0) + 0 + 10m),
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, -10, 10, (1000000m - 0) + 0 + 10m),
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, -10, 20, (1000000m - 0) + 0 + 10m),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 10, 1, 1000000m - 10000m),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 10, -1, (1000000m - 10000m) + 10000m + 10000m),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 10, -10, (1000000m - 10000m) + 10000m + 10000m),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 10, -20, (1000000m - 10000m) + 10000m + 10000m),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -10, -1, 1000000m - 0),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -10, 1, (1000000m - 0) + 0 + 10010m),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -10, 10, (1000000m - 0) + 0 + 10010m),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -10, 20, (1000000m - 0) + 0 + 10010m),
        };

        [TestCaseSource(nameof(PositionGroupBuyingPowerTestCases))]
        public void BuyingPowerForPositionGroupCalculation(OptionStrategyDefinition optionStrategyDefinition, int initialPositionQuantity,
            int orderQuantity, decimal expectedBuyingPower)
        {
            var initialMargin = _portfolio.MarginRemaining;
            var initialPositionGroup = SetUpOptionStrategy(optionStrategyDefinition, initialPositionQuantity, updateCashbook: false);

            var finalQuantity = initialPositionQuantity + orderQuantity;
            IPositionGroup finalPositionGroup = null;

            if (finalQuantity != 0)
            {
                finalPositionGroup = initialPositionGroup.WithQuantity(
                    Math.Sign(initialPositionQuantity) == Math.Sign(finalQuantity)
                        // Positive to create a group in the same "side" of the initial position, without changing position signs
                        ? Math.Abs(finalQuantity)
                        // Negative to create a group in the opposite "side" of the initial position, changing position signs
                        : -Math.Abs(finalQuantity),
                    _portfolio.Positions);
            }
            else
            {
                finalPositionGroup = new PositionGroup(initialPositionGroup.Key, 0, new Dictionary<Symbol, IPosition>());
            }

            Assert.AreEqual(Math.Abs(finalQuantity), finalPositionGroup.Quantity);
            // Final position is in the same "side", so the signs of the positions should be the same
            if (Math.Sign(initialPositionQuantity) == Math.Sign(finalQuantity))
            {
                Assert.IsTrue(finalPositionGroup.All(finalPosition =>
                    Math.Sign(finalPosition.Quantity) == Math.Sign(initialPositionGroup.GetPosition(finalPosition.Symbol).Quantity)));
            }
            // Final position is in the opposite "side", so the signs of the positions should be the opposite
            else if (Math.Sign(initialPositionQuantity) == -Math.Sign(finalQuantity))
            {
                Assert.IsTrue(finalPositionGroup.All(finalPosition =>
                    Math.Sign(finalPosition.Quantity) == -Math.Sign(initialPositionGroup.GetPosition(finalPosition.Symbol).Quantity)));
            }

            var buyingPower = finalPositionGroup.BuyingPowerModel.GetPositionGroupBuyingPower(new PositionGroupBuyingPowerParameters(
                _portfolio,
                finalPositionGroup,
                // The order direction is irrelevant for the position group buying power calculation
                OrderDirection.Buy));

            Assert.That(buyingPower.Value, Is.EqualTo(expectedBuyingPower).Within(1e-18));
        }

        private static readonly TestCaseData[] ReservedBuyingPowerImpactTestCases = new[]
        {
            // option strategy definition, initial position quantity, new position quantity
            // Starting from the "initial position quantity", we want to get the buying power available for an order that would get us to
            // the "new position quantity" (if we don't take into account the initial position).
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 10, 1), // Going from 10 to 11
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 10, -1), // Going from 10 to 9
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 10, -10), // Going from 10 to 0
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 10, -20), // Going from 10 to -10
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.ProtectiveCall, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.ProtectivePut, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.Straddle, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.Straddle, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.Straddle, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.Straddle, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.Straddle, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.Straddle, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.Straddle, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.Straddle, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.ShortStraddle, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.Strangle, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.Strangle, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.Strangle, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.Strangle, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.Strangle, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.Strangle, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.Strangle, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.Strangle, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.ShortStrangle, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.ShortCallCalendarSpread, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.ShortPutCalendarSpread, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -10, 20),
        };

        [TestCaseSource(nameof(ReservedBuyingPowerImpactTestCases))]
        public void ReservedBuyingPowerImpactCalculation(OptionStrategyDefinition optionStrategyDefinition, int initialPositionQuantity,
            int newGroupQuantity)
        {
            var initialMargin = _portfolio.MarginRemaining;
            var initialPositionGroup = SetUpOptionStrategy(optionStrategyDefinition, initialPositionQuantity);

            var finalQuantity = initialPositionQuantity + newGroupQuantity;
            var sign = Math.Sign(finalQuantity) == Math.Sign(initialPositionQuantity) ? 1 : -1;
            var finalPositionGroup = finalQuantity != 0
                ? initialPositionGroup.WithQuantity(sign * Math.Abs(finalQuantity), _portfolio.Positions)
                : PositionGroup.Empty(null);

            var buyingPowerImpact = initialPositionGroup.BuyingPowerModel.GetReservedBuyingPowerImpact(new ReservedBuyingPowerImpactParameters(_portfolio,
                finalPositionGroup, GetPositionGroupOrders(initialPositionGroup, initialPositionQuantity, newGroupQuantity)));

            var usedMargin = initialPositionGroup.BuyingPowerModel.GetReservedBuyingPowerForPositionGroup(
                new ReservedBuyingPowerForPositionGroupParameters(_portfolio, initialPositionGroup)).AbsoluteUsedBuyingPower;

            foreach (var contemplatedChangePosition in buyingPowerImpact.ContemplatedChanges)
            {
                var position = finalPositionGroup.SingleOrDefault(p => contemplatedChangePosition.Symbol == p.Symbol);
                Assert.IsNotNull(position);
                Assert.AreEqual(position.Quantity, contemplatedChangePosition.Quantity);
            }

            var newGroupInitialMarginRequirement = finalQuantity != 0
                ? finalPositionGroup.BuyingPowerModel.GetInitialMarginRequirement(_portfolio, finalPositionGroup)
                : 0m;
            var expectedDelta = newGroupInitialMarginRequirement - usedMargin;
            Assert.That(buyingPowerImpact.Delta, Is.EqualTo(expectedDelta).Within(1e-18));
            Assert.That(buyingPowerImpact.Current, Is.EqualTo(usedMargin).Within(1e-18));
            Assert.That(buyingPowerImpact.Contemplated, Is.EqualTo(newGroupInitialMarginRequirement).Within(1e-18));
        }

        private List<Order> GetStrategyOrders(decimal quantity)
        {
            var groupOrderManager = new GroupOrderManager(1, 2, quantity);
            return new List<Order>()
            {
                Order.CreateOrder(new SubmitOrderRequest(
                    OrderType.ComboMarket,
                    _callOption.Type,
                    _callOption.Symbol,
                    1m.GetOrderLegGroupQuantity(groupOrderManager),
                    0,
                    0,
                    _algorithm.Time,
                    "",
                    groupOrderManager: groupOrderManager)),
                Order.CreateOrder(new SubmitOrderRequest(
                    OrderType.ComboMarket,
                    _putOption.Type,
                    _putOption.Symbol,
                    1m.GetOrderLegGroupQuantity(groupOrderManager),
                    0,
                    0,
                    _algorithm.Time,
                    "",
                    groupOrderManager: groupOrderManager))
            };
        }

        private void SetUpOptionStrategy(int initialHoldingsQuantity)
        {
            const decimal price = 1.5m;
            const decimal underlyingPrice = 300m;

            _equity.SetMarketPrice(new Tick { Value = underlyingPrice });
            _callOption.SetMarketPrice(new Tick { Value = price });
            _putOption.SetMarketPrice(new Tick { Value = price });

            _callOption.Holdings.SetHoldings(1m, initialHoldingsQuantity);
            _putOption.Holdings.SetHoldings(1m, initialHoldingsQuantity);

            Assert.AreEqual(1, _portfolio.Positions.Groups.Count);

            var positionGroup = _portfolio.Positions.Groups.First();
            Assert.AreEqual(initialHoldingsQuantity < 0 ? OptionStrategyDefinitions.ShortStraddle.Name : OptionStrategyDefinitions.Straddle.Name,
                positionGroup.BuyingPowerModel.ToString());

            var callOptionPosition = positionGroup.Positions.Single(x => x.Symbol == _callOption.Symbol);
            Assert.AreEqual(initialHoldingsQuantity, callOptionPosition.Quantity);

            var putOptionPosition = positionGroup.Positions.Single(x => x.Symbol == _putOption.Symbol);
            Assert.AreEqual(initialHoldingsQuantity, putOptionPosition.Quantity);
        }

        private void ComputeAndAssertQuantityForDeltaBuyingPower(IPositionGroup positionGroup, decimal expectedQuantity, decimal deltaBuyingPower)
        {
            var quantity = positionGroup.BuyingPowerModel.GetMaximumLotsForDeltaBuyingPower(new GetMaximumLotsForDeltaBuyingPowerParameters(
                _portfolio, positionGroup, deltaBuyingPower, minimumOrderMarginPortfolioPercentage: 0)).NumberOfLots;

            Assert.AreEqual(expectedQuantity, quantity);
        }

        private List<Order> GetPositionGroupOrders(IPositionGroup positionGroup, decimal initialPositionGroupQuantity, decimal quantity)
        {
            var groupOrderManager = new GroupOrderManager(1, positionGroup.Count, quantity);
            return positionGroup.Positions.Select(position => Order.CreateOrder(new SubmitOrderRequest(
                OrderType.ComboMarket,
                position.Symbol.SecurityType,
                position.Symbol,
                (position.Quantity / initialPositionGroupQuantity).GetOrderLegGroupQuantity(groupOrderManager),
                0,
                0,
                _algorithm.Time,
                "",
                groupOrderManager: groupOrderManager))).ToList();
        }

        private IPositionGroup SetUpOptionStrategy(OptionStrategyDefinition optionStrategyDefinition, int initialHoldingsQuantity, int? strike = null,
            bool updateCashbook = true)
        {
            if (initialHoldingsQuantity == 0)
            {
                var group = SetUpOptionStrategy(optionStrategyDefinition, 1, updateCashbook: false);
                foreach (var position in group.Positions)
                {
                    var security = _algorithm.Securities[position.Symbol];
                    security.Holdings.SetHoldings(0, 0);
                }
                Assert.AreEqual(0, _portfolio.Positions.Groups.Count);

                return group;
            }

            var may172023 = new DateTime(2023, 05, 17);
            var may192023 = new DateTime(2023, 05, 19);

            var spyMay19_300Call = _algorithm.AddOptionContract(Symbols.CreateOptionSymbol("SPY", OptionRight.Call, 300, may192023));
            spyMay19_300Call.SetMarketPrice(new Tick { Value = 112m });
            var spyMay19_310Call = _algorithm.AddOptionContract(Symbols.CreateOptionSymbol("SPY", OptionRight.Call, 310, may192023));
            spyMay19_310Call.SetMarketPrice(new Tick { Value = 100m });
            var spyMay19_320Call = _algorithm.AddOptionContract(Symbols.CreateOptionSymbol("SPY", OptionRight.Call, 320, may192023));
            spyMay19_320Call.SetMarketPrice(new Tick { Value = 92m });
            var spyMay19_330Call = _algorithm.AddOptionContract(Symbols.CreateOptionSymbol("SPY", OptionRight.Call, 330, may192023));
            spyMay19_330Call.SetMarketPrice(new Tick { Value = 82m });

            var spyMay17_200Call = _algorithm.AddOptionContract(Symbols.CreateOptionSymbol("SPY", OptionRight.Call, 200, may172023));
            spyMay17_200Call.SetMarketPrice(new Tick { Value = 220m });
            var spyMay17_400Call = _algorithm.AddOptionContract(Symbols.CreateOptionSymbol("SPY", OptionRight.Call, 400, may172023));
            spyMay17_400Call.SetMarketPrice(new Tick { Value = 28m });
            var spyMay17_300Call = _algorithm.AddOptionContract(Symbols.CreateOptionSymbol("SPY", OptionRight.Call, 300, may172023));
            spyMay17_300Call.SetMarketPrice(new Tick { Value = 110m });
            var spyMay17_500Call = _algorithm.AddOptionContract(Symbols.CreateOptionSymbol("SPY", OptionRight.Call, 500, may172023));
            spyMay17_500Call.SetMarketPrice(new Tick { Value = 0.04m });

            var spyMay19_300Put = _algorithm.AddOptionContract(Symbols.CreateOptionSymbol("SPY", OptionRight.Put, 300, may192023));
            spyMay19_300Put.SetMarketPrice(new Tick { Value = 0.02m });
            var spyMay19_310Put = _algorithm.AddOptionContract(Symbols.CreateOptionSymbol("SPY", OptionRight.Put, 310, may192023));
            spyMay19_310Put.SetMarketPrice(new Tick { Value = 0.03m });
            var spyMay19_320Put = _algorithm.AddOptionContract(Symbols.CreateOptionSymbol("SPY", OptionRight.Put, 320, may192023));
            spyMay19_320Put.SetMarketPrice(new Tick { Value = 0.05m });
            var spyMay17_300Put = _algorithm.AddOptionContract(Symbols.CreateOptionSymbol("SPY", OptionRight.Put, 300, may172023));
            spyMay17_300Put.SetMarketPrice(new Tick { Value = 0.01m });

            _equity.SetMarketPrice(new Tick { Value = 410m });
            _equity.SetLeverage(4);

            var expectedPositionGroupBPMStrategy = optionStrategyDefinition.Name;

            if (optionStrategyDefinition.Name == OptionStrategyDefinitions.CoveredCall.Name)
            {
                _equity.Holdings.SetHoldings(_equity.Price, initialHoldingsQuantity * _callOption.ContractMultiplier);

                var optionContract = spyMay19_300Call;
                if (strike.HasValue)
                {
                    switch (strike.Value)
                    {
                        case 200:
                            optionContract = spyMay17_200Call;
                            break;
                        case 300:
                            optionContract = spyMay17_300Call;
                            break;
                        case 400:
                            optionContract = spyMay17_400Call;
                            break;
                        case 500:
                            optionContract = spyMay17_500Call;
                            break;
                    }
                }

                optionContract.Holdings.SetHoldings(optionContract.Price, -initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.ProtectiveCall.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.ProtectiveCall.Name)
            {
                _equity.Holdings.SetHoldings(_equity.Price, -initialHoldingsQuantity * _callOption.ContractMultiplier);
                spyMay19_300Call.Holdings.SetHoldings(spyMay19_300Call.Price, initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.CoveredCall.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.CoveredPut.Name)
            {
                _equity.Holdings.SetHoldings(_equity.Price, -initialHoldingsQuantity * _putOption.ContractMultiplier);
                spyMay19_300Put.Holdings.SetHoldings(spyMay19_300Put.Price, -initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.ProtectivePut.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.ProtectivePut.Name)
            {
                _equity.Holdings.SetHoldings(_equity.Price, initialHoldingsQuantity * _putOption.ContractMultiplier);
                spyMay19_300Put.Holdings.SetHoldings(spyMay19_300Put.Price, initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.CoveredPut.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.BearCallSpread.Name)
            {
                var shortCallOption = spyMay19_300Call;
                var longCallOption = spyMay19_310Call;

                shortCallOption.Holdings.SetHoldings(shortCallOption.Price, -initialHoldingsQuantity);
                longCallOption.Holdings.SetHoldings(longCallOption.Price, initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.BullCallSpread.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.BearPutSpread.Name)
            {
                var longPutOption = spyMay19_310Put;
                var shortPutOption = spyMay19_300Put;

                longPutOption.Holdings.SetHoldings(longPutOption.Price, initialHoldingsQuantity);
                shortPutOption.Holdings.SetHoldings(shortPutOption.Price, -initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.BullPutSpread.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.BullCallSpread.Name)
            {
                var shortCallOption = spyMay19_310Call;
                var longCallOption = spyMay19_300Call;

                longCallOption.Holdings.SetHoldings(longCallOption.Price, initialHoldingsQuantity);
                shortCallOption.Holdings.SetHoldings(shortCallOption.Price, -initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.BearCallSpread.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.BullPutSpread.Name)
            {
                var longPutOption = spyMay19_300Put;
                var shortPutOption = spyMay19_310Put;

                longPutOption.Holdings.SetHoldings(longPutOption.Price, initialHoldingsQuantity);
                shortPutOption.Holdings.SetHoldings(shortPutOption.Price, -initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.BearPutSpread.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.Straddle.Name)
            {
                spyMay19_300Call.Holdings.SetHoldings(spyMay19_300Call.Price, initialHoldingsQuantity);
                spyMay19_300Put.Holdings.SetHoldings(spyMay19_300Put.Price, initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.ShortStraddle.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.ShortStraddle.Name)
            {
                spyMay19_300Call.Holdings.SetHoldings(spyMay19_300Call.Price, -initialHoldingsQuantity);
                spyMay19_300Put.Holdings.SetHoldings(spyMay19_300Put.Price, -initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.Straddle.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.Strangle.Name)
            {
                spyMay19_310Call.Holdings.SetHoldings(spyMay19_310Call.Price, initialHoldingsQuantity);
                spyMay19_300Put.Holdings.SetHoldings(spyMay19_300Put.Price, initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.ShortStrangle.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.ShortStrangle.Name)
            {
                spyMay19_310Call.Holdings.SetHoldings(spyMay19_310Call.Price, -initialHoldingsQuantity);
                spyMay19_300Put.Holdings.SetHoldings(spyMay19_300Put.Price, -initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.Strangle.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.ButterflyCall.Name)
            {
                var lowerStrikeCallOption = spyMay19_300Call;
                var middleStrikeCallOption = spyMay19_310Call;
                var upperStrikeCallOption = spyMay19_320Call;

                lowerStrikeCallOption.Holdings.SetHoldings(lowerStrikeCallOption.Price, initialHoldingsQuantity);
                middleStrikeCallOption.Holdings.SetHoldings(middleStrikeCallOption.Price, -2 * initialHoldingsQuantity);
                upperStrikeCallOption.Holdings.SetHoldings(upperStrikeCallOption.Price, initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.ShortButterflyCall.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.ShortButterflyCall.Name)
            {
                var lowerStrikeCallOption = spyMay19_300Call;
                var middleStrikeCallOption = spyMay19_310Call;
                var upperStrikeCallOption = spyMay19_320Call;

                lowerStrikeCallOption.Holdings.SetHoldings(lowerStrikeCallOption.Price, -initialHoldingsQuantity);
                middleStrikeCallOption.Holdings.SetHoldings(middleStrikeCallOption.Price, 2 * initialHoldingsQuantity);
                upperStrikeCallOption.Holdings.SetHoldings(middleStrikeCallOption.Price, -initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.ButterflyCall.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.ButterflyPut.Name)
            {
                var lowerStrikePutOption = spyMay19_300Put;
                var middleStrikePutOption = spyMay19_310Put;
                var upperStrikePutOption = spyMay19_320Put;

                lowerStrikePutOption.Holdings.SetHoldings(lowerStrikePutOption.Price, initialHoldingsQuantity);
                middleStrikePutOption.Holdings.SetHoldings(middleStrikePutOption.Price, -2 * initialHoldingsQuantity);
                upperStrikePutOption.Holdings.SetHoldings(upperStrikePutOption.Price, initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.ShortButterflyPut.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.ShortButterflyPut.Name)
            {
                var lowerStrikePutOption = spyMay19_300Put;
                var middleStrikePutOption = spyMay19_310Put;
                var upperStrikePutOption = spyMay19_320Put;

                lowerStrikePutOption.Holdings.SetHoldings(lowerStrikePutOption.Price, -initialHoldingsQuantity);
                middleStrikePutOption.Holdings.SetHoldings(middleStrikePutOption.Price, 2 * initialHoldingsQuantity);
                upperStrikePutOption.Holdings.SetHoldings(upperStrikePutOption.Price, -initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.ButterflyPut.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.CallCalendarSpread.Name)
            {
                var longCallOption = spyMay19_300Call;
                var shortCallOption = spyMay17_300Call;

                longCallOption.Holdings.SetHoldings(longCallOption.Price, initialHoldingsQuantity);
                shortCallOption.Holdings.SetHoldings(shortCallOption.Price, -initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.ShortCallCalendarSpread.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.ShortCallCalendarSpread.Name)
            {
                var longCallOption = spyMay19_300Call;
                var shortCallOption = spyMay17_300Call;

                longCallOption.Holdings.SetHoldings(longCallOption.Price, -initialHoldingsQuantity);
                shortCallOption.Holdings.SetHoldings(shortCallOption.Price, initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.CallCalendarSpread.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.PutCalendarSpread.Name)
            {
                var longPutOption = spyMay19_300Put;
                var shortPutOption = spyMay17_300Put;

                longPutOption.Holdings.SetHoldings(longPutOption.Price, initialHoldingsQuantity);
                shortPutOption.Holdings.SetHoldings(shortPutOption.Price, -initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.ShortPutCalendarSpread.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.ShortPutCalendarSpread.Name)
            {
                var longPutOption = spyMay19_300Put;
                var shortPutOption = spyMay17_300Put;

                longPutOption.Holdings.SetHoldings(longPutOption.Price, -initialHoldingsQuantity);
                shortPutOption.Holdings.SetHoldings(shortPutOption.Price, initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.PutCalendarSpread.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.IronCondor.Name)
            {
                var longPutOption = spyMay19_300Put;
                var shortPutOption = spyMay19_310Put;
                var shortCallOption = spyMay19_320Call;
                var longCallOption = spyMay19_330Call;

                longPutOption.Holdings.SetHoldings(longPutOption.Price, initialHoldingsQuantity);
                shortPutOption.Holdings.SetHoldings(shortPutOption.Price, -initialHoldingsQuantity);
                shortCallOption.Holdings.SetHoldings(shortCallOption.Price, -initialHoldingsQuantity);
                longCallOption.Holdings.SetHoldings(longCallOption.Price, initialHoldingsQuantity);
            }

            var positionGroup = _portfolio.Positions.Groups.Single();
            Assert.AreEqual(expectedPositionGroupBPMStrategy, positionGroup.BuyingPowerModel.ToString());

            if (updateCashbook)
            {
                // Update the cashbook after setting up the holdings for the strategy.
                // We would need to wire too many things up in order to emulate the engine and update the cashbook automatically.
                foreach (var position in positionGroup)
                {
                    _portfolio.CashBook.Single().Value.AddAmount(-_portfolio.Securities[position.Symbol].Holdings.HoldingsValue);
                }
            }

            return positionGroup;
        }
    }
}
