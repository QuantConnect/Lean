# encoding: utf-8
# module QuantConnect.Api calls itself Api
# from QuantConnect.Common, Version=2.4.0.0, Culture=neutral, PublicKeyToken=null
# by generator 1.145
# no doc

# imports
import datetime
import QuantConnect
import QuantConnect.Api
import QuantConnect.Packets
import System
import typing

# no functions
# classes

class RestResponse(System.object):
    """
    Base API response class for the QuantConnect API.
    
    RestResponse()
    """
    Errors: typing.List[str]
    Success: bool

class AuthenticationResponse(QuantConnect.Api.RestResponse):
    """
    Verify if the credentials are OK.
    
    AuthenticationResponse()
    """

class Backtest(QuantConnect.Api.RestResponse):
    """
    Backtest response packet from the QuantConnect.com API.
    
    Backtest()
    """
    BacktestId: str
    Completed: bool
    Created: datetime.datetime
    Error: str
    Name: str
    Note: str
    Progress: float
    Result: QuantConnect.Packets.BacktestResult
    StackTrace: str

class BacktestList(QuantConnect.Api.RestResponse):
    """
    Collection container for a list of backtests for a project
    
    BacktestList()
    """
    Backtests: typing.List[QuantConnect.Api.Backtest]

class BacktestReport(QuantConnect.Api.RestResponse):
    """
    Backtest Report Response wrapper
    
    BacktestReport()
    """
    Report: str



class Compile(QuantConnect.Api.RestResponse):
    """
    Response from the compiler on a build event
    
    Compile()
    """
    CompileId: str
    Logs: typing.List[str]
    State: QuantConnect.Api.CompileState

class CompileState(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    State of the compilation request
    
    enum CompileState, values: BuildError (2), BuildSuccess (1), InQueue (0)
    """
    value__: int
    BuildError: 'CompileState'
    BuildSuccess: 'CompileState'
    InQueue: 'CompileState'


class Link(QuantConnect.Api.RestResponse):
    """
    Response from reading purchased data
    
    Link()
    """
    DataLink: str



class Project(QuantConnect.Api.RestResponse):
    """
    Response from reading a project by id.
    
    Project()
    """
    Created: datetime.datetime
    Language: QuantConnect.Language
    Modified: datetime.datetime
    Name: str
    ProjectId: int

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

