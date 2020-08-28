from .__Python_1 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Securities
import QuantConnect.Python
import QuantConnect.Orders.Slippage
import QuantConnect.Orders.Fills
import QuantConnect.Orders.Fees
import QuantConnect.Orders
import QuantConnect.Interfaces
import QuantConnect.Indicators
import QuantConnect.Data.Market
import QuantConnect.Data
import QuantConnect.Brokerages
import QuantConnect
import Python.Runtime
import datetime

# no functions
# classes

class BrokerageMessageHandlerPythonWrapper(System.object, QuantConnect.Brokerages.IBrokerageMessageHandler):
    """
    Provides a wrapper for QuantConnect.Brokerages.IBrokerageMessageHandler implementations written in python
    
    BrokerageMessageHandlerPythonWrapper(model: PyObject)
    """
    def Handle(self, message: QuantConnect.Brokerages.BrokerageMessageEvent) -> None:
        pass

    def __init__(self, model: Python.Runtime.PyObject) -> QuantConnect.Python.BrokerageMessageHandlerPythonWrapper:
        pass


class BrokerageModelPythonWrapper(System.object, QuantConnect.Brokerages.IBrokerageModel):
    """
    Provides an implementation of QuantConnect.Brokerages.IBrokerageModel that wraps a Python.Runtime.PyObject object
    
    BrokerageModelPythonWrapper(model: PyObject)
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

    def __init__(self, model: Python.Runtime.PyObject) -> QuantConnect.Python.BrokerageModelPythonWrapper:
        pass

    AccountType: QuantConnect.AccountType

    DefaultMarkets: System.Collections.Generic.IReadOnlyDictionary[QuantConnect.SecurityType, str]

    RequiredFreeBuyingPowerPercent: float



class BuyingPowerModelPythonWrapper(System.object, QuantConnect.Securities.IBuyingPowerModel):
    """
    Wraps a Python.Runtime.PyObject object that represents a security's model of buying power
    
    BuyingPowerModelPythonWrapper(model: PyObject)
    """
    def GetBuyingPower(self, parameters: QuantConnect.Securities.BuyingPowerParameters) -> QuantConnect.Securities.BuyingPower:
        pass

    def GetLeverage(self, security: QuantConnect.Securities.Security) -> float:
        pass

    def GetMaximumOrderQuantityForDeltaBuyingPower(self, parameters: QuantConnect.Securities.GetMaximumOrderQuantityForDeltaBuyingPowerParameters) -> QuantConnect.Securities.GetMaximumOrderQuantityResult:
        pass

    def GetMaximumOrderQuantityForTargetBuyingPower(self, parameters: QuantConnect.Securities.GetMaximumOrderQuantityForTargetBuyingPowerParameters) -> QuantConnect.Securities.GetMaximumOrderQuantityResult:
        pass

    def GetReservedBuyingPowerForPosition(self, parameters: QuantConnect.Securities.ReservedBuyingPowerForPositionParameters) -> QuantConnect.Securities.ReservedBuyingPowerForPosition:
        pass

    def HasSufficientBuyingPowerForOrder(self, parameters: QuantConnect.Securities.HasSufficientBuyingPowerForOrderParameters) -> QuantConnect.Securities.HasSufficientBuyingPowerForOrderResult:
        pass

    def SetLeverage(self, security: QuantConnect.Securities.Security, leverage: float) -> None:
        pass

    def __init__(self, model: Python.Runtime.PyObject) -> QuantConnect.Python.BuyingPowerModelPythonWrapper:
        pass


class DataConsolidatorPythonWrapper(System.object, System.IDisposable, QuantConnect.Data.Consolidators.IDataConsolidator):
    """
    Provides an Data Consolidator that wraps a Python.Runtime.PyObject object that represents a custom Python consolidator
    
    DataConsolidatorPythonWrapper(consolidator: PyObject)
    """
    def Dispose(self) -> None:
        pass

    def Scan(self, currentLocalTime: datetime.datetime) -> None:
        pass

    def Update(self, data: QuantConnect.Data.IBaseData) -> None:
        pass

    def __init__(self, consolidator: Python.Runtime.PyObject) -> QuantConnect.Python.DataConsolidatorPythonWrapper:
        pass

    Consolidated: QuantConnect.Data.IBaseData

    InputType: type

    OutputType: type

    WorkingData: QuantConnect.Data.IBaseData


    DataConsolidated: BoundEvent


class FeeModelPythonWrapper(QuantConnect.Orders.Fees.FeeModel, QuantConnect.Orders.Fees.IFeeModel):
    """
    Provides an order fee model that wraps a Python.Runtime.PyObject object that represents a model that simulates order fees
    
    FeeModelPythonWrapper(model: PyObject)
    """
    def GetOrderFee(self, parameters: QuantConnect.Orders.Fees.OrderFeeParameters) -> QuantConnect.Orders.Fees.OrderFee:
        pass

    def __init__(self, model: Python.Runtime.PyObject) -> QuantConnect.Python.FeeModelPythonWrapper:
        pass


class FillModelPythonWrapper(QuantConnect.Orders.Fills.FillModel, QuantConnect.Orders.Fills.IFillModel):
    """
    Wraps a Python.Runtime.PyObject object that represents a model that simulates order fill events
    
    FillModelPythonWrapper(model: PyObject)
    """
    def Fill(self, parameters: QuantConnect.Orders.Fills.FillModelParameters) -> QuantConnect.Orders.Fills.Fill:
        pass

    def LimitFill(self, asset: QuantConnect.Securities.Security, order: QuantConnect.Orders.LimitOrder) -> QuantConnect.Orders.OrderEvent:
        pass

    def MarketFill(self, asset: QuantConnect.Securities.Security, order: QuantConnect.Orders.MarketOrder) -> QuantConnect.Orders.OrderEvent:
        pass

    def MarketOnCloseFill(self, asset: QuantConnect.Securities.Security, order: QuantConnect.Orders.MarketOnCloseOrder) -> QuantConnect.Orders.OrderEvent:
        pass

    def MarketOnOpenFill(self, asset: QuantConnect.Securities.Security, order: QuantConnect.Orders.MarketOnOpenOrder) -> QuantConnect.Orders.OrderEvent:
        pass

    def StopLimitFill(self, asset: QuantConnect.Securities.Security, order: QuantConnect.Orders.StopLimitOrder) -> QuantConnect.Orders.OrderEvent:
        pass

    def StopMarketFill(self, asset: QuantConnect.Securities.Security, order: QuantConnect.Orders.StopMarketOrder) -> QuantConnect.Orders.OrderEvent:
        pass

    def __init__(self, model: Python.Runtime.PyObject) -> QuantConnect.Python.FillModelPythonWrapper:
        pass

    PythonWrapper: QuantConnect.Python.FillModelPythonWrapper


class MarginCallModelPythonWrapper(System.object, QuantConnect.Securities.IMarginCallModel):
    """
    Provides a margin call model that wraps a Python.Runtime.PyObject object that represents the model responsible for picking which orders should be executed during a margin call
    
    MarginCallModelPythonWrapper(model: PyObject)
    """
    def ExecuteMarginCall(self, generatedMarginCallOrders: typing.List[QuantConnect.Orders.SubmitOrderRequest]) -> typing.List[QuantConnect.Orders.OrderTicket]:
        pass

    def GetMarginCallOrders(self, issueMarginCallWarning: bool) -> typing.List[QuantConnect.Orders.SubmitOrderRequest]:
        pass

    def __init__(self, model: Python.Runtime.PyObject) -> QuantConnect.Python.MarginCallModelPythonWrapper:
        pass


class PandasConverter(System.object):
    """
    Collection of methods that converts lists of objects in pandas.DataFrame
    
    PandasConverter()
    """
    @typing.overload
    def GetDataFrame(self, data: typing.List[QuantConnect.Data.Slice]) -> Python.Runtime.PyObject:
        pass

    @typing.overload
    def GetDataFrame(self, data: typing.List[QuantConnect.Python.T]) -> Python.Runtime.PyObject:
        pass

    def GetDataFrame(self, *args) -> Python.Runtime.PyObject:
        pass

    def GetIndicatorDataFrame(self, data: System.Collections.Generic.IDictionary[str, typing.List[QuantConnect.Indicators.IndicatorDataPoint]]) -> Python.Runtime.PyObject:
        pass

    def ToString(self) -> str:
        pass


class PandasData(System.object):
    """
    Organizes a list of data to create pandas.DataFrames
    
    PandasData(data: object)
    """
    @typing.overload
    def Add(self, baseData: object) -> None:
        pass

    @typing.overload
    def Add(self, ticks: typing.List[QuantConnect.Data.Market.Tick], tradeBar: QuantConnect.Data.Market.TradeBar, quoteBar: QuantConnect.Data.Market.QuoteBar) -> None:
        pass

    def Add(self, *args) -> None:
        pass

    def ToPandasDataFrame(self, levels: int) -> Python.Runtime.PyObject:
        pass

    def __init__(self, data: object) -> QuantConnect.Python.PandasData:
        pass

    IsCustomData: bool

    Levels: int



class PythonActivator(System.object):
    """
    Provides methods for creating new instances of python custom data objects
    
    PythonActivator(type: Type, value: PyObject)
    """
    def __init__(self, type: type, value: Python.Runtime.PyObject) -> QuantConnect.Python.PythonActivator:
        pass

    Factory: typing.Callable[[typing.List[object]], object]

    Type: type
