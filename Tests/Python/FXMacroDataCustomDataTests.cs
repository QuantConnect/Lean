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
using System.IO;
using System.Linq;
using NodaTime;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Python;

namespace QuantConnect.Tests.Python
{
    [TestFixture]
    public class FXMacroDataCustomDataTests
    {
        [Test]
        public void MacroIndicatorReaderUsesAnnouncementDatetime()
        {
            using (Py.GIL())
            {
                using var module = LoadAlgorithmModule();
                using var pythonType = module.GetAttr("FXMacroDataMacroIndicator");
                dynamic pythonTypeDynamic = pythonType;

                var type = Extensions.CreateType(pythonType);
                var reader = new PythonData(pythonTypeDynamic());
                var config = CreateConfig(type);

                var fixture = File.ReadAllText(
                    Path.Combine(
                        Globals.DataFolder,
                        "fxmacrodata",
                        "usd_inflation.json"
                    )
                );

                var collection = reader.Reader(
                    config,
                    fixture,
                    new DateTime(2026, 10, 1),
                    false
                ) as BaseDataCollection;

                Assert.IsNotNull(collection);

                var points = collection.Data
                    .Cast<PythonData>()
                    .OrderBy(x => x.Time)
                    .ToList();

                Assert.AreEqual(2, points.Count);

                var first = points[0];
                var second = points[1];

                Assert.AreEqual(
                    new DateTime(2026, 8, 12, 12, 30, 0),
                    first.Time
                );
                Assert.AreEqual(
                    new DateTime(2026, 9, 11, 12, 30, 0),
                    second.Time
                );

                Assert.AreEqual(
                    "2026-07-31",
                    first["reference_date"]
                );
                Assert.AreEqual(
                    "2026-08-31",
                    second["reference_date"]
                );

                Assert.AreEqual(3.4m, first.Value);
                Assert.AreEqual(3.4m, second.Value);

                Assert.AreEqual("BLS", first["source"]);
                Assert.AreEqual("BLS", second["source"]);

                Assert.AreEqual(
                    1786537800L,
                    Convert.ToInt64(first["announcement_datetime"])
                );
                Assert.AreEqual(
                    1789129800L,
                    Convert.ToInt64(second["announcement_datetime"])
                );

                Assert.AreEqual(
                    "2026-08-12T12:30:00+00:00",
                    first["announcement_datetime_utc"]
                );
                Assert.AreEqual(
                    "2026-09-11T12:30:00+00:00",
                    second["announcement_datetime_utc"]
                );

                Assert.AreNotEqual(
                    first.Time.ToString("yyyy-MM-dd"),
                    first["reference_date"]
                );
                Assert.AreNotEqual(
                    second.Time.ToString("yyyy-MM-dd"),
                    second["reference_date"]
                );
            }
        }

        [Test]
        public void ReleaseCalendarReaderUsesAnnouncementDatetimeAndMetadata()
        {
            using (Py.GIL())
            {
                using var module = LoadAlgorithmModule();
                using var pythonType = module.GetAttr("FXMacroDataReleaseCalendar");
                dynamic pythonTypeDynamic = pythonType;

                var type = Extensions.CreateType(pythonType);
                var reader = new PythonData(pythonTypeDynamic());
                var config = CreateConfig(type);

                var fixture = File.ReadAllText(
                    Path.Combine(
                        Globals.DataFolder,
                        "fxmacrodata",
                        "usd_calendar.json"
                    )
                );

                var collection = reader.Reader(
                    config,
                    fixture,
                    new DateTime(2026, 10, 1),
                    false
                ) as BaseDataCollection;

                Assert.IsNotNull(collection);

                var points = collection.Data
                    .Cast<PythonData>()
                    .OrderBy(x => x.Time)
                    .ToList();

                Assert.IsNotEmpty(points);

                var hasDistinctReferencePeriod = false;

                foreach (var point in points)
                {
                    var announcementDatetime = Convert.ToInt64(
                        point["announcement_datetime"]
                    );

                    var expectedTime = DateTimeOffset
                        .FromUnixTimeSeconds(announcementDatetime)
                        .UtcDateTime;

                    Assert.AreEqual(
                        expectedTime.Ticks,
                        point.Time.Ticks
                    );

                    Assert.IsFalse(
                        string.IsNullOrWhiteSpace(
                            Convert.ToString(
                                point["announcement_datetime_utc"]
                            )
                        )
                    );

                    Assert.IsFalse(
                        string.IsNullOrWhiteSpace(
                            Convert.ToString(point["release"])
                        )
                    );

                    Assert.IsFalse(
                        string.IsNullOrWhiteSpace(
                            Convert.ToString(point["name"])
                        )
                    );

                    Assert.IsFalse(
                        string.IsNullOrWhiteSpace(
                            Convert.ToString(point["source"])
                        )
                    );

                    Assert.IsFalse(
                        string.IsNullOrWhiteSpace(
                            Convert.ToString(point["source_url"])
                        )
                    );

                    var marketTier = Convert.ToInt32(
                        point["market_tier"]
                    );

                    Assert.That(
                        marketTier,
                        Is.InRange(1, 3)
                    );

                    var referenceDate = Convert.ToString(
                        point["reference_date"]
                    );

                    if (!string.IsNullOrWhiteSpace(referenceDate)
                        && point.Time.ToString("yyyy-MM-dd") != referenceDate)
                    {
                        hasDistinctReferencePeriod = true;
                    }
                }

                Assert.IsTrue(
                    hasDistinctReferencePeriod,
                    "Fixture should demonstrate that reference date and release time are distinct."
                );
            }
        }

        private static PyModule LoadAlgorithmModule()
        {
            var algorithmPath = Path.GetFullPath(
                Path.Combine(
                    Globals.DataFolder,
                    "..",
                    "Algorithm.Python",
                    "FXMacroDataCustomDataRegressionAlgorithm.py"
                )
            );

            var source = File.ReadAllText(algorithmPath);

            return PyModule.FromString(
                $"FXMacroDataCustomDataTests_{Guid.NewGuid():N}",
                source
            );
        }

        private static SubscriptionDataConfig CreateConfig(Type type)
        {
            return new SubscriptionDataConfig(
                type,
                Symbols.SPY,
                Resolution.Daily,
                DateTimeZone.Utc,
                DateTimeZone.Utc,
                false,
                false,
                false,
                isCustom: true
            );
        }
    }
}
