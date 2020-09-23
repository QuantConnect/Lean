import typing
import System.IO
import System
import QuantConnect.Data.Custom.SEC
import QuantConnect.Data
import QuantConnect
import datetime

class SECReportIndexFile(System.object):
    """ SECReportIndexFile() """
    Directory: QuantConnect.Data.Custom.SEC.SECReportIndexDirectory

class SECReportIndexItem(System.object):
    """ SECReportIndexItem() """
    FileType: str
    LastModified: datetime.datetime
    Name: str
    Size: str

class SECReportMailAddress(System.object):
    """ SECReportMailAddress() """
    City: str
    State: str
    StreetOne: str
    StreetTwo: str
    Zip: str

class SECReportSubmission(System.object):
    """ SECReportSubmission() """
    AccessionNumber: str
    Documents: typing.List[QuantConnect.Data.Custom.SEC.SECReportDocument]
    Filers: typing.List[QuantConnect.Data.Custom.SEC.SECReportFiler]
    FilingDate: datetime.datetime
    FilingDateChange: datetime.datetime
    FormType: str
    Items: typing.List[str]
    MadeAvailableAt: datetime.datetime
    Period: datetime.datetime
    PublicDocumentCount: str
