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
using QuantConnect.Lean.DataSource.CascadeThetaData.Converters;

namespace QuantConnect.Lean.DataSource.CascadeThetaData.Models.Common;

/// <summary>
/// Represents the base header response.
/// </summary>
public readonly struct BaseHeaderResponse
{
    /// <summary>
    /// Gets the next page value.
    /// </summary>
    [JsonProperty("next_page")]
    [JsonConverter(typeof(ThetaDataNullStringConverter))]
    public string NextPage { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseHeaderResponse"/> struct.
    /// </summary>
    /// <param name="nextPage">The next page value.</param>
    [JsonConstructor]
    public BaseHeaderResponse(string nextPage)
    {
        NextPage = nextPage;
    }
}
