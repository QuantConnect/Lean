
from datetime import timedelta

class BasicTemplateFuturesConsolidationAlgorithm(QCAlgorithm):
    '''This example demonstrates how to use consolidator with futures for a given underlying.'''

    def Initialize(self):
        self.SetStartDate(2013, 10, 07)
        self.SetEndDate(2013, 10, 11)
        self.SetCash(1000000)

        # Subscribe and set our expiry filter for the futures chain
        future = self.AddFuture(Futures.Indices.SP500EMini)
        future.SetFilter(timedelta(0), timedelta(182))

        self._futureContracts = []

    def OnData(self,slice):
        for chain in slice.FutureChains:
            for contract in chain.Value:
                if contract.Symbol not in self._futureContracts:
                    self._futureContracts.append(contract.Symbol)

                    consolidator = QuoteBarConsolidator(timedelta(minutes=5))
                    consolidator.DataConsolidated += self.OnDataConsolidated
                    self.SubscriptionManager.AddConsolidator(contract.Symbol, consolidator)

                    self.Log("Added new consolidator for " + str(contract.Symbol.Value))

    def OnDataConsolidated(self, sender, quoteBar):
        self.Log("OnDataConsolidated called on " + str(self.Time))
        self.Log(str(quoteBar))
