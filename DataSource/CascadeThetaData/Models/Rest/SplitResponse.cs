/*
 * Cascade Labs - ThetaData Split Response Model
 */

using Newtonsoft.Json;
using QuantConnect.Lean.DataSource.CascadeThetaData.Converters;

namespace QuantConnect.Lean.DataSource.CascadeThetaData.Models.Rest;

/// <summary>
/// Represents a stock split response from the ThetaData API
/// API format: [ms_of_day, split_date, before_shares, after_shares, date]
/// </summary>
[JsonConverter(typeof(ThetaDataSplitConverter))]
public readonly struct SplitResponse
{
    /// <summary>
    /// The milliseconds since 00:00:00.000 (midnight) Eastern Time (ET)
    /// </summary>
    public uint MsOfDay { get; }

    /// <summary>
    /// The date the split occurred, formatted as YYYYMMDD
    /// </summary>
    public DateTime SplitDate { get; }

    /// <summary>
    /// Number of shares before the split
    /// </summary>
    public decimal BeforeShares { get; }

    /// <summary>
    /// Number of shares after the split
    /// </summary>
    public decimal AfterShares { get; }

    /// <summary>
    /// The query date (date parameter used in the API request)
    /// </summary>
    public DateTime QueryDate { get; }

    /// <summary>
    /// Gets the split factor (before_shares / after_shares)
    /// For a 2:1 split, this would be 0.5 (1 share becomes 2)
    /// </summary>
    public decimal SplitFactor => AfterShares != 0 ? BeforeShares / AfterShares : 1m;

    /// <summary>
    /// Initializes a new instance of the <see cref="SplitResponse"/> struct
    /// </summary>
    public SplitResponse(uint msOfDay, DateTime splitDate, decimal beforeShares, decimal afterShares, DateTime queryDate)
    {
        MsOfDay = msOfDay;
        SplitDate = splitDate;
        BeforeShares = beforeShares;
        AfterShares = afterShares;
        QueryDate = queryDate;
    }
}
