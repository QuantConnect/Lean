from datetime import timedelta

class BasicTemplateFuturesFeaturesAlgorithm(QCAlgorithm):
    ''' This example demonstrates how to get access to futures contract features (symbol, bid/ask price etc.)
        It also shows how to get access to futures history for a given underlying
        and get those features in history request '''

    def Initialize(self):
        self.SetStartDate(2016, 8, 17)
        self.SetEndDate(2016, 8, 20)
        self.SetCash(1000000)

        # Subscribe and set our expiry filter for the futures chain
        # find the front contract expiring no earlier than in 90 days
        self.roots = [Futures.Indices.SP500EMini, Futures.Metals.Gold]
        for root in self.roots:
        	self.AddFuture(root,Resolution.Minute).SetFilter(timedelta(0), timedelta(182))

        self.SetBenchmark("SPY")


    def OnData(self,slice):
        if not self.Portfolio.Invested:
            for chain in slice.FutureChains:
            	for contract in chain.Value:
            	    self.Log("{0},Bid={1} Ask={2} Last={3} OI={4}".format(
                             contract.Symbol.Value,
                             contract.BidPrice,
                             contract.AskPrice,
                             contract.LastPrice,
                             contract.OpenInterest))

    def OnOrderEvent(self, orderEvent):
    	# Order fill event handler. On an order fill update the resulting information is passed to this method.
        # Order event details containing details of the events
        self.Log(str(orderEvent))

    def OnSecuritiesChanged(self, changes):
		if changes == SecurityChanges.None: return
		for change in changes.AddedSecurities:
			history = self.History(change.Symbol, 1, Resolution.Minute)
			history = history.sortlevel(['time'], ascending=False)[:3]

			self.Log("History: " + str(history.index.get_level_values('symbol').values[0])
						+ ": " + str(history.index.get_level_values('time').values[0])
						+ " > " + str(history['close'].values))
