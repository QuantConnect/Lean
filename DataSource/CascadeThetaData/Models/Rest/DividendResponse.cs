/*
 * Cascade Labs - ThetaData Dividend Response Model
 */

using Newtonsoft.Json;
using QuantConnect.Lean.DataSource.CascadeThetaData.Converters;

namespace QuantConnect.Lean.DataSource.CascadeThetaData.Models.Rest;

/// <summary>
/// Represents a dividend response from the ThetaData API
/// API format: [ms_of_day, ex_date, record_date, payment_date, ann_date, dividend_amount, undefined, less_amount, date]
/// </summary>
[JsonConverter(typeof(ThetaDataDividendConverter))]
public readonly struct DividendResponse
{
    /// <summary>
    /// The milliseconds since 00:00:00.000 (midnight) Eastern Time (ET)
    /// </summary>
    public uint MsOfDay { get; }

    /// <summary>
    /// The ex-dividend date formatted as YYYYMMDD
    /// </summary>
    public DateTime ExDate { get; }

    /// <summary>
    /// The record date formatted as YYYYMMDD
    /// </summary>
    public DateTime RecordDate { get; }

    /// <summary>
    /// The payment date formatted as YYYYMMDD
    /// </summary>
    public DateTime PaymentDate { get; }

    /// <summary>
    /// The announcement date formatted as YYYYMMDD
    /// </summary>
    public DateTime AnnouncementDate { get; }

    /// <summary>
    /// The dividend amount per share
    /// </summary>
    public decimal DividendAmount { get; }

    /// <summary>
    /// The less amount (tax withholding, etc.)
    /// </summary>
    public decimal LessAmount { get; }

    /// <summary>
    /// The query date (date parameter used in the API request)
    /// </summary>
    public DateTime QueryDate { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DividendResponse"/> struct
    /// </summary>
    public DividendResponse(
        uint msOfDay,
        DateTime exDate,
        DateTime recordDate,
        DateTime paymentDate,
        DateTime announcementDate,
        decimal dividendAmount,
        decimal lessAmount,
        DateTime queryDate)
    {
        MsOfDay = msOfDay;
        ExDate = exDate;
        RecordDate = recordDate;
        PaymentDate = paymentDate;
        AnnouncementDate = announcementDate;
        DividendAmount = dividendAmount;
        LessAmount = lessAmount;
        QueryDate = queryDate;
    }
}
