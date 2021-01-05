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

using Microsoft.Extensions.CommandLineUtils;
using NUnit.Framework;
using QuantConnect.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace QuantConnect.Tests.Configuration
{
    [TestFixture]
    public class ApplicationParserTests
    {
        private static readonly List<CommandLineOption> Options = new List<CommandLineOption>
            {
                new CommandLineOption("config", CommandOptionType.SingleValue),
                new CommandLineOption("algorithm-id", CommandOptionType.SingleValue),

                // limits on number of symbols to allow
                new CommandLineOption("symbol-minute-limit", CommandOptionType.SingleValue),
                new CommandLineOption("symbol-second-limit", CommandOptionType.SingleValue),
                new CommandLineOption("symbol-tick-limit", CommandOptionType.SingleValue),

                new CommandLineOption("debugging", CommandOptionType.SingleValue),

                // if one uses true in following token, market hours will remain open all hours and all days.
                // if one uses false will make lean operate only during regular market hours.
                new CommandLineOption("force-exchange-always-open", CommandOptionType.NoValue),

                // parameters to set in the algorithm (the below are just samples)
                new CommandLineOption("parameters", CommandOptionType.MultipleValue)
            };

        [Test]
        public void ReturnProperNumberOfArgs()
        {
            var args = $"--algorithm-id value --debugging true --symbol-tick-limit 100 --parameters \"ema-slow\":1,\"ema-fast\":\"10\"";

            var options = ApplicationParser.Parse(
                "Test AppName",
                "Test Description",
                "Test Help Text",
                args.Split(new[] { " " }, StringSplitOptions.None),
                Options);

            Assert.AreEqual(Regex.Matches(args, "--").Count, options.Count);
        }

        [Test]
        public void ThrowIfNoValueCommandOptionProvided()
        {
            var args = $"--force-exchange-always-open false";

            Assert.Throws<CommandParsingException>(
                () =>
                {
                    var options = ApplicationParser.Parse(
                        "Test AppName",
                        "Test Description",
                        "Test Help Text",
                        args.Split(new[] { " " }, StringSplitOptions.None),
                        Options);
                }
            );
        }

        [TestCase("algorithm-id", "\"AlgorithmId\"")]
        [TestCase("debugging", "false")]
        [TestCase("debugging", "\"true\"")]
        [TestCase("symbol-second-limit", "100")]
        [TestCase("symbol-second-limit", "\"100\"")]
        [TestCase("symbol-second-limit", "100.0")]
        [TestCase("symbol-second-limit", "\"100.0\"")]
        public void ParseSingleValueArgs(string command, object value)
        {
            var args = $"--{command} {value}";

            var options = ApplicationParser.Parse(
                "Test AppName",
                "Test Description",
                "Test Help Text",
                args.Split(new[] { " " }, StringSplitOptions.None),
                Options);

            Assert.AreEqual(1, options.Count);
            foreach (var option in options)
            {
                Assert.IsInstanceOf<string>(option.Value);
                Assert.AreEqual(value, option.Value);
            }
        }

        [Test]
        public void ParseMultiValueArgs()
        {
            var args = $"--parameters \"ema-slow\":1,\"ema-fast\":\"10\",\"line-slow\":20.0,\"line-fast\":\"100.0\"";

            var options = ApplicationParser.Parse(
                "Test AppName",
                "Test Description",
                "Test Help Text",
                args.Split(new[] { " " }, StringSplitOptions.None),
                Options);

            Assert.AreEqual(1, options.Count);
            Assert.IsTrue(options.ContainsKey("parameters"));

            var parameters = options["parameters"] as Dictionary<string, string>;
            Assert.IsNotNull(parameters);
            Assert.AreEqual(4, parameters.Keys.Count);
            foreach (var parameter in parameters)
            {
                Assert.IsInstanceOf<string>(parameter.Value);
            }
        }

        [TestCase("algorithmId", true, 100, 100.5, 1, 100.5)]
        [TestCase("algorithmId", "true", "100", "100.5", "1", "100.5")]
        public void MergeWithArguments(string str, object bValue, object iValue, object dValue, object iParamValue, object dParamValue)
        {
            var args = $"--algorithm-id {str} --debugging {bValue} --symbol-tick-limit {iValue} --symbol-second-limit {Convert.ToString(dValue, CultureInfo.InvariantCulture)} --parameters ema-slow:{iParamValue},ema-fast:{Convert.ToString(dParamValue, CultureInfo.InvariantCulture)}";

            var options = ApplicationParser.Parse(
                "Test AppName",
                "Test Description",
                "Test Help Text",
                args.Split(new[] { " " }, StringSplitOptions.None),
                Options);

            Config.MergeCommandLineArgumentsWithConfiguration(options);

            Assert.AreEqual("algorithmId", Config.Get("algorithm-id"));
            Assert.AreEqual(true, Config.GetBool("debugging"));
            Assert.AreEqual(100, Config.GetInt("symbol-tick-limit"));
            Assert.AreEqual(100.5, Config.GetDouble("symbol-second-limit"));

            var parameters = new Dictionary<string, string>();

            var parametersConfigString = Config.Get("parameters");
            if (parametersConfigString != string.Empty)
            {
                parameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(parametersConfigString);
            }

            Assert.Contains("ema-slow", parameters.Keys);
            Assert.AreEqual(1, parameters["ema-slow"].ConvertTo<int>());
            Assert.Contains("ema-fast", parameters.Keys);
            Assert.AreEqual(100.5, parameters["ema-fast"].ConvertTo<double>());
        }
    }
}
