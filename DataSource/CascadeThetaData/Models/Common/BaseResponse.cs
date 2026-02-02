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
using QuantConnect.Lean.DataSource.CascadeThetaData.Models.Interfaces;

namespace QuantConnect.Lean.DataSource.CascadeThetaData.Models.Common;

/// <summary>
/// Represents a base response containing a header and a collection of items of type T.
/// </summary>
/// <typeparam name="T">The type of items in the response.</typeparam>
public readonly struct BaseResponse<T> : IBaseResponse
{
    /// <summary>
    /// Gets the header of the response.
    /// </summary>
    [JsonProperty("header")]
    public BaseHeaderResponse Header { get; }

    /// <summary>
    /// Gets the collection of items in the response.
    /// </summary>
    [JsonProperty("response")]
    public IEnumerable<T> Response { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseResponse{T}"/> struct.
    /// </summary>
    /// <param name="response">The collection of items in the response.</param>
    /// <param name="header">The header of the response.</param>
    [JsonConstructor]
    public BaseResponse(IEnumerable<T> response, BaseHeaderResponse header)
    {
        Response = response;
        Header = header;
    }
}