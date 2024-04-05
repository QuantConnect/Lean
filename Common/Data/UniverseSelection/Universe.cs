/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Data.UniverseSelection
{
    /// <summary>
    /// Provides a base class for all universes to derive from.
    /// </summary>
    public abstract class Universe : IDisposable
    {
        /// <summary>
        /// Used to round the members time in universe <see cref="CanRemoveMember"/>, this is
        /// done because we can not guarantee exact selection time in live mode, see GH issue 3287
        /// </summary>
        private TimeSpan? _minimumTimeInUniverseRoundingInterval;

        /// <summary>
        /// Gets a value indicating that no change to the universe should be made
        /// </summary>
        public static readonly UnchangedUniverse Unchanged = UnchangedUniverse.Instance;

        private HashSet<Symbol> _previousSelections;

        /// <summary>
        /// Gets the internal security collection used to define membership in this universe
        /// </summary>
        public virtual ConcurrentDictionary<Symbol, Member> Securities
        {
            get;
            private set;
        }

        /// <summary>
        /// The currently selected symbol set
        /// </summary>
        /// <remarks>This set might be different than <see cref="Securities"/> which might hold members that are no longer selected
        /// but have not been removed yet, this can be because they have some open position, orders, haven't completed the minimum time in universe</remarks>
        public HashSet<Symbol> Selected
        {
            get; set;
        }

        /// <summary>
        /// True if this universe filter can run async in the data stack
        /// </summary>
        public virtual bool Asynchronous
        {
            get
            {
                if (UniverseSettings.Asynchronous.HasValue)
                {
                    return UniverseSettings.Asynchronous.Value;
                }
                return false;
            }
            set
            {
                UniverseSettings.Asynchronous = value;
            }
        }

        /// <summary>
        /// Event fired when the universe selection has changed
        /// </summary>
        public event EventHandler SelectionChanged;

        /// <summary>
        /// Gets the security type of this universe
        /// </summary>
        public SecurityType SecurityType => Configuration.SecurityType;

        /// <summary>
        /// Gets the market of this universe
        /// </summary>
        public string Market => Configuration.Market;

        /// <summary>
        /// Gets the symbol of this universe
        /// </summary>
        public Symbol Symbol => Configuration.Symbol;

        /// <summary>
        /// Gets the data type of this universe
        /// </summary>
        public Type DataType => Configuration.Type;

        /// <summary>
        /// Flag indicating if disposal of this universe has been requested
        /// </summary>
        public virtual bool DisposeRequested
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets the settings used for subscriptions added for this universe
        /// </summary>
        public virtual UniverseSettings UniverseSettings
        {
            get; set;
        }

        /// <summary>
        /// Gets the configuration used to get universe data
        /// </summary>
        public virtual SubscriptionDataConfig Configuration
        {
            get; private set;
        }

        /// <summary>
        /// Gets the current listing of members in this universe. Modifications
        /// to this dictionary do not change universe membership.
        /// </summary>
        public Dictionary<Symbol, Security> Members
        {
            get { return Securities.Select(x => x.Value.Security).ToDictionary(x => x.Symbol); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Universe"/> class
        /// </summary>
        /// <param name="config">The configuration used to source data for this universe</param>
        protected Universe(SubscriptionDataConfig config)
        {
            _previousSelections = new HashSet<Symbol>();
            Securities = new ConcurrentDictionary<Symbol, Member>();

            Configuration = config;
        }

        /// <summary>
        /// Determines whether or not the specified security can be removed from
        /// this universe. This is useful to prevent securities from being taken
        /// out of a universe before the algorithm has had enough time to make
        /// decisions on the security
        /// </summary>
        /// <param name="utcTime">The current utc time</param>
        /// <param name="security">The security to check if its ok to remove</param>
        /// <returns>True if we can remove the security, false otherwise</returns>
        public virtual bool CanRemoveMember(DateTime utcTime, Security security)
        {
            // can always remove securities after dispose requested
            if (DisposeRequested)
            {
                return true;
            }

            // can always remove delisted securities from the universe
            if (security.IsDelisted)
            {
                return true;
            }

            Member member;
            if (Securities.TryGetValue(security.Symbol, out member))
            {
                if (_minimumTimeInUniverseRoundingInterval == null)
                {
                    // lets set _minimumTimeInUniverseRoundingInterval once
                    _minimumTimeInUniverseRoundingInterval = UniverseSettings.MinimumTimeInUniverse;
                    AdjustMinimumTimeInUniverseRoundingInterval();
                }

                var timeInUniverse = utcTime - member.Added;
                if (timeInUniverse.Round(_minimumTimeInUniverseRoundingInterval.Value)
                    >= UniverseSettings.MinimumTimeInUniverse)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Performs universe selection using the data specified
        /// </summary>
        /// <param name="utcTime">The current utc time</param>
        /// <param name="data">The symbols to remain in the universe</param>
        /// <returns>The data that passes the filter</returns>
        public IEnumerable<Symbol> PerformSelection(DateTime utcTime, BaseDataCollection data)
        {
            // select empty set of symbols after dispose requested
            if (DisposeRequested)
            {
                OnSelectionChanged();
                return Enumerable.Empty<Symbol>();
            }

            var selections = data.FilteredContracts;
            if (selections == null)
            {
                // only trigger selection if it hasn't already been run
                var result = SelectSymbols(utcTime, data);
                if (ReferenceEquals(result, Unchanged))
                {
                    data.FilteredContracts = _previousSelections;
                    return Unchanged;
                }

                selections = result.ToHashSet();
                data.FilteredContracts = selections;
            }

            var hasDiffs = _previousSelections.AreDifferent(selections);
            _previousSelections = selections;
            if (!hasDiffs)
            {
                return Unchanged;
            }

            OnSelectionChanged(selections);
            return selections;
        }

        /// <summary>
        /// Performs universe selection using the data specified
        /// </summary>
        /// <param name="utcTime">The current utc time</param>
        /// <param name="data">The symbols to remain in the universe</param>
        /// <returns>The data that passes the filter</returns>
        public abstract IEnumerable<Symbol> SelectSymbols(DateTime utcTime, BaseDataCollection data);

        /// <summary>
        /// Creates and configures a security for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol of the security to be created</param>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="marketHoursDatabase">The market hours database</param>
        /// <param name="symbolPropertiesDatabase">The symbol properties database</param>
        /// <returns>The newly initialized security object</returns>
        /// <obsolete>The CreateSecurity won't be called</obsolete>
        [Obsolete("CreateSecurity is obsolete and will not be called. The system will create the required Securities based on selected symbols")]
        public virtual Security CreateSecurity(Symbol symbol, IAlgorithm algorithm, MarketHoursDatabase marketHoursDatabase, SymbolPropertiesDatabase symbolPropertiesDatabase)
        {
            throw new InvalidOperationException("CreateSecurity is obsolete and should not be called." +
                "The system will create the required Securities based on selected symbols");
        }

        /// <summary>
        /// Gets the subscription requests to be added for the specified security
        /// </summary>
        /// <param name="security">The security to get subscriptions for</param>
        /// <param name="currentTimeUtc">The current time in utc. This is the frontier time of the algorithm</param>
        /// <param name="maximumEndTimeUtc">The max end time</param>
        /// <returns>All subscriptions required by this security</returns>
        [Obsolete("This overload is obsolete and will not be called. It was not capable of creating new SubscriptionDataConfig due to lack of information")]
        public virtual IEnumerable<SubscriptionRequest> GetSubscriptionRequests(Security security, DateTime currentTimeUtc, DateTime maximumEndTimeUtc)
        {
            throw new InvalidOperationException("This overload is obsolete and should not be called." +
                "It was not capable of creating new SubscriptionDataConfig due to lack of information");
        }


        /// <summary>
        /// Gets the subscription requests to be added for the specified security
        /// </summary>
        /// <param name="security">The security to get subscriptions for</param>
        /// <param name="currentTimeUtc">The current time in utc. This is the frontier time of the algorithm</param>
        /// <param name="maximumEndTimeUtc">The max end time</param>
        /// <param name="subscriptionService">Instance which implements <see cref="ISubscriptionDataConfigService"/> interface</param>
        /// <returns>All subscriptions required by this security</returns>
        public virtual IEnumerable<SubscriptionRequest> GetSubscriptionRequests(Security security,
            DateTime currentTimeUtc,
            DateTime maximumEndTimeUtc,
            ISubscriptionDataConfigService subscriptionService)
        {
            var result = subscriptionService.Add(security.Symbol,
                UniverseSettings.Resolution,
                UniverseSettings.FillForward,
                UniverseSettings.ExtendedMarketHours,
                dataNormalizationMode: UniverseSettings.DataNormalizationMode,
                subscriptionDataTypes: UniverseSettings.SubscriptionDataTypes,
                dataMappingMode: UniverseSettings.DataMappingMode,
                contractDepthOffset: (uint)Math.Abs(UniverseSettings.ContractDepthOffset));
            return result.Select(config => new SubscriptionRequest(isUniverseSubscription: false,
                universe: this,
                security: security,
                configuration: config,
                startTimeUtc: currentTimeUtc,
                endTimeUtc: maximumEndTimeUtc));
        }

        /// <summary>
        /// Determines whether or not the specified symbol is currently a member of this universe
        /// </summary>
        /// <param name="symbol">The symbol whose membership is to be checked</param>
        /// <returns>True if the specified symbol is part of this universe, false otherwise</returns>
        public bool ContainsMember(Symbol symbol)
        {
            return Securities.ContainsKey(symbol);
        }

        /// <summary>
        /// Adds the specified security to this universe
        /// </summary>
        /// <param name="utcTime">The current utc date time</param>
        /// <param name="security">The security to be added</param>
        /// <param name="isInternal">True if internal member</param>
        /// <returns>True if the security was successfully added,
        /// false if the security was already in the universe</returns>
        internal virtual bool AddMember(DateTime utcTime, Security security, bool isInternal)
        {
            // never add members to disposed universes
            if (DisposeRequested)
            {
                return false;
            }

            if (security.IsDelisted)
            {
                return false;
            }

            return Securities.TryAdd(security.Symbol, new Member(utcTime, security, isInternal));
        }

        /// <summary>
        /// Tries to remove the specified security from the universe. This
        /// will first check to verify that we can remove the security by
        /// calling the <see cref="CanRemoveMember"/> function.
        /// </summary>
        /// <param name="utcTime">The current utc time</param>
        /// <param name="security">The security to be removed</param>
        /// <returns>True if the security was successfully removed, false if
        /// we're not allowed to remove or if the security didn't exist</returns>
        internal virtual bool RemoveMember(DateTime utcTime, Security security)
        {
            if (CanRemoveMember(utcTime, security))
            {
                Member member;
                return Securities.TryRemove(security.Symbol, out member);
            }
            return false;
        }

        /// <summary>
        /// Marks this universe as disposed and ready to remove all child subscriptions
        /// </summary>
        public virtual void Dispose()
        {
            DisposeRequested = true;
        }

        /// <summary>
        /// Event invocator for the <see cref="SelectionChanged"/> event
        /// </summary>
        /// <param name="selection">The current universe selection</param>
        protected void OnSelectionChanged(HashSet<Symbol> selection = null)
        {
            SelectionChanged?.Invoke(this, new SelectionEventArgs(selection ?? new HashSet<Symbol>()));
        }

        /// <summary>
        /// Provides a value to indicate that no changes should be made to the universe.
        /// This value is intended to be returned by reference via <see cref="Universe.SelectSymbols"/>
        /// </summary>
        public sealed class UnchangedUniverse : IEnumerable<string>, IEnumerable<Symbol>
        {
            /// <summary>
            /// Read-only instance of the <see cref="UnchangedUniverse"/> value
            /// </summary>
            public static readonly UnchangedUniverse Instance = new UnchangedUniverse();
            private UnchangedUniverse() { }
            IEnumerator<Symbol> IEnumerable<Symbol>.GetEnumerator() { yield break; }
            IEnumerator<string> IEnumerable<string>.GetEnumerator() { yield break; }
            IEnumerator IEnumerable.GetEnumerator() { yield break; }
        }

        /// <summary>
        /// Will adjust the <see cref="_minimumTimeInUniverseRoundingInterval"/>
        /// so rounding is performed as expected
        /// </summary>
        private void AdjustMinimumTimeInUniverseRoundingInterval()
        {
            if (_minimumTimeInUniverseRoundingInterval >= Time.OneDay)
            {
                _minimumTimeInUniverseRoundingInterval = Time.OneDay;
            }
            else if (_minimumTimeInUniverseRoundingInterval >= Time.OneHour)
            {
                _minimumTimeInUniverseRoundingInterval = Time.OneHour;
            }
            else if (_minimumTimeInUniverseRoundingInterval >= Time.OneMinute)
            {
                _minimumTimeInUniverseRoundingInterval = Time.OneMinute;
            }
            else if (_minimumTimeInUniverseRoundingInterval >= Time.OneSecond)
            {
                _minimumTimeInUniverseRoundingInterval = Time.OneSecond;
            }
        }

        /// <summary>
        /// Member of the Universe
        /// </summary>
        public sealed class Member
        {
            /// <summary>
            /// DateTime when added
            /// </summary>
            public readonly DateTime Added;

            /// <summary>
            /// The security that was added
            /// </summary>
            public readonly Security Security;

            /// <summary>
            /// True if the security was added as internal by this universe
            /// </summary>
            public readonly bool IsInternal;

            /// <summary>
            /// Initialize a new member for the universe
            /// </summary>
            /// <param name="added">DateTime added</param>
            /// <param name="security">Security to add</param>
            /// <param name="isInternal">True if internal member</param>
            public Member(DateTime added, Security security, bool isInternal)
            {
                Added = added;
                Security = security;
                IsInternal = isInternal;
            }
        }

        /// <summary>
        /// Event fired when the universe selection changes
        /// </summary>
        public class SelectionEventArgs : EventArgs
        {
            /// <summary>
            /// The current universe selection
            /// </summary>
            public HashSet<Symbol> CurrentSelection { get; }

            /// <summary>
            /// Creates a new instance
            /// </summary>
            public SelectionEventArgs(HashSet<Symbol> currentSelection)
            {
                CurrentSelection = currentSelection;
            }
        }
    }
}
