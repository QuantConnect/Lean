# QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
# Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

from datetime import datetime
from json import dumps
from requests import get as requests_get, Request
from time import mktime
from quantconnect import ApiConnection

DOWNLOAD_CHUNK_SIZE = 256 * 1024

class Api:
    """QuantConnect.com Interaction Via API.
    Attributes:
        userId(int/str): User Id number from QuantConnect.com account. Found at www.quantconnect.com/account.
        token(str): Access token for the QuantConnect account. Found at www.quantconnect.com/account.
    """
    def __init__(self, userId, token):
        self.api_connection = ApiConnection(userId, token)

    def connected(self):
        """Check if Api is successfully connected with correct credentials"""
        return self.api_connection.connected()

    def read_project(self, projectId):
        """Get details about a single project.
        Args:
            projectId(int): Id of the project.
        Returns:
            ProjectResponse that contains information regarding the project.
        """
        request = Request('GET', "projects/read", params = { "projectId": projectId })

        return self.api_connection.try_request(request)

    def list_projects(self):
        """List details of all projects
        Returns:
            ProjectResponse that contains information regarding the project.
        """
        request = Request('GET', "projects/read")

        return self.api_connection.try_request(request)

    def create_project(self, name, language):
        """Create a project with the specified name and language via QuantConnect.com API
        Args:
            name(str): Project name
            language(str): Programming language to use (Language must be C#, F# or Py).
        Returns:
            Project object from the API.
        """
        request = Request('POST', "projects/create",
            data=dumps({ "name": name, "language": language }))

        return self.api_connection.try_request(request)

    def add_project_file(self, projectId, name, content):
        """Add a file to a project.
        Args:
            projectId(int): The project to which the file should be added.
            name(str): The name of the new file.
            content(str): The content of the new file.
        Returns:
            ProjectResponse that contains information regarding the project.
        """
        request = Request('POST', "files/create",
            params = 
            {
                "projectId" : projectId,
                "name" : name,
                "content" : content
            })

        return self.api_connection.try_request(request)

    def update_project_filename(self, projectId, oldFileName, newFileName):
        """Update the name of a file
        Args:
            projectId(int): Project id to which the file belongs
            oldFileName(str): The current name of the file
            newFileName(str): The new name for the file
        Returns:
            Response indicating success
        """
        request = Request('POST', "files/update",
            params = 
            {
                "projectId": projectId, 
                "name": oldFileName,
                "newName": newFileName
             })

        return self.api_connection.try_request(request)

    def update_project_file_content(self, projectId, fileName, newFileContents):
        """Update the contents of a file
        Args:
            projectId(int): Project id to which the file belongs
            fileName(str): The name of the file that should be updated
            newFileContents(str): The new contents of the file
        Returns:
            Response indicating success
        """
        request = Request('POST', "files/update",
            params = 
            {
                "projectId": projectId,
                "name": fileName,
                "content": newFileContents
            })

        return self.api_connection.try_request(request)

    def read_project_files(self, projectId):
        """Read all files in a project
        Args:
            projectId(int): Project id to which the file belongs
        Returns:
            ProjectFilesResponse that includes the information about all files in the project
        """
        request = Request('GET', "files/read", params = { "projectId" : projectId })

        return self.api_connection.try_request(request)

    def read_project_file(self, projectId, fileName):
        """Read a file in a project
        Args:
            projectId(int): Project id to which the file belongs
            fileName(str): The name of the file that should be updated
        Returns:
            ProjectFilesResponse that includes the file information
        """
        request = Request('GET', "files/read",
            params = 
            {
                "projectId": projectId,
                "name": fileName
            })

        return self.api_connection.try_request(request)

    def delete_project_file(self, projectId, name):
        """Delete a file in a project
        Args:
            projectId(int): Project id to which the file belongs
            name(str): The name of the file that should be deleted
        Returns:
            ProjectFilesResponse that includes the information about all files in the project
        """
        request = Request('POST', "files/delete",
            params =
            {
                "projectId": projectId, 
                "name": name
            })

        return self.api_connection.try_request(request)

    def delete_project(self, projectId):
        """Delete a project
        Args:
            projectId(int): Project id we own and wish to delete
        Returns:
            Response indicating success
        """
        request = Request('POST', "projects/delete",
            data=dumps({ "projectId": projectId }))

        return self.api_connection.try_request(request)

    def create_compile(self, projectId):
        """Create a new compile job request for this project id.
        Args:
            projectId(int): Project id we wish to compile.
        Returns:
            Compile object result
        """
        request = Request('POST', "compile/create",
            data=dumps({ "projectId": projectId }))

        return self.api_connection.try_request(request)

    def read_compile(self, projectId, compileId):
        """Read a compile packet job result.
        Args:
            projectId(int): Project id we sent for compile
            compileId(str): Compile id return from the creation request
        Returns:
            Response result
        """
        request = Request('GET', "compile/read",
            params =
            {
                "projectId": projectId,
                "compileId": compileId
            })

        return self.api_connection.try_request(request)

    def create_backtest(self, projectId, compileId, backtestName):
        """Create a new backtest request and get the id.
        Args:
            projectId(int): Id for the project to backtest
            compileId(str): Compile id return from the creation request
            backtestName(str): Name for the new backtest
        Returns:
            Backtest
        """
        request = Request('POST', "backtests/create",
            params = 
            {
                "projectId": projectId,
                "compileId": compileId,
                "backtestName": backtestName
            })

        return self.api_connection.try_request(request)

    def read_backtest(self, projectId, backtestId):
        """Read out a backtest in the project id specified.
        Args:
            projectId(int): Project id to read.
            backtestId(str): Specific backtest id to read.
        Returns:
            Backtest
        """
        request = Request('GET', "backtests/read",
            params =
            {
                "backtestId": backtestId, 
                "projectId": projectId
            })

        return self.api_connection.try_request(request)

    def update_backtest(self, projectId, backtestId, name, note):
        """Read out a backtest in the project id specified.
        Args:
            projectId(str): Project for the backtest we want to update
            backtestId(str): Specific backtest id to read
            name(str): Name we'd like to assign to the backtest
            note(str): Note attached to the backtest
        Returns:
            Request Response
        """
        request = Request('POST', "backtests/update", 
            data = dumps(
                {
                    "projectId": projectId,
                    "backtestId": backtestId,
                    "name": name,
                    "note": note
                }))

        return self.api_connection.try_request(request)

    def list_backtests(self, projectId):
        """List all the backtests for a project
        Args:
            projectId(int): Project id we'd like to get a list of backtest for
        Returns:
            Backtest list
        """
        request = Request('GET', "backtests/read", params = { "projectId": projectId })

        return self.api_connection.try_request(request)

    def delete_backtest(self, projectId, backtestId):
        """Delete a backtest from the specified project and backtestId.
        Args:
            projectId(int): Project for the backtest we want to delete
            backtestId(str): Backtest id we want to delete
        Returns:
            Response
        """
        request = Request('GET', "backtests/delete",
            params = 
            {
                "backtestId": backtestId,
                "projectId": projectId
            })

        return self.api_connection.try_request(request)

    def create_live_algorithm(self, projectId, compileId, serverType, baseLiveAlgorithmSettings, versionId="-1"):
        """Create a live algorithm.
        Args:
            projectId(int): Id of the project on QuantConnect
            compileId(str): Id of the compilation on QuantConnect
            serverType(str): Type of server instance that will run the algorithm
            baseLiveAlgorithmSettings(BaseLiveAlgorithmSettings): Brokerage specific
            versionId(str): The version of the Lean used to run the algorithm.
                       -1 is master, however, sometimes this can create problems with live deployments.
                       If you experience problems using, try specifying the version of Lean you would like to use.
        Returns:
            Information regarding the new algorithm
        """
        request = Request('POST', "live/create", headers = {"Accept": "application/json"},
            data = dumps(
                {
                    "versionId": versionId,
                    "projectId": projectId,
                    "compileId": compileId,
                    "serverType": serverType,
                    "brokerage": baseLiveAlgorithmSettings
                }))

        return self.api_connection.try_request(request)

    def list_live_algorithms(self, status=None, startTime=None, endTime=None):
        """Get a list of live running algorithms for user.
        Args:
            status(str): Filter the statuses of the algorithms returned from the api
                         Only the following statuses are supported by the Api:
                         "Liquidated", "Running", "RuntimeError", "Stopped",
            startTime(datetime): Earliest launched time of the algorithms returned by the Api
            endTime(datetime): Latest launched time of the algorithms returned by the Api
        Returns:
            Live list
        """

        if (
            status != None and
            status != "Running" and
            status != "RuntimeError" and
            status != "Stopped" and
            status != "Liquidated"):
            raise ValueError(
                "The Api only supports Algorithm Statuses of Running, Stopped, RuntimeError and Liquidated")

        request = Request('GET', "live/read",
            params =
            {
                "status": str(status), 
                "start": 0 if startTime == None else mktime(startTime.timetuple()),
                "end": mktime(datetime.utcnow().timetuple()) if endTime == None else mktime(endTime.timetuple())
            })

        return self.api_connection.try_request(request)

    def read_live_algorithm(self, projectId, deployId):
        """Get a list of live running algorithms for user.
        Args:
            projectId(int): Project Id of the live running algorithm
            deployId: Unique live algorithm deployment identifier
        Returns:
            Live list
        """
        request = Request('GET', "live/read",
            params = 
            {
                "projectId": projectId,
                "deployId": deployId
            })

        return self.api_connection.try_request(request)

    def liquidate_live_algorithm(self, projectId):
        """Liquidate a live algorithm from the specified project and deployId.
        Args:
            projectId(int): Project for the live instance we want to stop
        Returns:
            Request response
        """
        request = Request('POST', "live/update/liquidate", params = { "projectId": projectId })

        return self.api_connection.try_request(request)

    def stop_live_algorithm(self, projectId):
        """Stop a live algorithm from the specified project and deployId.
        Args:
            projectId(int): Project for the live instance we want to stop.
        Returns:
            Request response
        """
        request = Request('POST', "live/update/stop", params = { "projectId": projectId })

        return self.api_connection.try_request(request)

    def read_live_logs(self, projectId, algorithmId, startTime=None, endTime=None):
        """Gets the logs of a specific live algorithm.
        Args:
            projectId(int): Project Id of the live running algorithm
            algorithmId(str): Algorithm Id of the live running algorithm
            startTime(datetime): No logs will be returned before this time
            endTime(datetime): No logs will be returned after this time
        Returns:
            List of strings that represent the logs of the algorithm
        """
        request = Request('GET', "live/read/log", 
            params = 
            {
                "format": "json",
                "projectId": projectId,
                "algorithmId": algorithmId,
                "start": 0 if startTime == None else mktime(startTime.timetuple()),
                "end": mktime(datetime.utcnow().timetuple()) if endTime == None else mktime(endTime.timetuple())
            })

        return self.api_connection.try_request(request)

    def read_data_link(self, symbol, securityType, market, resolution, date):
        """Gets the link to the downloadable data.
        Args:
            symbol(str): Symbol of security of which data will be requested
            securityType(str): Type of underlying asset
            market(str): e.g. CBOE, CBOT, FXCM, GDAX etc. 
            resolution(str): Resolution of data requested
            date: Date of the data requested
        Returns:
            List of strings that represent the logs of the algorithm
        """
        request = Request('GET', "data/read",
            params = 
            {
                "format": "link",
                "ticker": symbol.lower(),
                "type": securityType.lower(),
                "market": market,
                "resolution": resolution,
                "date": date.strftime("%Y%m%d")
            })

        return self.api_connection.try_request(request)

    def read_backtest_report(self, projectId, backtestId):
        """Read out the report of a backtest in the project id specified.
        Args:
            projectId(int): Project id to read.
            backtestId(str): Specific backtest id to read.
        Returns:
            BacktestReport report
        """
        request = Request('POST', "backtests/read/report",
            params =
            {
                "backtestId": backtestId, 
                "projectId": projectId
            })

        return self.api_connection.try_request(request)

    def download_data(self, symbol, securityType, market, resolution, date, fileName):
        """Method to download and save the data purchased through QuantConnect
        Args:
            symbol(str): Symbol of security of which data will be requested.
            securityType(str): Type of underlying asset
            market(str): e.g. CBOE, CBOT, FXCM, GDAX etc. 
            resolution(str): Resolution of data requested.
            date(datetime): Date of the data requested.
            fileName(str): file name of data download
        Returns:
            bool indicating whether the data was successfully downloaded or not
        """

        # Get a link to the data
        link = self.read_data_link(symbol, securityType, market, resolution, date)
        
        # Make sure the link was successfully retrieved
        if link['success']:
            # download and save the data
            request = requests_get(link['link'], stream=True)
            with open(fileName + '.zip', "wb") as code:
                for chunk in request.iter_content(DOWNLOAD_CHUNK_SIZE):
                    code.write(chunk)

        return link['success']
