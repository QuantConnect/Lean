using System;
using QuantConnect.Data.Market;
using System.Collections.Generic;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    public interface ITickGenerator
    {
        Symbol Symbol { get; }
        IEnumerable<IEnumerable<Tick>> GenerateTicks();

        /// <summary>
        /// Generates a random <see cref="Tick"/> that is at most the specified <paramref name="maximumPercentDeviation"/> away from the
        /// <paramref name="previousValue"/> and is of the requested <paramref name="tickType"/>
        /// </summary>
        /// <param name="dateTime">The time of the generated tick</param>
        /// <param name="tickType">The type of <see cref="Tick"/> to be generated</param>
        /// <param name="previousValue">The previous price, used as a reference for generating
        /// new random prices for the next time step</param>
        /// <param name="maximumPercentDeviation">The maximum percentage to deviate from the
        /// <paramref name="previousValue"/>, for example, 1 would indicate a maximum of 1% deviation from the
        /// <paramref name="previousValue"/>. For a previous price of 100, this would yield a price between 99 and 101 inclusive</param>
        /// <returns>A random <see cref="Tick"/> value that is within the specified <paramref name="maximumPercentDeviation"/>
        /// from the <paramref name="previousValue"/></returns>
        Tick NextTick(
            DateTime dateTime,
            TickType tickType,
            decimal previousValue,
            decimal maximumPercentDeviation
            );

        /// <summary>
        /// Generates a random <see cref="DateTime"/> suitable for use as a tick's emit time.
        /// If the density provided is <see cref="DataDensity.Dense"/>, then at least one tick will be generated per <paramref name="resolution"/> step.
        /// If the density provided is <see cref="DataDensity.Sparse"/>, then at least one tick will be generated every 5 <paramref name="resolution"/> steps.
        /// if the density provided is <see cref="DataDensity.VerySparse"/>, then at least one tick will be generated every 50 <paramref name="resolution"/> steps.
        /// Times returned are guaranteed to be within market hours for the specified Symbol
        /// </summary>
        /// <param name="previous">The previous tick time</param>
        /// <param name="resolution">The requested resolution of data</param>
        /// <param name="density">The requested data density</param>
        /// <returns>A new <see cref="DateTime"/> that is after <paramref name="previous"/> according to the specified <paramref name="resolution"/>
        /// and <paramref name="density"/> specified</returns>
        DateTime NextTickTime( DateTime previous, Resolution resolution, DataDensity density);


    }
}
