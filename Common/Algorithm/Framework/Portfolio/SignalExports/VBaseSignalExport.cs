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

using QuantConnect.Interfaces;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace QuantConnect.Algorithm.Framework.Portfolio.SignalExports
{
    /// <summary>
    /// Exports signals of desired positions to vBase stamping API using JSON and HTTPS.
    /// Accepts signals in quantity(number of shares) i.e symbol:"SPY", quant:40
    /// </summary>
    public class VBaseSignalExport : BaseSignalExport
    {
        private const string ApiBaseUrl = "https://app.vbase.com/api";

        /// <summary>
        /// API key provided by vBase
        /// </summary>
        private readonly string _apiKey;

        /// <summary>
        /// The collection CID (SHA3-256 hash of collection name) to which we stamp signals
        /// </summary>
        private readonly string _collectionCid;

        /// <summary>
        /// Whether vBase should store the stamped file (defaults true)
        /// </summary>
        private readonly bool _storeStampedFile;

        /// <summary>
        /// Whether this request is idempotent (if true only first identical portfolio stored)
        /// </summary>
        private readonly bool _idempotent;

        /// <summary>
        /// The name of this signal export
        /// </summary>
        protected override string Name => "vBase";

        private static RateGate _requestsRateLimiter;

        private readonly Uri _stampApiUrl;

        /// <summary>
        /// Initializes a new instance of the <see cref="VBaseSignalExport"/> class.
        /// </summary>
        /// <param name="apiKey">The API key for vBase authentication.</param>
        /// <param name="collectionName">The target collection name.</param>
        /// <param name="storeStampedFile">Whether to store the stamped file (default true).</param>
        /// <param name="idempotent">
        /// A boolean indicating whether to make the request idempotent.
        /// If the request is idempotent, only the first stamp for a given portfolio will be made.
        /// If the request is not idempotent, a new stamp will be made for each request.
        /// </param>
        public VBaseSignalExport(
            string apiKey,
            string collectionName,
            bool storeStampedFile = true,
            bool idempotent = false)
        {
            _apiKey = apiKey;

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new ArgumentException("vBaseSignalExport: API key not provided");
            }
            if (string.IsNullOrWhiteSpace(collectionName))
            {
                throw new ArgumentException("vBaseSignalExport: Collection name not provided");
            }

            _stampApiUrl = new Uri(ApiBaseUrl.TrimEnd('/') + "/v1/stamp/"); ;

            var collectionCidBytes = SHA3_256.HashData(Encoding.UTF8.GetBytes(collectionName));
            _collectionCid = "0x" + collectionCidBytes.ToHexString().ToLowerInvariant();

            _storeStampedFile = storeStampedFile;
            _idempotent = idempotent;
            _requestsRateLimiter = new RateGate(10, TimeSpan.FromMinutes(5));
        }

        /// <summary>
        /// Converts targets to CSV and posts them to vBase stamping endpoint
        /// </summary>
        /// <param name="parameters">Signal export parameters (targets + algorithm)</param>
        /// <returns>True if request succeeded</returns>
        public override bool Send(SignalExportTargetParameters parameters)
        {
            if (!base.Send(parameters))
            {
                return false;
            }

            var csv = BuildCsv(parameters);
            _requestsRateLimiter?.WaitToProceed();
            return Stamp(csv, parameters.Algorithm);
        }

        /// <summary>
        /// Builds a CSV (sym,wt) for the given targets converting percent holdings into absolute quantity using PortfolioTarget.Percent
        /// </summary>
        /// <param name="parameters">Signal export parameters</param>
        /// <returns>Resulting CSV string</returns>
        protected virtual string BuildCsv(SignalExportTargetParameters parameters)
        {
            var algorithm = parameters.Algorithm;
            var csv = "sym,wt\n";

            var targets = parameters.Targets.Select(target =>
                    PortfolioTarget.Percent(algorithm, target.Symbol, target.Quantity)
                )
                .Where(absoluteTarget => absoluteTarget != null);

            foreach (var target in targets)
            {
                csv += $"{target.Symbol.Value},{target.Quantity.ToStringInvariant()}\n";
            }
            return csv;
        }

        /// <summary>
        /// Sends the CSV payload to the vBase stamping API
        /// </summary>
        private bool Stamp(string csv, IAlgorithm algorithm)
        {
            try
            {
                var contentPairs = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("collectionCid", _collectionCid),
                    new KeyValuePair<string, string>("data", csv),
                    new KeyValuePair<string, string>("storeStampedFile", _storeStampedFile.ToString()),
                    new KeyValuePair<string, string>("idempotent", _idempotent.ToString())
                };

                using var httpContent = new FormUrlEncodedContent(contentPairs);
                using var request = new HttpRequestMessage(HttpMethod.Post, _stampApiUrl)
                {
                    Content = httpContent
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                var response = HttpClient.SendAsync(request).Result;
                var body = response.Content.ReadAsStringAsync().Result;
                if (!response.IsSuccessStatusCode)
                {
                    algorithm.Error($"vBase API returned {response.StatusCode}. Body: {body}");
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                algorithm.Error($"vBase signal export failed: {e.Message}");
                return false;
            }
        }
    }
}
