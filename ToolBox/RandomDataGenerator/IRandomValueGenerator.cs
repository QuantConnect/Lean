using System;
using QuantConnect.Data.Market;
using QuantConnect.Securities;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// Defines a type capable of producing random values for use in random data generation
    /// </summary>
    /// <remarks>
    /// Any parameters referenced as a percentage value are always in 'percent space', meaning 1 is 1%.
    /// </remarks>
    public interface IRandomValueGenerator
    {
        /// <summary>
        /// Randomly return a <see cref="bool"/> value with the specified odds of being true
        /// </summary>
        /// <param name="percentOddsForTrue">The percent odds of being true in percent space, so 10 => 10%</param>
        /// <returns>True or false</returns>
        bool NextBool(double percentOddsForTrue);

        /// <summary>
        /// Generates a random <see cref="string"/> within the specified lengths.
        /// </summary>
        /// <param name="minLength">The minimum length, inclusive</param>
        /// <param name="maxLength">The maximum length, inclusive</param>
        /// <returns>A new upper case string within the specified lengths</returns>
        string NextUpperCaseString(int minLength, int maxLength);

        /// <summary>
        /// Generates a random <see cref="decimal"/> suitable as a price. This should observe minimum price
        /// variations if available in <see cref="SymbolPropertiesDatabase"/>, and if not, truncating to 2
        /// decimal places.
        /// </summary>
        /// <exception cref="ArgumentException">Throw when the <paramref name="referencePrice"/> or <paramref name="maximumPercentDeviation"/>
        /// is less than or equal to zero.</exception>
        /// <param name="securityType">The security type the price is being generated for</param>
        /// <param name="market">The market of the security the price is being generated for</param>
        /// <param name="referencePrice">The reference price used as the mean of random price generation</param>
        /// <param name="maximumPercentDeviation">The maximum percent deviation. This value is in percent space,
        ///     so a value of 1m is equal to 1%.</param>
        /// <returns>A new decimal suitable for usage as price within the specified deviation from the reference price</returns>
        decimal NextPrice(SecurityType securityType, string market, decimal referencePrice, decimal maximumPercentDeviation);

        /// <summary>
        /// Generates a random <see cref="DateTime"/> between the specified <paramref name="minDateTime"/> and
        /// <paramref name="maxDateTime"/>. <paramref name="dayOfWeek"/> is optionally specified to force the
        /// result to a particular day of the week
        /// </summary>
        /// <param name="minDateTime">The minimum date time, inclusive</param>
        /// <param name="maxDateTime">The maximum date time, inclusive</param>
        /// <param name="dayOfWeek">Optional. The day of week to force</param>
        /// <returns>A new <see cref="DateTime"/> within the specified range and optionally of the specified day of week</returns>
        DateTime NextDate(DateTime minDateTime, DateTime maxDateTime, DayOfWeek? dayOfWeek);

        /// <summary>
        /// Generates a random <see cref="DateTime"/> suitable for use as a tick's emit time.
        /// If the density provided is <see cref="DataDensity.Dense"/>, then at least one tick will be generated per <paramref name="resolution"/> step.
        /// If the density provided is <see cref="DataDensity.Sparse"/>, then at least one tick will be generated every 5 <paramref name="resolution"/> steps.
        /// if the density provided is <see cref="DataDensity.VerySparse"/>, then at least one tick will be generated every 50 <paramref name="resolution"/> steps.
        /// Times returned are guaranteed to be within market hours for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol to generate a new tick time for</param>
        /// <param name="previous">The previous tick time</param>
        /// <param name="resolution">The requested resolution of data</param>
        /// <param name="density">The requested data density</param>
        /// <returns>A new <see cref="DateTime"/> that is after <paramref name="previous"/> according to the specified <paramref name="resolution"/>
        /// and <paramref name="density"/> specified</returns>
        DateTime NextTickTime(Symbol symbol, DateTime previous, Resolution resolution, DataDensity density);

        /// <summary>
        /// Generates a random <see cref="Tick"/> that is at most the specified <paramref name="maximumPercentDeviation"/> away from the
        /// <paramref name="previousValue"/> and is of the requested <paramref name="tickType"/>
        /// </summary>
        /// <param name="symbol">The symbol of the generated tick</param>
        /// <param name="dateTime">The time of the generated tick</param>
        /// <param name="tickType">The type of <see cref="Tick"/> to be generated</param>
        /// <param name="previousValue">The previous price, used as a reference for generating
        /// new random prices for the next time step</param>
        /// <param name="maximumPercentDeviation">The maximum percentage to deviate from the
        /// <paramref name="previousValue"/>, for example, 1 would indicate a maximum of 1% deviation from the
        /// <paramref name="previousValue"/>. For a previous price of 100, this would yield a price between 99 and 101 inclusive</param>
        /// <returns>A random <see cref="Tick"/> value that is within the specified <paramref name="maximumPercentDeviation"/>
        /// from the <paramref name="previousValue"/></returns>
        Tick NextTick(Symbol symbol, DateTime dateTime, TickType tickType, decimal previousValue, decimal maximumPercentDeviation);

        /// <summary>
        /// Generates a new random <see cref="Symbol"/> object of the specified security type.
        /// </summary>
        /// <remarks>
        /// A valid implementation will keep track of generated symbol objects to ensure duplicates
        /// are not generated.
        /// </remarks>
        /// <exception cref="ArgumentException">Throw when specifying <see cref="SecurityType.Option"/> or
        /// <see cref="SecurityType.Future"/>. To generate symbols for the derivative security types, please
        /// use <see cref="NextOption"/> and <see cref="NextFuture"/> respectively</exception>
        /// <param name="securityType">The security type of the generated symbol</param>
        /// <param name="market"></param>
        /// <returns>A new symbol object of the specified security type</returns>
        Symbol NextSymbol(SecurityType securityType, string market);

        /// <summary>
        /// Generates a new random option <see cref="Symbol"/>. The generated option contract symbol will have an
        /// expiry between the specified <paramref name="minExpiry"/> and <paramref name="maxExpiry"/>. The strike
        /// price will be within the specified <paramref name="maximumStrikePriceDeviation"/> of the <paramref name="underlyingPrice"/>
        /// and should be rounded to reasonable value for the given price. For example, a price of 100 dollars would round
        /// to 5 dollar increments and a price of 5 dollars would round to 50 cent increments
        /// </summary>
        /// <remarks>
        /// Standard contracts expiry on the third Friday.
        /// Weekly contracts expiry every week on Friday
        /// </remarks>
        /// <param name="market"></param>
        /// <param name="minExpiry">The minimum expiry date, inclusive</param>
        /// <param name="maxExpiry">The maximum expiry date, inclusive</param>
        /// <param name="underlyingPrice">The option's current underlying price</param>
        /// <param name="maximumStrikePriceDeviation">The strike price's maximum percent deviation from the underlying price</param>
        /// <returns>A new option contract symbol within the specified expiration and strike price parameters</returns>
        Symbol NextOption(string market, DateTime minExpiry, DateTime maxExpiry, decimal underlyingPrice, decimal maximumStrikePriceDeviation);

        /// <summary>
        /// Generates a new random future <see cref="Symbol"/>. The generates future contract symbol will have an
        /// expiry between the specified <paramref name="minExpiry"/> and <paramref name="maxExpiry"/>.
        /// </summary>
        /// <param name="market"></param>
        /// <param name="minExpiry">The minimum expiry date, inclusive</param>
        /// <param name="maxExpiry">The maximum expiry date, inclusive</param>
        /// <returns>A new future contract symbol with the specified expiration parameters</returns>
        Symbol NextFuture(string market, DateTime minExpiry, DateTime maxExpiry);
    }
}