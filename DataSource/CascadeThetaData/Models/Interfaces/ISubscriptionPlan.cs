/*
 * CASCADELABS.IO
 * Cascade Labs LLC
 */

using QuantConnect.Util;

namespace QuantConnect.Lean.DataSource.CascadeThetaData.Models.Interfaces;

/// <summary>
/// The <c>ISubscriptionPlan</c> interface defines the base structure for different price plans offered by ThetaData for users.
/// For detailed documentation on ThetaData subscription plans, refer to the following links:
/// <list type="bullet">
///    <item>
///        <term>https://www.thetadata.net/subscribe</term>
///        <description>Institutional Data Retail Pricing</description>
///    </item>
///    <item>
///        <term>https://http-docs.thetadata.us/Articles/Getting-Started/Subscriptions.html#options-data</term>
///        <description>Initial Access Date Based on Subscription Plan</description>
///    </item>
///</list>
/// </summary>
public interface ISubscriptionPlan
{
    /// <summary>
    /// Gets the set of resolutions accessible under the subscription plan.
    /// </summary>
    public HashSet<Resolution> AccessibleResolutions { get; }

    /// <summary>
    /// Gets the date when the user first accessed the subscription plan.
    /// </summary>
    public DateTime FirstAccessDate { get; }

    /// <summary>
    /// Gets the maximum number of contracts that can be streamed simultaneously under the subscription plan.
    /// </summary>
    public uint MaxStreamingContracts { get; }

    /// <summary>
    /// Represents a rate limiting mechanism that controls the rate of access to a resource.
    /// </summary>
    public RateGate? RateGate { get; }
}
