# encoding: utf-8
# module QuantConnect.Securities.Forex calls itself Forex
# from QuantConnect.Common, Version=2.4.0.0, Culture=neutral, PublicKeyToken=null
# by generator 1.145
# no doc

# imports
import datetime
import QuantConnect
import QuantConnect.Data
import QuantConnect.Securities
import QuantConnect.Securities.Forex
import System.Collections.Concurrent
import typing

# no functions
# classes

class Forex(QuantConnect.Securities.Security, QuantConnect.Securities.IBaseCurrencySymbol, QuantConnect.Interfaces.ISecurityPrice):
    """
    FOREX Security Object Implementation for FOREX Assets
    
    Forex(exchangeHours: SecurityExchangeHours, quoteCurrency: Cash, config: SubscriptionDataConfig, symbolProperties: SymbolProperties, currencyConverter: ICurrencyConverter, registeredTypes: IRegisteredSecurityDataTypesProvider)
    Forex(symbol: Symbol, exchangeHours: SecurityExchangeHours, quoteCurrency: Cash, symbolProperties: SymbolProperties, currencyConverter: ICurrencyConverter, registeredTypes: IRegisteredSecurityDataTypesProvider, securityCache: SecurityCache)
    """
    @staticmethod
    def DecomposeCurrencyPair(currencyPair: str, baseCurrency: str, quoteCurrency: str) -> None:
        pass

    @typing.overload
    def __init__(self, exchangeHours: QuantConnect.Securities.SecurityExchangeHours, quoteCurrency: QuantConnect.Securities.Cash, config: QuantConnect.Data.SubscriptionDataConfig, symbolProperties: QuantConnect.Securities.SymbolProperties, currencyConverter: QuantConnect.Securities.ICurrencyConverter, registeredTypes: QuantConnect.Securities.IRegisteredSecurityDataTypesProvider) -> QuantConnect.Securities.Forex.Forex:
        pass

    @typing.overload
    def __init__(self, symbol: QuantConnect.Symbol, exchangeHours: QuantConnect.Securities.SecurityExchangeHours, quoteCurrency: QuantConnect.Securities.Cash, symbolProperties: QuantConnect.Securities.SymbolProperties, currencyConverter: QuantConnect.Securities.ICurrencyConverter, registeredTypes: QuantConnect.Securities.IRegisteredSecurityDataTypesProvider, securityCache: QuantConnect.Securities.SecurityCache) -> QuantConnect.Securities.Forex.Forex:
        pass

    def __init__(self, *args) -> QuantConnect.Securities.Forex.Forex:
        pass

    BaseCurrencySymbol: str

    SubscriptionsBag: System.Collections.Concurrent.ConcurrentBag[QuantConnect.Data.SubscriptionDataConfig]


class ForexCache(QuantConnect.Securities.SecurityCache):
    """
    Forex specific caching support
    
    ForexCache()
    """

class ForexDataFilter(QuantConnect.Securities.SecurityDataFilter, QuantConnect.Securities.Interfaces.ISecurityDataFilter):
    """
    Forex packet by packet data filtering mechanism for dynamically detecting bad ticks.
    
    ForexDataFilter()
    """
    def Filter(self, vehicle: QuantConnect.Securities.Security, data: QuantConnect.Data.BaseData) -> bool:
        pass


class ForexExchange(QuantConnect.Securities.SecurityExchange):
    """
    Forex exchange class - information and helper tools for forex exchange properties
    
    ForexExchange()
    ForexExchange(exchangeHours: SecurityExchangeHours)
    """
    @typing.overload
    def __init__(self) -> QuantConnect.Securities.Forex.ForexExchange:
        pass

    @typing.overload
    def __init__(self, exchangeHours: QuantConnect.Securities.SecurityExchangeHours) -> QuantConnect.Securities.Forex.ForexExchange:
        pass

    def __init__(self, *args) -> QuantConnect.Securities.Forex.ForexExchange:
        pass

    TradingDaysPerYear: int



class ForexHolding(QuantConnect.Securities.SecurityHolding):
    """
    FOREX holdings implementation of the base securities class
    
    ForexHolding(security: Forex, currencyConverter: ICurrencyConverter)
    """
    def TotalCloseProfitPips(self) -> float:
        pass

    def __init__(self, security: QuantConnect.Securities.Forex.Forex, currencyConverter: QuantConnect.Securities.ICurrencyConverter) -> QuantConnect.Securities.Forex.ForexHolding:
        pass



