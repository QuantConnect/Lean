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

using Newtonsoft.Json.Linq;
using QuantConnect.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace QuantConnect.Algorithm.Framework.Portfolio.SignalExports
{
    /// <summary>
    /// Exports signals of the desired positions to CrunchDAO API.
    /// Accepts signals in percentage i.e ticker:"SPY", date: "2020-10-04", signal:0.54
    /// </summary>
    public class CrunchDAOSignalExport : BaseSignalExport
    {
        /// <summary>
        /// CrunchDAO API endpoint
        /// </summary>
        private readonly Uri _destination;

        /// <summary>
        /// Model ID or Name
        /// </summary>
        private readonly string _model;

        /// <summary>
        /// Submission Name (Optional)
        /// </summary>
        private readonly string _submissionName;

        /// <summary>
        /// Comment (Optional)
        /// </summary>
        private readonly string _comment;

        /// <summary>
        /// HashSet of allowed SecurityTypes for CrunchDAO
        /// </summary>
        private readonly HashSet<SecurityType> _allowedSecurityTypes = new()
        {
            SecurityType.Equity,
            SecurityType.Index
        };

        /// <summary>
        /// The name of this signal export
        /// </summary>
        protected override string Name { get; } = "CrunchDAO";

        /// <summary>
        /// HashSet property of allowed SecurityTypes for CrunchDAO
        /// </summary>
        protected override HashSet<SecurityType> AllowedSecurityTypes => _allowedSecurityTypes;

        /// <summary>
        /// CrunchDAOSignalExport constructor. It obtains the required information for CrunchDAO API requests.
        /// See (https://colab.research.google.com/drive/1YW1xtHrIZ8ZHW69JvNANWowmxPcnkNu0?authuser=1#scrollTo=aPyWNxtuDc-X)
        /// </summary>
        /// <param name="apiKey">API key provided by CrunchDAO</param>
        /// <param name="model">Model ID or Name</param>
        /// <param name="submissionName">Submission Name (Optional)</param>
        /// <param name="comment">Comment (Optional)</param>
        public CrunchDAOSignalExport(string apiKey, string model, string submissionName = "", string comment = "")
        {
            _model = model;
            _submissionName = submissionName;
            _comment = comment;
            _destination = new Uri($"https://api.tournament.crunchdao.com/v3/alpha-submissions?apiKey={apiKey}");
        }

        /// <summary>
        /// Verifies every holding is a stock, creates a message with the desired positions
        /// using the expected CrunchDAO API format and then sends it with the other required
        /// body features</summary>
        /// <param name="parameters">A list of holdings from the portfolio,
        /// expected to be sent to CrunchDAO API and the algorithm being ran</param>
        /// <returns>True if the positions were sent to CrunchDAO succesfully and errors were returned, false otherwise</returns>
        public override bool Send(SignalExportTargetParameters parameters)
        {
            if (!base.Send(parameters))
            {
                return false;
            }

            var positions = ConvertToCSVFormat(parameters);
            var result = SendPositions(positions);

            return result;
        }

        /// <summary>
        /// Converts the list of holdings into a CSV format string
        /// </summary>
        /// <param name="parameters">A list of holdings from the portfolio,
        /// expected to be sent to CrunchDAO API and the algorithm being ran</param>
        /// <returns>A CSV format string of the given holdings with the required features(ticker, date, signal)</returns>
        protected string ConvertToCSVFormat(SignalExportTargetParameters parameters)
        {
            var holdings = parameters.Targets;
            var algorithm = parameters.Algorithm;
            var positions = "ticker,date,signal\n";

            foreach (var holding in holdings)
            {
                positions += $"{holding.Symbol},{algorithm.Securities[holding.Symbol].LocalTime.ToString("yyyy-MM-dd")},{holding.Quantity}\n";
            }

            return positions;
        }

        /// <summary>
        /// Sends the desired positions, with the other required features, to CrunchDAO API using a POST request. It logs
        /// the message retrieved by the API if there was a HttpRequestException
        /// </summary>
        /// <param name="positions">A CSV format string of the given holdings with the required features</param>
        /// <returns>True if the positions were sent to CrunchDAO successfully and errors were returned. False, otherwise</returns>
        private bool SendPositions(string positions)
        {
            // Create positions stream
            var positionsStream = new MemoryStream();
            using var writer = new StreamWriter(positionsStream);
            writer.Write(positions);
            writer.Flush();
            positionsStream.Position = 0;

            // Create the required body features for the POST request
            using var file = new StreamContent(positionsStream);
            using var model = new StringContent(_model);
            using var submissionName = new StringContent(_submissionName);
            using var comment = new StringContent(_comment);

            // Crete the httpMessage to be sent and add the different POST request body features
            using var httpMessage = new MultipartFormDataContent
            {
                { model, "model" },
                { submissionName, "label" },
                { comment, "comment" },
                { file, "file", "submission.csv" }
            };

            // Send the httpMessage
            using HttpResponseMessage response = HttpClient.PostAsync(_destination, httpMessage).Result;
            if (!response.IsSuccessStatusCode)
            {
                Log.Error($"CrunchDAOSignalExport.SendPositions(): CrunchDAO API returned HttpRequestException {response.StatusCode}");
                return false;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Locked || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                var responseContent = response.Content.ReadAsStringAsync().Result;
                var parsedResponseContent = JObject.Parse(responseContent);
                Log.Error($"CrunchDAOSignalExport.SendPositions(): CrunchDAO API returned code: {parsedResponseContent["code"]} message:{parsedResponseContent["message"]}");
                return false;
            }

            return true;
        }
    }
}
