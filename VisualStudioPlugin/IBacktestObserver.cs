using System;

namespace QuantConnect.VisualStudioPlugin
{
    internal interface IBacktestObserver
    {
        void NewBacktest(int projectId, string backtestId, string backtestName, DateTime creationDateTime);
        void UpdateBacktest(int projectId, string backtestId, decimal progress);
        void BacktestFinished(int projectId, string backtestId, bool succeeded);
    }
}
