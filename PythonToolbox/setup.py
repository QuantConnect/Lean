# -*- coding: utf-8 -*-
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

from setuptools import setup, find_packages

# https://github.com/QuantConnect/Lean/blob/master/LICENSE
with open('../LICENSE') as f:
    license = f.read()

with open('README.rst') as f:
    readme = f.read()

setup(
     name='quantconnect',
     version='0.1',
     description = 'QuantConnect API',
     long_description=readme,
     author = 'QuantConnect Python Team',
     author_email = 'support@quantconnect.com',
     url='https://www.quantconnect.com/',
     license=license,
     packages = find_packages(exclude=('tests', 'docs')),
     install_requires=['matplotlib', 'pandas', 'requests']
     )