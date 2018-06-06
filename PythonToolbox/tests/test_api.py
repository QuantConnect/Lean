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

from quantconnect import ApiConnection
from quantconnect.api import Api
from quantconnect.LeanReportCreator import LeanReportCreator
import unittest
import os

class TestApi(unittest.TestCase):

    def setUp(self):
        # Please add your credentials. Found at https://www.quantconnect.com/account 
        self.userId = ""
        self.testToken = ""
        self.api = Api(self.userId, self.testToken)

    def test_Projects_CanBeCreatedAndDeleted_Successfully(self):
        """Test creating and deleting projects with the Api"""

        name = "Test Project "
        # Test create a new project successfully
        project = self.api.create_project(name, "Py")
        self.assertTrue(project['success'])
        self.assertTrue(project['projects'][0]['projectId'] > 0)
        self.assertTrue(project['projects'][0]['name'] == name)

        # Delete the project
        deleteProject = self.api.delete_project(project['projects'][0]['projectId'])
        self.assertTrue(deleteProject['success'])

        # Make sure the project is really deleted
        projectList = self.api.list_projects()
        self.assertFalse(any(project['projects'][0]['projectId'] == projectList['projects'][i]['projectId']
                             for i in range(len(projectList['projects']))))

    def test_ApiConnectionWillAuthenticate_ValidCredentials_Successfully(self):
        """Test successfully authenticating with the ApiConnection using valid credentials."""
        connection = ApiConnection(self.userId, self.testToken)
        self.assertTrue(connection.connected())

    def test_ApiWillAuthenticate_ValidCredentials_Successfully(self):
        """Test successfully authenticating with the API using valid credentials."""
        api = Api(self.userId, self.testToken)
        self.assertTrue(api.connected())

    def test_ApiConnectionWillAuthenticate_InvalidCredentials_Unsuccessfully(self):
        """Test that the ApiConnection will reject invalid credentials"""
        connection = ApiConnection(self.userId, "")
        self.assertFalse(connection.connected())

    def test_ApiWillAuthenticate_InvalidCredentials_Unsuccessfully(self):
        """Test that the Api will reject invalid credentials"""
        api = Api(self.userId, "")
        self.assertFalse(api.connected())

    def test_CRUD_ProjectFiles_Successfully(self):
        """Test updating the files associated with a project"""

        real_file_code = get_content('BasicTemplateAlgorithm.py')
        second_real_file_code = get_content('BasicTemplateForexAlgorithm.py')

        fakeFile = {"name":"Hello.py", "code": "Hello World!"}
        realFile = {"name":"main.py", "code": real_file_code}
        secondRealFile = {"name":"lol.py", "code": second_real_file_code}

        # Create a new project and make sure there are no files
        project = self.api.create_project("Test project - ", "Py")
        self.assertTrue(project['success'])
        self.assertTrue(project['projects'][0]['projectId'] > 0)

        # Add random file
        randomAdd = self.api.add_project_file(project['projects'][0]['projectId'], fakeFile["name"], fakeFile["code"])
        self.assertTrue(randomAdd['success'])
        self.assertTrue(randomAdd['files'][0]['content'] == fakeFile['code'])
        self.assertTrue(randomAdd['files'][0]['name'] == fakeFile['name'])

        # Update names of file
        updatedName = self.api.update_project_filename(project['projects'][0]['projectId'], randomAdd['files'][0]['name'], realFile['name'])
        self.assertTrue(updatedName['success'])

        # Replace content of file
        updateContents = self.api.update_project_file_content(project['projects'][0]['projectId'], realFile["name"], realFile['code'])
        self.assertTrue(updateContents['success'])

        # Read single file
        readFile = self.api.read_project_file(project['projects'][0]['projectId'], realFile['name'])
        self.assertTrue(readFile['success'])
        self.assertTrue(readFile['files'][0]['content'] == realFile['code'])
        self.assertTrue(readFile['files'][0]['name'] == realFile['name'])

        # Add a second file
        secondFile = self.api.add_project_file(project['projects'][0]['projectId'], secondRealFile['name'], secondRealFile['code'])
        self.assertTrue(secondFile['success'])
        self.assertTrue(secondFile['files'][0]['content'] == secondRealFile['code'])
        self.assertTrue(secondFile['files'][0]['name'] == secondRealFile['name'])

        # Read multiple files
        readFiles = self.api.read_project_files(project['projects'][0]['projectId'])
        self.assertTrue(readFiles['success'])
        self.assertTrue(len(readFiles['files']) == 2)

        # Delete the second file
        deleteFile = self.api.delete_project_file(project['projects'][0]['projectId'], secondRealFile['name'])
        self.assertTrue(deleteFile['success'])

        # Read files
        readFilesAgain = self.api.read_project_files(project['projects'][0]['projectId'])
        self.assertTrue(readFilesAgain['success'])
        self.assertTrue(len(readFilesAgain['files']) == 1)
        self.assertTrue(readFilesAgain['files'][0]['name'] == realFile['name'])

        # Delete the project
        deleteProject = self.api.delete_project(project['projects'][0]['projectId'])
        self.assertTrue(deleteProject['success'])

    def test_LeanReportCreator(self):
        lrc = LeanReportCreator('--backtest=./json/sample.json --output=./outputs_test/Report.html --user=user_data.json')
        lrc.create()

def get_content(file):
    with open("../Algorithm.Python/" + file, 'r') as f:
        return f.read()

if __name__ == '__main__':
    unittest.main(argv=['first-arg-is-ignored'], exit=False)