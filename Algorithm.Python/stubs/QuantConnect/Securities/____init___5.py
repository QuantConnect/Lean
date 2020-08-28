from .____init___6 import *
import typing
import System.Linq.Expressions
import System.Dynamic
import System.Collections.Generic
import System.Collections.Concurrent
import System.Collections
import System
import QuantConnect.Securities.Interfaces
import QuantConnect.Securities
import QuantConnect.Orders.Slippage
import QuantConnect.Orders.Fills
import QuantConnect.Orders.Fees
import QuantConnect.Orders
import QuantConnect.Interfaces
import QuantConnect.Indicators
import QuantConnect.Data.UniverseSelection
import QuantConnect.Data.Market
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect.Brokerages
import QuantConnect.Algorithm.Framework.Portfolio
import QuantConnect
import Python.Runtime
import NodaTime
import datetime



class ReservedBuyingPowerForPositionParameters(System.object):
    """
    Defines the parameters for QuantConnect.Securities.IBuyingPowerModel.GetReservedBuyingPowerForPosition(QuantConnect.Securities.ReservedBuyingPowerForPositionParameters)
    
    ReservedBuyingPowerForPositionParameters(security: Security)
    """
    def ResultInAccountCurrency(self, reservedBuyingPower: float) -> QuantConnect.Securities.ReservedBuyingPowerForPosition:
        pass

    def __init__(self, security: QuantConnect.Securities.Security) -> QuantConnect.Securities.ReservedBuyingPowerForPositionParameters:
        pass

    Security: QuantConnect.Securities.Security



class Security(System.object, QuantConnect.Interfaces.ISecurityPrice):
    """
    A base vehicle properties class for providing a common interface to all assets in QuantConnect.
    
    Security(exchangeHours: SecurityExchangeHours, config: SubscriptionDataConfig, quoteCurrency: Cash, symbolProperties: SymbolProperties, currencyConverter: ICurrencyConverter, registeredTypesProvider: IRegisteredSecurityDataTypesProvider, cache: SecurityCache)
    Security(symbol: Symbol, exchangeHours: SecurityExchangeHours, quoteCurrency: Cash, symbolProperties: SymbolProperties, currencyConverter: ICurrencyConverter, registeredTypesProvider: IRegisteredSecurityDataTypesProvider, cache: SecurityCache)
    """
    def GetLastData(self) -> QuantConnect.Data.BaseData:
        pass

    def IsCustomData(self) -> bool:
        pass

    def RefreshDataNormalizationModeProperty(self) -> None:
        pass

    @typing.overload
    def SetBuyingPowerModel(self, buyingPowerModel: QuantConnect.Securities.IBuyingPowerModel) -> None:
        pass

    @typing.overload
    def SetBuyingPowerModel(self, pyObject: Python.Runtime.PyObject) -> None:
        pass

    def SetBuyingPowerModel(self, *args) -> None:
        pass

    def SetDataNormalizationMode(self, mode: QuantConnect.DataNormalizationMode) -> None:
        pass

    @typing.overload
    def SetFeeModel(self, feelModel: QuantConnect.Orders.Fees.IFeeModel) -> None:
        pass

    @typing.overload
    def SetFeeModel(self, feelModel: Python.Runtime.PyObject) -> None:
        pass

    def SetFeeModel(self, *args) -> None:
        pass

    @typing.overload
    def SetFillModel(self, fillModel: QuantConnect.Orders.Fills.IFillModel) -> None:
        pass

    @typing.overload
    def SetFillModel(self, fillModel: Python.Runtime.PyObject) -> None:
        pass

    def SetFillModel(self, *args) -> None:
        pass

    def SetLeverage(self, leverage: float) -> None:
        pass

    def SetLocalTimeKeeper(self, localTimeKeeper: QuantConnect.LocalTimeKeeper) -> None:
        pass

    @typing.overload
    def SetMarginModel(self, marginModel: QuantConnect.Securities.IBuyingPowerModel) -> None:
        pass

    @typing.overload
    def SetMarginModel(self, pyObject: Python.Runtime.PyObject) -> None:
        pass

    def SetMarginModel(self, *args) -> None:
        pass

    def SetMarketPrice(self, data: QuantConnect.Data.BaseData) -> None:
        pass

    def SetRealTimePrice(self, data: QuantConnect.Data.BaseData) -> None:
        pass

    @typing.overload
    def SetSlippageModel(self, slippageModel: QuantConnect.Orders.Slippage.ISlippageModel) -> None:
        pass

    @typing.overload
    def SetSlippageModel(self, slippageModel: Python.Runtime.PyObject) -> None:
        pass

    def SetSlippageModel(self, *args) -> None:
        pass

    @typing.overload
    def SetVolatilityModel(self, volatilityModel: QuantConnect.Securities.IVolatilityModel) -> None:
        pass

    @typing.overload
    def SetVolatilityModel(self, volatilityModel: Python.Runtime.PyObject) -> None:
        pass

    def SetVolatilityModel(self, *args) -> None:
        pass

    def ToString(self) -> str:
        pass

    def Update(self, data: typing.List[QuantConnect.Data.BaseData], dataType: type, containsFillForwardData: typing.Optional[bool]) -> None:
        pass

    @typing.overload
    def __init__(self, exchangeHours: QuantConnect.Securities.SecurityExchangeHours, config: QuantConnect.Data.SubscriptionDataConfig, quoteCurrency: QuantConnect.Securities.Cash, symbolProperties: QuantConnect.Securities.SymbolProperties, currencyConverter: QuantConnect.Securities.ICurrencyConverter, registeredTypesProvider: QuantConnect.Securities.IRegisteredSecurityDataTypesProvider, cache: QuantConnect.Securities.SecurityCache) -> QuantConnect.Securities.Security:
        pass

    @typing.overload
    def __init__(self, symbol: QuantConnect.Symbol, exchangeHours: QuantConnect.Securities.SecurityExchangeHours, quoteCurrency: QuantConnect.Securities.Cash, symbolProperties: QuantConnect.Securities.SymbolProperties, currencyConverter: QuantConnect.Securities.ICurrencyConverter, registeredTypesProvider: QuantConnect.Securities.IRegisteredSecurityDataTypesProvider, cache: QuantConnect.Securities.SecurityCache) -> QuantConnect.Securities.Security:
        pass

    def __init__(self, *args) -> QuantConnect.Securities.Security:
        pass

    AskPrice: float

    AskSize: float

    BidPrice: float

    BidSize: float

    BuyingPowerModel: QuantConnect.Securities.IBuyingPowerModel

    Cache: QuantConnect.Securities.SecurityCache

    Close: float

    Data: object

    DataFilter: QuantConnect.Securities.Interfaces.ISecurityDataFilter

    DataNormalizationMode: QuantConnect.DataNormalizationMode

    Exchange: QuantConnect.Securities.SecurityExchange

    FeeModel: QuantConnect.Orders.Fees.IFeeModel

    FillModel: QuantConnect.Orders.Fills.IFillModel

    Fundamentals: QuantConnect.Data.Fundamental.Fundamentals

    HasData: bool

    High: float

    Holdings: QuantConnect.Securities.SecurityHolding

    HoldStock: bool

    Invested: bool

    IsDelisted: bool

    IsExtendedMarketHours: bool

    IsFillDataForward: bool

    IsTradable: bool

    Leverage: float

    LocalTime: datetime.datetime

    Low: float

    MarginModel: QuantConnect.Securities.IBuyingPowerModel

    Open: float

    OpenInterest: int

    PortfolioModel: QuantConnect.Securities.ISecurityPortfolioModel

    Price: float

    PriceVariationModel: QuantConnect.Securities.IPriceVariationModel

    QuoteCurrency: QuantConnect.Securities.Cash

    Resolution: QuantConnect.Resolution

    SettlementModel: QuantConnect.Securities.ISettlementModel

    SlippageModel: QuantConnect.Orders.Slippage.ISlippageModel

    SubscriptionDataConfig: QuantConnect.Data.SubscriptionDataConfig

    Subscriptions: typing.List[QuantConnect.Data.SubscriptionDataConfig]

    Symbol: QuantConnect.Symbol

    SymbolProperties: QuantConnect.Securities.SymbolProperties

    Type: QuantConnect.SecurityType

    VolatilityModel: QuantConnect.Securities.IVolatilityModel

    Volume: float

    SubscriptionsBag: System.Collections.Concurrent.ConcurrentBag[QuantConnect.Data.SubscriptionDataConfig]

    NullLeverage: Decimal


class SecurityCache(System.object):
    """
    Base class caching caching spot for security data and any other temporary properties.
    
    SecurityCache()
    """
    def AddData(self, data: QuantConnect.Data.BaseData) -> None:
        pass

    def AddDataList(self, data: typing.List[QuantConnect.Data.BaseData], dataType: type, containsFillForwardData: typing.Optional[bool]) -> None:
        pass

    def GetAll(self) -> typing.List[QuantConnect.Securities.T]:
        pass

    @typing.overload
    def GetData(self) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def GetData(self) -> QuantConnect.Securities.T:
        pass

    def GetData(self, *args) -> QuantConnect.Securities.T:
        pass

    def HasData(self, type: type) -> bool:
        pass

    def Reset(self) -> None:
        pass

    @staticmethod
    def ShareTypeCacheInstance(sourceToShare: QuantConnect.Securities.SecurityCache, targetToModify: QuantConnect.Securities.SecurityCache) -> None:
        pass

    def StoreData(self, data: typing.List[QuantConnect.Data.BaseData], dataType: type) -> None:
        pass

    def TryGetValue(self, type: type, data: typing.List) -> bool:
        pass

    AskPrice: float

    AskSize: float

    BidPrice: float

    BidSize: float

    Close: float

    High: float

    Low: float

    Open: float

    OpenInterest: int

    Price: float

    Volume: float



class SecurityCacheDataStoredEventArgs(System.EventArgs):
    """
    Event args for SecurityCache.DataStored event
    
    SecurityCacheDataStoredEventArgs(dataType: Type, data: IReadOnlyList[BaseData])
    """
    def __init__(self, dataType: type, data: typing.List[QuantConnect.Data.BaseData]) -> QuantConnect.Securities.SecurityCacheDataStoredEventArgs:
        pass

    Data: typing.List[QuantConnect.Data.BaseData]

    DataType: type



class SecurityCacheProvider(System.object):
    """
    A helper class that will provide QuantConnect.Securities.SecurityCache instances
    
    SecurityCacheProvider(securityProvider: ISecurityProvider)
    """
    def GetSecurityCache(self, symbol: QuantConnect.Symbol) -> QuantConnect.Securities.SecurityCache:
        pass

    def __init__(self, securityProvider: QuantConnect.Securities.ISecurityProvider) -> QuantConnect.Securities.SecurityCacheProvider:
        pass


class SecurityDatabaseKey(System.object, System.IEquatable[SecurityDatabaseKey]):
    """
    Represents the key to a single entry in the QuantConnect.Securities.MarketHoursDatabase or the QuantConnect.Securities.SymbolPropertiesDatabase
    
    SecurityDatabaseKey(market: str, symbol: str, securityType: SecurityType)
    """
    @typing.overload
    def Equals(self, other: QuantConnect.Securities.SecurityDatabaseKey) -> bool:
        pass

    @typing.overload
    def Equals(self, obj: object) -> bool:
        pass

    def Equals(self, *args) -> bool:
        pass

    def GetHashCode(self) -> int:
        pass

    @staticmethod
    def Parse(key: str) -> QuantConnect.Securities.SecurityDatabaseKey:
        pass

    def ToString(self) -> str:
        pass

    def __init__(self, market: str, symbol: str, securityType: QuantConnect.SecurityType) -> QuantConnect.Securities.SecurityDatabaseKey:
        pass

    Market: str
    SecurityType: QuantConnect.SecurityType
    Symbol: str
    Wildcard: str
