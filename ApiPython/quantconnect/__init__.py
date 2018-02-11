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

from hashlib import sha256
from logging import exception
from requests import Request, Session
from time import time

def create_secure_hash(timestamp, token):
    """Generate a secure hash for the authorization headers.
    Returns:
        Time based hash of user token and timestamp."""
    hash_data = sha256()
    hash_data.update('{0}:{1}'.format(token, timestamp).encode('utf-8'))
    return hash_data.hexdigest()


class ApiConnection:
    """API Connection and Hash Manager
    Attributes:
        client(str): Authorized client to use for requests.
        userId(int/str): User Id number from QuantConnect.com account. Found at www.quantconnect.com/account.
        token(str): Access token for the QuantConnect account. Found at www.quantconnect.com/account.
    """
    def __init__(self, userId, token):
        self.client = 'https://www.quantconnect.com/api/v2/'
        self.userId = str(userId)
        self.token = token
        if len(self.userId) * len(self.token) == 0:
            exception('Cannot use empty string for userId or token. Found yours at www.quantconnect.com/account')


    def connected(self):
        """Return true if connected successfully."""
        request = Request('GET', 'authenticate')
        result = self.try_request(request)
        return result['success']

    def try_request(self, request):
        """Place a secure request and get back an object of type T.
        Args:
            request: Result object of the request
        Returns:
            result: request response
        """
        timestamp = int(time())
        hash = create_secure_hash(timestamp, self.token)
        request.auth = (self.userId, hash)
        request.headers.update({'Timestamp': str(timestamp)})
        request.url = self.client + request.url

        try:
            session = Session()
            response = session.send(request.prepare())
            session.close()
            return response.json()
        except:
            exception('Failed to make REST request to {0}'.format(request.url))
            return { 'success': False }