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
using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using QuantConnect.Configuration;
using static System.FormattableString;

namespace QuantConnect.Tests.Configuration
{
    [TestFixture]
    public class ConfigTests
    {
        [OneTimeSetUp]
        public void SetUp()
        {
            Config.Reset();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            Config.Reset();
        }

        [Test]
        public void SetRespectsEnvironment()
        {
            bool betaMode = Config.GetBool("beta-mode");
            var env = Config.Get("environment");
            Config.Set(env + ".beta-mode", betaMode ? "false" : "true");


            bool betaMode2 = Config.GetBool("beta-mode");
            Assert.AreNotEqual(betaMode, betaMode2);
        }

        [Test]
        public void ChangeConfigurationFileNameWrites()
        {
            // we need to load the current config, since it's lazy we get the current env to load it up
            var env = Config.GetEnvironment();
            var tempFile = Path.GetTempFileName();
            Config.SetConfigurationFile(tempFile);
            Config.Write();
            Assert.True(File.Exists(tempFile));
            Assert.True(File.ReadAllText(tempFile).Length > 0);
            File.Delete(tempFile);

            var defaultFile = "config.json";
            Config.SetConfigurationFile(defaultFile);
            Assert.True(File.Exists(defaultFile));
            Assert.True(File.ReadAllText(defaultFile).Length > 0);
        }

        [Test]
        public void FlattenTest()
        {
            // read in and rewrite the environment based on the settings
            const string overrideEnvironment = "live-paper.beta";

            var config = JObject.Parse(
@"{
   'some-setting': 'false',                 
    environments: {
        'live-paper': {
            'some-setting': 'true',
            'environments': {
                'beta': {
                    'some-setting2': 'true'
                }
            }
        }
    }
}");

            var configCopy = config.DeepClone();

            var clone = Config.Flatten(config, overrideEnvironment);

            // remove environments
            Assert.IsNull(clone.Property("environment"));
            Assert.IsNull(clone.Property("environments"));

            // properly applied environment
            Assert.AreEqual("true", clone.Property("some-setting").Value.ToString());
            Assert.AreEqual("true", clone.Property("some-setting2").Value.ToString());

            Assert.AreEqual(configCopy, config);
        }

        [Test]
        public void GetValueHandlesDateTime()
        {
            GetValueHandles(new DateTime(2015, 1, 2, 3, 4, 5));
        }

        [Test]
        public void GetValueHandlesTimeSpan()
        {
            GetValueHandles(new TimeSpan(1, 2, 3, 4, 5));
        }

        private static readonly TestCaseData[] DecimalValue =
        {
            new TestCaseData(100m)
        };

        private static readonly TestCaseData[] DecimalWithExpectedValue =
        {
            new TestCaseData(100m, (int)100)
        };

        [TestCase("value")]
        [TestCase("true")]
        [TestCase(true)]
        [TestCase("1")]
        [TestCase(1)]
        [TestCase("1.0")]
        [TestCase(1d)]
        [Test, TestCaseSource(nameof(DecimalValue))]
        public void GetString(object value)
        {
            Config.MergeCommandLineArgumentsWithConfiguration(new Dictionary<string, object>() { { "temp-value", value } });

            var actual = Config.Get("temp-value");
            Assert.AreEqual(typeof(string), actual.GetType());
        }

        [TestCase("true", true)]
        [TestCase(true, true)]
        [TestCase("false", false)]
        [TestCase(false, false)]
        public void GetBool(object value, bool expected)
        {
            Config.MergeCommandLineArgumentsWithConfiguration(new Dictionary<string, object>() { { "temp-value", value } });

            Assert.AreEqual(expected, Config.GetBool("temp-value"));
        }

        [TestCase("1", 1)]
        [TestCase(1, 1)]
        [TestCase(2d, 2)]
        [Test, TestCaseSource(nameof(DecimalWithExpectedValue))]
        public void GetInt(object value, object expected)
        {
            Config.MergeCommandLineArgumentsWithConfiguration(new Dictionary<string, object>() { { "temp-value", value } });

            Assert.AreEqual(expected, Config.GetInt("temp-value"));
        }

        [TestCase("100.0", 100)]
        [TestCase(50d, 50)]
        [Test, TestCaseSource(nameof(DecimalWithExpectedValue))]
        public void GetDouble(object value, double expected)
        {
            Config.MergeCommandLineArgumentsWithConfiguration(new Dictionary<string, object>() { { "temp-value", value } });

            Assert.AreEqual(expected, Config.GetDouble("temp-value"));
        }

        private void GetValueHandles<T>(T value)
        {
            var configValue = Invariant($"{value}");
            Config.Set("temp-value", configValue);
            var actual = Config.GetValue<T>("temp-value");
            Assert.AreEqual(value, actual);
        }
    }
}
