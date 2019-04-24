/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
*/

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed class FakeThrottler : IThrottler
    {
        private static readonly Lazy<Task> _completedTask = new Lazy<Task>(()=>Task.Run(()=>{}));

        private FakeThrottler() { }

        public static IThrottler Instance { get; } = new FakeThrottler();

        public Int32 MaxRetryAttempts { get; set; } = 1;

        public HashSet<Int32> RetryHttpStatuses { get; set; } = new HashSet<Int32>();

        public Task WaitToProceed() { return _completedTask.Value; }

        public Boolean CheckHttpResponse(HttpResponseMessage response) => true;
    }
}
