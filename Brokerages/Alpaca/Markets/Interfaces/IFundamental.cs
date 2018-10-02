/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/commit/161b114b4b40d852a14a903bd6e69d26fe637922
*/

using System;

// ReSharper disable InconsistentNaming

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Encapsulates asset fundamental information from Alpaca REST API.
    /// </summary>
    public interface IFundamental
    {
        /// <summary>
        /// Gets unique asset identifier
        /// </summary>
        Guid AssetId { get; }

        /// <summary>
        /// Gets name.
        /// </summary>
        String Symbol { get; }

        /// <summary>
        /// Gets full name.
        /// </summary>
        String FullName { get; }

        /// <summary>
        /// Gets industry.
        /// </summary>
        String Industry { get; }

        /// <summary>
        /// Gets industry group.
        /// </summary>
        String IndustryGroup { get; }

        /// <summary>
        /// Gets sector.
        /// </summary>
        String Sector { get; }

        /// <summary>
        /// Gets short description.
        /// </summary>
        String ShortDescription { get; }

        /// <summary>
        /// Gets long description.
        /// </summary>
        String LongDescription { get; }

        Decimal pe_ratio { get; }

        Decimal peg_ratio { get; }

        Decimal beta { get; }

        Decimal eps { get; }

        /// <summary>
        /// Gets market capitalization value.
        /// </summary>
        Decimal MarketCapitalization { get; }

        /// <summary>
        /// Gets shares outstanding value.
        /// </summary>
        Decimal SharesOutstanding { get; }

        /// <summary>
        /// Gets avarage volume.
        /// </summary>
        Decimal AvgVolume { get; }

        /// <summary>
        /// Gets 52 weeks high value.
        /// </summary>
        Decimal FiftyTwoWeekHigh { get; }

        /// <summary>
        /// Gets 52 weeks low value.
        /// </summary>
        Decimal FiftyTwoWeekLow { get;  }

        /// <summary>
        /// Gets divident rate.
        /// </summary>
        Decimal DividentsRate { get; }

        Decimal roa { get; }

        Decimal roe { get; }

        Decimal ps { get; }

        Decimal pc { get; }

        /// <summary>
        /// Gets gross margin value.
        /// </summary>
        Decimal GrossMargin { get; }
    }
}