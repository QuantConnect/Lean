#
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
#

from clr import AddReference
AddReference("QuantConnect.Common")

from QuantConnect.Data.Consolidators import *
from QuantConnect.Data.Market import *
from datetime import datetime,timedelta,timezone
import unittest
    
class BaseDataConsolidatorTests(unittest.TestCase):
    def test_Pyobjectconstructor(self):
        consolidator = BaseDataConsolidator(self.Func)
        global consolidated 
        def func(sender,tradebar):
            global consolidated
            consolidated = tradebar
        consolidated = None
        consolidator.DataConsolidated += func
        reference_date = datetime(year=2015, month = 4, day = 13)

        tck = Tick()
        tck.Time = reference_date
        print(reference_date)
        consolidator.Update(tck)
        self.assertIsNone(consolidated);
        reference_date += timedelta(hours = 17)
        print(reference_date)
        tck.Time = reference_date
        consolidator.Update(tck)
        self.assertIsNotNone(consolidated);
    
    @staticmethod
    def Func(dt):
        period = timedelta(days=1)
        tz = timezone(timedelta(0))
        start_time = datetime(year = dt.year, month = dt.month, day = dt.day, hour = 17,tzinfo = tz)
        dt = dt.astimezone(tz)
        if start_time > dt:
            start_time = start_time - period
        return CalendarInfo(start_time, period)

if __name__ == '__main__':
    unittest.main()