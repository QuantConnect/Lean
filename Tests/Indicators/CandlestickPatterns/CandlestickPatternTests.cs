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
                    rows.Add(new TestCaseData(new Counterattack(), "CDLCOUNTERATTACK", testFileName).SetName("Counterattack-" + testFileName));
                    rows.Add(new TestCaseData(new DarkCloudCover(), "CDLDARKCLOUDCOVER", testFileName).SetName("DarkCloudCover-" + testFileName));
                    rows.Add(new TestCaseData(new Doji(), "CDLDOJI", testFileName).SetName("Doji-" + testFileName));
                    rows.Add(new TestCaseData(new DojiStar(), "CDLDOJISTAR", testFileName).SetName("DojiStar-" + testFileName));
                    rows.Add(new TestCaseData(new DragonflyDoji(), "CDLDRAGONFLYDOJI", testFileName).SetName("DragonflyDoji-" + testFileName));
                    rows.Add(new TestCaseData(new Engulfing(), "CDLENGULFING", testFileName).SetName("Engulfing-" + testFileName));
                    rows.Add(new TestCaseData(new EveningDojiStar(), "CDLEVENINGDOJISTAR", testFileName).SetName("EveningDojiStar-" + testFileName));
                    rows.Add(new TestCaseData(new EveningStar(), "CDLEVENINGSTAR", testFileName).SetName("EveningStar-" + testFileName));
                    rows.Add(new TestCaseData(new GapSideBySideWhite(), "CDLGAPSIDESIDEWHITE", testFileName).SetName("GapSideBySideWhite-" + testFileName));
                    rows.Add(new TestCaseData(new GravestoneDoji(), "CDLGRAVESTONEDOJI", testFileName).SetName("GravestoneDoji-" + testFileName));
                    rows.Add(new TestCaseData(new Hammer(), "CDLHAMMER", testFileName).SetName("Hammer-" + testFileName));
                    rows.Add(new TestCaseData(new HangingMan(), "CDLHANGINGMAN", testFileName).SetName("HangingMan-" + testFileName));
                    rows.Add(new TestCaseData(new Harami(), "CDLHARAMI", testFileName).SetName("Harami-" + testFileName));
                    rows.Add(new TestCaseData(new HaramiCross(), "CDLHARAMICROSS", testFileName).SetName("HaramiCross-" + testFileName));
                    if (testFileName.Contains("ewz"))
                    {
                        // Lean uses decimals while TA-lib uses doubles, so this test only passes with the ewz test file 
                        rows.Add(new TestCaseData(new HighWaveCandle(), "CDLHIGHWAVE", testFileName).SetName("HighWaveCandle-" + testFileName));
                    }
                    rows.Add(new TestCaseData(new Hikkake(), "CDLHIKKAKE", testFileName).SetName("Hikkake-" + testFileName));
                    if (testFileName.Contains("spy"))
                    {
                        // Lean uses decimals while TA-lib uses doubles, so this test only passes with the spy test file 
                        rows.Add(new TestCaseData(new HikkakeModified(), "CDLHIKKAKEMOD", testFileName).SetName("HikkakeModified-" + testFileName));
                    }
                    rows.Add(new TestCaseData(new HomingPigeon(), "CDLHOMINGPIGEON", testFileName).SetName("HomingPigeon-" + testFileName));
                    rows.Add(new TestCaseData(new IdenticalThreeCrows(), "CDLIDENTICAL3CROWS", testFileName).SetName("IdenticalThreeCrows-" + testFileName));
                    rows.Add(new TestCaseData(new InNeck(), "CDLINNECK", testFileName).SetName("InNeck-" + testFileName));
                    rows.Add(new TestCaseData(new InvertedHammer(), "CDLINVERTEDHAMMER", testFileName).SetName("InvertedHammer-" + testFileName));
                    rows.Add(new TestCaseData(new Kicking(), "CDLKICKING", testFileName).SetName("Kicking-" + testFileName));
                    rows.Add(new TestCaseData(new KickingByLength(), "CDLKICKINGBYLENGTH", testFileName).SetName("KickingByLength-" + testFileName));
                    rows.Add(new TestCaseData(new LadderBottom(), "CDLLADDERBOTTOM", testFileName).SetName("LadderBottom-" + testFileName));
                    rows.Add(new TestCaseData(new LongLeggedDoji(), "CDLLONGLEGGEDDOJI", testFileName).SetName("LongLeggedDoji-" + testFileName));
                    rows.Add(new TestCaseData(new LongLineCandle(), "CDLLONGLINE", testFileName).SetName("LongLineCandle-" + testFileName));
                    rows.Add(new TestCaseData(new Marubozu(), "CDLMARUBOZU", testFileName).SetName("Marubozu-" + testFileName));
                    rows.Add(new TestCaseData(new MatchingLow(), "CDLMATCHINGLOW", testFileName).SetName("MatchingLow-" + testFileName));
                    rows.Add(new TestCaseData(new MatHold(), "CDLMATHOLD", testFileName).SetName("MatHold-" + testFileName));
                    rows.Add(new TestCaseData(new MorningDojiStar(), "CDLMORNINGDOJISTAR", testFileName).SetName("MorningDojiStar-" + testFileName));
                    if (!testFileName.Contains("eurusd"))
                    {
                        // Lean uses decimals while TA-lib uses doubles, so this test does not pass with the eurusd test file 
                        rows.Add(new TestCaseData(new MorningStar(), "CDLMORNINGSTAR", testFileName).SetName("MorningStar-" + testFileName));
                    }
                    rows.Add(new TestCaseData(new OnNeck(), "CDLONNECK", testFileName).SetName("OnNeck-" + testFileName));
                    rows.Add(new TestCaseData(new Piercing(), "CDLPIERCING", testFileName).SetName("Piercing-" + testFileName));
                    if (!testFileName.Contains("spy"))
                    {
                        // Lean uses decimals while TA-lib uses doubles, so this test does not pass with the spy test file 
                        rows.Add(new TestCaseData(new RickshawMan(), "CDLRICKSHAWMAN", testFileName).SetName("RickshawMan-" + testFileName));
                    }
                    rows.Add(new TestCaseData(new RiseFallThreeMethods(), "CDLRISEFALL3METHODS", testFileName).SetName("RiseFallThreeMethods-" + testFileName));
                    rows.Add(new TestCaseData(new SeparatingLines(), "CDLSEPARATINGLINES", testFileName).SetName("SeparatingLines-" + testFileName));
                    rows.Add(new TestCaseData(new ShootingStar(), "CDLSHOOTINGSTAR", testFileName).SetName("ShootingStar-" + testFileName));
                    if (!testFileName.Contains("spy"))
                    {
                        // Lean uses decimals while TA-lib uses doubles, so this test does not pass with the spy test file 
                        rows.Add(new TestCaseData(new ShortLineCandle(), "CDLSHORTLINE", testFileName).SetName("ShortLineCandle-" + testFileName));
                    }
                    if (!testFileName.Contains("spy"))
                    {
                        // Lean uses decimals while TA-lib uses doubles, so this test does not pass with the spy test file 
                        rows.Add(new TestCaseData(new SpinningTop(), "CDLSPINNINGTOP", testFileName).SetName("SpinningTop-" + testFileName));
                    }
                    rows.Add(new TestCaseData(new StalledPattern(), "CDLSTALLEDPATTERN", testFileName).SetName("StalledPattern-" + testFileName));
                    if (testFileName.Contains("ewz"))
                    {
                        // Lean uses decimals while TA-lib uses doubles, so this test only passes with the ewz test file 
                        rows.Add(new TestCaseData(new StickSandwich(), "CDLSTICKSANDWICH", testFileName).SetName("StickSandwich-" + testFileName));
                    }
                    rows.Add(new TestCaseData(new Takuri(), "CDLTAKURI", testFileName).SetName("Takuri-" + testFileName));
                    rows.Add(new TestCaseData(new TasukiGap(), "CDLTASUKIGAP", testFileName).SetName("TasukiGap-" + testFileName));
                    rows.Add(new TestCaseData(new Thrusting(), "CDLTHRUSTING", testFileName).SetName("Thrusting-" + testFileName));
                    rows.Add(new TestCaseData(new Tristar(), "CDLTRISTAR", testFileName).SetName("Tristar-" + testFileName));
                    rows.Add(new TestCaseData(new UniqueThreeRiver(), "CDLUNIQUE3RIVER", testFileName).SetName("UniqueThreeRiver-" + testFileName));
                    rows.Add(new TestCaseData(new UpsideGapTwoCrows(), "CDLUPSIDEGAP2CROWS", testFileName).SetName("UpsideGapTwoCrows-" + testFileName));
                    rows.Add(new TestCaseData(new UpDownGapThreeMethods(), "CDLXSIDEGAP3METHODS", testFileName).SetName("UpDownGapThreeMethods-" + testFileName));
                }

                return rows.ToArray();
            }
        }

        [Test, TestCaseSource("PatternTestParameters")]
        public void ComparesAgainstExternalData(IndicatorBase<IBaseDataBar> indicator, string columnName, string testFileName)
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

        private static Action<IndicatorBase<IBaseDataBar>, double> Assertion
        {
            get
            {
                return (indicator, expected) =>
                {
                    // Trace line for debugging
                    // Console.WriteLine(indicator.Current.EndTime + "\t" + expected + "\t" + indicator.Current.Value * 100);

                    Assert.AreEqual(expected, (double) indicator.Current.Value * 100);
                };
            }
        }
    }
}
