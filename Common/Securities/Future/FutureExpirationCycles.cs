using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Static class contains definitions of popular futures expiration cycles
    /// </summary>
    public static class FutureExpirationCycles
    {
        /// <summary>
        /// January Cycle: Expirations in January, April, July, October (the first month of each quarter)
        /// </summary>
        public static readonly int[] January = new int[] { 1, 4, 7, 10 };

        /// <summary>
        /// February Cycle: Expirations in February, May, August, November (second month)
        /// </summary>
        public static readonly int[] February = new int[] { 2, 5, 8, 11 };

        /// <summary>
        /// March Cycle: Expirations in March, June, September, December (third month)
        /// </summary>
        public static readonly int[] March = new int[] { 3, 6, 9, 12 };

        /// <summary>
        /// All Year Cycle: Expirations in every month of the year
        /// </summary>
        public static readonly int[] AllYear = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };

        /// <summary>
        /// HMUZ Cycle
        /// </summary>
        public static readonly int[] HMUZ = March;

        /// <summary>
        /// HKNUZ Cycle
        /// </summary>
        public static readonly int[] HKNUZ = new int[] { 3, 5, 7, 9, 12 };

        /// <summary>
        /// HKNUVZ Cycle
        /// </summary>
        public static readonly int[] HKNUVZ = new int[] { 3, 5, 7, 9, 10, 12 };

        /// <summary>
        /// FHKNQUVZ Cycle
        /// </summary>
        public static readonly int[] FHKNQUVZ = new int[] { 1, 3, 5, 7, 9, 10, 12 };

        /// <summary>
        /// FHKNQUX Cycle
        /// </summary>
        public static readonly int[] FHKNQUX = new int[] { 1, 3, 5, 7, 8, 9, 11 };

        /// <summary>
        /// FGHJKMNQUVXZ Cycle
        /// </summary>
        public static readonly int[] FGHJKMNQUVXZ = AllYear;


    }
}
