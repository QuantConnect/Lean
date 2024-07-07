/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using QuantConnect.Interfaces;

namespace QuantConnect.Tests.Research.RegressionTemplates
{
    /// <summary>
    /// Basic template framework for regression testing of research notebooks
    /// </summary>
    public class BasicTemplateCustomDataTypeHistoryResearchPython : IRegressionResearchDefinition
    {
        /// <summary>
        /// Expected output from the reading the raw notebook file
        /// </summary>
        /// <remarks>Requires to be implemented last in the file <see cref="ResearchRegressionTests.UpdateResearchRegressionOutputInSourceFile"/>
        /// get should start from next line</remarks>
        public string ExpectedOutput =>
            "{ \"cells\": [  {   \"cell_type\": \"markdown\",   \"id\": \"d0ea3064\",   \"metadata\": {    \"papermill\": {     \"duration\": 0.003996,     \"end_t"
            + "ime\": \"2023-02-17T21:33:10.586532\",     \"exception\": false,     \"start_time\": \"2023-02-17T21:33:10.582536\",     \"status\": \"completed\"    "
            + "},    \"tags\": []   },   \"source\": [    \"![QuantConnect Logo](https://cdn.quantconnect.com/web/i/qc_notebook_logo_rev0.png)\",    \"## Welcome to "
            + "The QuantConnect Research Page\",    \"#### Refer to this page for documentation https://www.quantconnect.com/docs/research/overview#\",    \"#### Con"
            + "tribute to this template file https://github.com/QuantConnect/Lean/blob/master/Research/BasicQuantBookTemplate.ipynb\"   ]  },  {   \"cell_type\": \"m"
            + "arkdown\",   \"id\": \"e3e3d0c7\",   \"metadata\": {    \"papermill\": {     \"duration\": 0.003965,     \"end_time\": \"2023-02-17T21:33:10.593525\","
            + "     \"exception\": false,     \"start_time\": \"2023-02-17T21:33:10.589560\",     \"status\": \"completed\"    },    \"tags\": []   },   \"source\": "
            + "[    \"## QuantBook Basics\",    \"\",    \"### Start QuantBook\",    \"- Add the references and imports\",    \"- Create a QuantBook instance\"   ]  "
            + "},  {   \"cell_type\": \"code\",   \"execution_count\": 1,   \"id\": \"d0e8fcfc\",   \"metadata\": {    \"execution\": {     \"iopub.execute_input\": "
            + "\"2023-02-17T21:33:10.602549Z\",     \"iopub.status.busy\": \"2023-02-17T21:33:10.601530Z\",     \"iopub.status.idle\": \"2023-02-17T21:33:10.616522Z\""
            + ",     \"shell.execute_reply\": \"2023-02-17T21:33:10.614535Z\"    },    \"papermill\": {     \"duration\": 0.021984,     \"end_time\": \"2023-02-17T21"
            + ":33:10.618527\",     \"exception\": false,     \"start_time\": \"2023-02-17T21:33:10.596543\",     \"status\": \"completed\"    },    \"tags\": []   }"
            + ",   \"outputs\": [],   \"source\": [    \"import warnings\",    \"warnings.filterwarnings(\\\"ignore\\\")\"   ]  },  {   \"cell_type\": \"code\",   \""
            + "execution_count\": 2,   \"id\": \"a845a7ca\",   \"metadata\": {    \"execution\": {     \"iopub.execute_input\": \"2023-02-17T21:33:10.627549Z\",     "
            + "\"iopub.status.busy\": \"2023-02-17T21:33:10.627549Z\",     \"iopub.status.idle\": \"2023-02-17T21:33:15.142098Z\",     \"shell.execute_reply\": \"202"
            + "3-02-17T21:33:15.140599Z\"    },    \"papermill\": {     \"duration\": 4.526574,     \"end_time\": \"2023-02-17T21:33:15.149104\",     \"exception\": "
            + "false,     \"start_time\": \"2023-02-17T21:33:10.622530\",     \"status\": \"completed\"    },    \"tags\": []   },   \"outputs\": [],   \"source\": ["
            + "    \"# Load in our startup script, required to set runtime for PythonNet\",    \"%run ./start.py\"   ]  },  {   \"cell_type\": \"code\",   \"executio"
            + "n_count\": 3,   \"id\": \"9b0cd81c\",   \"metadata\": {    \"execution\": {     \"iopub.execute_input\": \"2023-02-17T21:33:15.162102Z\",     \"iopub."
            + "status.busy\": \"2023-02-17T21:33:15.160599Z\",     \"iopub.status.idle\": \"2023-02-17T21:33:15.266621Z\",     \"shell.execute_reply\": \"2023-02-17T"
            + "21:33:15.263102Z\"    },    \"papermill\": {     \"duration\": 0.117533,     \"end_time\": \"2023-02-17T21:33:15.270138\",     \"exception\": false,  "
            + "   \"start_time\": \"2023-02-17T21:33:15.152605\",     \"status\": \"completed\"    },    \"tags\": []   },   \"outputs\": [],   \"source\": [    \"cl"
            + "ass CustomDataType(PythonData):\",    \"\",    \"    def GetSource(self, config: SubscriptionDataConfig, date: datetime, isLive: bool) -> Subscription"
            + "DataSource:\",    \"        source = \\\"https://www.dl.dropboxusercontent.com/s/d83xvd7mm9fzpk0/path_to_my_csv_data.csv?dl=0\\\"\",    \"        retu"
            + "rn SubscriptionDataSource(source, SubscriptionTransportMedium.RemoteFile)\",    \"\",    \"    def Reader(self, config: SubscriptionDataConfig, line: "
            + "str, date: datetime, isLive: bool) -> BaseData:\",    \"        if not (line.strip()):\",    \"            return None\",    \"\",    \"        data ="
            + " line.split(',')\",    \"        obj_data = CustomDataType()\",    \"        obj_data.Symbol = config.Symbol\",    \"\",    \"        try:\",    \"   "
            + "         obj_data.Time = datetime.strptime(data[0], '%Y-%m-%d %H:%M:%S') + timedelta(hours=20)\",    \"            obj_data[\\\"open\\\"] = float(data"
            + "[1])\",    \"            obj_data[\\\"high\\\"] = float(data[2])\",    \"            obj_data[\\\"low\\\"] = float(data[3])\",    \"            obj_da"
            + "ta[\\\"close\\\"] = float(data[4])\",    \"            obj_data.Value = obj_data[\\\"close\\\"]\",    \"\",    \"            # property for asserting "
            + "the correct data is fetched\",    \"            obj_data[\\\"some_property\\\"] = \\\"some property value\\\"\",    \"        except ValueError:\",   "
            + " \"            return None\",    \"\",    \"        return obj_data\",    \"\",    \"    def __str__ (self):\",    \"        return f\\\"Time: {self.T"
            + "ime}, Value: {self.Value}, SomeProperty: {self['some_property']}, Open: {self['open']}, High: {self['high']}, Low: {self['low']}, Close: {self['close'"
            + "]}\\\"\"   ]  },  {   \"cell_type\": \"code\",   \"execution_count\": 4,   \"id\": \"e21f2b04\",   \"metadata\": {    \"execution\": {     \"iopub.exe"
            + "cute_input\": \"2023-02-17T21:33:15.282600Z\",     \"iopub.status.busy\": \"2023-02-17T21:33:15.281102Z\",     \"iopub.status.idle\": \"2023-02-17T21:"
            + "33:17.198103Z\",     \"shell.execute_reply\": \"2023-02-17T21:33:17.196601Z\"    },    \"papermill\": {     \"duration\": 1.926991,     \"end_time\": "
            + "\"2023-02-17T21:33:17.200100\",     \"exception\": false,     \"start_time\": \"2023-02-17T21:33:15.273109\",     \"status\": \"completed\"    },    \""
            + "tags\": []   },   \"outputs\": [],   \"source\": [    \"# Create an instance\",    \"qb = QuantBook()\",    \"symbol = qb.AddData(CustomDataType, \\\""
            + "CustomDataType\\\", Resolution.Hour).Symbol\",    \"\",    \"startDate = datetime(2017, 8, 20)\",    \"endDate = startDate + timedelta(hours=48)\",   "
            + " \"history = list(qb.History[CustomDataType](symbol, startDate, endDate, Resolution.Hour))\",    \"\",    \"if len(history) == 0:\",    \"    raise Ex"
            + "ception(\\\"No history data returned\\\")\"   ]  } ], \"metadata\": {  \"kernelspec\": {   \"display_name\": \"Python 3 (ipykernel)\",   \"language\":"
            + " \"python\",   \"name\": \"python3\"  },  \"language_info\": {   \"codemirror_mode\": {    \"name\": \"ipython\",    \"version\": 3   },   \"file_exte"
            + "nsion\": \".py\",   \"mimetype\": \"text/x-python\",   \"name\": \"python\",   \"nbconvert_exporter\": \"python\",   \"pygments_lexer\": \"ipython3\","
            + "   \"version\": \"3.8.10\"  },  \"papermill\": {   \"default_parameters\": {},   \"duration\": 13.405899,   \"end_time\": \"2023-02-17T21:33:20.058754"
            + "\",   \"environment_variables\": {},   \"exception\": null,   \"input_path\": \"C:\\\\Users\\\\jhona\\\\QuantConnect\\\\Lean\\\\Tests\\\\bin\\\\Debug\\\\R"
            + "esearch\\\\RegressionTemplates\\\\BasicTemplateCustomDataTypeHistoryResearchPython.ipynb\",   \"output_path\": \"C:\\\\Users\\\\jhona\\\\QuantConnect\\\\L"
            + "ean\\\\Tests\\\\bin\\\\Debug\\\\Research\\\\RegressionTemplates\\\\BasicTemplateCustomDataTypeHistoryResearchPython-output.ipynb\",   \"parameters\": "
            + "{},   \"start_time\": \"2023-02-17T21:33:06.652855\",   \"version\": \"2.4.0\"  },  \"vscode\": {   \"interpreter\": {    \"hash\": \"9650cb4e16cdd4a8"
            + "e8e2d128bf38d875813998db22a3c986335f89e0cb4d7bb2\"   }  } }, \"nbformat\": 4, \"nbformat_minor\": 5}";
    }
}
