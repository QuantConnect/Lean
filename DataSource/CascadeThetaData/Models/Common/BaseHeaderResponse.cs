/*
 * CASCADELABS.IO
 * Cascade Labs LLC
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
