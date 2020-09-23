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



class OrderType(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Type of the order: market, limit or stop
    
    enum OrderType, values: Limit (1), Market (0), MarketOnClose (5), MarketOnOpen (4), OptionExercise (6), StopLimit (3), StopMarket (2)
    """
    value__: int
    Limit: 'OrderType'
    Market: 'OrderType'
    MarketOnClose: 'OrderType'
    MarketOnOpen: 'OrderType'
    OptionExercise: 'OrderType'
    StopLimit: 'OrderType'
    StopMarket: 'OrderType'


class StopLimitOrder(QuantConnect.Orders.Order):
    """
    Stop Market Order Type Definition
    
    StopLimitOrder()
    StopLimitOrder(symbol: Symbol, quantity: Decimal, stopPrice: Decimal, limitPrice: Decimal, time: DateTime, tag: str, properties: IOrderProperties)
    """
    def ApplyUpdateOrderRequest(self, request: QuantConnect.Orders.UpdateOrderRequest) -> None:
        pass

    def Clone(self) -> QuantConnect.Orders.Order:
        pass

    def ToString(self) -> str:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Orders.StopLimitOrder:
        pass

    @typing.overload
    def __init__(self, symbol: QuantConnect.Symbol, quantity: float, stopPrice: float, limitPrice: float, time: datetime.datetime, tag: str, properties: QuantConnect.Interfaces.IOrderProperties) -> QuantConnect.Orders.StopLimitOrder:
        pass

    def __init__(self, *args) -> QuantConnect.Orders.StopLimitOrder:
        pass

    LimitPrice: float

    StopPrice: float

    StopTriggered: bool

    Type: QuantConnect.Orders.OrderType



class StopMarketOrder(QuantConnect.Orders.Order):
    """
    Stop Market Order Type Definition
    
    StopMarketOrder()
    StopMarketOrder(symbol: Symbol, quantity: Decimal, stopPrice: Decimal, time: DateTime, tag: str, properties: IOrderProperties)
    """
    def ApplyUpdateOrderRequest(self, request: QuantConnect.Orders.UpdateOrderRequest) -> None:
        pass

    def Clone(self) -> QuantConnect.Orders.Order:
        pass

    def ToString(self) -> str:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Orders.StopMarketOrder:
        pass

    @typing.overload
    def __init__(self, symbol: QuantConnect.Symbol, quantity: float, stopPrice: float, time: datetime.datetime, tag: str, properties: QuantConnect.Interfaces.IOrderProperties) -> QuantConnect.Orders.StopMarketOrder:
        pass

    def __init__(self, *args) -> QuantConnect.Orders.StopMarketOrder:
        pass

    Type: QuantConnect.Orders.OrderType

    StopPrice: float


class SubmitOrderRequest(QuantConnect.Orders.OrderRequest):
    """
    Defines a request to submit a new order
    
    SubmitOrderRequest(orderType: OrderType, securityType: SecurityType, symbol: Symbol, quantity: Decimal, stopPrice: Decimal, limitPrice: Decimal, time: DateTime, tag: str, properties: IOrderProperties)
    """
    def ToString(self) -> str:
        pass

    def __init__(self, orderType: QuantConnect.Orders.OrderType, securityType: QuantConnect.SecurityType, symbol: QuantConnect.Symbol, quantity: float, stopPrice: float, limitPrice: float, time: datetime.datetime, tag: str, properties: QuantConnect.Interfaces.IOrderProperties) -> QuantConnect.Orders.SubmitOrderRequest:
        pass

    LimitPrice: float

    OrderProperties: QuantConnect.Interfaces.IOrderProperties

    OrderRequestType: QuantConnect.Orders.OrderRequestType

    OrderType: QuantConnect.Orders.OrderType

    Quantity: float

    SecurityType: QuantConnect.SecurityType

    StopPrice: float

    Symbol: QuantConnect.Symbol



class TimeInForce(System.object, QuantConnect.Interfaces.ITimeInForceHandler):
    """ Time In Force - defines the length of time over which an order will continue working before it is canceled """
    @staticmethod
    def GoodTilDate(expiry: datetime.datetime) -> QuantConnect.Orders.TimeInForce:
        pass

    def IsFillValid(self, security: QuantConnect.Securities.Security, order: QuantConnect.Orders.Order, fill: QuantConnect.Orders.OrderEvent) -> bool:
        pass

    def IsOrderExpired(self, security: QuantConnect.Securities.Security, order: QuantConnect.Orders.Order) -> bool:
        pass

    Day: DayTimeInForce
    GoodTilCanceled: GoodTilCanceledTimeInForce


class TimeInForceJsonConverter(Newtonsoft.Json.JsonConverter):
    """
    Provides an implementation of Newtonsoft.Json.JsonConverter that can deserialize TimeInForce objects
    
    TimeInForceJsonConverter()
    """
    def CanConvert(self, objectType: type) -> bool:
        pass

    def ReadJson(self, reader: Newtonsoft.Json.JsonReader, objectType: type, existingValue: object, serializer: Newtonsoft.Json.JsonSerializer) -> object:
        pass

    def WriteJson(self, writer: Newtonsoft.Json.JsonWriter, value: object, serializer: Newtonsoft.Json.JsonSerializer) -> None:
        pass

    CanWrite: bool



class UpdateOrderFields(System.object):
    """
    Specifies the data in an order to be updated
    
    UpdateOrderFields()
    """
    LimitPrice: typing.Optional[float]

    Quantity: typing.Optional[float]

    StopPrice: typing.Optional[float]

    Tag: str



class UpdateOrderRequest(QuantConnect.Orders.OrderRequest):
    """
    Defines a request to update an order's values
    
    UpdateOrderRequest(time: DateTime, orderId: int, fields: UpdateOrderFields)
    """
    def ToString(self) -> str:
        pass

    def __init__(self, time: datetime.datetime, orderId: int, fields: QuantConnect.Orders.UpdateOrderFields) -> QuantConnect.Orders.UpdateOrderRequest:
        pass

    LimitPrice: typing.Optional[float]

    OrderRequestType: QuantConnect.Orders.OrderRequestType

    Quantity: typing.Optional[float]

    StopPrice: typing.Optional[float]
