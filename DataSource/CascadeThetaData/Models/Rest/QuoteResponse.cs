/*
 * CASCADELABS.IO
 * Cascade Labs LLC
 */

using Newtonsoft.Json;
using QuantConnect.Lean.DataSource.CascadeThetaData.Converters;

namespace QuantConnect.Lean.DataSource.CascadeThetaData.Models.Rest;

/// <summary>
/// Represents a Quote containing information about the last NBBO (National Best Bid and Offer) for a financial instrument.
/// </summary>
[JsonConverter(typeof(ThetaDataQuoteConverter))]
public readonly struct QuoteResponse
{
    /// <summary>
    /// The milliseconds since 00:00:00.000 (midnight) Eastern Time (ET).
    /// </summary>
    public uint TimeMilliseconds { get; }

    /// <summary>
    /// The last NBBO bid size.
    /// </summary>
    public decimal BidSize { get; }

    /// <summary>
    /// The last NBBO bid exchange.
    /// </summary>
    public byte BidExchange { get; }

    /// <summary>
    /// The last NBBO bid price.
    /// </summary>
    public decimal BidPrice { get; }

    /// <summary>
    /// The last NBBO bid condition.
    /// </summary>
    public string BidCondition { get; }

    /// <summary>
    /// The last NBBO ask size.
    /// </summary>
    public decimal AskSize { get; }

    /// <summary>
    /// The last NBBO ask exchange.
    /// </summary>
    public byte AskExchange { get; }

    /// <summary>
    /// The last NBBO ask price.
    /// </summary>
    public decimal AskPrice { get; }

    /// <summary>
    /// The last NBBO ask condition.
    /// </summary>
    public string AskCondition { get; }

    /// <summary>
    /// The date formatted as YYYYMMDD. (e.g. "20240328" -> 2024/03/28)
    /// </summary>
    public DateTime Date { get; }

    /// <summary>
    /// Gets the DateTime representation of the last quote time. DateTime is New York Time (EST) Time Zone!
    /// </summary>
    /// <remarks>
    /// This property calculates the <see cref="Date"/> by adding the <seealso cref="TimeMilliseconds"/> to the Date property.
    /// </remarks>
    public DateTime DateTimeMilliseconds { get => Date.AddMilliseconds(TimeMilliseconds); }

    /// <summary>
    /// Initializes a new instance of the <see cref="QuoteResponse"/> struct.
    /// </summary>
    /// <param name="timeMilliseconds">Milliseconds since 00:00:00.000 (midnight) Eastern Time (ET).</param>
    /// <param name="bidSize">The last NBBO bid size.</param>
    /// <param name="bidExchange">The last NBBO bid exchange.</param>
    /// <param name="bidPrice">The last NBBO bid price.</param>
    /// <param name="bidCondition">The last NBBO bid condition.</param>
    /// <param name="askSize">The last NBBO ask size.</param>
    /// <param name="askExchange">The last NBBO ask exchange.</param>
    /// <param name="askPrice">The last NBBO ask price.</param>
    /// <param name="askCondition">The last NBBO ask condition.</param>
    /// <param name="date">The date formatted as YYYYMMDD.</param>
    public QuoteResponse(uint timeMilliseconds, decimal bidSize, byte bidExchange, decimal bidPrice, string bidCondition,
        decimal askSize, byte askExchange, decimal askPrice, string askCondition, DateTime date)
    {
        TimeMilliseconds = timeMilliseconds;
        BidSize = bidSize;
        BidExchange = bidExchange;
        BidPrice = bidPrice;
        BidCondition = bidCondition;
        AskSize = askSize;
        AskExchange = askExchange;
        AskPrice = askPrice;
        AskCondition = askCondition;
        Date = date;
    }
}
