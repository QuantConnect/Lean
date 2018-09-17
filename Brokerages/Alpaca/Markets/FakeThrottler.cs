/*
 * The official C# API client for alpaca brokerage
 * https://github.com/alpacahq/alpaca-trade-api-csharp
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed class FakeThrottler : IThrottler
    {
        private FakeThrottler() { }

        public static IThrottler Instance { get; } = new FakeThrottler();

        public Int32 MaxAttempts => 1;

        public void WaitToProceed() { }
    }
}