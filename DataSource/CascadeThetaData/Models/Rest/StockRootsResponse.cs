/*
 * Cascade Labs - ThetaData Stock Roots Response Model
 */

using Newtonsoft.Json;
using QuantConnect.Lean.DataSource.CascadeThetaData.Models.Common;
using QuantConnect.Lean.DataSource.CascadeThetaData.Models.Interfaces;

namespace QuantConnect.Lean.DataSource.CascadeThetaData.Models.Rest;

/// <summary>
/// Represents a stock roots response from the ThetaData API
/// Endpoint: /v2/list/roots/stock
/// </summary>
public readonly struct StockRootsResponse : IBaseResponse
{
    /// <summary>
    /// Gets the header of the response
    /// </summary>
    [JsonProperty("header")]
    public BaseHeaderResponse Header { get; }

    /// <summary>
    /// Gets the collection of stock ticker symbols
    /// </summary>
    [JsonProperty("response")]
    public IEnumerable<string> Response { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StockRootsResponse"/> struct
    /// </summary>
    [JsonConstructor]
    public StockRootsResponse(BaseHeaderResponse header, IEnumerable<string> response)
    {
        Header = header;
        Response = response;
    }
}
