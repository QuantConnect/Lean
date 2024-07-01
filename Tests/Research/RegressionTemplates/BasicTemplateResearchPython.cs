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
            "{ \"cells\": [  {   \"cell_type\": \"markdown\",   \"id\": \"f5416762\",   \"metadata\": {    \"papermill\": {     \"duration\": 0.001898,     \"end_t" +
            "ime\": \"2024-06-06T22:15:49.572477\",     \"exception\": false,     \"start_time\": \"2024-06-06T22:15:49.570579\",     \"status\": \"completed\"    " +
            "},    \"tags\": []   },   \"source\": [    \"![QuantConnect Logo](https://cdn.quantconnect.com/web/i/qc_notebook_logo_rev0.png)\",    \"## Welcome to " +
            "The QuantConnect Research Page\",    \"#### Refer to this page for documentation https://www.quantconnect.com/docs/research/overview#\",    \"#### Con" +
            "tribute to this template file https://github.com/QuantConnect/Lean/blob/master/Research/BasicQuantBookTemplate.ipynb\"   ]  },  {   \"cell_type\": \"m" +
            "arkdown\",   \"id\": \"44ed65de\",   \"metadata\": {    \"papermill\": {     \"duration\": 0.001,     \"end_time\": \"2024-06-06T22:15:49.574475\",   " +
            "  \"exception\": false,     \"start_time\": \"2024-06-06T22:15:49.573475\",     \"status\": \"completed\"    },    \"tags\": []   },   \"source\": [  " +
            "  \"## QuantBook Basics\",    \"\",    \"### Start QuantBook\",    \"- Add the references and imports\",    \"- Create a QuantBook instance\"   ]  }, " +
            " {   \"cell_type\": \"code\",   \"execution_count\": 1,   \"id\": \"677a4f25\",   \"metadata\": {    \"execution\": {     \"iopub.execute_input\": \"2" +
            "024-06-06T22:15:49.577476Z\",     \"iopub.status.busy\": \"2024-06-06T22:15:49.577476Z\",     \"iopub.status.idle\": \"2024-06-06T22:15:49.581464Z\", " +
            "    \"shell.execute_reply\": \"2024-06-06T22:15:49.581464Z\"    },    \"papermill\": {     \"duration\": 0.006902,     \"end_time\": \"2024-06-06T22:1" +
            "5:49.582478\",     \"exception\": false,     \"start_time\": \"2024-06-06T22:15:49.575576\",     \"status\": \"completed\"    },    \"tags\": []   }, " +
            "  \"outputs\": [],   \"source\": [    \"import warnings\",    \"warnings.filterwarnings(\\\"ignore\\\")\"   ]  },  {   \"cell_type\": \"code\",   \"ex" +
            "ecution_count\": 2,   \"id\": \"e752d505\",   \"metadata\": {    \"execution\": {     \"iopub.execute_input\": \"2024-06-06T22:15:49.584480Z\",     \"" +
            "iopub.status.busy\": \"2024-06-06T22:15:49.584480Z\",     \"iopub.status.idle\": \"2024-06-06T22:15:51.028418Z\",     \"shell.execute_reply\": \"2024-" +
            "06-06T22:15:51.028418Z\"    },    \"papermill\": {     \"duration\": 1.445955,     \"end_time\": \"2024-06-06T22:15:51.029433\",     \"exception\": fa" +
            "lse,     \"start_time\": \"2024-06-06T22:15:49.583478\",     \"status\": \"completed\"    },    \"tags\": []   },   \"outputs\": [],   \"source\": [  " +
            "  \"# Load in our startup script, required to set runtime for PythonNet\",    \"%run ./start.py\"   ]  },  {   \"cell_type\": \"code\",   \"execution_" +
            "count\": 3,   \"id\": \"08d48a2d\",   \"metadata\": {    \"execution\": {     \"iopub.execute_input\": \"2024-06-06T22:15:51.032433Z\",     \"iopub.st" +
            "atus.busy\": \"2024-06-06T22:15:51.032433Z\",     \"iopub.status.idle\": \"2024-06-06T22:15:51.344980Z\",     \"shell.execute_reply\": \"2024-06-06T22" +
            ":15:51.344980Z\"    },    \"papermill\": {     \"duration\": 0.315568,     \"end_time\": \"2024-06-06T22:15:51.345999\",     \"exception\": false,    " +
            " \"start_time\": \"2024-06-06T22:15:51.030431\",     \"status\": \"completed\"    },    \"tags\": []   },   \"outputs\": [],   \"source\": [    \"# Cr" +
            "eate an instance\",    \"qb = QuantBook()\",    \"\",    \"# Select asset data\",    \"spy = qb.AddEquity(\\\"SPY\\\")\"   ]  },  {   \"cell_type\": \"" +
            "markdown\",   \"id\": \"07f707ad\",   \"metadata\": {    \"papermill\": {     \"duration\": 0.000998,     \"end_time\": \"2024-06-06T22:15:51.348997\"" +
            ",     \"exception\": false,     \"start_time\": \"2024-06-06T22:15:51.347999\",     \"status\": \"completed\"    },    \"tags\": []   },   \"source\":" +
            " [    \"### Historical Data Requests\",    \"\",    \"We can use the QuantConnect API to make Historical Data Requests. The data will be presented as " +
            "multi-index pandas.DataFrame where the first index is the Symbol.\",    \"\",    \"For more information, please follow the [link](https://www.quantcon" +
            "nect.com/docs#Historical-Data-Historical-Data-Requests).\"   ]  },  {   \"cell_type\": \"code\",   \"execution_count\": 4,   \"id\": \"ef440f9e\",   \"" +
            "metadata\": {    \"execution\": {     \"iopub.execute_input\": \"2024-06-06T22:15:51.352999Z\",     \"iopub.status.busy\": \"2024-06-06T22:15:51.35299" +
            "9Z\",     \"iopub.status.idle\": \"2024-06-06T22:15:51.356860Z\",     \"shell.execute_reply\": \"2024-06-06T22:15:51.356860Z\"    },    \"papermill\":" +
            " {     \"duration\": 0.00689,     \"end_time\": \"2024-06-06T22:15:51.357885\",     \"exception\": false,     \"start_time\": \"2024-06-06T22:15:51.35" +
            "0995\",     \"status\": \"completed\"    },    \"tags\": []   },   \"outputs\": [],   \"source\": [    \"startDate = DateTime(2021,1,1)\",    \"endDat" +
            "e = DateTime(2021,12,31)\"   ]  },  {   \"cell_type\": \"code\",   \"execution_count\": 5,   \"id\": \"33a5fd5d\",   \"metadata\": {    \"execution\":" +
            " {     \"iopub.execute_input\": \"2024-06-06T22:15:51.361875Z\",     \"iopub.status.busy\": \"2024-06-06T22:15:51.360883Z\",     \"iopub.status.idle\"" +
            ": \"2024-06-06T22:15:51.453871Z\",     \"shell.execute_reply\": \"2024-06-06T22:15:51.453871Z\"    },    \"papermill\": {     \"duration\": 0.095009, " +
            "    \"end_time\": \"2024-06-06T22:15:51.454884\",     \"exception\": false,     \"start_time\": \"2024-06-06T22:15:51.359875\",     \"status\": \"comp" +
            "leted\"    },    \"scrolled\": true,    \"tags\": []   },   \"outputs\": [],   \"source\": [    \"# Gets historical data from the subscribed assets, t" +
            "he last 360 datapoints with daily resolution\",    \"h1 = qb.History(qb.Securities.Keys, startDate, endDate, Resolution.Daily)\",    \"\",    \"if h1." +
            "shape[0] < 1:\",    \"    raise Exception(\\\"History request resulted in no data\\\")\"   ]  },  {   \"cell_type\": \"markdown\",   \"id\": \"e8f9c90" +
            "d\",   \"metadata\": {    \"papermill\": {     \"duration\": 0.000996,     \"end_time\": \"2024-06-06T22:15:51.457883\",     \"exception\": false,    " +
            " \"start_time\": \"2024-06-06T22:15:51.456887\",     \"status\": \"completed\"    },    \"tags\": []   },   \"source\": [    \"### Indicators\",    \"" +
            "\",    \"We can easily get the indicator of a given symbol with QuantBook. \",    \"\",    \"For all indicators, please checkout QuantConnect Indicato" +
            "rs [Reference Table](https://www.quantconnect.com/docs#Indicators-Reference-Table)\"   ]  },  {   \"cell_type\": \"code\",   \"execution_count\": 6,  " +
            " \"id\": \"dcc7e1f0\",   \"metadata\": {    \"execution\": {     \"iopub.execute_input\": \"2024-06-06T22:15:51.461887Z\",     \"iopub.status.busy\": " +
            "\"2024-06-06T22:15:51.461887Z\",     \"iopub.status.idle\": \"2024-06-06T22:15:51.502741Z\",     \"shell.execute_reply\": \"2024-06-06T22:15:51.502741" +
            "Z\"    },    \"papermill\": {     \"duration\": 0.044872,     \"end_time\": \"2024-06-06T22:15:51.503757\",     \"exception\": false,     \"start_time" +
            "\": \"2024-06-06T22:15:51.458885\",     \"status\": \"completed\"    },    \"tags\": []   },   \"outputs\": [],   \"source\": [    \"# Example with BB" +
            ", it is a datapoint indicator\",    \"# Define the indicator\",    \"bb = BollingerBands(30, 2)\",    \"\",    \"# Gets historical data of indicator\"" +
            ",    \"bbdf = qb.IndicatorHistory(bb, \\\"SPY\\\", startDate, endDate, Resolution.Daily).data_frame\",    \"\",    \"# drop undesired fields\",    \"bbdf = b" +
            "bdf.drop('standarddeviation', axis=1)\",    \"\",    \"if bbdf.shape[0] < 1:\",    \"    raise Exception(\\\"Bollinger Bands resulted in no data\\\")\"" +
            "   ]  },  {   \"cell_type\": \"code\",   \"execution_count\": null,   \"id\": \"3095c061\",   \"metadata\": {    \"papermill\": {     \"duration\": 0." +
            "001003,     \"end_time\": \"2024-06-06T22:15:51.506759\",     \"exception\": false,     \"start_time\": \"2024-06-06T22:15:51.505756\",     \"status\"" +
            ": \"completed\"    },    \"tags\": []   },   \"outputs\": [],   \"source\": []  } ], \"metadata\": {  \"kernelspec\": {   \"display_name\": \"Python 3" +
            " (ipykernel)\",   \"language\": \"python\",   \"name\": \"python3\"  },  \"language_info\": {   \"codemirror_mode\": {    \"name\": \"ipython\",    \"" +
            "version\": 3   },   \"file_extension\": \".py\",   \"mimetype\": \"text/x-python\",   \"name\": \"python\",   \"nbconvert_exporter\": \"python\",   \"" +
            "pygments_lexer\": \"ipython3\",   \"version\": \"3.11.7\"  },  \"papermill\": {   \"default_parameters\": {},   \"duration\": 6.254067,   \"end_time\"" +
            ": \"2024-06-06T22:15:54.143637\",   \"environment_variables\": {},   \"exception\": null,   \"input_path\": \"D:\\\\QuantConnect\\\\MyLean\\\\Lean\\\\T" +
            "ests\\\\bin\\\\Debug\\\\Research\\\\RegressionTemplates\\\\BasicTemplateResearchPython.ipynb\",   \"output_path\": \"D:\\\\QuantConnect\\\\MyLean\\\\L" +
            "ean\\\\Tests\\\\bin\\\\Debug\\\\Research\\\\RegressionTemplates\\\\BasicTemplateResearchPython-output.ipynb\",   \"parameters\": {},   \"start_time\":" +
            " \"2024-06-06T22:15:47.889570\",   \"version\": \"2.4.0\"  } }, \"nbformat\": 4, \"nbformat_minor\": 5}";
    }
}
