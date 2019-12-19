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

using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using System;
using System.Collections.Generic;
using System.Threading;

namespace QuantConnect.Report
{
    /// <summary>
    /// Fake synchronizer
    /// </summary>
    public class NullSynchronizer : ISynchronizer
    {
        private DateTime _frontierUtc;
        private readonly DateTime _endTimeUtc;
        private readonly List<BaseData> _data = new List<BaseData>();
        private readonly List<UpdateData<SubscriptionDataConfig>> _consolidatorUpdateData = new List<UpdateData<SubscriptionDataConfig>>();
        private readonly List<TimeSlice> _timeSlices = new List<TimeSlice>();
        private readonly TimeSpan _frontierStepSize = TimeSpan.FromSeconds(1);
        private readonly List<UpdateData<ISecurityPrice>> _securitiesUpdateData = new List<UpdateData<ISecurityPrice>>();
        public int Count => _timeSlices.Count;

        public NullSynchronizer(IAlgorithm algorithm)
        {
        }

        public IEnumerable<TimeSlice> StreamData(CancellationToken cancellationToken)
        {
            return _timeSlices;
        }

        private IEnumerable<TimeSlice> GenerateTimeSlices()
        {
            var bars = new TradeBars();
            var quotes = new QuoteBars();
            var ticks = new Ticks();
            var options = new OptionChains();
            var futures = new FuturesChains();
            var splits = new Splits();
            var dividends = new Dividends();
            var delistings = new Delistings();
            var symbolChanges = new SymbolChangedEvents();
            var dataFeedPackets = new List<DataFeedPacket>();
            var customData = new List<UpdateData<ISecurityPrice>>();
            var changes = SecurityChanges.None;
            do
            {
                var slice = new Slice(default(DateTime), _data, bars, quotes, ticks, options, futures, splits, dividends, delistings, symbolChanges);
                var timeSlice = new TimeSlice(_frontierUtc, _data.Count, slice, dataFeedPackets, _securitiesUpdateData, _consolidatorUpdateData, customData, changes, new Dictionary<Universe, BaseDataCollection>());
                yield return timeSlice;
                _frontierUtc += _frontierStepSize;
            }
            while (_frontierUtc <= _endTimeUtc);
        }
    }
}
