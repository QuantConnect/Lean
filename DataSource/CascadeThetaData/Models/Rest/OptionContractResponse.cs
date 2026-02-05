/*
 * Cascade Labs - ThetaData Option Contract Response Model
 */

using Newtonsoft.Json;
using QuantConnect.Lean.DataSource.CascadeThetaData.Converters;

namespace QuantConnect.Lean.DataSource.CascadeThetaData.Models.Rest;

/// <summary>
/// Represents an option contract response from the ThetaData API
/// API format: [root, expiry, strike, right] e.g., ["AAPL", 20230616, 260000, "P"]
/// </summary>
[JsonConverter(typeof(ThetaDataOptionContractConverter))]
public readonly struct OptionContractResponse
{
    /// <summary>
    /// The underlying root symbol (e.g., "AAPL")
    /// </summary>
    public string Root { get; }

    /// <summary>
    /// The option expiration date
    /// </summary>
    public DateTime Expiry { get; }

    /// <summary>
    /// The strike price in 1/10 cents (divide by 1000 for dollars)
    /// e.g., 260000 = $260.00
    /// </summary>
    public decimal Strike { get; }

    /// <summary>
    /// The option right: "C" for Call, "P" for Put
    /// </summary>
    public string Right { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OptionContractResponse"/> struct
    /// </summary>
    public OptionContractResponse(string root, DateTime expiry, decimal strike, string right)
    {
        Root = root;
        Expiry = expiry;
        Strike = strike;
        Right = right;
    }
}
