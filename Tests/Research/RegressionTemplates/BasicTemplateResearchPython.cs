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
    public class BasicTemplateResearchPython : IRegressionResearchDefinition
    {
        /// <summary>
        /// Expected output from the reading the raw notebook file
        /// </summary>
        /// <remarks>Requires to be implemented last in the file <see cref="ResearchRegressionTests.UpdateResearchRegressionOutputInSourceFile"/>
        /// get should start from next line</remarks>
        public string ExpectedOutput =>
            "{ \"cells\": [  {   \"cell_type\": \"markdown\",   \"id\": \"151a5f39\",   \"metadata\": {    \"papermill\": {     \"duration\": 0.004934,     \"end_t" +
            "ime\": \"2022-03-02T20:45:02.751048\",     \"exception\": false,     \"start_time\": \"2022-03-02T20:45:02.746114\",     \"status\": \"completed\"    " +
            "},    \"tags\": []   },   \"source\": [    \"![QuantConnect Logo](https://cdn.quantconnect.com/web/i/qc_notebook_logo_rev0.png)\",    \"## Welcome to " +
            "The QuantConnect Research Page\",    \"#### Refer to this page for documentation https://www.quantconnect.com/docs/research/overview#\",    \"#### Con" +
            "tribute to this template file https://github.com/QuantConnect/Lean/blob/master/Research/BasicQuantBookTemplate.ipynb\"   ]  },  {   \"cell_type\": \"m" +
            "arkdown\",   \"id\": \"c704973c\",   \"metadata\": {    \"papermill\": {     \"duration\": 0.002998,     \"end_time\": \"2022-03-02T20:45:02.757046\"," +
            "     \"exception\": false,     \"start_time\": \"2022-03-02T20:45:02.754048\",     \"status\": \"completed\"    },    \"tags\": []   },   \"source\": " +
            "[    \"## QuantBook Basics\",    \"\",    \"### Start QuantBook\",    \"- Add the references and imports\",    \"- Create a QuantBook instance\"   ]  " +
            "},  {   \"cell_type\": \"code\",   \"execution_count\": 1,   \"id\": \"7d7ae597\",   \"metadata\": {    \"execution\": {     \"iopub.execute_input\": " +
            "\"2022-03-02T20:45:02.767047Z\",     \"iopub.status.busy\": \"2022-03-02T20:45:02.767047Z\",     \"iopub.status.idle\": \"2022-03-02T20:45:02.779449Z\"" +
            ",     \"shell.execute_reply\": \"2022-03-02T20:45:02.778361Z\"    },    \"papermill\": {     \"duration\": 0.018403,     \"end_time\": \"2022-03-02T20" +
            ":45:02.779449\",     \"exception\": false,     \"start_time\": \"2022-03-02T20:45:02.761046\",     \"status\": \"completed\"    },    \"tags\": []   }" +
            ",   \"outputs\": [],   \"source\": [    \"import warnings\",    \"warnings.filterwarnings(\\\"ignore\\\")\"   ]  },  {   \"cell_type\": \"code\",   \"" +
            "execution_count\": 2,   \"id\": \"ba9ca1e0\",   \"metadata\": {    \"execution\": {     \"iopub.execute_input\": \"2022-03-02T20:45:02.788066Z\",     " +
            "\"iopub.status.busy\": \"2022-03-02T20:45:02.788066Z\",     \"iopub.status.idle\": \"2022-03-02T20:45:04.033428Z\",     \"shell.execute_reply\": \"202" +
            "2-03-02T20:45:04.033428Z\"    },    \"papermill\": {     \"duration\": 1.250062,     \"end_time\": \"2022-03-02T20:45:04.033428\",     \"exception\": " +
            "false,     \"start_time\": \"2022-03-02T20:45:02.783366\",     \"status\": \"completed\"    },    \"tags\": []   },   \"outputs\": [],   \"source\": [" +
            "    \"# Load in our startup script, required to set runtime for PythonNet\",    \"%run ./start.py\"   ]  },  {   \"cell_type\": \"code\",   \"executio" +
            "n_count\": 3,   \"id\": \"81d99304\",   \"metadata\": {    \"execution\": {     \"iopub.execute_input\": \"2022-03-02T20:45:04.337379Z\",     \"iopub." +
            "status.busy\": \"2022-03-02T20:45:04.337379Z\",     \"iopub.status.idle\": \"2022-03-02T20:45:04.394682Z\",     \"shell.execute_reply\": \"2022-03-02T" +
            "20:45:04.393670Z\"    },    \"papermill\": {     \"duration\": 0.357254,     \"end_time\": \"2022-03-02T20:45:04.394682\",     \"exception\": false,  " +
            "   \"start_time\": \"2022-03-02T20:45:04.037428\",     \"status\": \"completed\"    },    \"tags\": []   },   \"outputs\": [],   \"source\": [    \"# " +
            "Create an instance\",    \"qb = QuantBook()\",    \"\",    \"# Select asset data\",    \"spy = qb.AddEquity(\\\"SPY\\\")\"   ]  },  {   \"cell_type\":" +
            " \"markdown\",   \"id\": \"600efd4c\",   \"metadata\": {    \"papermill\": {     \"duration\": 0.003987,     \"end_time\": \"2022-03-02T20:45:04.40267" +
            "0\",     \"exception\": false,     \"start_time\": \"2022-03-02T20:45:04.398683\",     \"status\": \"completed\"    },    \"tags\": []   },   \"source" +
            "\": [    \"### Historical Data Requests\",    \"\",    \"We can use the QuantConnect API to make Historical Data Requests. The data will be presented " +
            "as multi-index pandas.DataFrame where the first index is the Symbol.\",    \"\",    \"For more information, please follow the [link](https://www.quant" +
            "connect.com/docs#Historical-Data-Historical-Data-Requests).\"   ]  },  {   \"cell_type\": \"code\",   \"execution_count\": 4,   \"id\": \"8c5f2d19\", " +
            "  \"metadata\": {    \"execution\": {     \"iopub.execute_input\": \"2022-03-02T20:45:04.412679Z\",     \"iopub.status.busy\": \"2022-03-02T20:45:04.4" +
            "12679Z\",     \"iopub.status.idle\": \"2022-03-02T20:45:04.425671Z\",     \"shell.execute_reply\": \"2022-03-02T20:45:04.425671Z\"    },    \"papermil" +
            "l\": {     \"duration\": 0.019992,     \"end_time\": \"2022-03-02T20:45:04.425671\",     \"exception\": false,     \"start_time\": \"2022-03-02T20:45:" +
            "04.405679\",     \"status\": \"completed\"    },    \"tags\": []   },   \"outputs\": [],   \"source\": [    \"startDate = DateTime(2021,1,1)\",    \"e" +
            "ndDate = DateTime(2021,12,31)\"   ]  },  {   \"cell_type\": \"code\",   \"execution_count\": 5,   \"id\": \"b519c565\",   \"metadata\": {    \"executi" +
            "on\": {     \"iopub.execute_input\": \"2022-03-02T20:45:04.438685Z\",     \"iopub.status.busy\": \"2022-03-02T20:45:04.437679Z\",     \"iopub.status.i" +
            "dle\": \"2022-03-02T20:45:04.569852Z\",     \"shell.execute_reply\": \"2022-03-02T20:45:04.569852Z\"    },    \"papermill\": {     \"duration\": 0.141" +
            "25,     \"end_time\": \"2022-03-02T20:45:04.570929\",     \"exception\": false,     \"start_time\": \"2022-03-02T20:45:04.429679\",     \"status\": \"" +
            "completed\"    },    \"scrolled\": true,    \"tags\": []   },   \"outputs\": [],   \"source\": [    \"# Gets historical data from the subscribed asset" +
            "s, the last 360 datapoints with daily resolution\",    \"h1 = qb.History(qb.Securities.Keys, startDate, endDate, Resolution.Daily)\",    \"\",    \"if" +
            " h1.shape[0] < 1:\",    \"    raise Exception(\\\"History request resulted in no data\\\")\"   ]  },  {   \"cell_type\": \"markdown\",   \"id\": \"a63" +
            "6ffdc\",   \"metadata\": {    \"papermill\": {     \"duration\": 0.003002,     \"end_time\": \"2022-03-02T20:45:04.577859\",     \"exception\": false," +
            "     \"start_time\": \"2022-03-02T20:45:04.574857\",     \"status\": \"completed\"    },    \"tags\": []   },   \"source\": [    \"### Indicators\",  " +
            "  \"\",    \"We can easily get the indicator of a given symbol with QuantBook. \",    \"\",    \"For all indicators, please checkout QuantConnect Indi" +
            "cators [Reference Table](https://www.quantconnect.com/docs#Indicators-Reference-Table)\"   ]  },  {   \"cell_type\": \"code\",   \"execution_count\": " +
            "6,   \"id\": \"62398b73\",   \"metadata\": {    \"execution\": {     \"iopub.execute_input\": \"2022-03-02T20:45:04.593850Z\",     \"iopub.status.busy" +
            "\": \"2022-03-02T20:45:04.593850Z\",     \"iopub.status.idle\": \"2022-03-02T20:45:04.647088Z\",     \"shell.execute_reply\": \"2022-03-02T20:45:04.64" +
            "7088Z\"    },    \"papermill\": {     \"duration\": 0.065235,     \"end_time\": \"2022-03-02T20:45:04.647088\",     \"exception\": false,     \"start_" +
            "time\": \"2022-03-02T20:45:04.581853\",     \"status\": \"completed\"    },    \"tags\": []   },   \"outputs\": [],   \"source\": [    \"# Example wit" +
            "h BB, it is a datapoint indicator\",    \"# Define the indicator\",    \"bb = BollingerBands(30, 2)\",    \"\",    \"# Gets historical data of indicat" +
            "or\",    \"bbdf = qb.Indicator(bb, \\\"SPY\\\", startDate, endDate, Resolution.Daily)\",    \"\",    \"# drop undesired fields\",    \"bbdf = bbdf.dro" +
            "p('standarddeviation', axis=1)\",    \"\",    \"if bbdf.shape[0] < 1:\",    \"    raise Exception(\\\"Bollinger Bands resulted in no data\\\")\"   ]  },  {" +
            "   \"cell_type\": \"code\",   \"execution_count\": null,   \"id\": \"6a7127bf\",   \"metadata\": {    \"papermill\": {     \"duration\": 0.003998,    " +
            " \"end_time\": \"2022-03-02T20:45:04.655082\",     \"exception\": false,     \"start_time\": \"2022-03-02T20:45:04.651084\",     \"status\": \"complet" +
            "ed\"    },    \"tags\": []   },   \"outputs\": [],   \"source\": []  } ], \"metadata\": {  \"kernelspec\": {   \"display_name\": \"Python 3 (ipykernel" +
            ")\",   \"language\": \"python\",   \"name\": \"python3\"  },  \"language_info\": {   \"codemirror_mode\": {    \"name\": \"ipython\",    \"version\": " +
            "3   },   \"file_extension\": \".py\",   \"mimetype\": \"text/x-python\",   \"name\": \"python\",   \"nbconvert_exporter\": \"python\",   \"pygments_le" +
            "xer\": \"ipython3\",   \"version\": \"3.6.8\"  },  \"papermill\": {   \"default_parameters\": {},   \"duration\": 3.263241,   \"end_time\": \"2022-03-" +
            "02T20:45:05.007754\",   \"environment_variables\": {},   \"exception\": null,   \"input_path\": \"D:\\\\quantconnect\\\\Lean\\\\Tests\\\\bin\\\\Debug\\\\B" +
            "asicTemplateResearchPython.ipynb\",   \"output_path\": \"D:\\\\quantconnect\\\\Lean\\\\Tests\\\\bin\\\\Debug\\\\BasicTemplateResearchPython-output.ipy" +
            "nb\",   \"parameters\": {},   \"start_time\": \"2022-03-02T20:45:01.744513\",   \"version\": \"2.3.4\"  } }, \"nbformat\": 4, \"nbformat_minor\": 5}";
    }
}
