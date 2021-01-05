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

from base64 import b64encode
from datetime import datetime as dt
from hashlib import sha256
from json import dumps, loads
from requests import get, post
from time import mktime, time
from quantconnect.Result import Result

DOWNLOAD_CHUNK_SIZE = 256 * 1024

class Api:
    '''QuantConnect.com Interaction Via API.

    Args:
        userId(int/str): User Id number found at www.quantconnect.com/account.
        token(str): Access token found at www.quantconnect.com/account.
        debug(boolean): True to enable debugging messages'''

    def __init__(self, userId, token, debug = False):
        '''Creates a new instance of Api'''
        self.__url = 'https://www.quantconnect.com/api/v2/'
        self.__userId =  userId
        self.__token = token
        self.__debug = debug

    def Execute(self, endpoint, data = None, is_post = False, headers = {}):
        '''Execute an authenticated request to the QuantConnect API
        Args:
            endpoint(str): Request end point.
            data(dict): Request values
            is_post(boolean): True if POST request, GET request otherwise
            headers(dict): Additional headers'''
        url = self.__url + endpoint

        # Create authenticated timestamped token.
        timestamp = str(int(time()))

        # Attach timestamp to token for increasing token randomness
        timeStampedToken = f'{self.__token}:{timestamp}'

        # Hash token for transport
        apiToken = sha256(timeStampedToken.encode('utf-8')).hexdigest()

        # Attach in headers for basic authentication.
        authentication = f'{self.__userId}:{apiToken}'
        basic = b64encode(authentication.encode('utf-8')).decode('ascii')
        headers.update({ 'Authorization': f'Basic {basic}', 'Timestamp': timestamp })

        if is_post:
            response = post(url = url, data = data, headers = headers)
        else:   # Encode the request in parameters of URL.
            response = get(url = url, params = data, headers = headers)

        if self.__debug:
            print(url)
            self.__pretty_print(response)

        # Convert to object for parsing.
        try:
            result = response.json()
        except:
            result = {
                'success': False,
                'messages': [
                    'API returned a result which cannot be parsed into JSON. Please inspect the raw result below:',
                    response.text
                ]}

        if not result['success']:
            message = ''
            for name, value in result.items():
                if isinstance(value, str):
                    message += f'{name}: {value} '
                if isinstance(value, list):
                    message += f'{name}: {", ".join(value)} '
            print(f'There was an exception processing your request: {message}')

        return result

    def connected(self):
        '''Check whether Api is successfully connected with correct credentials'''
        return self.Execute('authenticate')['success']

    def list_projects(self):
        '''Read back a list of all projects on the account for a user.

        Returns:
            Dictionary that contains for list of projects.
        '''
        return self.Execute('projects/read')

    def create_project(self, name, language):
        '''Create a project with the specified name and language via QuantConnect.com API

        Args:
            name(str): Project name
            language(str): Programming language to use (Language must be C#, F# or Py).
        Returns:
            Dictionary that includes information about the newly created project.
        '''
        return self.Execute('projects/create',
            {
                'name': name,
                'language': language
            }, True)

    def read_project(self, projectId):
        '''Read in a project from the QuantConnect.com API.

        Args:
            projectId(int): Project id you own
        Returns:
            Dictionary that includes information about a specific project
        '''
        return self.Execute('projects/read', { 'projectId': projectId })

    def add_project_file(self, projectId, name, content):
        '''Add a file to a project.

        Args:
            projectId(int): The project to which the file should be added.
            name(str): The name of the new file.
            content(str): The content of the new file.
        Returns:
            Disctionary that includes information about the newly created file
        '''
        return self.Execute('files/create',
            {
                'projectId' : projectId,
                'name' : name,
                'content' : content
            }, True)

    def update_project_filename(self, projectId, oldFileName, newFileName):
        '''Update the name of a file

        Args:
            projectId(int): Project id to which the file belongs
            oldFileName(str): The current name of the file
            newFileName(str): The new name for the file
        Returns:
            Dictionary indicating success
        '''
        return self.Execute('files/update',
            {
                'projectId' : projectId,
                'name': oldFileName,
                'newName': newFileName
            }, True)

    def update_project_file_content(self, projectId, fileName, newFileContents):
        '''Update the contents of a file

        Args:
            projectId(int): Project id to which the file belongs
            fileName(str): The name of the file that should be updated
            newFileContents(str): The new contents of the file
        Returns:
            Dictionary indicating success
        '''
        return self.Execute('files/update',
            {
                'projectId': projectId,
                'name': fileName,
                'content': newFileContents
            }, True)

    def read_project_files(self, projectId):
        '''Read all files in a project

        Args:
            projectId(int): Project id to which the file belongs
        Returns:
            Dictionary that includes the information about all files in the project
        '''
        return self.Execute('files/read', { 'projectId': projectId })

    def read_project_file(self, projectId, fileName):
        '''Read a file in a project

        Args:
            projectId(int): Project id to which the file belongs
            fileName(str): The name of the file
        Returns:
            Dictionary that includes the file information
        '''
        return self.Execute('files/read',
            {
                'projectId': projectId,
                'name': fileName
            })

    def delete_project_file(self, projectId, name):
        '''Delete a file in a project

        Args:
            projectId(int): Project id to which the file belongs
            name(str): The name of the file that should be deleted
        Returns:
            Dictionary indicating success
        '''
        return self.Execute('files/delete',
            {
                'projectId' : projectId,
                'name' : name
            }, True)

    def delete_project(self, projectId):
        '''Delete a specific project owned by the user from QuantConnect.com

        Args:
            projectId(int): Project id we own and wish to delete
        Returns:
            Dictionary indicating success
        '''
        return self.Execute('projects/delete', { 'projectId' : projectId }, True)

    def create_compile(self, projectId):
        '''Create a new compile job request for this project id.

        Args:
            projectId(int): Project id we wish to compile.
        Returns:
            Dictionary that includes the compile information
        '''
        return self.Execute('compile/create', { 'projectId' : projectId }, True)

    def read_compile(self, projectId, compileId):
        '''Read a compile packet job result.
        Args:
            projectId(int): Project id we sent for compile
            compileId(str): Compile id return from the creation request
        Returns:
            Dictionary that includes the compile information
        '''
        return self.Execute('compile/read',
            {
                'projectId' : projectId,
                'compileId': compileId
            })

    def list_backtests(self, projectId):
        '''Get a list of backtests for a specific project id

        Args:
            projectId(int): Project id we'd like to get a list of backtest for
        Returns:
            Dictionary that includes the list of backtest
        '''
        return self.Execute('backtests/read', { 'projectId': projectId })

    def create_backtest(self, projectId, compileId, backtestName):
        '''Create a new backtest from a specified projectId and compileId

        Args:
            projectId(int): Id for the project to backtest
            compileId(str): Compile id return from the creation request
            backtestName(str): Name for the new backtest
        Returns:
            Dictionary that includes the backtest information
        '''
        return self.Execute('backtests/create',
            {
                'projectId' : projectId,
                'compileId': compileId,
                'backtestName': backtestName
            }, True)

    def read_backtest(self, projectId, backtestId, json_format = True):
        '''Read out the full result of a specific backtest.

        Args:
            projectId(int): Project id for the backtest we'd like to read
            backtestId(str): Backtest id for the backtest we'd like to read
            parsed(boolean): True if parse the results as pandas.DataFrame
        Returns:
            dictionary that includes the backtest information or Result object
        '''
        json =  self.Execute('backtests/read',
            {
                'projectId' : projectId,
                'backtestId': backtestId
            })

        return json if json_format else Result(json)

    def read_backtest_report(self, projectId, backtestId, save=False):
        '''Read out the report of a backtest in the project id specified.

        Args:
            projectId(int): Project id to read.
            backtestId(str): Specific backtest id to read.
            save(boolean): True if data should be saved to disk
        Returns:
            Dictionary that contains the backtest report
        '''
        json = self.Execute('backtests/read/report',
            {
                'projectId': projectId,
                'backtestId': backtestId,
            }, True)

        if save and json['success']:
            with open(backtestId + '.html', "w") as fp:
                fp.write(json['report'])
            print(f'Log saved as {backtestId}.html')
            
        return json

    def update_backtest(self, projectId, backtestId, backtestName = '', backtestNote = ''):
        '''Update the backtest name.

        Args:
            projectId(str): Project id to update
            backtestId(str): Specific backtest id to read
            backtestName(str): New backtest name to set
            note(str): Note attached to the backtest
        Returns:
            Dictionary indicating success
        '''
        return self.Execute('backtests/update',
            {
                'projectId' : projectId,
                'backtestId': backtestId,
                'name': backtestName,
                'note': backtestNote
            }, True)

    def delete_backtest(self, projectId, backtestId):
        '''Delete a backtest from the specified project and backtestId.

        Args:
            projectId(int): Project for the backtest we want to delete
            backtestId(str): Backtest id we want to delete
        Returns:
            Dictionary indicating success
        '''
        return self.Execute('backtests/delete',
            {
                'projectId': projectId,
                'backtestId': backtestId
            })

    def list_live_algorithms(self, status, startTime=None, endTime=None):
        '''Get a list of live running algorithms for a logged in user.

        Args:
            status(str): Filter the statuses of the algorithms returned from the api
                         Only the following statuses are supported by the Api:
                         "Liquidated", "Running", "RuntimeError", "Stopped",
            startTime(datetime): Earliest launched time of the algorithms returned by the Api
            endTime(datetime): Latest launched time of the algorithms returned by the Api
        Returns:
            Dictionary that includes the list of live algorithms
        '''
        if (status != None and
            status != "Running" and
            status != "RuntimeError" and
            status != "Stopped" and
            status != "Liquidated"):
            raise ValueError(
                "The Api only supports Algorithm Statuses of Running, Stopped, RuntimeError and Liquidated")

        if endTime == None:
            endTime = dt.utcnow()

        return self.Execute('live/read',
            {
                'status': str(status),
                'end': mktime(endTime.timetuple()),
                'start': 0 if startTime == None else mktime(startTime.timetuple())
            })

    def create_live_algorithm(self, projectId, compileId, serverType, baseLiveAlgorithmSettings, versionId="-1"):
        '''Create a new live algorithm for a logged in user.

        Args:
            projectId(int): Id of the project on QuantConnect
            compileId(str): Id of the compilation on QuantConnect
            serverType(str): Type of server instance that will run the algorithm
            baseLiveAlgorithmSettings(BaseLiveAlgorithmSettings): Brokerage specific
            versionId(str): The version of the Lean used to run the algorithm.
                       -1 is master, however, sometimes this can create problems with live deployments.
                       If you experience problems using, try specifying the version of Lean you would like to use.
        Returns:
            Dictionary that contains information regarding the new algorithm
        '''
        return self.Execute('live/create',
            {
                'projectId': projectId,
                'compileId': compileId,
                'versionId': versionId,
                'serverType': serverType,
                'brokerage': baseLiveAlgorithmSettings
            },
            True,
            headers = {"Accept": "application/json"})

    def read_live_algorithm(self, projectId, deployId = None, json_format = True):
        '''Read out a live algorithm in the project id specified.

        Args:
            projectId(int): Project id to read
            deployId: Specific instance id to read
        Returns:
            Dictionary that contains information regarding the live algorithm or Result object
        '''
        json = self.Execute('live/read',
            {
                'projectId': projectId,
                'deployId': deployId
            })

        return json if json_format else Result(json)

    def liquidate_live_algorithm(self, projectId):
        '''Liquidate a live algorithm from the specified project.

        Args:
            projectId(int): Project for the live instance we want to liquidate
        Returns:
            Dictionary indicating success
        '''
        return self.Execute('live/update/liquidate', { 'projectId': projectId }, True)

    def stop_live_algorithm(self, projectId):
        '''Stop a live algorithm from the specified project.

        Args:
            projectId(int): Project for the live instance we want to stop.
        Returns:
            Dictionary indicating success
        '''
        return self.Execute('live/update/stop', { 'projectId': projectId }, True)

    def read_live_logs(self, projectId, algorithmId, startTime=None, endTime=None, save=False):
        '''Gets the logs of a specific live algorithm.

        Args:
            projectId(int): Project Id of the live running algorithm
            algorithmId(str): Algorithm Id of the live running algorithm
            startTime(datetime): No logs will be returned before this time. Should be in UTC
            endTime(datetime): No logs will be returned after this time. Should be in UTC
            save(boolean): True if data should be saved to disk
        Returns:
            List of strings that represent the logs of the algorithm
        '''
        if endTime == None:
            endTime = dt.utcnow()

        json = self.Execute('live/read/log',
            {
                'format': 'json',
                'projectId': projectId,
                'algorithmId': algorithmId,
                'end': mktime(endTime.timetuple()),
                'start': 0 if startTime == None else mktime(startTime.timetuple())
            })

        if save and json['success']:
            with open(algorithmId + '.txt', "w") as fp:
                fp.write('\n'.join(json['LiveLogs']))
            print(f'Log saved as {algorithmId}.txt')

        return json

    def read_data_link(self, symbol, securityType, market, resolution, date):
        '''Gets the link to the downloadable data.

        Args:
            symbol(str): Symbol of security of which data will be requested
            securityType(str): Type of underlying asset
            market(str): e.g. CBOE, CBOT, FXCM, GDAX etc. 
            resolution(str): Resolution of data requested
            date: Date of the data requested
        Returns:
            Dictionary that contains the link to the downloadable data.
        '''
        return self.Execute('data/read',
            {
                'format': 'link',
                'ticker': symbol.lower(),
                'type': securityType.lower(),
                'market': market.lower(),
                'resolution': resolution.lower(),
                'date': date.strftime("%Y%m%d")
            })

    def download_data(self, symbol, securityType, market, resolution, date, fileName):
        '''Method to download and save the data purchased through QuantConnect

        Args:
            symbol(str): Symbol of security of which data will be requested.
            securityType(str): Type of underlying asset
            market(str): e.g. CBOE, CBOT, FXCM, GDAX etc. 
            resolution(str): Resolution of data requested.
            date(datetime): Date of the data requested.
            fileName(str): file name of data download
        Returns:
            Boolean indicating whether the data was successfully downloaded or not
        '''

        # Get a link to the data
        link = self.read_data_link(symbol, securityType, market, resolution, date)

        # Make sure the link was successfully retrieved
        if not link['success']:
            return False

        # download and save the data
        with open(fileName + '.zip', "wb") as code:
            request = get(link['link'], stream=True)
            for chunk in request.iter_content(DOWNLOAD_CHUNK_SIZE):
                code.write(chunk)

        return True

    def __pretty_print(self, result):
        '''Print out a nice formatted version of the request'''
        print ('')
        try:
            parsed = loads(result.text)
            print (dumps(parsed, indent=4, sort_keys=True))
        except Exception  as err:
            print ('Fall back error (text print)')
            print ('')
            print (result.text)
            print ('')
            print (err)
        print ('')