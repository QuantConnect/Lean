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
using System.Security.Cryptography;
using System.Text;

namespace QuantConnect.Algorithm.Framework.Portfolio.SignalExports
{
    /// <summary>
    /// Exports signals of desired positions to vBase stamping API using JSON and HTTPS.
    /// Accepts signals in quantity(number of shares) i.e symbol:"SPY", quant:40
    /// </summary>
    public class VBaseSignalExport: BaseSignalExport
    {
        /// <summary>
        /// API key provided by vBase
        /// </summary>
        private readonly string _apiKey;

        /// <summary>
        /// The base URL for the vBase staping API
        /// </summary>
        private readonly string _apiBaseUrl;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="VBaseSignalExport"/> class.
        /// </summary>
        /// <param name="apiKey">The API key for vBase authentication.</param>
        /// <param name="collectionName">The target collection name.</param>
        /// <param name="apiBaseUrl">The base URL for the vBase staping API (default https://app.vbase.com/api).</param>
        /// <param name="storeStampedFile">Whether to store the stamped file (default true).</param>
        /// <param name="idempotent">
        /// A boolean indicating whether to make the request idempotent.
        /// If the request is idempotent, only the first stamp for a given portfolio will be made.
        /// If the request is not idempotent, a new stamp will be made for each request.
        /// </param>
        /// <param name="requestsRateLimiter">Rate limit calls to vBase API.</param>
        public VBaseSignalExport(
            string apiKey,
            string collectionName,
            string apiBaseUrl = "https://app.vbase.com/api",
            bool storeStampedFile = true,
            bool idempotent = false,
            RateGate requestsRateLimiter = null)
        {

            _apiKey = apiKey;

            SHA3_256 sha3 = SHA3_256.Create();
            byte[] collectionCidBytes = sha3.ComputeHash(Encoding.UTF8.GetBytes(collectionName));
            _collectionCid = "0x" + collectionCidBytes
                .ToHexString()
                .ToLowerInvariant();

            _apiBaseUrl = apiBaseUrl?.TrimEnd('/') ?? "https://app.vbase.com/api";
            _storeStampedFile = storeStampedFile;
            _idempotent = idempotent;
            _requestsRateLimiter = requestsRateLimiter;
            
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

            string csv = BuildCsv(parameters);
            _requestsRateLimiter?.WaitToProceed();
            var result = Stamp(csv, parameters.Algorithm);
            return result;
        }

        /// <summary>
        /// Builds a CSV (sym,wt) for the given targets converting percent holdings into absolute quantity using PortfolioTarget.Percent
        /// </summary>
        /// <param name="parameters">Signal export parameters</param>
        /// <returns>Resulting CSV string</returns>
        protected virtual string BuildCsv(SignalExportTargetParameters parameters)
        {
            var algorithm = parameters.Algorithm;
            string csv = "sym,wt\n";

            var targets = parameters.Targets.Select(target =>
                    PortfolioTarget.Percent(algorithm, target.Symbol, target.Quantity)
                )
                .Where(absoluteTarget => absoluteTarget != null);

            foreach (var target in targets)
            {
                csv += $"{target.Symbol.ID},{target.Quantity.ToStringInvariant()}\n";
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
                var endpoint = new Uri(_apiBaseUrl + "/v1/stamp/");

                var contentPairs = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("collectionCid", _collectionCid),
                    new KeyValuePair<string, string>("data", csv),
                    new KeyValuePair<string, string>("storeStampedFile", _storeStampedFile.ToString()),
                    new KeyValuePair<string, string>("idempotent", _idempotent.ToString())
                };
                
                using var httpContent = new FormUrlEncodedContent(contentPairs);
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = httpContent
                };
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

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
