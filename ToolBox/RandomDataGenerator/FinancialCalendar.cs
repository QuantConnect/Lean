using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    public static class FinancialCalendar
    {
        /// <summary>
        /// Converts integer to month between 1-12.
        /// </summary>
        /// <param name="month">Month as integer</param>
        /// <returns><see cref="Month"/></returns>
        /// <remarks>
        /// In the case that the `month` passed is greater
        /// than 12 or less than or equal to 0, 
        /// then we consider the months as a year
        /// and calculate what month it would be. Example:
        /// 
        /// <code>
        /// var month = 15;
        /// month.ToMonth() == Month.March; // returns true
        /// </code>
        /// </remarks>
        public static int NormalizeMonth(this int month)
        {
            if (month > 12 || month <= 0)
            {
                if (month % 12 == 0)
                {
                    return 12;
                }
                // Use a real modulo instead of C#'s remainder operator
                return ((month % 12) + 12) % 12;
            }
            return month;
        }

        /// <summary>
        /// Gets the next month after the current month
        /// </summary>
        /// <param name="currentMonth">Starting month</param>
        /// <returns>Next month</returns>
        public static int NextMonth(this int currentMonth)
        {
            if (currentMonth == 12)
            {
                return 1;
            }
            return currentMonth + 1;
        }

        public static int PreviousMonth(this int currentMonth)
        {
            if (currentMonth == 1)
            {
                return 12;
            }
            return currentMonth - 1;
        }

        /// <summary>
        /// Calculate the next financial quarter
        /// </summary>
        /// <param name="currentMonth">Starting month</param>
        /// <returns>
        /// Returns the next quarter. If the current month is a quarter, we return
        /// the quarter that comes after it.
        /// </returns>
        public static int NextQuarter(this int currentMonth)
        {
            if (currentMonth.IsFinancialQuarter())
            {
                currentMonth = currentMonth.NextMonth();
            }
            while (!currentMonth.IsFinancialQuarter())
            {
                currentMonth = currentMonth.NextMonth();
            }
            return currentMonth;
        }

        /// <summary>
        /// Gets the previous quarter from the starting month
        /// </summary>
        /// <param name="currentMonth">Starting month</param>
        /// <returns>
        /// Returns the prvious quarter. If the current month is a quarter, we return
        /// the quarter that came before it.
        /// </returns>
        public static int PreviousQuarter(this int currentMonth)
        {
            if (currentMonth.IsFinancialQuarter())
            {
                currentMonth = currentMonth.PreviousMonth();
            }
            while (!currentMonth.IsFinancialQuarter())
            {
                currentMonth = currentMonth.PreviousMonth();
            }
            return currentMonth;
        }

        /// <summary>
        /// Gets the next financial statement month
        /// </summary>
        /// <param name="currentMonth">Starting month</param>
        /// <returns></returns>
        public static int NextFinancialStatement(this int currentMonth)
        {
            if (currentMonth.IsFinancialQuarter())
            {
                return currentMonth.NextMonth();
            }
            return currentMonth.NextQuarter().NextMonth();
        }

        /// <summary>
        /// Gets the previous financial statement month
        /// </summary>
        /// <param name="currentMonth">Starting month</param>
        /// <returns></returns>
        public static int PreviousFinancialStatement(this int currentMonth)
        {
            if (currentMonth.IsFinancialStatementMonth())
            {
                return currentMonth.PreviousMonth().PreviousQuarter().NextMonth();
            }
            return currentMonth.PreviousQuarter().NextMonth();
        }

        /// <summary>
        /// Determines whether the current month is a quarter
        /// </summary>
        /// <param name="currentMonth">Starting month</param>
        /// <returns><see cref="bool"/></returns>
        public static bool IsFinancialQuarter(this int currentMonth)
        {
            return currentMonth % 3 == 0;
        }

        /// <summary>
        /// Determins whether the current month is a financial statement month
        /// </summary>
        /// <param name="currentMonth">Starting month</param>
        /// <returns></returns>
        public static bool IsFinancialStatementMonth(this int currentMonth)
        {
            return currentMonth.PreviousMonth() % 3 == 0;
        }
    }
}
