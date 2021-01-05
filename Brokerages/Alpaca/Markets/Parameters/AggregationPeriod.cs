/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;
using System.Globalization;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Encapsulates account history period request duration - value and unit pair.
    /// </summary>
    public struct AggregationPeriod : IEquatable<AggregationPeriod>
    {
        /// <summary>
        /// Creates new instance of <see cref="AggregationPeriod"/> object.
        /// </summary>
        /// <param name="value">Duration value in units.</param>
        /// <param name="unit">Duration units (days, weeks, etc.)</param>
        public AggregationPeriod(
            Int32 value,
            AggregationPeriodUnit unit)
        {
            Value = value;
            Unit = unit;
        }

        /// <summary>
        /// Gets specified duration units.
        /// </summary>
        public AggregationPeriodUnit Unit { get; }

        /// <summary>
        /// Gets specified duration value.
        /// </summary>
        public Int32 Value { get; }

        /// <inheritdoc />
        public bool Equals(AggregationPeriod other) => Unit == other.Unit && Value == other.Value;

        /// <inheritdoc />
        public override String ToString() =>
            $"{Value.ToString("D", CultureInfo.InvariantCulture)}/{Unit.ToEnumString()}";

        /// <inheritdoc />
        public override Boolean Equals(Object other) => (other.GetType().Name == nameof(AggregationPeriod)) && Equals((AggregationPeriod)other);

        /// <inheritdoc />
        public override Int32 GetHashCode()
        {
            var hashCode = -2109781847;
            hashCode = hashCode * -1521134295 + Unit.GetHashCode();
            hashCode = hashCode * -1521134295 + Value.GetHashCode();
            return hashCode;
        }

        /// <summary>
        /// Returns <c>true</c> if compared objects are equal.
        /// </summary>
        /// <param name="lhs">Left hand side for compare./</param>
        /// <param name="rhs">Right hand side for compare.</param>
        /// <returns>Returns <c>true</c> if compared objects are equal.</returns>
        public static Boolean operator ==(AggregationPeriod lhs, AggregationPeriod rhs) => lhs.Equals(rhs);

        /// <summary>
        /// Returns <c>true</c> if compared objects are not equal.
        /// </summary>
        /// <param name="lhs">Left hand side for compare./</param>
        /// <param name="rhs">Right hand side for compare.</param>
        /// <returns>Returns <c>true</c> if compared objects are not equal.</returns>
        public static Boolean operator !=(AggregationPeriod lhs, AggregationPeriod rhs) => !(lhs == rhs);
    }
}
