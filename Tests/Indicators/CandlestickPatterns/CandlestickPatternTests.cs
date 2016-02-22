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
using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Indicators.CandlestickPatterns;

namespace QuantConnect.Tests.Indicators.CandlestickPatterns
{
    [TestFixture]
    public class CandlestickPatternTests
    {
        private readonly string[] _testFileNames =
        {
            "spy_candle_patterns.txt", "ewz_candle_patterns.txt", "eurusd_candle_patterns.txt"
        };

        public virtual TestCaseData[] PatternTestParameters
        {
            get
            {
                var rows = new List<TestCaseData>();

                foreach (var testFileName in _testFileNames)
                {
                    rows.Add(new TestCaseData(new TwoCrows(), "CDL2CROWS", testFileName).SetName("TwoCrows-" + testFileName));
                    rows.Add(new TestCaseData(new ThreeBlackCrows(), "CDL3BLACKCROWS", testFileName).SetName("ThreeBlackCrows-" + testFileName));
                    rows.Add(new TestCaseData(new ThreeInside(), "CDL3INSIDE", testFileName).SetName("ThreeInside-" + testFileName));
                    rows.Add(new TestCaseData(new ThreeLineStrike(), "CDL3LINESTRIKE", testFileName).SetName("ThreeLineStrike-" + testFileName));
                    rows.Add(new TestCaseData(new ThreeOutside(), "CDL3OUTSIDE", testFileName).SetName("ThreeOutside-" + testFileName));
                    rows.Add(new TestCaseData(new ThreeStarsInSouth(), "CDL3STARSINSOUTH", testFileName).SetName("ThreeStarsInSouth-" + testFileName));
                    rows.Add(new TestCaseData(new ThreeWhiteSoldiers(), "CDL3WHITESOLDIERS", testFileName).SetName("ThreeWhiteSoldiers-" + testFileName));
                    rows.Add(new TestCaseData(new AbandonedBaby(), "CDLABANDONEDBABY", testFileName).SetName("AbandonedBaby-" + testFileName));
                    rows.Add(new TestCaseData(new AdvanceBlock(), "CDLADVANCEBLOCK", testFileName).SetName("AdvanceBlock-" + testFileName));
                    rows.Add(new TestCaseData(new BeltHold(), "CDLBELTHOLD", testFileName).SetName("BeltHold-" + testFileName));
                    rows.Add(new TestCaseData(new Breakaway(), "CDLBREAKAWAY", testFileName).SetName("Breakaway-" + testFileName));
                    rows.Add(new TestCaseData(new ClosingMarubozu(), "CDLCLOSINGMARUBOZU", testFileName).SetName("ClosingMarubozu-" + testFileName));
                    rows.Add(new TestCaseData(new ConcealedBabySwallow(), "CDLCONCEALBABYSWALL", testFileName).SetName("ConcealedBabySwallow-" + testFileName));
                }

                return rows.ToArray();
            }
        }

        [Test, TestCaseSource("PatternTestParameters")]
        public void ComparesAgainstExternalData(IndicatorBase<TradeBar> indicator, string columnName, string testFileName)
        {
            TestHelper.TestIndicator(indicator, testFileName, columnName, Assertion);
        }

        [Test, TestCaseSource("PatternTestParameters")]
        public void ComparesAgainstExternalDataAfterReset(CandlestickPattern indicator, string columnName, string testFileName)
        {
            TestHelper.TestIndicator(indicator, testFileName, columnName, Assertion);
            indicator.Reset();
            TestHelper.TestIndicator(indicator, testFileName, columnName, Assertion);
        }

        [Test, TestCaseSource("PatternTestParameters")]
        public void ResetsProperly(CandlestickPattern indicator, string columnName, string testFileName)
        {
            TestHelper.TestIndicatorReset(indicator, testFileName);
        }

        private static Action<IndicatorBase<TradeBar>, double> Assertion
        {
            get { return (indicator, expected) => Assert.AreEqual(expected, (double)indicator.Current.Value * 100); }
        }
    }
}
