# encoding: utf-8
# module QuantConnect.Securities.Cfd calls itself Cfd
# from QuantConnect.Common, Version=2.4.0.0, Culture=neutral, PublicKeyToken=null
# by generator 1.145
# no doc

# imports
import datetime
import QuantConnect
import QuantConnect.Data
import QuantConnect.Securities
import QuantConnect.Securities.Cfd
import System.Collections.Concurrent
import typing

# no functions
# classes

class Cfd(QuantConnect.Securities.Security, QuantConnect.Interfaces.ISecurityPrice):
    """
    CFD Security Object Implementation for CFD Assets
    
    Cfd(exchangeHours: SecurityExchangeHours, quoteCurrency: Cash, config: SubscriptionDataConfig, symbolProperties: SymbolProperties, currencyConverter: ICurrencyConverter, registeredTypes: IRegisteredSecurityDataTypesProvider)
    Cfd(symbol: Symbol, exchangeHours: SecurityExchangeHours, quoteCurrency: Cash, symbolProperties: SymbolProperties, currencyConverter: ICurrencyConverter, registeredTypes: IRegisteredSecurityDataTypesProvider, securityCache: SecurityCache)
    """
    @typing.overload
    def __init__(self, exchangeHours: QuantConnect.Securities.SecurityExchangeHours, quoteCurrency: QuantConnect.Securities.Cash, config: QuantConnect.Data.SubscriptionDataConfig, symbolProperties: QuantConnect.Securities.SymbolProperties, currencyConverter: QuantConnect.Securities.ICurrencyConverter, registeredTypes: QuantConnect.Securities.IRegisteredSecurityDataTypesProvider) -> QuantConnect.Securities.Cfd.Cfd:
        pass

    @typing.overload
    def __init__(self, symbol: QuantConnect.Symbol, exchangeHours: QuantConnect.Securities.SecurityExchangeHours, quoteCurrency: QuantConnect.Securities.Cash, symbolProperties: QuantConnect.Securities.SymbolProperties, currencyConverter: QuantConnect.Securities.ICurrencyConverter, registeredTypes: QuantConnect.Securities.IRegisteredSecurityDataTypesProvider, securityCache: QuantConnect.Securities.SecurityCache) -> QuantConnect.Securities.Cfd.Cfd:
        pass

    def __init__(self, *args) -> QuantConnect.Securities.Cfd.Cfd:
        pass

    ContractMultiplier: float

    MinimumPriceVariation: float

    SubscriptionsBag: System.Collections.Concurrent.ConcurrentBag[QuantConnect.Data.SubscriptionDataConfig]


class CfdCache(QuantConnect.Securities.SecurityCache):
    """
    CFD specific caching support
    
    CfdCache()
    """

class CfdDataFilter(QuantConnect.Securities.SecurityDataFilter, QuantConnect.Securities.Interfaces.ISecurityDataFilter):
    """
    CFD packet by packet data filtering mechanism for dynamically detecting bad ticks.
    
    CfdDataFilter()
    """

class CfdExchange(QuantConnect.Securities.SecurityExchange):
    """
    CFD exchange class - information and helper tools for CFD exchange properties
    
    CfdExchange(exchangeHours: SecurityExchangeHours)
    """
    def __init__(self, exchangeHours: QuantConnect.Securities.SecurityExchangeHours) -> QuantConnect.Securities.Cfd.CfdExchange:
        pass

    TradingDaysPerYear: int



class CfdHolding(QuantConnect.Securities.SecurityHolding):
    """
    CFD holdings implementation of the base securities class
    
    CfdHolding(security: Cfd, currencyConverter: ICurrencyConverter)
    """
    def __init__(self, security: QuantConnect.Securities.Cfd.Cfd, currencyConverter: QuantConnect.Securities.ICurrencyConverter) -> QuantConnect.Securities.Cfd.CfdHolding:
        pass



