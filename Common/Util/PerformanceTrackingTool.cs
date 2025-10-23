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
 *
*/

using System;
using QuantConnect.Interfaces;
using System.Runtime.CompilerServices;

namespace QuantConnect.Util
{
    /// <summary>
    /// Helper class to track algorithm performance
    /// </summary>
    public class PerformanceTrackingTool
    {
        private Series _onDataSeries;
        private Series _dataSubscriptionSeries;
        private Series _scheduleSeries;
        private Series _selectionSeries;
        private Series _sliceCreationSeries;
        private Series _wallTimeSeries;
        private Series _securityUpdatesSeries;
        private Series _consolidatorsSeries;
        private Series _transactionSeries;
        private Series _splitsDividendsDelistingSeries;

        private Series _activeSecuritiesCount;
        private Series _consumedDataPointsCount;
        private Series _consumedHistoryDataPointsCount;

        private Series _cpuUsage;
        private Series _managedRamUsage;
        private Series _totalRamUsage;

        private PerformanceTimer _onData;
        private PerformanceTimer _dataSubscription;
        private PerformanceTimer _schedule;
        private PerformanceTimer _selection;
        private PerformanceTimer _securityUpdates;
        private PerformanceTimer _sliceCreation;
        private PerformanceTimer _consolidators;
        private PerformanceTimer _transactions;
        private PerformanceTimer _splitsDividendsDelisting;
        private bool _sampleEnabled;

        private IAlgorithm _algorithm;
        private long _previousDps;
        private long _previousHistoryDps;
        private DateTime _startWallTime;
        private DateTime _nextSampleAlgoTime;
        private DateTime _previousSampleAlgoTime;

        private DateTime _previousSampleWallTime;

        /// <summary>
        /// Gets the number of data points processed per second
        /// </summary>
        public long DataPoints { get; private set; }

        /// <summary>
        /// Gets the number of data points of algorithm history provider
        /// </summary>
        public int HistoryDataPoints => _algorithm?.HistoryProvider?.DataPointCount ?? 0;

        public void Initialize(IAlgorithm algorithm)
        {
            _algorithm = algorithm;
            _sampleEnabled = algorithm.Settings.PerformanceSamplePeriod > TimeSpan.Zero;
            if (_sampleEnabled)
            {
                _onData = new();
                _dataSubscription = new();
                _schedule = new();
                _selection = new();
                _sliceCreation = new();
                _consolidators = new();
                _securityUpdates = new();
                _transactions = new();
                _splitsDividendsDelisting = new();

                var chart = new Chart("Performance");

                _onDataSeries = new Series(PerformanceTarget.OnData.ToString(), unit: "Δ");
                _dataSubscriptionSeries = new Series(PerformanceTarget.Subscriptions.ToString(), unit: "Δ");
                _scheduleSeries = new Series(PerformanceTarget.Schedule.ToString(), unit: "Δ");
                _selectionSeries = new Series(PerformanceTarget.Selection.ToString(), unit: "Δ");
                _sliceCreationSeries = new Series(PerformanceTarget.Slice.ToString(), unit: "Δ");
                _consolidatorsSeries = new Series(PerformanceTarget.Consolidators.ToString(), unit: "Δ");
                _securityUpdatesSeries = new Series(PerformanceTarget.Securities.ToString(), unit: "Δ");
                _transactionSeries = new Series(PerformanceTarget.Transactions.ToString(), unit: "Δ");
                _splitsDividendsDelistingSeries = new Series(PerformanceTarget.SplitsDividendsDelisting.ToString(), unit: "Δ");
                _wallTimeSeries = new Series("WallTime", unit: "Δ");
                _activeSecuritiesCount = new Series("ActiveSecurities", unit: "#");
                _consumedDataPointsCount = new Series("DataPoints", SeriesType.Bar, 1, unit: "#");
                _consumedHistoryDataPointsCount = new Series("HistoryDataPoints", SeriesType.Bar, 1, unit: "#");
                _cpuUsage = new Series("CPU", unit: "%");
                _managedRamUsage = new Series("ManagedRAM", unit: string.Empty);
                _totalRamUsage = new Series("TotalRAM", unit: string.Empty);

                chart.AddSeries(_cpuUsage);
                chart.AddSeries(_managedRamUsage);
                chart.AddSeries(_totalRamUsage);
                chart.AddSeries(_onDataSeries);
                chart.AddSeries(_consolidatorsSeries);
                chart.AddSeries(_dataSubscriptionSeries);
                chart.AddSeries(_scheduleSeries);
                chart.AddSeries(_wallTimeSeries);
                chart.AddSeries(_securityUpdatesSeries);
                chart.AddSeries(_selectionSeries);
                chart.AddSeries(_sliceCreationSeries);
                chart.AddSeries(_activeSecuritiesCount);
                chart.AddSeries(_consumedDataPointsCount);
                chart.AddSeries(_consumedHistoryDataPointsCount);
                chart.AddSeries(_transactionSeries);
                chart.AddSeries(_splitsDividendsDelistingSeries);
                algorithm.AddChart(chart);

                _previousSampleWallTime = _startWallTime = DateTime.UtcNow;
                _previousSampleAlgoTime = algorithm.UtcTime;
                _nextSampleAlgoTime = _previousSampleAlgoTime + _algorithm.Settings.PerformanceSamplePeriod;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Sample(int dataPointCount, DateTime utcAlgoTime)
        {
            DataPoints += dataPointCount;
            if (!_sampleEnabled)
            {
                return;
            }

            if (utcAlgoTime >= _nextSampleAlgoTime)
            {
                var nowUtc = DateTime.UtcNow;

                // these share the same unit, real wall time
                _onDataSeries.AddPoint(utcAlgoTime, _onData.GetAndReset());
                _dataSubscriptionSeries.AddPoint(utcAlgoTime, _dataSubscription.GetAndReset());
                _scheduleSeries.AddPoint(utcAlgoTime, _schedule.GetAndReset());
                _selectionSeries.AddPoint(utcAlgoTime, _selection.GetAndReset());
                _sliceCreationSeries.AddPoint(utcAlgoTime, _sliceCreation.GetAndReset());
                _consolidatorsSeries.AddPoint(utcAlgoTime, _consolidators.GetAndReset());
                _securityUpdatesSeries.AddPoint(utcAlgoTime, _securityUpdates.GetAndReset());
                _transactionSeries.AddPoint(utcAlgoTime, _transactions.GetAndReset());
                _splitsDividendsDelistingSeries.AddPoint(utcAlgoTime, _splitsDividendsDelisting.GetAndReset());
                _wallTimeSeries.AddPoint(utcAlgoTime, (decimal)Math.Round((nowUtc - _previousSampleWallTime).TotalSeconds, 2));

                _activeSecuritiesCount.AddPoint(utcAlgoTime, _algorithm.UniverseManager.ActiveSecurities.Count);
                _consumedDataPointsCount.AddPoint(utcAlgoTime, DataPoints - _previousDps);
                _consumedHistoryDataPointsCount.AddPoint(utcAlgoTime, HistoryDataPoints - _previousHistoryDps);

                _cpuUsage.AddPoint(utcAlgoTime, (int)OS.CpuUsage);
                _managedRamUsage.AddPoint(utcAlgoTime, OS.TotalPhysicalMemoryUsed);
                _totalRamUsage.AddPoint(utcAlgoTime, OS.ApplicationMemoryUsed);

                _previousHistoryDps = HistoryDataPoints;
                _previousDps = DataPoints;
                _previousSampleWallTime = nowUtc;
                _nextSampleAlgoTime = utcAlgoTime + _algorithm.Settings.PerformanceSamplePeriod;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start(PerformanceTarget target)
        {
            if (!_sampleEnabled)
            {
                return;
            }
            Get(target).Start();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Stop(PerformanceTarget target)
        {
            if (!_sampleEnabled)
            {
                return;
            }
            Get(target).Stop();
        }

        public void Shutdown()
        {
            if (!_sampleEnabled)
            {
                return;
            }

            var endTime = DateTime.UtcNow;
            var message = $"Dps {DataPoints}. HistoryDps {HistoryDataPoints}." +
                $" TotalRuntime: {(endTime - _startWallTime):hh\\:mm\\:ss}." +
                $" OnData: {_onData.GetTotalTime()}s." +
                $" DataSubscription: {_dataSubscription.GetTotalTime()}s." +
                $" SliceCreation: {_sliceCreation.GetTotalTime()}s." +
                $" Selection: {_selection.GetTotalTime()}s." +
                $" Schedule: {_schedule.GetTotalTime()}s." +
                $" Consolidators: {_consolidators.GetTotalTime()}s." +
                $" Securities: {_securityUpdates.GetTotalTime()}s." +
                $" Transactions: {_transactions.GetTotalTime()}s." +
                $" SplitsDividendsDelisting: {_splitsDividendsDelisting.GetTotalTime()}s." +
                $" ActiveSecurities: {_algorithm.UniverseManager.ActiveSecurities.Count}";

            Logging.Log.Trace($"PerformanceTrackingTool.Summary(): {message}");
            _algorithm.Debug(message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private PerformanceTimer Get(PerformanceTarget target)
        {
            switch (target)
            {
                case PerformanceTarget.Subscriptions:
                    return _dataSubscription;
                case PerformanceTarget.Slice:
                    return _sliceCreation;
                case PerformanceTarget.Selection:
                    return _selection;
                case PerformanceTarget.Schedule:
                    return _schedule;
                case PerformanceTarget.OnData:
                    return _onData;
                case PerformanceTarget.Consolidators:
                    return _consolidators;
                case PerformanceTarget.Securities:
                    return _securityUpdates;
                case PerformanceTarget.Transactions:
                    return _transactions;
                case PerformanceTarget.SplitsDividendsDelisting:
                    return _splitsDividendsDelisting;
                default:
                    throw new ArgumentException(nameof(target));
            }
        }
    }

    public enum PerformanceTarget
    {
        Selection,
        Subscriptions,
        Slice,
        OnData,
        Schedule,
        Consolidators,
        Securities,
        Transactions,
        SplitsDividendsDelisting,
    }
}
