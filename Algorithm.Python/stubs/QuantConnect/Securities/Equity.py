# encoding: utf-8
# module QuantConnect.Securities.Equity calls itself Equity
# from QuantConnect.Common, Version=2.4.0.0, Culture=neutral, PublicKeyToken=null
# by generator 1.145
# no doc

# imports
import datetime
import QuantConnect
import QuantConnect.Data
import QuantConnect.Securities
import QuantConnect.Securities.Equity
import System.Collections.Concurrent
import typing

# no functions
# classes

class Equity(QuantConnect.Securities.Security, QuantConnect.Interfaces.ISecurityPrice):
    """
    Equity Security Type : Extension of the underlying Security class for equity specific behaviours.
    
    Equity(symbol: Symbol, exchangeHours: SecurityExchangeHours, quoteCurrency: Cash, symbolProperties: SymbolProperties, currencyConverter: ICurrencyConverter, registeredTypes: IRegisteredSecurityDataTypesProvider, securityCache: SecurityCache)
    Equity(exchangeHours: SecurityExchangeHours, config: SubscriptionDataConfig, quoteCurrency: Cash, symbolProperties: SymbolProperties, currencyConverter: ICurrencyConverter, registeredTypes: IRegisteredSecurityDataTypesProvider)
    """
    def SetDataNormalizationMode(self, mode: QuantConnect.DataNormalizationMode) -> None:
        pass

    @typing.overload
    def __init__(self, symbol: QuantConnect.Symbol, exchangeHours: QuantConnect.Securities.SecurityExchangeHours, quoteCurrency: QuantConnect.Securities.Cash, symbolProperties: QuantConnect.Securities.SymbolProperties, currencyConverter: QuantConnect.Securities.ICurrencyConverter, registeredTypes: QuantConnect.Securities.IRegisteredSecurityDataTypesProvider, securityCache: QuantConnect.Securities.SecurityCache) -> QuantConnect.Securities.Equity.Equity:
        pass

    @typing.overload
    def __init__(self, exchangeHours: QuantConnect.Securities.SecurityExchangeHours, config: QuantConnect.Data.SubscriptionDataConfig, quoteCurrency: QuantConnect.Securities.Cash, symbolProperties: QuantConnect.Securities.SymbolProperties, currencyConverter: QuantConnect.Securities.ICurrencyConverter, registeredTypes: QuantConnect.Securities.IRegisteredSecurityDataTypesProvider) -> QuantConnect.Securities.Equity.Equity:
        pass

    def __init__(self, *args) -> QuantConnect.Securities.Equity.Equity:
        pass

    SubscriptionsBag: System.Collections.Concurrent.ConcurrentBag[QuantConnect.Data.SubscriptionDataConfig]
    DefaultSettlementDays: int
    DefaultSettlementTime: TimeSpan


class EquityCache(QuantConnect.Securities.SecurityCache):
    """
    Equity cache override.
    
    EquityCache()
    """

class EquityDataFilter(QuantConnect.Securities.SecurityDataFilter, QuantConnect.Securities.Interfaces.ISecurityDataFilter):
    """
    Equity security type data filter
    
    EquityDataFilter()
    """
    def Filter(self, vehicle: QuantConnect.Securities.Security, data: QuantConnect.Data.BaseData) -> bool:
        pass


class EquityExchange(QuantConnect.Securities.SecurityExchange):
    """
    Equity exchange information
    
    EquityExchange()
    EquityExchange(exchangeHours: SecurityExchangeHours)
    """
    @typing.overload
    def __init__(self) -> QuantConnect.Securities.Equity.EquityExchange:
        pass

    @typing.overload
    def __init__(self, exchangeHours: QuantConnect.Securities.SecurityExchangeHours) -> QuantConnect.Securities.Equity.EquityExchange:
        pass

    def __init__(self, *args) -> QuantConnect.Securities.Equity.EquityExchange:
        pass

    TradingDaysPerYear: int



class EquityHolding(QuantConnect.Securities.SecurityHolding):
    """
    Holdings class for equities securities: no specific properties here but it is a placeholder for future equities specific behaviours.
    
    EquityHolding(security: Security, currencyConverter: ICurrencyConverter)
    """
    def __init__(self, security: QuantConnect.Securities.Security, currencyConverter: QuantConnect.Securities.ICurrencyConverter) -> QuantConnect.Securities.Equity.EquityHolding:
        pass



