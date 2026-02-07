/*
 * CASCADELABS.IO
 * Cascade Labs LLC
 */

using QuantConnect.Lean.DataSource.CascadeThetaData.Models.Common;

namespace QuantConnect.Lean.DataSource.CascadeThetaData.Models.Interfaces;

/// <summary>
/// Represents the base interface for response objects.
/// </summary>
public interface IBaseResponse
{
    /// <summary>
    /// Gets the header of the response.
    /// </summary>
    public BaseHeaderResponse Header { get; }
}
