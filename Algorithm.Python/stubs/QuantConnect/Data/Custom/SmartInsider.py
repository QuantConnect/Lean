from .__SmartInsider_1 import *
import typing
import System.IO
import System
import QuantConnect.Data.Custom.SmartInsider
import QuantConnect.Data
import QuantConnect
import NodaTime
import datetime

# no functions
# classes

class SmartInsiderEvent(QuantConnect.Data.BaseData, QuantConnect.Data.IBaseData):
    """
    SmartInsider Intention and Transaction events. These are fields
                that are shared between intentions and transactions.
    
    SmartInsiderEvent()
    SmartInsiderEvent(tsvLine: str)
    """
    def DataTimeZone(self) -> NodaTime.DateTimeZone:
        pass

    def FromRawData(self, line: str) -> None:
        pass

    @staticmethod
    def ParseDate(date: str) -> datetime.datetime:
        pass

    def ToLine(self) -> str:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Data.Custom.SmartInsider.SmartInsiderEvent:
        pass

    @typing.overload
    def __init__(self, tsvLine: str) -> QuantConnect.Data.Custom.SmartInsider.SmartInsiderEvent:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Custom.SmartInsider.SmartInsiderEvent:
        pass

    AnnouncedIn: str

    AnnouncementDate: typing.Optional[datetime.datetime]

    CompanyID: typing.Optional[int]

    CompanyName: str

    EventType: typing.Optional[QuantConnect.Data.Custom.SmartInsider.SmartInsiderEventType]

    ICBCode: typing.Optional[int]

    ICBIndustry: str

    ICBSector: str

    ICBSubSector: str

    ICBSuperSector: str

    ISIN: str

    LastCloseEnded: typing.Optional[datetime.datetime]

    LastIDsUpdate: typing.Optional[datetime.datetime]

    LastUpdate: datetime.datetime

    NextCloseBegin: typing.Optional[datetime.datetime]

    NextResultsAnnouncementsDate: typing.Optional[datetime.datetime]

    PreviousResultsAnnouncementDate: typing.Optional[datetime.datetime]

    SecurityDescription: str

    TickerCountry: str

    TickerSymbol: str

    TimeProcessed: typing.Optional[datetime.datetime]

    TimeProcessedUtc: typing.Optional[datetime.datetime]

    TimeReleased: typing.Optional[datetime.datetime]

    TimeReleasedUtc: typing.Optional[datetime.datetime]

    TransactionID: str

    USDMarketCap: typing.Optional[float]



class SmartInsiderEventType(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Describes what will or has taken place in an execution
    
    enum SmartInsiderEventType, values: Authorization (0), Cancellation (6), DownwardsRevision (4), Intention (1), NotSpecified (10), PlanReStarted (9), PlanSuspension (8), RevisedDetails (5), SeekAuthorization (7), Transaction (2), UpwardsRevision (3)
    """
    value__: int
    Authorization: 'SmartInsiderEventType'
    Cancellation: 'SmartInsiderEventType'
    DownwardsRevision: 'SmartInsiderEventType'
    Intention: 'SmartInsiderEventType'
    NotSpecified: 'SmartInsiderEventType'
    PlanReStarted: 'SmartInsiderEventType'
    PlanSuspension: 'SmartInsiderEventType'
    RevisedDetails: 'SmartInsiderEventType'
    SeekAuthorization: 'SmartInsiderEventType'
    Transaction: 'SmartInsiderEventType'
    UpwardsRevision: 'SmartInsiderEventType'


class SmartInsiderExecution(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Describes how the transaction was executed
    
    enum SmartInsiderExecution, values: Market (0), OffMarket (2), TenderOffer (1)
    """
    value__: int
    Market: 'SmartInsiderExecution'
    OffMarket: 'SmartInsiderExecution'
    TenderOffer: 'SmartInsiderExecution'


class SmartInsiderExecutionEntity(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Entity that intends to or executed the transaction
    
    enum SmartInsiderExecutionEntity, values: Broker (2), EmployeeBenefitTrust (4), EmployerBenefitTrust (3), Issuer (0), Subsidiary (1), ThirdParty (5)
    """
    value__: int
    Broker: 'SmartInsiderExecutionEntity'
    EmployeeBenefitTrust: 'SmartInsiderExecutionEntity'
    EmployerBenefitTrust: 'SmartInsiderExecutionEntity'
    Issuer: 'SmartInsiderExecutionEntity'
    Subsidiary: 'SmartInsiderExecutionEntity'
    ThirdParty: 'SmartInsiderExecutionEntity'


class SmartInsiderExecutionHolding(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Details regarding the way holdings will be or were processed in a buyback execution
    
    enum SmartInsiderExecutionHolding, values: Cancellation (1), Error (6), NotReported (4), SatisfyEmployeeTax (3), SatisfyStockVesting (5), Treasury (0), Trust (2)
    """
    value__: int
    Cancellation: 'SmartInsiderExecutionHolding'
    Error: 'SmartInsiderExecutionHolding'
    NotReported: 'SmartInsiderExecutionHolding'
    SatisfyEmployeeTax: 'SmartInsiderExecutionHolding'
    SatisfyStockVesting: 'SmartInsiderExecutionHolding'
    Treasury: 'SmartInsiderExecutionHolding'
    Trust: 'SmartInsiderExecutionHolding'


class SmartInsiderIntention(QuantConnect.Data.Custom.SmartInsider.SmartInsiderEvent, QuantConnect.Data.IBaseData):
    """
    Smart Insider Intentions - Intention to execute a stock buyback and details about the future event
    
    SmartInsiderIntention()
    SmartInsiderIntention(line: str)
    """
    @typing.overload
    def Clone(self) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Clone(self, fillForward: bool) -> QuantConnect.Data.BaseData:
        pass

    def Clone(self, *args) -> QuantConnect.Data.BaseData:
        pass

    def FromRawData(self, line: str) -> None:
        pass

    @typing.overload
    def GetSource(self, config: QuantConnect.Data.SubscriptionDataConfig, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.SubscriptionDataSource:
        pass

    @typing.overload
    def GetSource(self, config: QuantConnect.Data.SubscriptionDataConfig, date: datetime.datetime, datafeed: QuantConnect.DataFeedEndpoint) -> str:
        pass

    def GetSource(self, *args) -> str:
        pass

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, stream: System.IO.StreamReader, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime, datafeed: QuantConnect.DataFeedEndpoint) -> QuantConnect.Data.BaseData:
        pass

    def Reader(self, *args) -> QuantConnect.Data.BaseData:
        pass

    def ToLine(self) -> str:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Data.Custom.SmartInsider.SmartInsiderIntention:
        pass

    @typing.overload
    def __init__(self, line: str) -> QuantConnect.Data.Custom.SmartInsider.SmartInsiderIntention:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Custom.SmartInsider.SmartInsiderIntention:
        pass

    Amount: typing.Optional[int]

    AmountValue: typing.Optional[int]

    AuthorizationEndDate: typing.Optional[datetime.datetime]

    AuthorizationStartDate: typing.Optional[datetime.datetime]

    Execution: typing.Optional[QuantConnect.Data.Custom.SmartInsider.SmartInsiderExecution]

    ExecutionEntity: typing.Optional[QuantConnect.Data.Custom.SmartInsider.SmartInsiderExecutionEntity]

    ExecutionHolding: typing.Optional[QuantConnect.Data.Custom.SmartInsider.SmartInsiderExecutionHolding]

    MaximumPrice: typing.Optional[float]

    MinimumPrice: typing.Optional[float]

    NoteText: str

    Percentage: typing.Optional[float]

    PriceCurrency: str

    ValueCurrency: str



class SmartInsiderTransaction(QuantConnect.Data.Custom.SmartInsider.SmartInsiderEvent, QuantConnect.Data.IBaseData):
    """
    Smart Insider Transaction - Execution of a stock buyback and details about the event occurred
    
    SmartInsiderTransaction()
    SmartInsiderTransaction(line: str)
    """
    @typing.overload
    def Clone(self) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Clone(self, fillForward: bool) -> QuantConnect.Data.BaseData:
        pass

    def Clone(self, *args) -> QuantConnect.Data.BaseData:
        pass

    def FromRawData(self, line: str) -> None:
        pass

    @typing.overload
    def GetSource(self, config: QuantConnect.Data.SubscriptionDataConfig, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.SubscriptionDataSource:
        pass

    @typing.overload
    def GetSource(self, config: QuantConnect.Data.SubscriptionDataConfig, date: datetime.datetime, datafeed: QuantConnect.DataFeedEndpoint) -> str:
        pass

    def GetSource(self, *args) -> str:
        pass

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, stream: System.IO.StreamReader, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime, datafeed: QuantConnect.DataFeedEndpoint) -> QuantConnect.Data.BaseData:
        pass

    def Reader(self, *args) -> QuantConnect.Data.BaseData:
        pass

    def ToLine(self) -> str:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Data.Custom.SmartInsider.SmartInsiderTransaction:
        pass

    @typing.overload
    def __init__(self, line: str) -> QuantConnect.Data.Custom.SmartInsider.SmartInsiderTransaction:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Custom.SmartInsider.SmartInsiderTransaction:
        pass

    Amount: typing.Optional[float]

    AmountAdjustedFactor: typing.Optional[float]

    BuybackDate: typing.Optional[datetime.datetime]

    BuybackPercentage: typing.Optional[float]

    ConversionRate: typing.Optional[float]

    Currency: str

    EURValue: typing.Optional[float]

    Execution: typing.Optional[QuantConnect.Data.Custom.SmartInsider.SmartInsiderExecution]

    ExecutionEntity: typing.Optional[QuantConnect.Data.Custom.SmartInsider.SmartInsiderExecutionEntity]

    ExecutionHolding: typing.Optional[QuantConnect.Data.Custom.SmartInsider.SmartInsiderExecutionHolding]

    ExecutionPrice: typing.Optional[float]

    GBPValue: typing.Optional[float]

    NoteText: str

    PriceAdjustedFactor: typing.Optional[float]

    TreasuryHolding: typing.Optional[int]

    USDValue: typing.Optional[float]

    VolumePercentage: typing.Optional[float]
