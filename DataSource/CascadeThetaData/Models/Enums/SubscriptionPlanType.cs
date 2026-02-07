/*
 * CASCADELABS.IO
 * Cascade Labs LLC
 */

namespace QuantConnect.Lean.DataSource.CascadeThetaData.Models.Enums;

/// <summary>
/// Enum representing different subscription plan types.
/// following link: <see href="https://www.thetadata.net/subscribe" /> 
/// </summary>
public enum SubscriptionPlanType
{
    /// <summary>
    /// Free subscription plan.
    /// </summary>
    Free = 0,

    /// <summary>
    /// Value subscription plan.
    /// </summary>
    Value = 1,

    /// <summary>
    /// Standard subscription plan.
    /// </summary>
    Standard = 2,

    /// <summary>
    /// Pro subscription plan.
    /// </summary>
    Pro = 3
}
