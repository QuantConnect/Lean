from .____init___2 import *
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



class OrderExtensions(System.object):
    """ Provides extension methods for the QuantConnect.Orders.Order class and for the QuantConnect.Orders.OrderStatus enumeration """
    @staticmethod
    def IsClosed(status: QuantConnect.Orders.OrderStatus) -> bool:
        pass

    @staticmethod
    def IsFill(status: QuantConnect.Orders.OrderStatus) -> bool:
        pass

    @staticmethod
    def IsLimitOrder(orderType: QuantConnect.Orders.OrderType) -> bool:
        pass

    @staticmethod
    def IsOpen(status: QuantConnect.Orders.OrderStatus) -> bool:
        pass

    @staticmethod
    def IsStopOrder(orderType: QuantConnect.Orders.OrderType) -> bool:
        pass

    __all__: list


class OrderField(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Specifies an order field that does not apply to all order types
    
    enum OrderField, values: LimitPrice (0), StopPrice (1)
    """
    value__: int
    LimitPrice: 'OrderField'
    StopPrice: 'OrderField'


class OrderJsonConverter(Newtonsoft.Json.JsonConverter):
    """
    Provides an implementation of Newtonsoft.Json.JsonConverter that can deserialize Orders
    
    OrderJsonConverter()
    """
    def CanConvert(self, objectType: type) -> bool:
        pass

    @staticmethod
    def CreateOrderFromJObject(jObject: Newtonsoft.Json.Linq.JObject) -> QuantConnect.Orders.Order:
        pass

    def ReadJson(self, reader: Newtonsoft.Json.JsonReader, objectType: type, existingValue: object, serializer: Newtonsoft.Json.JsonSerializer) -> object:
        pass

    def WriteJson(self, writer: Newtonsoft.Json.JsonWriter, value: object, serializer: Newtonsoft.Json.JsonSerializer) -> None:
        pass

    CanWrite: bool



class OrderRequestStatus(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Specifies the status of a request
    
    enum OrderRequestStatus, values: Error (3), Processed (2), Processing (1), Unprocessed (0)
    """
    value__: int
    Error: 'OrderRequestStatus'
    Processed: 'OrderRequestStatus'
    Processing: 'OrderRequestStatus'
    Unprocessed: 'OrderRequestStatus'


class OrderRequestType(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Specifies the type of QuantConnect.Orders.OrderRequest
    
    enum OrderRequestType, values: Cancel (2), Submit (0), Update (1)
    """
    value__: int
    Cancel: 'OrderRequestType'
    Submit: 'OrderRequestType'
    Update: 'OrderRequestType'


class OrderResponse(System.object):
    """
    Represents a response to an QuantConnect.Orders.OrderRequest. See QuantConnect.Orders.OrderRequest.Response property for
                a specific request's response value
    """
    @staticmethod
    def Error(request: QuantConnect.Orders.OrderRequest, errorCode: QuantConnect.Orders.OrderResponseErrorCode, errorMessage: str) -> QuantConnect.Orders.OrderResponse:
        pass

    @staticmethod
    def InvalidStatus(request: QuantConnect.Orders.OrderRequest, order: QuantConnect.Orders.Order) -> QuantConnect.Orders.OrderResponse:
        pass

    @staticmethod
    def Success(request: QuantConnect.Orders.OrderRequest) -> QuantConnect.Orders.OrderResponse:
        pass

    def ToString(self) -> str:
        pass

    @staticmethod
    def UnableToFindOrder(request: QuantConnect.Orders.OrderRequest) -> QuantConnect.Orders.OrderResponse:
        pass

    @staticmethod
    def WarmingUp(request: QuantConnect.Orders.OrderRequest) -> QuantConnect.Orders.OrderResponse:
        pass

    @staticmethod
    def ZeroQuantity(request: QuantConnect.Orders.OrderRequest) -> QuantConnect.Orders.OrderResponse:
        pass

    ErrorCode: QuantConnect.Orders.OrderResponseErrorCode

    ErrorMessage: str

    IsError: bool

    IsProcessed: bool

    IsSuccess: bool

    OrderId: int


    Unprocessed: 'OrderResponse'


class OrderResponseErrorCode(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Error detail code
    
    enum OrderResponseErrorCode, values: AlgorithmWarmingUp (-24), BrokerageFailedToCancelOrder (-8), BrokerageFailedToSubmitOrder (-5), BrokerageFailedToUpdateOrder (-6), BrokerageHandlerRefusedToUpdateOrder (-7), BrokerageModelRefusedToSubmitOrder (-4), BrokerageModelRefusedToUpdateOrder (-25), ConversionRateZero (-27), ExceededMaximumOrders (-20), ExchangeNotOpen (-15), ForexBaseAndQuoteCurrenciesRequired (-17), ForexConversionRateZero (-18), InsufficientBuyingPower (-3), InvalidOrderStatus (-9), InvalidRequest (-22), MarketOnCloseOrderTooLate (-21), MissingSecurity (-14), None (0), NonExercisableSecurity (-29), NonTradableSecurity (-28), OrderAlreadyExists (-2), OrderQuantityLessThanLoteSize (-30), OrderQuantityZero (-11), PreOrderChecksError (-13), ProcessingError (-1), QuoteCurrencyRequired (-26), RequestCanceled (-23), SecurityHasNoData (-19), SecurityPriceZero (-16), UnableToFindOrder (-10), UnsupportedRequestType (-12)
    """
    value__: int
    AlgorithmWarmingUp: 'OrderResponseErrorCode'
    BrokerageFailedToCancelOrder: 'OrderResponseErrorCode'
    BrokerageFailedToSubmitOrder: 'OrderResponseErrorCode'
    BrokerageFailedToUpdateOrder: 'OrderResponseErrorCode'
    BrokerageHandlerRefusedToUpdateOrder: 'OrderResponseErrorCode'
    BrokerageModelRefusedToSubmitOrder: 'OrderResponseErrorCode'
    BrokerageModelRefusedToUpdateOrder: 'OrderResponseErrorCode'
    ConversionRateZero: 'OrderResponseErrorCode'
    ExceededMaximumOrders: 'OrderResponseErrorCode'
    ExchangeNotOpen: 'OrderResponseErrorCode'
    ForexBaseAndQuoteCurrenciesRequired: 'OrderResponseErrorCode'
    ForexConversionRateZero: 'OrderResponseErrorCode'
    InsufficientBuyingPower: 'OrderResponseErrorCode'
    InvalidOrderStatus: 'OrderResponseErrorCode'
    InvalidRequest: 'OrderResponseErrorCode'
    MarketOnCloseOrderTooLate: 'OrderResponseErrorCode'
    MissingSecurity: 'OrderResponseErrorCode'
    NonExercisableSecurity: 'OrderResponseErrorCode'
    NonTradableSecurity: 'OrderResponseErrorCode'
    OrderAlreadyExists: 'OrderResponseErrorCode'
    OrderQuantityLessThanLoteSize: 'OrderResponseErrorCode'
    OrderQuantityZero: 'OrderResponseErrorCode'
    PreOrderChecksError: 'OrderResponseErrorCode'
    ProcessingError: 'OrderResponseErrorCode'
    QuoteCurrencyRequired: 'OrderResponseErrorCode'
    RequestCanceled: 'OrderResponseErrorCode'
    SecurityHasNoData: 'OrderResponseErrorCode'
    SecurityPriceZero: 'OrderResponseErrorCode'
    UnableToFindOrder: 'OrderResponseErrorCode'
    UnsupportedRequestType: 'OrderResponseErrorCode'
    None_: 'OrderResponseErrorCode'


class OrderSizing(System.object):
    """ Provides methods for computing a maximum order size. """
    @staticmethod
    def AdjustByLotSize(security: QuantConnect.Securities.Security, quantity: float) -> float:
        pass

    @staticmethod
    def GetOrderSizeForMaximumValue(security: QuantConnect.Securities.Security, maximumOrderValueInAccountCurrency: float, desiredOrderSize: float) -> float:
        pass

    @staticmethod
    def GetOrderSizeForPercentVolume(security: QuantConnect.Securities.Security, maximumPercentCurrentVolume: float, desiredOrderSize: float) -> float:
        pass

    @staticmethod
    def GetUnorderedQuantity(algorithm: QuantConnect.Interfaces.IAlgorithm, target: QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget) -> float:
        pass

    __all__: list


class OrderStatus(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Fill status of the order class.
    
    enum OrderStatus, values: Canceled (5), CancelPending (8), Filled (3), Invalid (7), New (0), None (6), PartiallyFilled (2), Submitted (1), UpdateSubmitted (9)
    """
    value__: int
    Canceled: 'OrderStatus'
    CancelPending: 'OrderStatus'
    Filled: 'OrderStatus'
    Invalid: 'OrderStatus'
    New: 'OrderStatus'
    PartiallyFilled: 'OrderStatus'
    Submitted: 'OrderStatus'
    UpdateSubmitted: 'OrderStatus'
    None_: 'OrderStatus'


class OrderSubmissionData(System.object):
    """
    The purpose of this class is to store time and price information
                available at the time an order was submitted.
    
    OrderSubmissionData(bidPrice: Decimal, askPrice: Decimal, lastPrice: Decimal)
    """
    def Clone(self) -> QuantConnect.Orders.OrderSubmissionData:
        pass

    def __init__(self, bidPrice: float, askPrice: float, lastPrice: float) -> QuantConnect.Orders.OrderSubmissionData:
        pass

    AskPrice: float

    BidPrice: float

    LastPrice: float



class OrderTicket(System.object):
    """
    Provides a single reference to an order for the algorithm to maintain. As the order gets
                updated this ticket will also get updated
    
    OrderTicket(transactionManager: SecurityTransactionManager, submitRequest: SubmitOrderRequest)
    """
    def Cancel(self, tag: str) -> QuantConnect.Orders.OrderResponse:
        pass

    def Get(self, field: QuantConnect.Orders.OrderField) -> float:
        pass

    def GetMostRecentOrderRequest(self) -> QuantConnect.Orders.OrderRequest:
        pass

    def GetMostRecentOrderResponse(self) -> QuantConnect.Orders.OrderResponse:
        pass

    @staticmethod
    def InvalidCancelOrderId(transactionManager: QuantConnect.Securities.SecurityTransactionManager, request: QuantConnect.Orders.CancelOrderRequest) -> QuantConnect.Orders.OrderTicket:
        pass

    @staticmethod
    def InvalidSubmitRequest(transactionManager: QuantConnect.Securities.SecurityTransactionManager, request: QuantConnect.Orders.SubmitOrderRequest, response: QuantConnect.Orders.OrderResponse) -> QuantConnect.Orders.OrderTicket:
        pass

    @staticmethod
    def InvalidUpdateOrderId(transactionManager: QuantConnect.Securities.SecurityTransactionManager, request: QuantConnect.Orders.UpdateOrderRequest) -> QuantConnect.Orders.OrderTicket:
        pass

    @staticmethod
    def InvalidWarmingUp(transactionManager: QuantConnect.Securities.SecurityTransactionManager, submit: QuantConnect.Orders.SubmitOrderRequest) -> QuantConnect.Orders.OrderTicket:
        pass

    def ToString(self) -> str:
        pass

    def Update(self, fields: QuantConnect.Orders.UpdateOrderFields) -> QuantConnect.Orders.OrderResponse:
        pass

    def UpdateLimitPrice(self, limitPrice: float, tag: str) -> QuantConnect.Orders.OrderResponse:
        pass

    def UpdateQuantity(self, quantity: float, tag: str) -> QuantConnect.Orders.OrderResponse:
        pass

    def UpdateStopPrice(self, stopPrice: float, tag: str) -> QuantConnect.Orders.OrderResponse:
        pass

    def UpdateTag(self, tag: str) -> QuantConnect.Orders.OrderResponse:
        pass

    def __init__(self, transactionManager: QuantConnect.Securities.SecurityTransactionManager, submitRequest: QuantConnect.Orders.SubmitOrderRequest) -> QuantConnect.Orders.OrderTicket:
        pass

    AverageFillPrice: float

    CancelRequest: QuantConnect.Orders.CancelOrderRequest

    HasOrder: bool

    OrderClosed: System.Threading.WaitHandle

    OrderEvents: typing.List[QuantConnect.Orders.OrderEvent]

    OrderId: int

    OrderSet: System.Threading.WaitHandle

    OrderType: QuantConnect.Orders.OrderType

    Quantity: float

    QuantityFilled: float

    SecurityType: QuantConnect.SecurityType

    Status: QuantConnect.Orders.OrderStatus

    SubmitRequest: QuantConnect.Orders.SubmitOrderRequest

    Symbol: QuantConnect.Symbol

    Tag: str

    Time: datetime.datetime

    UpdateRequests: typing.List[QuantConnect.Orders.UpdateOrderRequest]
