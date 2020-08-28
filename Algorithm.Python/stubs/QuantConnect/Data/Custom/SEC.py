from .__SEC_1 import *
import typing
import System.IO
import System
import QuantConnect.Data.Custom.SEC
import QuantConnect.Data
import QuantConnect
import datetime

# no functions
# classes

class ISECReport(QuantConnect.Data.IBaseData):
    """
    Base interface for all SEC report types.
                Using an interface, we can retrieve all report types with a single
                call to QuantConnect.Data.Slice.Get
    """
    Report: QuantConnect.Data.Custom.SEC.SECReportSubmission



class SECReport10K(QuantConnect.Data.BaseData, QuantConnect.Data.Custom.SEC.ISECReport, QuantConnect.Data.IBaseData):
    """
    SEC 10-K report (annual earnings) QuantConnect.Data.BaseData implementation.
                Using this class, you can retrieve SEC report data for a security if it exists.
                If the ticker you want no longer trades, you can also use the CIK of the company
                you want data for as well except for currently traded stocks. This may change in the future.
    
    SECReport10K()
    SECReport10K(report: SECReportSubmission)
    """
    @typing.overload
    def Clone(self) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Clone(self, fillForward: bool) -> QuantConnect.Data.BaseData:
        pass

    def Clone(self, *args) -> QuantConnect.Data.BaseData:
        pass

    def DefaultResolution(self) -> QuantConnect.Resolution:
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

    def RequiresMapping(self) -> bool:
        pass

    def SupportedResolutions(self) -> typing.List[QuantConnect.Resolution]:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Data.Custom.SEC.SECReport10K:
        pass

    @typing.overload
    def __init__(self, report: QuantConnect.Data.Custom.SEC.SECReportSubmission) -> QuantConnect.Data.Custom.SEC.SECReport10K:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Custom.SEC.SECReport10K:
        pass

    Report: QuantConnect.Data.Custom.SEC.SECReportSubmission



class SECReport10Q(QuantConnect.Data.BaseData, QuantConnect.Data.Custom.SEC.ISECReport, QuantConnect.Data.IBaseData):
    """
    SEC 10-Q report (quarterly earnings) QuantConnect.Data.BaseData implementation.
                Using this class, you can retrieve SEC report data for a security if it exists.
                If the ticker you want no longer trades, you can also use the CIK of the company
                you want data for as well except for currently traded stocks. This may change in the future.
    
    SECReport10Q()
    SECReport10Q(report: SECReportSubmission)
    """
    @typing.overload
    def Clone(self) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Clone(self, fillForward: bool) -> QuantConnect.Data.BaseData:
        pass

    def Clone(self, *args) -> QuantConnect.Data.BaseData:
        pass

    def DefaultResolution(self) -> QuantConnect.Resolution:
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

    def RequiresMapping(self) -> bool:
        pass

    def SupportedResolutions(self) -> typing.List[QuantConnect.Resolution]:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Data.Custom.SEC.SECReport10Q:
        pass

    @typing.overload
    def __init__(self, report: QuantConnect.Data.Custom.SEC.SECReportSubmission) -> QuantConnect.Data.Custom.SEC.SECReport10Q:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Custom.SEC.SECReport10Q:
        pass

    Report: QuantConnect.Data.Custom.SEC.SECReportSubmission



class SECReport8K(QuantConnect.Data.BaseData, QuantConnect.Data.Custom.SEC.ISECReport, QuantConnect.Data.IBaseData):
    """
    SEC 8-K report (important investor notices) QuantConnect.Data.BaseData implementation.
                Using this class, you can retrieve SEC report data for a security if it exists.
                If the ticker you want no longer trades, you can also use the CIK of the company
                you want data for as well except for currently traded stocks. This may change in the future.
    
    SECReport8K()
    SECReport8K(report: SECReportSubmission)
    """
    @typing.overload
    def Clone(self) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Clone(self, fillForward: bool) -> QuantConnect.Data.BaseData:
        pass

    def Clone(self, *args) -> QuantConnect.Data.BaseData:
        pass

    def DefaultResolution(self) -> QuantConnect.Resolution:
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

    def RequiresMapping(self) -> bool:
        pass

    def SupportedResolutions(self) -> typing.List[QuantConnect.Resolution]:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Data.Custom.SEC.SECReport8K:
        pass

    @typing.overload
    def __init__(self, report: QuantConnect.Data.Custom.SEC.SECReportSubmission) -> QuantConnect.Data.Custom.SEC.SECReport8K:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Custom.SEC.SECReport8K:
        pass

    Report: QuantConnect.Data.Custom.SEC.SECReportSubmission



class SECReportBusinessAddress(System.object):
    """ SECReportBusinessAddress() """
    City: str
    Phone: str
    State: str
    StreetOne: str
    StreetTwo: str
    Zip: str

class SECReportCompanyData(System.object):
    """ SECReportCompanyData() """
    AssignedSic: str
    Cik: str
    ConformedName: str
    FiscalYearEnd: str
    IrsNumber: str
    StateOfIncorporation: str

class SECReportDateTimeConverter(Newtonsoft.Json.Converters.IsoDateTimeConverter):
    """
    Specifies format for parsing System.DateTime values from SEC data
    
    SECReportDateTimeConverter()
    """

class SECReportDocument(System.object):
    """ SECReportDocument() """
    Description: str
    Filename: str
    FormType: str
    Sequence: int
    Text: str

class SECReportFactory(System.object):
    """ SECReportFactory() """
    def CreateSECReport(self, xmlText: str) -> QuantConnect.Data.Custom.SEC.ISECReport:
        pass


class SECReportFiler(System.object):
    """ SECReportFiler() """
    BusinessAddress: typing.List[QuantConnect.Data.Custom.SEC.SECReportBusinessAddress]
    CompanyData: QuantConnect.Data.Custom.SEC.SECReportCompanyData
    FormerCompanies: typing.List[QuantConnect.Data.Custom.SEC.SECReportFormerCompany]
    MailingAddress: typing.List[QuantConnect.Data.Custom.SEC.SECReportMailAddress]
    Values: typing.List[QuantConnect.Data.Custom.SEC.SECReportFilingValues]

class SECReportFilingValues(System.object):
    """ SECReportFilingValues() """
    Act: str
    FileNumber: str
    FilmNumber: str
    FormType: str

class SECReportFormerCompany(System.object):
    """ SECReportFormerCompany() """
    Changed: datetime.datetime
    FormerConformedName: str

class SECReportIndexDirectory(System.object):
    """ SECReportIndexDirectory() """
    Items: typing.List[QuantConnect.Data.Custom.SEC.SECReportIndexItem]
    Name: str
    ParentDirectory: str
