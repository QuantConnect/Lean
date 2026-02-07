/*
 * CASCADELABS.IO
 * Cascade Labs LLC
 */

namespace QuantConnect.Lean.DataSource.CascadeThetaData.Models.Rest;

/// <summary>
/// Contains constant values for various request parameters used in API queries.
/// </summary>
public static class RequestParameters
{
    /// <summary>
    /// Represents the time interval in milliseconds since midnight Eastern Time (ET).
    /// Example values:
    /// - 09:30:00 ET = 34_200_000 ms
    /// - 16:00:00 ET = 57_600_000 ms
    /// </summary>
    public const string IntervalInMilliseconds = "ivl";

    /// <summary>
    /// Represents the start date for a query or request.
    /// </summary>
    public const string StartDate = "start_date";

    /// <summary>
    /// Represents the end date for a query or request.
    /// </summary>
    public const string EndDate = "end_date";
}
