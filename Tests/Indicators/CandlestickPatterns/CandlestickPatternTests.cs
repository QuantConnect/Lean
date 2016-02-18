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
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Indicators.CandlestickPatterns;

namespace QuantConnect.Tests.Indicators.CandlestickPatterns
{
    [TestFixture]
    public class CandlestickPatternTests
    {
        private const string TestFileName = "spy_candle_patterns.txt";

        public virtual TestCaseData[] PatternTestParameters
        {
            get
            {
                return new[]
                {
                    new TestCaseData(new TwoCrows(), "CDL2CROWS").SetName("TwoCrows"),
                    new TestCaseData(new ThreeBlackCrows(), "CDL3BLACKCROWS").SetName("ThreeBlackCrows"),
                    new TestCaseData(new ThreeInside(), "CDL3INSIDE").SetName("ThreeInside"),
                    new TestCaseData(new ThreeLineStrike(), "CDL3LINESTRIKE").SetName("ThreeLineStrike"),
                };
            }
        }

        [Test, TestCaseSource("PatternTestParameters")]
        public void ComparesAgainstExternalData(IndicatorBase<TradeBar> indicator, string columnName)
        {
            TestHelper.TestIndicator(indicator, TestFileName, columnName, Assertion);
        }

        [Test, TestCaseSource("PatternTestParameters")]
        public void ComparesAgainstExternalDataAfterReset(CandlestickPattern indicator, string columnName)
        {
            TestHelper.TestIndicator(indicator, TestFileName, columnName, Assertion);
            indicator.Reset();
            TestHelper.TestIndicator(indicator, TestFileName, columnName, Assertion);
        }

        [Test, TestCaseSource("PatternTestParameters")]
        public void ResetsProperly(CandlestickPattern indicator, string columnName)
        {
            TestHelper.TestIndicatorReset(indicator, TestFileName);
        }

        private static Action<IndicatorBase<TradeBar>, double> Assertion
        {
            get { return (indicator, expected) => Assert.AreEqual(expected, (double)indicator.Current.Value * 100); }
        }
    }
}
