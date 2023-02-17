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
    public class BasicTemplateCustomDataTypeHistoryResearchCSharp : IRegressionResearchDefinition
    {
        /// <summary>
        /// Expected output from the reading the raw notebook file
        /// </summary>
        /// <remarks>Requires to be implemented last in the file <see cref="ResearchRegressionTests.UpdateResearchRegressionOutputInSourceFile"/>
        /// get should start from next line</remarks>
        public string ExpectedOutput =>
            "{ \"cells\": [  {   \"cell_type\": \"markdown\",   \"id\": \"993eebad\",   \"metadata\": {    \"papermill\": {     \"duration\": null,     \"end_time\"" +
            ": null,     \"exception\": null,     \"start_time\": null,     \"status\": \"completed\"    },    \"tags\": []   },   \"source\": [    \"![QuantConnec" +
            "t Logo](https://cdn.quantconnect.com/web/i/icon.png)\",    \"<hr>\"   ]  },  {   \"cell_type\": \"markdown\",   \"id\": \"5dcfcd64\",   \"metadata\": " +
            "{    \"papermill\": {     \"duration\": null,     \"end_time\": null,     \"exception\": null,     \"start_time\": null,     \"status\": \"completed\"" +
            "    },    \"tags\": []   },   \"source\": [    \"# Custom data history\"   ]  },  {   \"cell_type\": \"code\",   \"execution_count\": null,   \"id\": " +
            "\"a3fb49a4\",   \"metadata\": {    \"papermill\": {     \"duration\": null,     \"end_time\": null,     \"exception\": null,     \"start_time\": null," +
            "     \"status\": \"completed\"    },    \"tags\": [],    \"vscode\": {     \"languageId\": \"csharp\"    }   },   \"outputs\": [],   \"source\": [    " +
            "\"// We need to load assemblies at the start in their own cell\",    \"#load \\\"../Initialize.csx\\\"\"   ]  },  {   \"cell_type\": \"code\",   \"exe" +
            "cution_count\": null,   \"id\": \"65c15cd5\",   \"metadata\": {    \"papermill\": {     \"duration\": null,     \"end_time\": null,     \"exception\":" +
            " null,     \"start_time\": null,     \"status\": \"completed\"    },    \"tags\": [],    \"vscode\": {     \"languageId\": \"csharp\"    }   },   \"ou" +
            "tputs\": [],   \"source\": [    \"// Initialize Lean Engine.\",    \"#load \\\"../QuantConnect.csx\\\"\",    \"\",    \"using System.Globalization;\"," +
            "    \"using QuantConnect;\",    \"using QuantConnect.Data;\",    \"using QuantConnect.Algorithm;\",    \"using QuantConnect.Research;\"   ]  },  {   \"" +
            "cell_type\": \"code\",   \"execution_count\": null,   \"id\": \"b050a297\",   \"metadata\": {    \"papermill\": {     \"duration\": null,     \"end_ti" +
            "me\": null,     \"exception\": null,     \"start_time\": null,     \"status\": \"completed\"    },    \"tags\": [],    \"vscode\": {     \"languageId\"" +
            ": \"csharp\"    }   },   \"outputs\": [],   \"source\": [    \"class CustomDataType : DynamicData\",    \"{\",    \"    public decimal Open;\",    \" " +
            "   public decimal High;\",    \"    public decimal Low;\",    \"    public decimal Close;\",    \"\",    \"    public override SubscriptionDataSource " +
            "GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)\",    \"    {\",    \"        var source = \\\"https://www.dl.dropboxusercont" +
            "ent.com/s/d83xvd7mm9fzpk0/path_to_my_csv_data.csv?dl=0\\\";\",    \"        return new SubscriptionDataSource(source, SubscriptionTransportMedium.Remo" +
            "teFile);\",    \"    }\",    \"\",    \"    public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode" +
            ")\",    \"    {\",    \"        if (string.IsNullOrWhiteSpace(line.Trim()))\",    \"        {\",    \"            return null;\",    \"        }\",   " +
            " \"\",    \"        try\",    \"        {\",    \"            var csv = line.Split(\\\",\\\");\",    \"            var data = new CustomDataType()\", " +
            "   \"            {\",    \"                Symbol = config.Symbol,\",    \"                Time = DateTime.ParseExact(csv[0], DateFormat.DB, CultureIn" +
            "fo.InvariantCulture).AddHours(20),\",    \"                Value = csv[4].ToDecimal(),\",    \"                Open = csv[1].ToDecimal(),\",    \"    " +
            "            High = csv[2].ToDecimal(),\",    \"                Low = csv[3].ToDecimal(),\",    \"                Close = csv[4].ToDecimal()\",    \"  " +
            "          };\",    \"\",    \"            return data;\",    \"        }\",    \"        catch\",    \"        {\",    \"            return null;\",  " +
            "  \"        }\",    \"    }\",    \"}\"   ]  },  {   \"cell_type\": \"code\",   \"execution_count\": null,   \"id\": \"28360142\",   \"metadata\": {  " +
            "  \"papermill\": {     \"duration\": null,     \"end_time\": null,     \"exception\": null,     \"start_time\": null,     \"status\": \"completed\"   " +
            " },    \"tags\": [],    \"vscode\": {     \"languageId\": \"csharp\"    }   },   \"outputs\": [],   \"source\": [    \"var qb = new QuantBook();\",   " +
            " \"var symbol = qb.AddData<CustomDataType>(\\\"CustomDataType\\\", Resolution.Hour).Symbol;\",    \"\",    \"var start = new DateTime(2017, 8, 20);\"," +
            "    \"var end = start.AddHours(48);\",    \"var history = qb.History<CustomDataType>(symbol, start, end, Resolution.Hour);\",    \"\",    \"foreach (v" +
            "ar data in history.Take(5))\",    \"{\",    \"    Console.WriteLine(data.ToString());\",    \"}\"   ]  } ], \"metadata\": {  \"kernelspec\": {   \"dis" +
            "play_name\": \"Foundation-C#-Default\",   \"language\": \"C#\",   \"name\": \"csharp\"  },  \"language_info\": {   \"file_extension\": \".cs\",   \"mi" +
            "metype\": \"text/x-csharp\",   \"name\": \"C#\",   \"pygments_lexer\": \"csharp\",   \"version\": \"10.0\"  },  \"papermill\": {   \"default_parameter" +
            "s\": {},   \"duration\": 0.806508,   \"end_time\": \"2023-02-17T19:22:14.110548\",   \"environment_variables\": {},   \"exception\": null,   \"input_p" +
            "ath\": \"C:\\\\Users\\\\jhona\\\\QuantConnect\\\\Lean\\\\Tests\\\\bin\\\\Debug\\\\Research\\\\RegressionTemplates\\\\BasicTemplateCustomDataTypeHistor" +
            "yResearchCSharp.ipynb\",   \"output_path\": \"C:\\\\Users\\\\jhona\\\\QuantConnect\\\\Lean\\\\Tests\\\\bin\\\\Debug\\\\Research\\\\RegressionTemplates" +
            "\\\\BasicTemplateCustomDataTypeHistoryResearchCSharp-output.ipynb\",   \"parameters\": {},   \"start_time\": \"2023-02-17T19:22:13.304040\",   \"versi" +
            "on\": \"2.4.0\"  } }, \"nbformat\": 4, \"nbformat_minor\": 5}";
    }
}
