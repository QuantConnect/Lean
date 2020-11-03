import typing
import System
import QuantConnect.Packets
import QuantConnect.Api
import QuantConnect
import Newtonsoft.Json.Linq
import Newtonsoft.Json
import datetime

class ProjectFile(System.object):
    """
    File for a project
    
    ProjectFile()
    """
    DateModified: datetime.datetime

    Code: str
    Name: str


class ProjectFilesResponse(QuantConnect.Api.RestResponse):
    """
    Response received when reading all files of a project
    
    ProjectFilesResponse()
    """
    Files: typing.List[QuantConnect.Api.ProjectFile]

class ProjectResponse(QuantConnect.Api.RestResponse):
    """
    Project list response
    
    ProjectResponse()
    """
    Projects: typing.List[QuantConnect.Api.Project]

class SKU(System.object):
    """
    Class for generating a SKU for a node with a given configuration
                Every SKU is made up of 3 variables:
                - Target environment (L for live, B for Backtest, R for Research)
                - CPU core count
                - Dedicated RAM (GB)
    
    SKU(cores: int, memory: int, target: NodeType)
    """
    def ToString(self) -> str:
        pass

    def __init__(self, cores: int, memory: int, target: QuantConnect.Api.NodeType) -> QuantConnect.Api.SKU:
        pass

    Cores: int
    Memory: int
    Target: QuantConnect.Api.NodeType

class Split(System.object):
    """
    Split returned from the api
    
    Split()
    """
    Date: datetime.datetime

    ReferencePrice: float

    SplitFactor: float

    Symbol: QuantConnect.Symbol

    SymbolID: str



class SplitList(QuantConnect.Api.RestResponse):
    """
    Collection container for a list of split objects
    
    SplitList()
    """
    Splits: typing.List[QuantConnect.Api.Split]



class TradierLiveAlgorithmSettings(QuantConnect.Api.BaseLiveAlgorithmSettings):
    """
    Live algorithm settings for trading with Tradier
    
    TradierLiveAlgorithmSettings(accessToken: str, dateIssued: str, refreshToken: str, account: str)
    """
    def __init__(self, accessToken: str, dateIssued: str, refreshToken: str, account: str) -> QuantConnect.Api.TradierLiveAlgorithmSettings:
        pass

    AccessToken: str

    DateIssued: str

    Lifetime: str

    RefreshToken: str
