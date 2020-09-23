from .__Brokerages_1 import *
import typing
import System.Collections.Generic
import System
import QuantConnect.Securities
import QuantConnect.Packets
import QuantConnect.Orders.Slippage
import QuantConnect.Orders.Fills
import QuantConnect.Orders.Fees
import QuantConnect.Orders
import QuantConnect.Interfaces
import QuantConnect.Data.Market
import QuantConnect.Brokerages
import QuantConnect
import datetime

# no functions
# classes

class DefaultBrokerageModel(System.object, QuantConnect.Brokerages.IBrokerageModel):
    """
    Provides a default implementation of QuantConnect.Brokerages.IBrokerageModel that allows all orders and uses
                the default transaction models
    
    DefaultBrokerageModel(accountType: AccountType)
    """
    def ApplySplit(self, tickets: typing.List[QuantConnect.Orders.OrderTicket], split: QuantConnect.Data.Market.Split) -> None:
        pass

    def CanExecuteOrder(self, security: QuantConnect.Securities.Security, order: QuantConnect.Orders.Order) -> bool:
        pass

    def CanSubmitOrder(self, security: QuantConnect.Securities.Security, order: QuantConnect.Orders.Order, message: QuantConnect.Brokerages.BrokerageMessageEvent) -> bool:
        pass

    def CanUpdateOrder(self, security: QuantConnect.Securities.Security, order: QuantConnect.Orders.Order, request: QuantConnect.Orders.UpdateOrderRequest, message: QuantConnect.Brokerages.BrokerageMessageEvent) -> bool:
        pass

    @typing.overload
    def GetBuyingPowerModel(self, security: QuantConnect.Securities.Security) -> QuantConnect.Securities.IBuyingPowerModel:
        pass

    @typing.overload
    def GetBuyingPowerModel(self, security: QuantConnect.Securities.Security, accountType: QuantConnect.AccountType) -> QuantConnect.Securities.IBuyingPowerModel:
        pass

    def GetBuyingPowerModel(self, *args) -> QuantConnect.Securities.IBuyingPowerModel:
        pass

    def GetFeeModel(self, security: QuantConnect.Securities.Security) -> QuantConnect.Orders.Fees.IFeeModel:
        pass

    def GetFillModel(self, security: QuantConnect.Securities.Security) -> QuantConnect.Orders.Fills.IFillModel:
        pass

    def GetLeverage(self, security: QuantConnect.Securities.Security) -> float:
        pass

    @typing.overload
    def GetSettlementModel(self, security: QuantConnect.Securities.Security) -> QuantConnect.Securities.ISettlementModel:
        pass

    @typing.overload
    def GetSettlementModel(self, security: QuantConnect.Securities.Security, accountType: QuantConnect.AccountType) -> QuantConnect.Securities.ISettlementModel:
        pass

    def GetSettlementModel(self, *args) -> QuantConnect.Securities.ISettlementModel:
        pass

    def GetSlippageModel(self, security: QuantConnect.Securities.Security) -> QuantConnect.Orders.Slippage.ISlippageModel:
        pass

    def __init__(self, accountType: QuantConnect.AccountType) -> QuantConnect.Brokerages.DefaultBrokerageModel:
        pass

    AccountType: QuantConnect.AccountType

    DefaultMarkets: System.Collections.Generic.IReadOnlyDictionary[QuantConnect.SecurityType, str]

    RequiredFreeBuyingPowerPercent: float


    DefaultMarketMap: ReadOnlyDictionary[SecurityType, str]


class AlpacaBrokerageModel(QuantConnect.Brokerages.DefaultBrokerageModel, QuantConnect.Brokerages.IBrokerageModel):
    """
    Alpaca Brokerage Model Implementation for Back Testing.
    
    AlpacaBrokerageModel(orderProvider: IOrderProvider, accountType: AccountType)
    """
    def CanSubmitOrder(self, security: QuantConnect.Securities.Security, order: QuantConnect.Orders.Order, message: QuantConnect.Brokerages.BrokerageMessageEvent) -> bool:
        pass

    def GetFeeModel(self, security: QuantConnect.Securities.Security) -> QuantConnect.Orders.Fees.IFeeModel:
        pass

    def GetFillModel(self, security: QuantConnect.Securities.Security) -> QuantConnect.Orders.Fills.IFillModel:
        pass

    def GetSlippageModel(self, security: QuantConnect.Securities.Security) -> QuantConnect.Orders.Slippage.ISlippageModel:
        pass

    def __init__(self, orderProvider: QuantConnect.Securities.IOrderProvider, accountType: QuantConnect.AccountType) -> QuantConnect.Brokerages.AlpacaBrokerageModel:
        pass

    DefaultMarkets: System.Collections.Generic.IReadOnlyDictionary[QuantConnect.SecurityType, str]


    DefaultMarketMap: ReadOnlyDictionary[SecurityType, str]


class AlphaStreamsBrokerageModel(QuantConnect.Brokerages.DefaultBrokerageModel, QuantConnect.Brokerages.IBrokerageModel):
    """
    Provides properties specific to Alpha Streams
    
    AlphaStreamsBrokerageModel(accountType: AccountType)
    """
    def GetFeeModel(self, security: QuantConnect.Securities.Security) -> QuantConnect.Orders.Fees.IFeeModel:
        pass

    def GetLeverage(self, security: QuantConnect.Securities.Security) -> float:
        pass

    @typing.overload
    def GetSettlementModel(self, security: QuantConnect.Securities.Security) -> QuantConnect.Securities.ISettlementModel:
        pass

    @typing.overload
    def GetSettlementModel(self, security: QuantConnect.Securities.Security, accountType: QuantConnect.AccountType) -> QuantConnect.Securities.ISettlementModel:
        pass

    def GetSettlementModel(self, *args) -> QuantConnect.Securities.ISettlementModel:
        pass

    def GetSlippageModel(self, security: QuantConnect.Securities.Security) -> QuantConnect.Orders.Slippage.ISlippageModel:
        pass

    def __init__(self, accountType: QuantConnect.AccountType) -> QuantConnect.Brokerages.AlphaStreamsBrokerageModel:
        pass


class BitfinexBrokerageModel(QuantConnect.Brokerages.DefaultBrokerageModel, QuantConnect.Brokerages.IBrokerageModel):
    """
    Provides Bitfinex specific properties
    
    BitfinexBrokerageModel(accountType: AccountType)
    """
    @typing.overload
    def GetBuyingPowerModel(self, security: QuantConnect.Securities.Security) -> QuantConnect.Securities.IBuyingPowerModel:
        pass

    @typing.overload
    def GetBuyingPowerModel(self, security: QuantConnect.Securities.Security, accountType: QuantConnect.AccountType) -> QuantConnect.Securities.IBuyingPowerModel:
        pass

    def GetBuyingPowerModel(self, *args) -> QuantConnect.Securities.IBuyingPowerModel:
        pass

    def GetFeeModel(self, security: QuantConnect.Securities.Security) -> QuantConnect.Orders.Fees.IFeeModel:
        pass

    def GetLeverage(self, security: QuantConnect.Securities.Security) -> float:
        pass

    def __init__(self, accountType: QuantConnect.AccountType) -> QuantConnect.Brokerages.BitfinexBrokerageModel:
        pass

    DefaultMarkets: System.Collections.Generic.IReadOnlyDictionary[QuantConnect.SecurityType, str]



class BrokerageFactoryAttribute(System.Attribute, System.Runtime.InteropServices._Attribute):
    """
    Represents the brokerage factory type required to load a data queue handler
    
    BrokerageFactoryAttribute(type: Type)
    """
    def __init__(self, type: type) -> QuantConnect.Brokerages.BrokerageFactoryAttribute:
        pass

    Type: type



class BrokerageMessageEvent(System.object):
    """
    Represents a message received from a brokerage
    
    BrokerageMessageEvent(type: BrokerageMessageType, code: int, message: str)
    BrokerageMessageEvent(type: BrokerageMessageType, code: str, message: str)
    """
    @staticmethod
    def Disconnected(message: str) -> QuantConnect.Brokerages.BrokerageMessageEvent:
        pass

    @staticmethod
    def Reconnected(message: str) -> QuantConnect.Brokerages.BrokerageMessageEvent:
        pass

    def ToString(self) -> str:
        pass

    @typing.overload
    def __init__(self, type: QuantConnect.Brokerages.BrokerageMessageType, code: int, message: str) -> QuantConnect.Brokerages.BrokerageMessageEvent:
        pass

    @typing.overload
    def __init__(self, type: QuantConnect.Brokerages.BrokerageMessageType, code: str, message: str) -> QuantConnect.Brokerages.BrokerageMessageEvent:
        pass

    def __init__(self, *args) -> QuantConnect.Brokerages.BrokerageMessageEvent:
        pass

    Code: str

    Message: str

    Type: QuantConnect.Brokerages.BrokerageMessageType



class BrokerageMessageType(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Specifies the type of message received from an IBrokerage implementation
    
    enum BrokerageMessageType, values: Disconnect (4), Error (2), Information (0), Reconnect (3), Warning (1)
    """
    value__: int
    Disconnect: 'BrokerageMessageType'
    Error: 'BrokerageMessageType'
    Information: 'BrokerageMessageType'
    Reconnect: 'BrokerageMessageType'
    Warning: 'BrokerageMessageType'


class BrokerageModel(System.object):
    """ Provides factory method for creating an QuantConnect.Brokerages.IBrokerageModel from the QuantConnect.Brokerages.BrokerageName enum """
    @staticmethod
    def Create(orderProvider: QuantConnect.Securities.IOrderProvider, brokerage: QuantConnect.Brokerages.BrokerageName, accountType: QuantConnect.AccountType) -> QuantConnect.Brokerages.IBrokerageModel:
        pass

    __all__: list


class BrokerageName(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Specifices what transaction model and submit/execution rules to use
    
    enum BrokerageName, values: Alpaca (13), AlphaStreams (14), Bitfinex (5), Default (0), FxcmBrokerage (4), GDAX (12), InteractiveBrokersBrokerage (1), OandaBrokerage (3), QuantConnectBrokerage (0), TradierBrokerage (2)
    """
    value__: int
    Alpaca: 'BrokerageName'
    AlphaStreams: 'BrokerageName'
    Bitfinex: 'BrokerageName'
    Default: 'BrokerageName'
    FxcmBrokerage: 'BrokerageName'
    GDAX: 'BrokerageName'
    InteractiveBrokersBrokerage: 'BrokerageName'
    OandaBrokerage: 'BrokerageName'
    QuantConnectBrokerage: 'BrokerageName'
    TradierBrokerage: 'BrokerageName'


class DefaultBrokerageMessageHandler(System.object, QuantConnect.Brokerages.IBrokerageMessageHandler):
    """
    Provides a default implementation o QuantConnect.Brokerages.IBrokerageMessageHandler that will forward
                messages as follows:
                Information -> IResultHandler.Debug
                Warning     -> IResultHandler.Error && IApi.SendUserEmail
                Error       -> IResultHandler.Error && IAlgorithm.RunTimeError
    
    DefaultBrokerageMessageHandler(algorithm: IAlgorithm, job: AlgorithmNodePacket, api: IApi, initialDelay: Nullable[TimeSpan], openThreshold: Nullable[TimeSpan])
    """
    def Handle(self, message: QuantConnect.Brokerages.BrokerageMessageEvent) -> None:
        pass

    def __init__(self, algorithm: QuantConnect.Interfaces.IAlgorithm, job: QuantConnect.Packets.AlgorithmNodePacket, api: QuantConnect.Interfaces.IApi, initialDelay: typing.Optional[datetime.timedelta], openThreshold: typing.Optional[datetime.timedelta]) -> QuantConnect.Brokerages.DefaultBrokerageMessageHandler:
        pass
