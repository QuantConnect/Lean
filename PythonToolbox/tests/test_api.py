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

from quantconnect.api import Api
from time import sleep
import unittest
import os

class TestApi(unittest.TestCase):

    def setUp(self):
        # Please add your credentials. Found at https://www.quantconnect.com/account 
        self.userId = 0
        self.testToken = ''
        self.api = Api(self.userId, self.testToken)

    def test_ApiWillAuthenticate_ValidCredentials_Successfully(self):
        '''Test successfully authenticating with the API using valid credentials.'''
        connected = Api(self.userId, self.testToken).connected()
        self.assertTrue(connected)

    def test_ApiWillAuthenticate_InvalidCredentials_Unsuccessfully(self):
        '''Test that the Api will reject invalid credentials'''
        connected = Api(self.userId, '').connected()
        self.assertFalse(connected)

    def test_Projects_CanBeCreatedAndDeleted_Successfully(self):
        '''Test creating and deleting projects with the Api'''

        name = 'Test Project '
        # Test create a new project successfully
        project = self.api.create_project(name, 'Py')
        projectId = project['projects'][0]['projectId']
        self.assertTrue(project['success'])
        self.assertTrue(projectId > 0)
        self.assertTrue(project['projects'][0]['name'] == name)

        # Delete the project
        deleteProject = self.api.delete_project(projectId)
        self.assertTrue(deleteProject['success'])

        # Make sure the project is really deleted
        projectList = self.api.list_projects()
        self.assertFalse(any(projectId == projectList['projects'][i]['projectId']
                             for i in range(len(projectList['projects']))))


    def test_CRUD_ProjectFiles_Successfully(self):
        '''Test updating the files associated with a project'''

        projectId = self.__createProjectAndGetId('Test project - ')

        fakeFile = {'name':'Hello.py', 'content': 'Hello World!'}
        realFile = {'name':'main.py', 'content': get_content('BasicTemplateAlgorithm.py')}
        secondRealFile = {'name':'lol.py', 'content': get_content('BasicTemplateForexAlgorithm.py')}

        # Add random file
        randomAdd = self.api.add_project_file(projectId, fakeFile['name'], fakeFile['content'])
        files = randomAdd.pop('files')[0]
        name = files['sname']
        self.assertTrue(randomAdd['success'])
        self.assertTrue(files['scontent'] == fakeFile['content'])
        self.assertTrue(name == fakeFile['name'])

        # Update names of file
        updatedName = self.api.update_project_filename(projectId, name, realFile['name'])
        self.assertTrue(updatedName['success'])

        # Replace content of file
        updateContents = self.api.update_project_file_content(projectId, realFile['name'], realFile['content'])
        self.assertTrue(updateContents['success'])

        # Read single file
        readFile = self.api.read_project_file(projectId, realFile['name'])
        files = readFile.pop('files')[0]
        self.assertTrue(readFile['success'])
        self.assertTrue(files['content'] == realFile['content'])
        self.assertTrue(files['name'] == realFile['name'])

        # Add a second file
        secondFile = self.api.add_project_file(projectId, secondRealFile['name'], secondRealFile['content'])
        files = secondFile.pop('files')[0]
        self.assertTrue(secondFile['success'])
        self.assertTrue(files['scontent'] == secondRealFile['content'])
        self.assertTrue(files['sname'] == secondRealFile['name'])

        # Read multiple files
        readFiles = self.api.read_project_files(projectId)
        self.assertTrue(readFiles['success'])
        self.assertTrue(len(readFiles['files']) == 2)

        # Delete the second file
        deleteFile = self.api.delete_project_file(projectId, secondRealFile['name'])
        self.assertTrue(deleteFile['success'])

        # Read files
        readFilesAgain = self.api.read_project_files(projectId)
        self.assertTrue(readFilesAgain['success'])
        self.assertTrue(len(readFilesAgain['files']) == 1)
        self.assertTrue(readFilesAgain['files'][0]['name'] == realFile['name'])

        # Delete the project
        self.assertTrue(self.api.delete_project(projectId)['success'])

    def test_Compiles_Project_Successfully(self):
        '''Testing creating and reading a compile request'''
        projectId = self.__createProjectAndGetId('Test project - ', 'BasicTemplateAlgorithm.py')

        result = self.api.create_compile(projectId)
        self.assertTrue(result['success'])
        self.assertEqual(result['state'], 'InQueue')
        compileId = result['compileId']

        attempts, result = self.__waitCompile(projectId, compileId)

        self.assertTrue(self.api.delete_project(projectId)['success'])
        self.assertEqual(result['state'], 'BuildSuccess', f'Fail after {attempts} attempts')            

    def test_Backtest_Project_Successfully(self):
        '''Testing creating and reading a backtest request'''
        projectId = self.__createProjectAndGetId('Test project - ', 'BasicTemplateAlgorithm.py')
        compileId = self.api.create_compile(projectId)['compileId']
        _, result = self.__waitCompile(projectId, compileId)

        result = self.api.create_backtest(projectId, compileId, 'Test backtest')
        self.assertTrue(result['success'])
        backtestId = result['backtestId']

        while(result['progress']<1):
            sleep(1)
            result = self.api.read_backtest(projectId, backtestId)

        self.assertTrue(self.api.delete_project(projectId)['success'])

        total_trades = int(result['result']['Statistics']['Total Trades'])
        self.assertEqual(1, total_trades, f'Fail total trades {total_trades}')
        return result['result']

    def __createProjectAndGetId(self, name, filename = ''):
        project = self.api.create_project(name, 'Py')
        projectId = project['projects'][0]['projectId']
        if len(filename) > 0:
            self.api.add_project_file(projectId, 'main.py', get_content(filename))
        return projectId

    def __waitCompile(self, projectId, compileId):
        attempts=0
        maxattempts=5
        while(maxattempts>attempts):
            sleep(1)
            result = self.api.read_compile(projectId, compileId)
            if result['state'] == 'BuildSuccess':
                attempts = maxattempts
            attempts += 1
        return attempts, result

def get_content(file):
    with open('../../Algorithm.Python/' + file, 'r') as f:
        content = f.read()
        return content[3:]

if __name__ == '__main__':
    os.chdir(os.path.dirname(os.path.abspath(__file__)))
    unittest.main(argv=['first-arg-is-ignored'], exit=False)