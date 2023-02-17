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
            "{ \"cells\": [  {   \"cell_type\": \"markdown\",   \"id\": \"cffa5698\",   \"metadata\": {    \"papermill\": {     \"duration\": 0.016987,     \"end_t" +
            "ime\": \"2023-02-17T19:22:20.995895\",     \"exception\": false,     \"start_time\": \"2023-02-17T19:22:20.978908\",     \"status\": \"completed\"    " +
            "},    \"tags\": []   },   \"source\": [    \"![QuantConnect Logo](https://cdn.quantconnect.com/web/i/qc_notebook_logo_rev0.png)\",    \"## Welcome to " +
            "The QuantConnect Research Page\",    \"#### Refer to this page for documentation https://www.quantconnect.com/docs/research/overview#\",    \"#### Con" +
            "tribute to this template file https://github.com/QuantConnect/Lean/blob/master/Research/BasicQuantBookTemplate.ipynb\"   ]  },  {   \"cell_type\": \"m" +
            "arkdown\",   \"id\": \"70f078aa\",   \"metadata\": {    \"papermill\": {     \"duration\": 0.025497,     \"end_time\": \"2023-02-17T19:22:21.033394\"," +
            "     \"exception\": false,     \"start_time\": \"2023-02-17T19:22:21.007897\",     \"status\": \"completed\"    },    \"tags\": []   },   \"source\": " +
            "[    \"## QuantBook Basics\",    \"\",    \"### Start QuantBook\",    \"- Add the references and imports\",    \"- Create a QuantBook instance\"   ]  " +
            "},  {   \"cell_type\": \"code\",   \"execution_count\": 1,   \"id\": \"0ca0fe27\",   \"metadata\": {    \"execution\": {     \"iopub.execute_input\": " +
            "\"2023-02-17T19:22:21.066897Z\",     \"iopub.status.busy\": \"2023-02-17T19:22:21.065898Z\",     \"iopub.status.idle\": \"2023-02-17T19:22:21.086395Z\"" +
            ",     \"shell.execute_reply\": \"2023-02-17T19:22:21.084896Z\"    },    \"papermill\": {     \"duration\": 0.046997,     \"end_time\": \"2023-02-17T19" +
            ":22:21.091894\",     \"exception\": false,     \"start_time\": \"2023-02-17T19:22:21.044897\",     \"status\": \"completed\"    },    \"tags\": []   }" +
            ",   \"outputs\": [],   \"source\": [    \"import warnings\",    \"warnings.filterwarnings(\\\"ignore\\\")\"   ]  },  {   \"cell_type\": \"code\",   \"" +
            "execution_count\": 2,   \"id\": \"82ab27ab\",   \"metadata\": {    \"execution\": {     \"iopub.execute_input\": \"2023-02-17T19:22:21.125400Z\",     " +
            "\"iopub.status.busy\": \"2023-02-17T19:22:21.123900Z\",     \"iopub.status.idle\": \"2023-02-17T19:22:25.488798Z\",     \"shell.execute_reply\": \"202" +
            "3-02-17T19:22:25.487818Z\"    },    \"papermill\": {     \"duration\": 4.388906,     \"end_time\": \"2023-02-17T19:22:25.491302\",     \"exception\": " +
            "false,     \"start_time\": \"2023-02-17T19:22:21.102396\",     \"status\": \"completed\"    },    \"tags\": []   },   \"outputs\": [],   \"source\": [" +
            "    \"# Load in our startup script, required to set runtime for PythonNet\",    \"%run ./start.py\"   ]  },  {   \"cell_type\": \"code\",   \"executio" +
            "n_count\": 3,   \"id\": \"297d8757\",   \"metadata\": {    \"execution\": {     \"iopub.execute_input\": \"2023-02-17T19:22:25.521298Z\",     \"iopub." +
            "status.busy\": \"2023-02-17T19:22:25.520297Z\",     \"iopub.status.idle\": \"2023-02-17T19:22:25.596300Z\",     \"shell.execute_reply\": \"2023-02-17T" +
            "19:22:25.594798Z\"    },    \"papermill\": {     \"duration\": 0.097502,     \"end_time\": \"2023-02-17T19:22:25.598301\",     \"exception\": false,  " +
            "   \"start_time\": \"2023-02-17T19:22:25.500799\",     \"status\": \"completed\"    },    \"tags\": []   },   \"outputs\": [],   \"source\": [    \"cl" +
            "ass CustomDataType(PythonData):\",    \"\",    \"    def GetSource(self, config: SubscriptionDataConfig, date: datetime, isLive: bool) -> Subscription" +
            "DataSource:\",    \"        source = \\\"https://www.dl.dropboxusercontent.com/s/d83xvd7mm9fzpk0/path_to_my_csv_data.csv?dl=0\\\"\",    \"        retu" +
            "rn SubscriptionDataSource(source, SubscriptionTransportMedium.RemoteFile)\",    \"\",    \"    def Reader(self, config: SubscriptionDataConfig, line: " +
            "str, date: datetime, isLive: bool) -> BaseData:\",    \"        if not (line.strip()):\",    \"            return None\",    \"\",    \"        data =" +
            " line.split(',')\",    \"        obj_data = CustomDataType()\",    \"        obj_data.Symbol = config.Symbol\",    \"\",    \"        try:\",    \"   " +
            "         obj_data.Time = datetime.strptime(data[0], '%Y-%m-%d %H:%M:%S') + timedelta(hours=20)\",    \"            obj_data[\\\"open\\\"] = float(data" +
            "[1])\",    \"            obj_data[\\\"high\\\"] = float(data[2])\",    \"            obj_data[\\\"low\\\"] = float(data[3])\",    \"            obj_da" +
            "ta[\\\"close\\\"] = float(data[4])\",    \"            obj_data.Value = obj_data[\\\"close\\\"]\",    \"\",    \"            # property for asserting " +
            "the correct data is fetched\",    \"            obj_data[\\\"some_property\\\"] = \\\"some property value\\\"\",    \"        except ValueError:\",   " +
            " \"            return None\",    \"\",    \"        return obj_data\",    \"\",    \"    def __str__ (self):\",    \"        return f\\\"Time: {self.T" +
            "ime}, Value: {self.Value}, SomeProperty: {self['some_property']}, Open: {self['open']}, High: {self['high']}, Low: {self['low']}, Close: {self['close'" +
            "]}\\\"\"   ]  },  {   \"cell_type\": \"code\",   \"execution_count\": 4,   \"id\": \"c2ca9e65\",   \"metadata\": {    \"execution\": {     \"iopub.exe" +
            "cute_input\": \"2023-02-17T19:22:25.631624Z\",     \"iopub.status.busy\": \"2023-02-17T19:22:25.631125Z\",     \"iopub.status.idle\": \"2023-02-17T19:" +
            "22:27.883290Z\",     \"shell.execute_reply\": \"2023-02-17T19:22:27.880788Z\"    },    \"papermill\": {     \"duration\": 2.281479,     \"end_time\": " +
            "\"2023-02-17T19:22:27.887289\",     \"exception\": false,     \"start_time\": \"2023-02-17T19:22:25.605810\",     \"status\": \"completed\"    },    \"" +
            "tags\": []   },   \"outputs\": [    {     \"name\": \"stdout\",     \"output_type\": \"stream\",     \"text\": [      \"23\",      \"CUSTOMDATATYPE.Cu" +
            "stomDataType: ¤5,813.60\",      \"CUSTOMDATATYPE.CustomDataType: ¤5,822.25\",      \"CUSTOMDATATYPE.CustomDataType: ¤5,655.90\",      \"CUSTOMDATATYPE" +
            ".CustomDataType: ¤5,667.65\",      \"CUSTOMDATATYPE.CustomDataType: ¤5,590.25\",      \"CUSTOMDATATYPE.CustomDataType: ¤5,609.10\",      \"CUSTOMDATAT" +
            "YPE.CustomDataType: ¤5,588.70\",      \"CUSTOMDATATYPE.CustomDataType: ¤5,682.35\",      \"CUSTOMDATATYPE.CustomDataType: ¤5,842.20\",      \"CUSTOMDA" +
            "TATYPE.CustomDataType: ¤5,898.85\",      \"CUSTOMDATATYPE.CustomDataType: ¤5,857.55\",      \"CUSTOMDATATYPE.CustomDataType: ¤5,770.90\",      \"CUSTO" +
            "MDATATYPE.CustomDataType: ¤5,836.95\",      \"CUSTOMDATATYPE.CustomDataType: ¤5,867.90\",      \"CUSTOMDATATYPE.CustomDataType: ¤5,811.55\",      \"CU" +
            "STOMDATATYPE.CustomDataType: ¤5,859.00\",      \"CUSTOMDATATYPE.CustomDataType: ¤5,816.70\",      \"CUSTOMDATATYPE.CustomDataType: ¤5,935.10\",      \"" +
            "CUSTOMDATATYPE.CustomDataType: ¤6,009.00\",      \"CUSTOMDATATYPE.CustomDataType: ¤5,842.20\",      \"CUSTOMDATATYPE.CustomDataType: ¤5,898.85\",     " +
            " \"CUSTOMDATATYPE.CustomDataType: ¤5,857.55\",      \"CUSTOMDATATYPE.CustomDataType: ¤5,770.90\"     ]    }   ],   \"source\": [    \"# Create an inst" +
            "ance\",    \"qb = QuantBook()\",    \"\",    \"symbol = qb.AddData(CustomDataType, \\\"CustomDataType\\\", Resolution.Hour).Symbol\",    \"\",    \"st" +
            "artDate = datetime(2017, 8, 20)\",    \"endDate = startDate + timedelta(hours=48)\",    \"history = list(qb.History[CustomDataType](symbol, startDate," +
            " endDate, Resolution.Hour))\",    \"\",    \"print(len(history))\",    \"\",    \"for data in history:\",    \"    print(str(data))\"   ]  },  {   \"c" +
            "ell_type\": \"code\",   \"execution_count\": null,   \"id\": \"efc5a8c1\",   \"metadata\": {    \"papermill\": {     \"duration\": 0.021002,     \"end" +
            "_time\": \"2023-02-17T19:22:27.942291\",     \"exception\": false,     \"start_time\": \"2023-02-17T19:22:27.921289\",     \"status\": \"completed\"  " +
            "  },    \"tags\": []   },   \"outputs\": [],   \"source\": []  } ], \"metadata\": {  \"kernelspec\": {   \"display_name\": \"Python 3 (ipykernel)\",  " +
            " \"language\": \"python\",   \"name\": \"python3\"  },  \"language_info\": {   \"codemirror_mode\": {    \"name\": \"ipython\",    \"version\": 3   }," +
            "   \"file_extension\": \".py\",   \"mimetype\": \"text/x-python\",   \"name\": \"python\",   \"nbconvert_exporter\": \"python\",   \"pygments_lexer\":" +
            " \"ipython3\",   \"version\": \"3.8.10\"  },  \"papermill\": {   \"default_parameters\": {},   \"duration\": 14.999576,   \"end_time\": \"2023-02-17T1" +
            "9:22:30.728687\",   \"environment_variables\": {},   \"exception\": null,   \"input_path\": \"C:\\\\Users\\\\jhona\\\\QuantConnect\\\\Lean\\\\Tests\\\\b" +
            "in\\\\Debug\\\\Research\\\\RegressionTemplates\\\\BasicTemplateCustomDataTypeHistoryResearchPython.ipynb\",   \"output_path\": \"C:\\\\Users\\\\jhona\\\\Q" +
            "uantConnect\\\\Lean\\\\Tests\\\\bin\\\\Debug\\\\Research\\\\RegressionTemplates\\\\BasicTemplateCustomDataTypeHistoryResearchPython-output.ipynb\",   " +
            "\"parameters\": {},   \"start_time\": \"2023-02-17T19:22:15.729111\",   \"version\": \"2.4.0\"  },  \"vscode\": {   \"interpreter\": {    \"hash\": \"" +
            "9650cb4e16cdd4a8e8e2d128bf38d875813998db22a3c986335f89e0cb4d7bb2\"   }  } }, \"nbformat\": 4, \"nbformat_minor\": 5}";
    }
}
