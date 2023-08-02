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

using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Report;
using QuantConnect.Report.ReportElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static QuantConnect.Report.Report;

namespace QuantConnect.Tests.Report
{
    [TestFixture]
    public class ResultDeserializationTests
    {
        public const string OrderStringReplace = "{{orderStringReplace}}";
        public const string OrderTypeStringReplace = "{{marketOrderType}}";
        public const string EmptyJson = "{}";

        public const string InvalidBacktestResultJson = "{\"RollingWindow\":{},\"TotalPerformance\":null,\"Charts\":{\"Equity\":{\"Name\":\"Equity\",\"ChartType\":0,\"Series\":{\"Performance\":{\"Name\":\"Performance\",\"Unit\":\"$\",\"Index\":0,\"Values\":[{\"x\":1583704925,\"y\":5.0},{\"x\":1583791325,\"y\":null},{\"x\":1583877725,\"y\":7.0},{\"x\":1583964125,\"y\":8.0},{\"x\":1584050525,\"y\":9.0}],\"SeriesType\":0,\"Color\":\"\",\"ScatterMarkerSymbol\":\"none\"}}}},\"Orders\":" + OrderStringReplace + ",\"ProfitLoss\":{},\"Statistics\":{},\"RuntimeStatistics\":{}}";
        public const string InvalidLiveResultJson = "{\"Holdings\":{},\"Cash\":{\"USD\":{\"SecuritySymbol\":{\"Value\":\"\",\"ID\":\" 0\",\"Permtick\":\"\"},\"Symbol\":\"USD\",\"Amount\":0.0,\"ConversionRate\":1.0,\"CurrencySymbol\":\"$\",\"ValueInAccountCurrency\":0.0}},\"ServerStatistics\":{\"CPU Usage\":\"0.0%\",\"Used RAM (MB)\":\"68\",\"Total RAM (MB)\":\"\",\"Used Disk Space (MB)\":\"1\",\"Total Disk Space (MB)\":\"5\",\"Hostname\":\"LEAN\",\"LEAN Version\":\"v2.4.0.0\"},\"Charts\":{\"Equity\":{\"Name\":\"Equity\",\"ChartType\":0,\"Series\":{\"Performance\":{\"Name\":\"Performance\",\"Unit\":\"$\",\"Index\":0,\"Values\":[{\"x\":1583705127,\"y\":5.0},{\"x\":1583791527,\"y\":null},{\"x\":1583877927,\"y\":7.0},{\"x\":1583964327,\"y\":8.0},{\"x\":1584050727,\"y\":9.0}],\"SeriesType\":0,\"Color\":\"\",\"ScatterMarkerSymbol\":\"none\"}}}},\"Orders\":" + OrderStringReplace + ",\"ProfitLoss\":{},\"Statistics\":{},\"RuntimeStatistics\":{}}";
        public const string OrderJson = @"{'1': {
    'Type':" + OrderTypeStringReplace + @",
    'Value':99986.827413672,
    'Id':1,
    'ContingentId':0,
    'BrokerId':[1],
    'Symbol':{'Value':'SPY',
    'Permtick':'SPY'},
    'Price':100.086914328,
    'Time':'2010-03-04T14:31:00Z',
    'Quantity':999,
    'Status':3,
    'Duration':2,
    'DurationValue':'2010-04-04T14:31:00Z',
    'Tag':'',
    'SecurityType':1,
    'Direction':0,
    'AbsoluteQuantity':999,
    'GroupOrderManager': {
        'Id': 1,
        'Count': 3,
        'Quantity': 10,
        'LimitPrice': 123.456,
        'OrderIds': [1, 2, 3]
    }
}}";

        [Test]
        public void BacktestResult_NullChartPoint_IsSkipped()
        {
            var converter = new NullResultValueTypeJsonConverter<BacktestResult>();
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            var deWithoutConverter = JsonConvert.DeserializeObject<BacktestResult>(InvalidBacktestResultJson.Replace(OrderStringReplace, EmptyJson), settings);
            var deWithConverter = JsonConvert.DeserializeObject<BacktestResult>(InvalidBacktestResultJson.Replace(OrderStringReplace, EmptyJson), converter);

            var noConverterPoints = GetChartPoints(deWithoutConverter).ToList();
            var withConverterPoints = GetChartPoints(deWithConverter).ToList();

            Assert.IsTrue(withConverterPoints.All(kvp => kvp.Value > 0));
            Assert.AreEqual(4, withConverterPoints.Count);

            var convertedSerialized = JsonConvert.SerializeObject(deWithConverter);
            var roundtripDeserialization = JsonConvert.DeserializeObject<BacktestResult>(convertedSerialized);

            Assert.IsTrue(withConverterPoints.SequenceEqual(GetChartPoints(roundtripDeserialization).ToList()));
        }

        [Test]
        public void LiveResult_NullChartPoint_IsSkipped()
        {
            var converter = new NullResultValueTypeJsonConverter<LiveResult>();
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            var deWithoutConverter = JsonConvert.DeserializeObject<LiveResult>(InvalidLiveResultJson.Replace(OrderStringReplace, EmptyJson), settings);
            var deWithConverter = JsonConvert.DeserializeObject<LiveResult>(InvalidLiveResultJson.Replace(OrderStringReplace, EmptyJson), converter);

            var noConverterPoints = GetChartPoints(deWithoutConverter).ToList();
            var withConverterPoints = GetChartPoints(deWithConverter).ToList();

            Assert.IsTrue(withConverterPoints.All(kvp => kvp.Value > 0));
            Assert.AreEqual(4, withConverterPoints.Count);

            var convertedSerialized = JsonConvert.SerializeObject(deWithConverter);
            var roundtripDeserialization = JsonConvert.DeserializeObject<LiveResult>(convertedSerialized);

            Assert.IsTrue(withConverterPoints.SequenceEqual(GetChartPoints(roundtripDeserialization).ToList()));
        }

        [Test]
        public void OrderTypeEnumStringAndValueDeserialization()
        {

            var settings = new JsonSerializerSettings
            {
                Converters = new[] { new NullResultValueTypeJsonConverter<LiveResult>() }
            };

            foreach (var orderType in (OrderType[])Enum.GetValues(typeof(OrderType)))
            {
                //var orderObjectType = OrderTypeNormalizingJsonConverter.TypeFromOrderTypeEnum(orderType);
                var intValueJson = OrderJson.Replace(OrderTypeStringReplace, ((int)orderType).ToStringInvariant());
                var upperCaseJson = OrderJson.Replace(OrderTypeStringReplace, $"'{orderType.ToStringInvariant().ToUpperInvariant()}'");
                var camelCaseJson = OrderJson.Replace(OrderTypeStringReplace, $"'{orderType.ToStringInvariant().ToCamelCase()}'");

                var intValueLiveResult = InvalidLiveResultJson.Replace(OrderStringReplace, intValueJson);
                var upperCaseLiveResult = InvalidLiveResultJson.Replace(OrderStringReplace, upperCaseJson);
                var camelCaseLiveResult = InvalidLiveResultJson.Replace(OrderStringReplace, camelCaseJson);

                var intInstance = JsonConvert.DeserializeObject<LiveResult>(intValueLiveResult, settings).Orders.Values.Single();
                var upperCaseInstance = JsonConvert.DeserializeObject<LiveResult>(upperCaseLiveResult, settings).Orders.Values.Single();
                var camelCaseInstance = JsonConvert.DeserializeObject<LiveResult>(camelCaseLiveResult, settings).Orders.Values.Single();

                CollectionAssert.AreEqual(intInstance.BrokerId, upperCaseInstance.BrokerId);
                Assert.AreEqual(intInstance.ContingentId, upperCaseInstance.ContingentId);
                Assert.AreEqual(intInstance.Direction, upperCaseInstance.Direction);
                Assert.AreEqual(intInstance.TimeInForce.GetType(), upperCaseInstance.TimeInForce.GetType());
                Assert.AreEqual(intInstance.Id, upperCaseInstance.Id);
                Assert.AreEqual(intInstance.Price, upperCaseInstance.Price);
                Assert.AreEqual(intInstance.PriceCurrency, upperCaseInstance.PriceCurrency);
                Assert.AreEqual(intInstance.SecurityType, upperCaseInstance.SecurityType);
                Assert.AreEqual(intInstance.Status, upperCaseInstance.Status);
                Assert.AreEqual(intInstance.Symbol, upperCaseInstance.Symbol);
                Assert.AreEqual(intInstance.Tag, upperCaseInstance.Tag);
                Assert.AreEqual(intInstance.Time, upperCaseInstance.Time);
                Assert.AreEqual(intInstance.CreatedTime, upperCaseInstance.CreatedTime);
                Assert.AreEqual(intInstance.LastFillTime, upperCaseInstance.LastFillTime);
                Assert.AreEqual(intInstance.LastUpdateTime, upperCaseInstance.LastUpdateTime);
                Assert.AreEqual(intInstance.CanceledTime, upperCaseInstance.CanceledTime);
                Assert.AreEqual(intInstance.Type, upperCaseInstance.Type);
                Assert.AreEqual(intInstance.Value, upperCaseInstance.Value);
                Assert.AreEqual(intInstance.Quantity, upperCaseInstance.Quantity);
                Assert.AreEqual(intInstance.TimeInForce.GetType(), upperCaseInstance.TimeInForce.GetType());
                Assert.AreEqual(intInstance.Symbol.ID.Market, upperCaseInstance.Symbol.ID.Market);
                Assert.AreEqual(intInstance.OrderSubmissionData?.AskPrice, upperCaseInstance.OrderSubmissionData?.AskPrice);
                Assert.AreEqual(intInstance.OrderSubmissionData?.BidPrice, upperCaseInstance.OrderSubmissionData?.BidPrice);
                Assert.AreEqual(intInstance.OrderSubmissionData?.LastPrice, upperCaseInstance.OrderSubmissionData?.LastPrice);

                CollectionAssert.AreEqual(intInstance.BrokerId, camelCaseInstance.BrokerId);
                Assert.AreEqual(intInstance.ContingentId, camelCaseInstance.ContingentId);
                Assert.AreEqual(intInstance.Direction, camelCaseInstance.Direction);
                Assert.AreEqual(intInstance.TimeInForce.GetType(), camelCaseInstance.TimeInForce.GetType());
                Assert.AreEqual(intInstance.Id, camelCaseInstance.Id);
                Assert.AreEqual(intInstance.Price, camelCaseInstance.Price);
                Assert.AreEqual(intInstance.PriceCurrency, camelCaseInstance.PriceCurrency);
                Assert.AreEqual(intInstance.SecurityType, camelCaseInstance.SecurityType);
                Assert.AreEqual(intInstance.Status, camelCaseInstance.Status);
                Assert.AreEqual(intInstance.Symbol, camelCaseInstance.Symbol);
                Assert.AreEqual(intInstance.Tag, camelCaseInstance.Tag);
                Assert.AreEqual(intInstance.Time, camelCaseInstance.Time);
                Assert.AreEqual(intInstance.CreatedTime, camelCaseInstance.CreatedTime);
                Assert.AreEqual(intInstance.LastFillTime, camelCaseInstance.LastFillTime);
                Assert.AreEqual(intInstance.LastUpdateTime, camelCaseInstance.LastUpdateTime);
                Assert.AreEqual(intInstance.CanceledTime, camelCaseInstance.CanceledTime);
                Assert.AreEqual(intInstance.Type, camelCaseInstance.Type);
                Assert.AreEqual(intInstance.Value, camelCaseInstance.Value);
                Assert.AreEqual(intInstance.Quantity, camelCaseInstance.Quantity);
                Assert.AreEqual(intInstance.TimeInForce.GetType(), camelCaseInstance.TimeInForce.GetType());
                Assert.AreEqual(intInstance.Symbol.ID.Market, camelCaseInstance.Symbol.ID.Market);
                Assert.AreEqual(intInstance.OrderSubmissionData?.AskPrice, camelCaseInstance.OrderSubmissionData?.AskPrice);
                Assert.AreEqual(intInstance.OrderSubmissionData?.BidPrice, camelCaseInstance.OrderSubmissionData?.BidPrice);
                Assert.AreEqual(intInstance.OrderSubmissionData?.LastPrice, camelCaseInstance.OrderSubmissionData?.LastPrice);
            }
        }

        [TestCaseSource(nameof(CreatesReportParametersTableCorrectlyTestCases))]
        public void CreatesReportParametersTableCorrectly(string parametersTemplate, Dictionary<string, string> parameters, string expectedParametersTable)
        {
            var algorithmConfiguration = new AlgorithmConfiguration { Parameters = parameters };
            var parametersReportElment = new ParametersReportElement("parameters", "", algorithmConfiguration, null, parametersTemplate);
            var parametersTable = parametersReportElment.Render();
            Assert.AreEqual(expectedParametersTable, parametersTable);
        }

        [TestCase(htmlExampleCode + @"

<!--crisis
<div class=""col-xs-4"">
    <table class=""crisis-chart table compact"">
        <thead>
        <tr>
            <th style=""display: block; height: 75px;"">{{$TEXT-CRISIS-TITLE}}</th>
        </tr>
        </thead>
        <tbody>
        <tr>
            <td style=""padding:0;"">
                <img src=""{{$PLOT-CRISIS-CONTENT}}"">
            </td>
        </tr>
        </tbody>
    </table>
</div>
crisis-->
",
            @"<!--crisis(\r|\n)*((\r|\n|.)*?)crisis-->", @"<div class=""col-xs-4"">
    <table class=""crisis-chart table compact"">
        <thead>
        <tr>
            <th style=""display: block; height: 75px;"">{{$TEXT-CRISIS-TITLE}}</th>
        </tr>
        </thead>
        <tbody>
        <tr>
            <td style=""padding:0;"">
                <img src=""{{$PLOT-CRISIS-CONTENT}}"">
            </td>
        </tr>
        </tbody>
    </table>
</div>
")]
        [TestCase(htmlExampleCode + @"
<!--parameters
<tr>
	<td class = ""title""> {{$FIRST-KPI-NAME}} </td><td> {{$FIRST-KPI-VALUE}} </td>
	<td class = ""title""> {{$SECOND-KPI-NAME}} </td><td> {{$SECOND-KPI-VALUE}} </td>
</tr>
parameters-->",
            @"<!--parameters(\r|\n)*((\r|\n|.)*?)parameters-->", @"<tr>
	<td class = ""title""> {{$FIRST-KPI-NAME}} </td><td> {{$FIRST-KPI-VALUE}} </td>
	<td class = ""title""> {{$SECOND-KPI-NAME}} </td><td> {{$SECOND-KPI-VALUE}} </td>
</tr>
")]
        [TestCase(@"<!--crisis<div class=""col-xs-4""><table class=""crisis-chart table compact""><thead><tr><th style=""display: block; height: 75px;"">{{$TEXT-CRISIS-TITLE}}</th></tr></thead><tbody><tr><td style=""padding:0;""><img src=""{{$PLOT-CRISIS-CONTENT}}""></td></tr></tbody></table></div>crisis-->",
            @"<!--crisis(\r|\n)*((\r|\n|.)*?)crisis-->", @"<div class=""col-xs-4""><table class=""crisis-chart table compact""><thead><tr><th style=""display: block; height: 75px;"">{{$TEXT-CRISIS-TITLE}}</th></tr></thead><tbody><tr><td style=""padding:0;""><img src=""{{$PLOT-CRISIS-CONTENT}}""></td></tr></tbody></table></div>")]
        [TestCase(@"<!--parameters<tr><td class = ""title""> {{$FIRST-KPI-NAME}} </td><td> {{$FIRST-KPI-VALUE}} </td></tr>parameters-->",
            @"<!--parameters(\r|\n)*((\r|\n|.)*?)parameters-->", @"<tr><td class = ""title""> {{$FIRST-KPI-NAME}} </td><td> {{$FIRST-KPI-VALUE}} </td></tr>")]
        public void GetsExpectedCrisisAndParametersHTMLCodes(string input, string pattern, string expected)
        {
            var htmlCode = GetRegexInInput(pattern, input);
            Assert.IsNotNull(htmlCode);
            Assert.AreEqual(expected, htmlCode);
        }

        [TestCase(htmlExampleCode + @"

<!--crisis
<div class=""col-xs-4"">
    <table class=""crisis-chart table compact"">
        <thead>
        <tr>
            <th style=""display: block; height: 75px;"">{{$TEXT-CRISIS-TITLE}}</th>
        </tr>
        </thead>
        <tbody>
        <tr>
            <td style=""padding:0;"">
                <img src=""{{$PLOT-CRISIS-CONTENT}}"">
            </td>
        </tr>
        </tbody>
    </table>
</div>
crisis-->
", @"<!--parameters(\r|\n)*((\r|\n|.)*?)parameters-->")]
        [TestCase(htmlExampleCode + @"
<!--parameters
<tr>
	<td class = ""title""> {{$FIRST-KPI-NAME}} </td><td> {{$FIRST-KPI-VALUE}} </td>
	<td class = ""title""> {{$SECOND-KPI-NAME}} </td><td> {{$SECOND-KPI-VALUE}} </td>
</tr>
parameters-->", @"<!--crisis(\r|\n)*((\r|\n|.)*?)crisis-->")]
        [TestCase(@"", @"<!--crisis(\r|\n)*((\r|\n|.)*?)crisis-->")]
        [TestCase(@"", @"<!--parameters(\r|\n)*((\r|\n|.)*?)parameters-->")]
        [TestCase(@"<div class=""col-xs-4""><table class=""crisis-chart table compact""><thead><tr><th style=""display: block; height: 75px;"">{{$TEXT-CRISIS-TITLE}}</th></tr></thead><tbody><tr><td style=""padding:0;""><img src=""{{$PLOT-CRISIS-CONTENT}}""></td></tr></tbody></table></div>crisis-->",
    @"<!--crisis(\r|\n)*((\r|\n|.)*?)crisis-->")]
        [TestCase(@"<!--crisis<div class=""col-xs-4""><table class=""crisis-chart table compact""><thead><tr><th style=""display: block; height: 75px;"">{{$TEXT-CRISIS-TITLE}}</th></tr></thead><tbody><tr><td style=""padding:0;""><img src=""{{$PLOT-CRISIS-CONTENT}}""></td></tr></tbody></table></div>",
    @"<!--crisis(\r|\n)*((\r|\n|.)*?)crisis-->")]
        [TestCase(@"<div class=""col-xs-4""><table class=""crisis-chart table compact""><thead><tr><th style=""display: block; height: 75px;"">{{$TEXT-CRISIS-TITLE}}</th></tr></thead><tbody><tr><td style=""padding:0;""><img src=""{{$PLOT-CRISIS-CONTENT}}""></td></tr></tbody></table></div>",
    @"<!--crisis(\r|\n)*((\r|\n|.)*?)crisis-->")]
        [TestCase(@"<tr><td class = ""title""> {{$FIRST-KPI-NAME}} </td><td> {{$FIRST-KPI-VALUE}} </td></tr>parameters-->",
    @"<!--parameters(\r|\n)*((\r|\n|.)*?)parameters-->")]
        [TestCase(@"<!--parameters<tr><td class = ""title""> {{$FIRST-KPI-NAME}} </td><td> {{$FIRST-KPI-VALUE}} </td></tr>",
    @"<!--parameters(\r|\n)*((\r|\n|.)*?)parameters-->")]
        [TestCase(@"<tr><td class = ""title""> {{$FIRST-KPI-NAME}} </td><td> {{$FIRST-KPI-VALUE}} </td></tr>",
    @"<!--parameters(\r|\n)*((\r|\n|.)*?)parameters-->")]
        public void FindsNoMatchingForCrisisAndParametersInGivenInput(string input, string pattern)
        {
            var matching = GetRegexInInput(pattern, input);
            Assert.IsNull(matching);
        }

        public IEnumerable<KeyValuePair<long, decimal>> GetChartPoints(Result result)
        {
            return result.Charts["Equity"].Series["Performance"].Values.Select(point => new KeyValuePair<long, decimal>(point.x, point.y));
        }

        private const string htmlExampleCode = @"            <div class=""page"" style=""{{$CSS-CRISIS-PAGE-STYLE}}"">
                <div class=""header"">
                    <div class=""header-left"">
                        <img src=""https://cdn.quantconnect.com/web/i/logo.png"">
                    </div>
                    <div class=""header-right"">Strategy Report Summary: {{$TEXT-STRATEGY-NAME}} {{$TEXT-STRATEGY-VERSION}}</div>
                </div>
                <div class=""content"">
                    <div class=""container-row"">
                        {{$HTML-CRISIS-PLOTS}}
                    </div>
                </div>
            </div>
			<div class=""page"" id=""parameters"" style=""{{$CSS-PARAMETERS-PAGE-STYLE}}"">
                <div class=""header"">
                    <div class=""header-left"">
                        <img src=""https://cdn.quantconnect.com/web/i/logo.png"">
                    </div>
                    <div class=""header-right"">Strategy Report Summary: {{$TEXT-STRATEGY-NAME}} {{$TEXT-STRATEGY-VERSION}}</div>
                </div>
                <div class=""content"">
                    <div class=""container-row"">
                        <div class=""col-xs-12"">
                            <table id=""key-characteristics"" class=""table compact"">
                                <thead>
                                <tr>
                                    <th class=""title"">Parameters</th>
                                </tr>
                                </thead>
                                <tbody>
                                    {{$PARAMETERS}}
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>
            </div>
    </body>
    </html>";

        public static object[] CreatesReportParametersTableCorrectlyTestCases = new object[]
        {
            // Happy test cases
            new object[] { @"<tr>
	<td class = ""title""> {{$KEY0}} </td><td> {{$VALUE0}} </td>
</tr>", new Dictionary<string, string>() { { "test-key-one", "1" }, { "test-key-two", "2" }, { "test-key-three", "three" } },
                @"<tr>
	<td class = ""title""> Test Key One </td><td> 1 </td>
</tr>
<tr>
	<td class = ""title""> Test Key Two </td><td> 2 </td>
</tr>
<tr>
	<td class = ""title""> Test Key Three </td><td> three </td>
</tr>"},

            new object[] { @"<tr>
	<td class = ""title""> {{$KEY0}} </td><td> {{$VALUE0}} </td>
	<td class = ""title""> {{$KEY1}} </td><td> {{$VALUE1}} </td>
</tr>", new Dictionary<string, string>() { { "test-key-one", "1" }, { "test-key-two", "2" }, { "test-key-three", "three" } }, @"<tr>
	<td class = ""title""> Test Key One </td><td> 1 </td>
	<td class = ""title""> Test Key Two </td><td> 2 </td>
</tr>
<tr>
	<td class = ""title""> Test Key Three </td><td> three </td>
	<td class = ""title"">  </td><td>  </td>
</tr>"},

            new object[] { @"<tr>
	<td class = ""title""> {{$KEY0}} </td><td> {{$VALUE0}} </td>
	<td class = ""title""> {{$KEY1}} </td><td> {{$VALUE1}} </td>
</tr>", new Dictionary<string, string>() { { "test-key-one", "1" }, { "test-key-two", "2" } }, @"<tr>
	<td class = ""title""> Test Key One </td><td> 1 </td>
	<td class = ""title""> Test Key Two </td><td> 2 </td>
</tr>"},

            new object[] { @"<tr>
	<td class = ""title""> {{$KEY0}} </td><td> {{$VALUE0}} </td>
	<td class = ""title""> {{$KEY1}} </td><td> {{$VALUE1}} </td>
    <td class = ""title""> {{$KEY2}} </td><td> {{$VALUE2}} </td>
</tr>", new Dictionary<string, string>() { { "test-key-one", "1" }, { "test-key-two", "2" }, { "test-key-three", "three" }, { "test-key-four", "4"} }, @"<tr>
	<td class = ""title""> Test Key One </td><td> 1 </td>
	<td class = ""title""> Test Key Two </td><td> 2 </td>
    <td class = ""title""> Test Key Three </td><td> three </td>
</tr>
<tr>
	<td class = ""title""> Test Key Four </td><td> 4 </td>
	<td class = ""title"">  </td><td>  </td>
    <td class = ""title"">  </td><td>  </td>
</tr>"},
            new object[] { @"<tr>
	<td class = ""title""> {{$KEY0}} </td><td> {{$VALUE0}} </td>
	<td class = ""title""> {{$KEY1}} </td><td> {{$VALUE1}} </td>
    <td class = ""title""> {{$KEY2}} </td><td> {{$VALUE2}} </td>
</tr>", new Dictionary<string, string>() { { "test-key-one", "1" }, { "test-key-two", "2" }, { "test-key-three", "three" }, { "test-key-four", "4"}, { "test-key-five", "5"} }, @"<tr>
	<td class = ""title""> Test Key One </td><td> 1 </td>
	<td class = ""title""> Test Key Two </td><td> 2 </td>
    <td class = ""title""> Test Key Three </td><td> three </td>
</tr>
<tr>
	<td class = ""title""> Test Key Four </td><td> 4 </td>
	<td class = ""title""> Test Key Five </td><td> 5 </td>
    <td class = ""title"">  </td><td>  </td>
</tr>"},

            new object[] { @"<tr>
	<td class = ""title""> {{$KEY0}} </td><td> {{$VALUE0}} </td>
	<td class = ""title""> {{$KEY1}} </td><td> {{$VALUE1}} </td>
    <td class = ""title""> {{$KEY2}} </td><td> {{$VALUE2}} </td>
    <td class = ""title""> {{$KEY3}} </td><td> {{$VALUE3}} </td>
    <td class = ""title""> {{$KEY4}} </td><td> {{$VALUE4}} </td>
    <td class = ""title""> {{$KEY5}} </td><td> {{$VALUE5}} </td>
    <td class = ""title""> {{$KEY6}} </td><td> {{$VALUE6}} </td>
    <td class = ""title""> {{$KEY7}} </td><td> {{$VALUE7}} </td>
    <td class = ""title""> {{$KEY8}} </td><td> {{$VALUE8}} </td>
    <td class = ""title""> {{$KEY9}} </td><td> {{$VALUE9}} </td>
    <td class = ""title""> {{$KEY10}} </td><td> {{$VALUE10}} </td>
    <td class = ""title""> {{$KEY11}} </td><td> {{$VALUE11}} </td>
</tr>", new Dictionary<string, string>() { { "test-key-one", "1" }, { "test-key-two", "2" }, { "test-key-three", "three" }, { "test-key-four", "4" }, { "test-key-five", "5" },
                { "test-key-six", "6" }, { "test-key-seven", "7" }, { "test-key-eight", "8" }, { "test-key-nine", "9" }, { "test-key-ten", "10"}, { "test-key-eleven", "11"}}, @"<tr>
	<td class = ""title""> Test Key One </td><td> 1 </td>
	<td class = ""title""> Test Key Two </td><td> 2 </td>
    <td class = ""title""> Test Key Three </td><td> three </td>
    <td class = ""title""> Test Key Four </td><td> 4 </td>
    <td class = ""title""> Test Key Five </td><td> 5 </td>
    <td class = ""title""> Test Key Six </td><td> 6 </td>
    <td class = ""title""> Test Key Seven </td><td> 7 </td>
    <td class = ""title""> Test Key Eight </td><td> 8 </td>
    <td class = ""title""> Test Key Nine </td><td> 9 </td>
    <td class = ""title""> Test Key Ten </td><td> 10 </td>
    <td class = ""title""> Test Key Eleven </td><td> 11 </td>
    <td class = ""title"">  </td><td>  </td>
</tr>"},
            // Sad test cases
            new object[] { @"<tr>
	<td class = ""title""> {{$KEY1}} </td><td> {{$VALUE1}} </td>
</tr>", new Dictionary<string, string>() { { "test-key-one", "1" }, { "test-key-two", "2" }, { "test-key-three", "three" } }, @"<tr>
	<td class = ""title""> {{$KEY1}} </td><td> {{$VALUE1}} </td>
</tr>
<tr>
	<td class = ""title""> {{$KEY1}} </td><td> {{$VALUE1}} </td>
</tr>
<tr>
	<td class = ""title""> {{$KEY1}} </td><td> {{$VALUE1}} </td>
</tr>"},

            new object[] { @"<tr>
	<td class = ""title""> {{$KEY0}} </td><td> {{$VALUE}} </td>
</tr>", new Dictionary<string, string>() { { "test-key-one", "1" }, { "test-key-two", "2" }, { "test-key-three", "three" } }, @"<tr>
	<td class = ""title""> Test Key One </td><td> {{$VALUE}} </td>
</tr>
<tr>
	<td class = ""title""> Test Key Two </td><td> {{$VALUE}} </td>
</tr>
<tr>
	<td class = ""title""> Test Key Three </td><td> {{$VALUE}} </td>
</tr>"},

            new object[] { @"<tr>
	<td class = ""title""> {{$KEY1}} </td><td> {{$VALUE0}} </td>
</tr>", new Dictionary<string, string>() { { "test-key-one", "1" }, { "test-key-two", "2" }, { "test-key-three", "three" } }, @"<tr>
	<td class = ""title""> {{$KEY1}} </td><td> 1 </td>
</tr>
<tr>
	<td class = ""title""> {{$KEY1}} </td><td> 2 </td>
</tr>
<tr>
	<td class = ""title""> {{$KEY1}} </td><td> three </td>
</tr>"},

            new object[] { @"<tr>
	<td class = ""title""> {{$KEY1}} </td><td> {{$VALUE1}} </td>
	<td class = ""title""> {{$KEY2}} </td><td> {{$VALUE2}} </td>
    <td class = ""title""> {{$KEY3}} </td><td> {{$VALUE3}} </td>
</tr>", new Dictionary<string, string>() { { "test-key-one", "1" }, { "test-key-two", "2" }, { "test-key-three", "three" }, { "test-key-four", "4"} }, @"<tr>
	<td class = ""title""> Test Key Two </td><td> 2 </td>
	<td class = ""title""> Test Key Three </td><td> three </td>
    <td class = ""title""> {{$KEY3}} </td><td> {{$VALUE3}} </td>
</tr>
<tr>
	<td class = ""title"">  </td><td>  </td>
	<td class = ""title"">  </td><td>  </td>
    <td class = ""title""> {{$KEY3}} </td><td> {{$VALUE3}} </td>
</tr>"},
        };
    }
}
