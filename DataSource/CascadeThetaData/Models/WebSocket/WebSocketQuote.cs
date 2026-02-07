/*
 * CASCADELABS.IO
 * Cascade Labs LLC
 */

using Newtonsoft.Json;
using QuantConnect.Lean.DataSource.CascadeThetaData.Converters;

namespace QuantConnect.Lean.DataSource.CascadeThetaData.Models.WebSocket;

public readonly struct WebSocketQuote
{
    [JsonProperty("ms_of_day")]
    public int TimeMilliseconds { get; }

    [JsonProperty("bid_size")]
    public int BidSize { get; }

    [JsonProperty("bid_exchange")]
    public byte BidExchange { get; }

    [JsonProperty("bid")]
    public decimal BidPrice { get; }

    [JsonProperty("bid_condition")]
    public int BidCondition { get; }

    [JsonProperty("ask_size")]
    public int AskSize { get; }

    [JsonProperty("ask_exchange")]
    public byte AskExchange { get; }

    [JsonProperty("ask")]
    public decimal AskPrice { get; }

    [JsonProperty("ask_condition")]
    public int AskCondition { get; }

    [JsonProperty("date")]
    [JsonConverter(typeof(DateTimeIntJsonConverter))]
    public DateTime Date { get; }

    /// <summary>
    /// Gets the DateTime representation of the last quote time. DateTime is New York Time (EST) Time Zone!
    /// </summary>
    /// <remarks>
    /// This property calculates the <see cref="Date"/> by adding the <seealso cref="TimeMilliseconds"/> to the Date property.
    /// </remarks>
    public DateTime DateTimeMilliseconds { get => Date.AddMilliseconds(TimeMilliseconds); }

    [JsonConstructor]
    public WebSocketQuote(
        int timeMilliseconds,
        int bidSize, byte bidExchange, decimal bidPrice, int bidCondition, int askSize, byte askExchange, decimal askPrice, int askCondition, DateTime date)
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
