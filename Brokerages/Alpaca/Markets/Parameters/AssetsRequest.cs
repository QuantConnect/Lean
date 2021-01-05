/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System.Collections.Generic;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Encapsulates request parameters for <see cref="AlpacaTradingClient.ListAssetsAsync(AssetsRequest,System.Threading.CancellationToken)"/> call.
    /// </summary>
    public sealed class AssetsRequest : Validation.IRequest
    {
        /// <summary>
        /// Gets or sets asset status for filtering.
        /// </summary>
        public AssetStatus? AssetStatus { get; set; }

        /// <summary>
        /// Gets or sets asset class for filtering.
        /// </summary>
        public AssetClass? AssetClass { get; set; }

        IEnumerable<RequestValidationException> Validation.IRequest.GetExceptions()
        {
            // TODO: olegra - add more validations
            yield break;
        }
    }
}
