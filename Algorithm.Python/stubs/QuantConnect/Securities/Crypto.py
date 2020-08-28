# encoding: utf-8
# module QuantConnect.Securities.Crypto calls itself Crypto
# from QuantConnect.Common, Version=2.4.0.0, Culture=neutral, PublicKeyToken=null
# by generator 1.145
# no doc

# imports
import datetime
import QuantConnect
import QuantConnect.Data
import QuantConnect.Securities
import QuantConnect.Securities.Crypto
import System.Collections.Concurrent
import typing

# no functions
# classes

class Crypto(QuantConnect.Securities.Security, QuantConnect.Securities.IBaseCurrencySymbol, QuantConnect.Interfaces.ISecurityPrice):
    """
    Crypto Security Object Implementation for Crypto Assets
    
    Crypto(exchangeHours: SecurityExchangeHours, quoteCurrency: Cash, config: SubscriptionDataConfig, symbolProperties: SymbolProperties, currencyConverter: ICurrencyConverter, registeredTypes: IRegisteredSecurityDataTypesProvider)
    Crypto(symbol: Symbol, exchangeHours: SecurityExchangeHours, quoteCurrency: Cash, symbolProperties: SymbolProperties, currencyConverter: ICurrencyConverter, registeredTypes: IRegisteredSecurityDataTypesProvider, securityCache: SecurityCache)
    """
    @staticmethod
    def DecomposeCurrencyPair(symbol: QuantConnect.Symbol, symbolProperties: QuantConnect.Securities.SymbolProperties, baseCurrency: str, quoteCurrency: str) -> None:
        pass

    @typing.overload
    def __init__(self, exchangeHours: QuantConnect.Securities.SecurityExchangeHours, quoteCurrency: QuantConnect.Securities.Cash, config: QuantConnect.Data.SubscriptionDataConfig, symbolProperties: QuantConnect.Securities.SymbolProperties, currencyConverter: QuantConnect.Securities.ICurrencyConverter, registeredTypes: QuantConnect.Securities.IRegisteredSecurityDataTypesProvider) -> QuantConnect.Securities.Crypto.Crypto:
        pass

    @typing.overload
    def __init__(self, symbol: QuantConnect.Symbol, exchangeHours: QuantConnect.Securities.SecurityExchangeHours, quoteCurrency: QuantConnect.Securities.Cash, symbolProperties: QuantConnect.Securities.SymbolProperties, currencyConverter: QuantConnect.Securities.ICurrencyConverter, registeredTypes: QuantConnect.Securities.IRegisteredSecurityDataTypesProvider, securityCache: QuantConnect.Securities.SecurityCache) -> QuantConnect.Securities.Crypto.Crypto:
        pass

    def __init__(self, *args) -> QuantConnect.Securities.Crypto.Crypto:
        pass

    BaseCurrencySymbol: str

    Price: float

    SubscriptionsBag: System.Collections.Concurrent.ConcurrentBag[QuantConnect.Data.SubscriptionDataConfig]


class CryptoExchange(QuantConnect.Securities.SecurityExchange):
    """
    Crypto exchange class - information and helper tools for Crypto exchange properties
    
    CryptoExchange(market: str)
    CryptoExchange(exchangeHours: SecurityExchangeHours)
    """
    @typing.overload
    def __init__(self, market: str) -> QuantConnect.Securities.Crypto.CryptoExchange:
        pass

    @typing.overload
    def __init__(self, exchangeHours: QuantConnect.Securities.SecurityExchangeHours) -> QuantConnect.Securities.Crypto.CryptoExchange:
        pass

    def __init__(self, *args) -> QuantConnect.Securities.Crypto.CryptoExchange:
        pass


class CryptoHolding(QuantConnect.Securities.SecurityHolding):
    """
    Crypto holdings implementation of the base securities class
    
    CryptoHolding(security: Crypto, currencyConverter: ICurrencyConverter)
    """
    def __init__(self, security: QuantConnect.Securities.Crypto.Crypto, currencyConverter: QuantConnect.Securities.ICurrencyConverter) -> QuantConnect.Securities.Crypto.CryptoHolding:
        pass



