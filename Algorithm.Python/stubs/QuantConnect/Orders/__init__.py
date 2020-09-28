from .____init___1 import *
import typing
import System.Threading
import System
import QuantConnect.Securities
import QuantConnect.Orders.Serialization
import QuantConnect.Orders.Fees
import QuantConnect.Orders
import QuantConnect.Interfaces
import QuantConnect.Algorithm.Framework.Portfolio
import QuantConnect
import Newtonsoft.Json.Linq
import Newtonsoft.Json
import datetime

# no functions
# classes

class OrderProperties(System.object, QuantConnect.Interfaces.IOrderProperties):
    """
    Contains additional properties and settings for an order
    
    OrderProperties()
    """
    def Clone(self) -> QuantConnect.Interfaces.IOrderProperties:
        pass

    TimeInForce: QuantConnect.Orders.TimeInForce



class BitfinexOrderProperties(QuantConnect.Orders.OrderProperties, QuantConnect.Interfaces.IOrderProperties):
    """
    Contains additional properties and settings for an order submitted to Bitfinex brokerage
    
    BitfinexOrderProperties()
    """
    def Clone(self) -> QuantConnect.Interfaces.IOrderProperties:
        pass

    Hidden: bool

    PostOnly: bool



class OrderRequest(System.object):
    """ Represents a request to submit, update, or cancel an order """
    def SetResponse(self, response: QuantConnect.Orders.OrderResponse, status: QuantConnect.Orders.OrderRequestStatus) -> None:
        pass

    def ToString(self) -> str:
        pass

    def __init__(self, *args): #cannot find CLR constructor
        pass

    OrderId: int

    OrderRequestType: QuantConnect.Orders.OrderRequestType

    Response: QuantConnect.Orders.OrderResponse

    Status: QuantConnect.Orders.OrderRequestStatus

    Tag: str

    Time: datetime.datetime



class CancelOrderRequest(QuantConnect.Orders.OrderRequest):
    """
    Defines a request to cancel an order
    
    CancelOrderRequest(time: DateTime, orderId: int, tag: str)
    """
    def ToString(self) -> str:
        pass

    def __init__(self, time: datetime.datetime, orderId: int, tag: str) -> QuantConnect.Orders.CancelOrderRequest:
        pass

    OrderRequestType: QuantConnect.Orders.OrderRequestType



class GDAXOrderProperties(QuantConnect.Orders.OrderProperties, QuantConnect.Interfaces.IOrderProperties):
    """
    Contains additional properties and settings for an order submitted to GDAX brokerage
    
    GDAXOrderProperties()
    """
    def Clone(self) -> QuantConnect.Interfaces.IOrderProperties:
        pass

    PostOnly: bool



class InteractiveBrokersOrderProperties(QuantConnect.Orders.OrderProperties, QuantConnect.Interfaces.IOrderProperties):
    """
    Contains additional properties and settings for an order submitted to Interactive Brokers
    
    InteractiveBrokersOrderProperties()
    """
    def Clone(self) -> QuantConnect.Interfaces.IOrderProperties:
        pass

    Account: str

    FaGroup: str

    FaMethod: str

    FaPercentage: int

    FaProfile: str



class Order(System.object):
    """ Order struct for placing new trade """
    def ApplyUpdateOrderRequest(self, request: QuantConnect.Orders.UpdateOrderRequest) -> None:
        pass

    def Clone(self) -> QuantConnect.Orders.Order:
        pass

    @staticmethod
    def CreateOrder(request: QuantConnect.Orders.SubmitOrderRequest) -> QuantConnect.Orders.Order:
        pass

    @staticmethod
    def FromSerialized(serializedOrder: QuantConnect.Orders.Serialization.SerializedOrder) -> QuantConnect.Orders.Order:
        pass

    def GetValue(self, security: QuantConnect.Securities.Security) -> float:
        pass

    def ToString(self) -> str:
        pass

    def __init__(self, *args): #cannot find CLR constructor
        pass

    AbsoluteQuantity: float

    BrokerId: typing.List[str]

    CanceledTime: typing.Optional[datetime.datetime]

    ContingentId: int

    CreatedTime: datetime.datetime

    Direction: QuantConnect.Orders.OrderDirection

    Id: int

    IsMarketable: bool

    LastFillTime: typing.Optional[datetime.datetime]

    LastUpdateTime: typing.Optional[datetime.datetime]

    OrderSubmissionData: QuantConnect.Orders.OrderSubmissionData

    Price: float

    PriceCurrency: str

    Properties: QuantConnect.Interfaces.IOrderProperties

    Quantity: float

    SecurityType: QuantConnect.SecurityType

    Status: QuantConnect.Orders.OrderStatus

    Symbol: QuantConnect.Symbol

    Tag: str

    Time: datetime.datetime

    TimeInForce: QuantConnect.Orders.TimeInForce

    Type: QuantConnect.Orders.OrderType

    Value: float



class LimitOrder(QuantConnect.Orders.Order):
    """
    Limit order type definition
    
    LimitOrder()
    LimitOrder(symbol: Symbol, quantity: Decimal, limitPrice: Decimal, time: DateTime, tag: str, properties: IOrderProperties)
    """
    def ApplyUpdateOrderRequest(self, request: QuantConnect.Orders.UpdateOrderRequest) -> None:
        pass

    def Clone(self) -> QuantConnect.Orders.Order:
        pass

    def ToString(self) -> str:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Orders.LimitOrder:
        pass

    @typing.overload
    def __init__(self, symbol: QuantConnect.Symbol, quantity: float, limitPrice: float, time: datetime.datetime, tag: str, properties: QuantConnect.Interfaces.IOrderProperties) -> QuantConnect.Orders.LimitOrder:
        pass

    def __init__(self, *args) -> QuantConnect.Orders.LimitOrder:
        pass

    LimitPrice: float

    Type: QuantConnect.Orders.OrderType



class MarketOnCloseOrder(QuantConnect.Orders.Order):
    """
    Market on close order type - submits a market order on exchange close
    
    MarketOnCloseOrder()
    MarketOnCloseOrder(symbol: Symbol, quantity: Decimal, time: DateTime, tag: str, properties: IOrderProperties)
    """
    def Clone(self) -> QuantConnect.Orders.Order:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Orders.MarketOnCloseOrder:
        pass

    @typing.overload
    def __init__(self, symbol: QuantConnect.Symbol, quantity: float, time: datetime.datetime, tag: str, properties: QuantConnect.Interfaces.IOrderProperties) -> QuantConnect.Orders.MarketOnCloseOrder:
        pass

    def __init__(self, *args) -> QuantConnect.Orders.MarketOnCloseOrder:
        pass

    Type: QuantConnect.Orders.OrderType


    DefaultSubmissionTimeBuffer: TimeSpan


class MarketOnOpenOrder(QuantConnect.Orders.Order):
    """
    Market on Open order type, submits a market order when the exchange opens
    
    MarketOnOpenOrder()
    MarketOnOpenOrder(symbol: Symbol, quantity: Decimal, time: DateTime, tag: str, properties: IOrderProperties)
    """
    def Clone(self) -> QuantConnect.Orders.Order:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Orders.MarketOnOpenOrder:
        pass

    @typing.overload
    def __init__(self, symbol: QuantConnect.Symbol, quantity: float, time: datetime.datetime, tag: str, properties: QuantConnect.Interfaces.IOrderProperties) -> QuantConnect.Orders.MarketOnOpenOrder:
        pass

    def __init__(self, *args) -> QuantConnect.Orders.MarketOnOpenOrder:
        pass

    Type: QuantConnect.Orders.OrderType



class MarketOrder(QuantConnect.Orders.Order):
    """
    Market order type definition
    
    MarketOrder()
    MarketOrder(symbol: Symbol, quantity: Decimal, time: DateTime, tag: str, properties: IOrderProperties)
    """
    def Clone(self) -> QuantConnect.Orders.Order:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Orders.MarketOrder:
        pass

    @typing.overload
    def __init__(self, symbol: QuantConnect.Symbol, quantity: float, time: datetime.datetime, tag: str, properties: QuantConnect.Interfaces.IOrderProperties) -> QuantConnect.Orders.MarketOrder:
        pass

    def __init__(self, *args) -> QuantConnect.Orders.MarketOrder:
        pass

    Type: QuantConnect.Orders.OrderType



class OptionExerciseOrder(QuantConnect.Orders.Order):
    """
    Option exercise order type definition
    
    OptionExerciseOrder()
    OptionExerciseOrder(symbol: Symbol, quantity: Decimal, time: DateTime, tag: str, properties: IOrderProperties)
    """
    def Clone(self) -> QuantConnect.Orders.Order:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Orders.OptionExerciseOrder:
        pass

    @typing.overload
    def __init__(self, symbol: QuantConnect.Symbol, quantity: float, time: datetime.datetime, tag: str, properties: QuantConnect.Interfaces.IOrderProperties) -> QuantConnect.Orders.OptionExerciseOrder:
        pass

    def __init__(self, *args) -> QuantConnect.Orders.OptionExerciseOrder:
        pass

    Type: QuantConnect.Orders.OrderType



class OrderDirection(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Direction of the order
    
    enum OrderDirection, values: Buy (0), Hold (2), Sell (1)
    """
    value__: int
    Buy: 'OrderDirection'
    Hold: 'OrderDirection'
    Sell: 'OrderDirection'


class OrderError(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Specifies the possible error states during presubmission checks
    
    enum OrderError, values: CanNotUpdateFilledOrder (-8), GeneralError (-7), InsufficientCapital (-4), MarketClosed (-3), MaxOrdersExceeded (-5), NoData (-2), None (0), TimestampError (-6), ZeroQuantity (-1)
    """
    value__: int
    CanNotUpdateFilledOrder: 'OrderError'
    GeneralError: 'OrderError'
    InsufficientCapital: 'OrderError'
    MarketClosed: 'OrderError'
    MaxOrdersExceeded: 'OrderError'
    NoData: 'OrderError'
    TimestampError: 'OrderError'
    ZeroQuantity: 'OrderError'
    None_: 'OrderError'


class OrderEvent(System.object):
    """
    Order Event - Messaging class signifying a change in an order state and record the change in the user's algorithm portfolio
    
    OrderEvent()
    OrderEvent(orderId: int, symbol: Symbol, utcTime: DateTime, status: OrderStatus, direction: OrderDirection, fillPrice: Decimal, fillQuantity: Decimal, orderFee: OrderFee, message: str)
    OrderEvent(order: Order, utcTime: DateTime, orderFee: OrderFee, message: str)
    """
    def Clone(self) -> QuantConnect.Orders.OrderEvent:
        pass

    @staticmethod
    def FromSerialized(serializedOrderEvent: QuantConnect.Orders.Serialization.SerializedOrderEvent) -> QuantConnect.Orders.OrderEvent:
        pass

    def ToString(self) -> str:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Orders.OrderEvent:
        pass

    @typing.overload
    def __init__(self, orderId: int, symbol: QuantConnect.Symbol, utcTime: datetime.datetime, status: QuantConnect.Orders.OrderStatus, direction: QuantConnect.Orders.OrderDirection, fillPrice: float, fillQuantity: float, orderFee: QuantConnect.Orders.Fees.OrderFee, message: str) -> QuantConnect.Orders.OrderEvent:
        pass

    @typing.overload
    def __init__(self, order: QuantConnect.Orders.Order, utcTime: datetime.datetime, orderFee: QuantConnect.Orders.Fees.OrderFee, message: str) -> QuantConnect.Orders.OrderEvent:
        pass

    def __init__(self, *args) -> QuantConnect.Orders.OrderEvent:
        pass

    AbsoluteFillQuantity: float

    Direction: QuantConnect.Orders.OrderDirection

    FillPrice: float

    FillPriceCurrency: str

    FillQuantity: float

    Id: int

    IsAssignment: bool

    LimitPrice: typing.Optional[float]

    Message: str

    OrderFee: QuantConnect.Orders.Fees.OrderFee

    OrderId: int

    Quantity: float

    Status: QuantConnect.Orders.OrderStatus

    StopPrice: typing.Optional[float]

    Symbol: QuantConnect.Symbol

    UtcTime: datetime.datetime
