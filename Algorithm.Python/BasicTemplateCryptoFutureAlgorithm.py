# QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
# Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

from AlgorithmImports import *

### <summary>
### Minute resolution regression algorithm trading Coin and USDT binance futures long and short asserting the behavior
### </summary>
class BasicTemplateCryptoFutureAlgorithm(QCAlgorithm):
    # <summary>
    # Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
    # </summary>

    def Initialize(self):
        self.SetStartDate(2022, 12, 13)
        self.SetEndDate(2022, 12, 13)

        self.SetTimeZone(TimeZones.Utc)

        try:
            self.SetBrokerageModel(BrokerageName.BinanceFutures, AccountType.Cash)
        except:
            # expected, we don't allow cash account type
            pass

        self.SetBrokerageModel(BrokerageName.BinanceFutures, AccountType.Margin)

        self.btcUsd = self.AddCryptoFuture("BTCUSD")
        self.adaUsdt = self.AddCryptoFuture("ADAUSDT")

        self.fast = self.EMA(self.btcUsd.Symbol, 30, Resolution.Minute)
        self.slow = self.EMA(self.btcUsd.Symbol, 60, Resolution.Minute)

        self.interestPerSymbol = {self.btcUsd.Symbol: 0, self.adaUsdt.Symbol: 0}

        self.SetCash(1000000)

        # the amount of BTC we need to hold to trade 'BTCUSD'
        self.btcUsd.BaseCurrency.SetAmount(0.005)
        # the amount of USDT we need to hold to trade 'ADAUSDT'
        self.adaUsdt.QuoteCurrency.SetAmount(200)

    # <summary>
    # OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
    # </summary>
    # <param name="data">Slice object keyed by symbol containing the stock data</param>
    def OnData(self, slice):
        interestRates = slice.Get(MarginInterestRate);
        for interestRate in interestRates:
            self.interestPerSymbol[interestRate.Key] += 1
            self.cachedInterestRate = self.Securities[interestRate.Key].Cache.GetData[MarginInterestRate]()
            if self.cachedInterestRate != interestRate.Value:
                raise Exception(f"Unexpected cached margin interest rate for {interestRate.Key}!")
            
        if self.fast > self.slow:
            if self.Portfolio.Invested == False and self.Transactions.OrdersCount == 0:
                self.ticket = self.Buy(self.btcUsd.Symbol, 50)
                if self.ticket.Status != OrderStatus.Invalid:
                    raise Exception(f"Unexpected valid order {self.ticket}, should fail due to margin not sufficient")
                
                self.Buy(self.btcUsd.Symbol, 1)

                self.marginUsed = self.Portfolio.TotalMarginUsed
                self.btcUsdHoldings = self.btcUsd.Holdings
                # Coin futures value is 100 USD
                self.holdingsValueBtcUsd = 100

                if abs(self.btcUsdHoldings.TotalSaleVolume - self.holdingsValueBtcUsd) > 1:
                    raise Exception(f"Unexpected TotalSaleVolume {self.btcUsdHoldings.TotalSaleVolume}")
                
                if abs(self.btcUsdHoldings.AbsoluteHoldingsCost - self.holdingsValueBtcUsd) > 1:
                    raise Exception(f"Unexpected holdings cost {self.btcUsdHoldings.HoldingsCost}")
                
                # margin used is based on the maintenance rate
                if (abs(self.btcUsdHoldings.AbsoluteHoldingsCost * 0.05 - self.marginUsed) > 1) or (BuyingPowerModelExtensions.GetMaintenanceMargin(self.btcUsd.BuyingPowerModel, self.btcUsd) != self.marginUsed):
                    raise Exception(f"Unexpected margin used {self.marginUsed}")
                
                self.Buy(self.adaUsdt.Symbol, 1000)

                self.marginUsed = self.Portfolio.TotalMarginUsed - self.marginUsed
                self.adaUsdtHoldings = self.adaUsdt.Holdings

                # USDT/BUSD futures value is based on it's price
                self.holdingsValueUsdt = self.adaUsdt.Price * self.adaUsdt.SymbolProperties.ContractMultiplier * 1000

                if abs(self.adaUsdtHoldings.TotalSaleVolume - self.holdingsValueUsdt) > 1:
                    raise Exception(f"Unexpected TotalSaleVolume {self.adaUsdtHoldings.TotalSaleVolume}")
                
                if abs(self.adaUsdtHoldings.AbsoluteHoldingsCost - self.holdingsValueUsdt) > 1:
                    raise Exception(f"Unexpected holdings cost {self.adaUsdtHoldings.HoldingsCost}")
                
                if (abs(self.adaUsdtHoldings.AbsoluteHoldingsCost * 0.05 - self.marginUsed) > 1) or (BuyingPowerModelExtensions.GetMaintenanceMargin(self.adaUsdt.BuyingPowerModel, self.adaUsdt) != self.marginUsed):
                    raise Exception(f"Unexpected margin used {self.marginUsed}")
                
                # position just opened should be just spread here
                self.profit = self.Portfolio.TotalUnrealizedProfit
                
                if (5 - abs(self.profit)) < 0:
                    raise Exception(f"Unexpected TotalUnrealizedProfit {self.Portfolio.TotalUnrealizedProfit}")

                if (self.Portfolio.TotalProfit != 0):
                    raise Exception(f"Unexpected TotalProfit {self.Portfolio.TotalProfit}")
                
        else:
            if self.Time.hour > 10 and self.Transactions.OrdersCount == 3:
                self.Sell(self.btcUsd.Symbol, 3)
                self.btcUsdHoldings = self.btcUsd.Holdings
                if abs(self.btcUsdHoldings.AbsoluteHoldingsCost - 100 * 2) > 1:
                    raise Exception(f"Unexpected holdings cost {self.btcUsdHoldings.HoldingsCost}")

                self.Sell(self.adaUsdt.Symbol, 3000)
                adaUsdtHoldings = self.adaUsdt.Holdings

                # USDT/BUSD futures value is based on it's price
                holdingsValueUsdt = self.adaUsdt.Price * self.adaUsdt.SymbolProperties.ContractMultiplier * 2000

                if abs(adaUsdtHoldings.AbsoluteHoldingsCost - holdingsValueUsdt) > 1:
                    raise Exception(f"Unexpected holdings cost {adaUsdtHoldings.HoldingsCost}")

                # position just opened should be just spread here
                profit = self.Portfolio.TotalUnrealizedProfit
                if (5 - abs(profit)) < 0:
                    raise Exception(f"Unexpected TotalUnrealizedProfit {self.Portfolio.TotalUnrealizedProfit}")
                # we barely did any difference on the previous trade
                if (5 - abs(self.Portfolio.TotalProfit)) < 0:
                    raise Exception(f"Unexpected TotalProfit {self.Portfolio.TotalProfit}")

                                            
    def OnEndOfAlgorithm(self):
        if self.interestPerSymbol[self.adaUsdt.Symbol] != 1:
                raise Exception(f"Unexpected interest rate count {self.interestPerSymbol[self.adaUsdt.Symbol]}")
        if self.interestPerSymbol[self.btcUsd.Symbol] != 3:
                raise Exception(f"Unexpected interest rate count {self.interestPerSymbol[self.btcUsd.Symbol]}")

    def OnOrderEvent(self, orderEvent):
        self.Debug("{0} {1}".format(self.Time, orderEvent))
