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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using com.fxcm.fix;
using com.fxcm.fix.pretrade;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Packets;
using HistoryRequest = QuantConnect.Data.HistoryRequest;

namespace QuantConnect.Brokerages.Fxcm
{
    /// <summary>
    /// FXCM brokerage - implementation of IHistoryProvider interface
    /// </summary>
    public partial class FxcmBrokerage
    {
        private readonly IList<BaseData> _lastHistoryChunk = new List<BaseData>();

        #region IHistoryProvider implementation

        /// <summary>
        /// Gets the total number of data points emitted by this history provider
        /// </summary>
        public int DataPointCount { get; private set; }

        /// <summary>
        /// Initializes this history provider to work for the specified job
        /// </summary>
        /// <param name="job">The job</param>
        /// <param name="mapFileProvider">Provider used to get a map file resolver to handle equity mapping</param>
        /// <param name="factorFileProvider">Provider used to get factor files to handle equity price scaling</param>
        /// <param name="statusUpdate">Function used to send status updates</param>
        public void Initialize(AlgorithmNodePacket job, IMapFileProvider mapFileProvider, IFactorFileProvider factorFileProvider, Action<int> statusUpdate)
        {
        }

        /// <summary>
        /// Gets the history for the requested securities
        /// </summary>
        /// <param name="requests">The historical data requests</param>
        /// <param name="sliceTimeZone">The time zone used when time stamping the slice instances</param>
        /// <returns>An enumerable of the slices of data covering the span specified in each request</returns>
        public IEnumerable<Slice> GetHistory(IEnumerable<HistoryRequest> requests, DateTimeZone sliceTimeZone)
        {
            foreach (var request in requests)
            {
                var interval = ToFxcmInterval(request.Resolution);

                // download data
                var history = new List<BaseData>();

                var end = request.EndTimeUtc;

                while (end > request.StartTimeUtc)
                {
                    _lastHistoryChunk.Clear();

                    var mdr = new MarketDataRequest();
                    mdr.setSubscriptionRequestType(SubscriptionRequestTypeFactory.SNAPSHOT);
                    mdr.setResponseFormat(IFixMsgTypeDefs.__Fields.MSGTYPE_FXCMRESPONSE);
                    mdr.setFXCMTimingInterval(interval);
                    mdr.setMDEntryTypeSet(MarketDataRequest.MDENTRYTYPESET_ALL);

                    mdr.setFXCMStartDate(new UTCDate(ToJavaDateUtc(request.StartTimeUtc)));
                    mdr.setFXCMStartTime(new UTCTimeOnly(ToJavaDateUtc(request.StartTimeUtc)));
                    mdr.setFXCMEndDate(new UTCDate(ToJavaDateUtc(end)));
                    mdr.setFXCMEndTime(new UTCTimeOnly(ToJavaDateUtc(end)));
                    mdr.addRelatedSymbol(_fxcmInstruments[_symbolMapper.GetBrokerageSymbol(request.Symbol)]);

                    AutoResetEvent autoResetEvent;
                    lock (_locker)
                    {
                        _currentRequest = _gateway.sendMessage(mdr);
                        autoResetEvent = new AutoResetEvent(false);
                        _mapRequestsToAutoResetEvents[_currentRequest] = autoResetEvent;
                        _pendingHistoryRequests.Add(_currentRequest);
                    }
                    if (!autoResetEvent.WaitOne(ResponseTimeout))
                    {
                        // no response, exit
                        break;
                    }

                    // Add data
                    history.InsertRange(0, _lastHistoryChunk.Where(x => x.Time.Date >= request.StartTimeUtc.Date));

                    if (end != _lastHistoryChunk[0].Time)
                    {
                        // new end date = first datapoint date.
                        end = _lastHistoryChunk[0].Time;
                    }
                    else
                    {
                        break;
                    }
                }

                DataPointCount += history.Count;

                foreach (var data in history)
                {
                    yield return new Slice(data.EndTime, new[] { data });
                }
            }
        }

        #endregion

    }
}
