using System;

namespace QuantConnect.Util
{
    /// <summary>
    /// Contains utils for decimals / ints etc - numbers
    /// </summary>
    public static class NumberUtils
    {
        /// <summary>
        /// returns amount of decimal places after comma. Example: 0.005 will return 3.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public static int GetDecimalPlaces(this decimal n)
        {
            n = Math.Abs(n); //make sure it is positive.
            n -= (int)n;     //remove the integer part of the number.
            var decimalPlaces = 0;
            while (n > 0)
            {
                decimalPlaces++;
                n *= 10;
                n -= (int)n;
            }
            return decimalPlaces;
        }
    }
}
