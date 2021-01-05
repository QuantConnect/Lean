/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Encapsulates request parameters for <see cref="AlpacaTradingClient.ListAccountActivitiesAsync(AccountActivitiesRequest,System.Threading.CancellationToken)"/> call.
    /// </summary>
    public sealed class AccountActivitiesRequest : Validation.IRequest
    {
        private readonly List<AccountActivityType> _accountActivityTypes = new List<AccountActivityType>();

        /// <summary>
        /// Creates new instance of <see cref="AccountActivitiesRequest"/> object for all activity types.
        /// </summary>
        public AccountActivitiesRequest()
        {
        }

        /// <summary>
        /// Creates new instance of <see cref="AccountActivitiesRequest"/> object for a single activity types.
        /// </summary>
        /// <param name="activityType">The activity type you want to view entries for.</param>
        public AccountActivitiesRequest(
            AccountActivityType activityType)
        {
            _accountActivityTypes.Add(activityType);
        }

        /// <summary>
        /// Creates new instance of <see cref="BarSetRequest"/> object for several activity types.
        /// </summary>
        /// <param name="activityTypes">The list of activity types you want to view entries for.</param>
        public AccountActivitiesRequest(
            IEnumerable<AccountActivityType> activityTypes)
        {
            _accountActivityTypes.AddRange(activityTypes.Distinct());
        }

        /// <summary>
        /// Gets the activity types you want to view entries for. Empty list means 'all activity types'.
        /// </summary>
        public IReadOnlyList<AccountActivityType> ActivityTypes => _accountActivityTypes;

        /// <summary>
        /// Gets the date for which you want to see activities.
        /// </summary>
        public DateTime? Date { get; private set; }

        /// <summary>
        /// Gets the upper date limit for requesting only activities submitted before this date.
        /// </summary>
        public DateTime? Until { get; private set; }

        /// <summary>
        /// Gets the lover date limit for requesting only activities submitted after this date.
        /// </summary>
        public DateTime? After { get; private set; }

        /// <summary>
        /// Gets or sets the sorting direction for results.
        /// </summary>
        public SortDirection? Direction { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of entries to return in the response.
        /// </summary>
        public Int64? PageSize { get; set; }

        /// <summary>
        /// Gets or sets the ID of the end of your current page of results.
        /// </summary>
        public String PageToken { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public AccountActivitiesRequest SetSingleDate(
            DateTime date)
        {
            Date = date;
            After = null;
            Until = null;
            return this;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="dateFrom"></param>
        /// <param name="dateInto"></param>
        /// <returns></returns>
        public AccountActivitiesRequest SetInclusiveTimeInterval(
            DateTime? dateFrom,
            DateTime? dateInto)
        {
            After = dateFrom;
            Until = dateInto;
            Date = null;
            return this;
        }

        IEnumerable<RequestValidationException> Validation.IRequest.GetExceptions()
        {
            if (After.HasValue &&
                Until.HasValue &&
                After > Until)
            {
                yield return new RequestValidationException(
                    "Time interval should be valid.", nameof(After));
                yield return new RequestValidationException(
                    "Time interval should be valid.", nameof(Until));
            }
        }

        internal AccountActivitiesRequest SetTimes(
            DateTime? date = null,
            DateTime? after = null,
            DateTime? until = null)
        {
            if (ReferenceEquals(null, date))
            {
                return SetInclusiveTimeInterval(after, until);
            }

            if (until.HasValue || after.HasValue)
            {
                throw new ArgumentException("You unable to specify 'date' and 'until'/'after' arguments in same call.");
            }

            return SetSingleDate(date.Value);

        }
    }
}
