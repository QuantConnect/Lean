using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json;

namespace QuantConnect.Configuration
{
    /// <summary>
    /// Command Line application parser
    /// </summary>
    public static class ApplicationParser
    {
        /// <summary>
        /// This function will parse args based on options and will show application name, version, help
        /// </summary>
        /// <param name="applicationName">The application name to be shown</param>
        /// <param name="applicationDescription">The application description to be shown</param>
        /// <param name="applicationHelpText">The application help text</param>
        /// <param name="args">The command line arguments</param>
        /// <param name="options">The applications command line available options</param>
        /// <param name="noArgsShowHelp">To show help when no command line arguments were provided</param>
        /// <returns>The user provided options. Key is option name</returns>
        public static Dictionary<string, object> Parse(string applicationName, string applicationDescription, string applicationHelpText,
                                                       string[] args, List<CommandLineOption> options, bool noArgsShowHelp = false)
        {
            var application = new CommandLineApplication
            {
                Name = applicationName,
                Description = applicationDescription,
                ExtendedHelpText = applicationHelpText
            };

            application.HelpOption("-?|-h|--help");

            // This is a helper/shortcut method to display version info - it is creating a regular Option, with some defaults.
            // The default help text is "Show version Information"
            application.VersionOption("-v|-V|--version",
                () =>
                    $"Version {Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}");

            var optionsObject = new Dictionary<string, object>();

            var listOfOptions = new List<CommandOption>();

            foreach (var option in options)
            {
                listOfOptions.Add(application.Option($"--{option.Name}", option.Description, option.Type));
            }

            application.OnExecute(() =>
            {
                foreach (var commandOption in listOfOptions.Where(option => option.HasValue()))
                {
                    var optionKey = commandOption.Template.Replace("--", "");
                    var matchingOption = options.Find(o => o.Name == optionKey);
                    switch (matchingOption.Type)
                    {
                        // Booleans, string and numbers
                        case CommandOptionType.NoValue:
                        case CommandOptionType.SingleValue:
                            optionsObject[optionKey] = ParseTypedArgument(commandOption.Value());
                            break;

                        // Parsing nested objects
                        case CommandOptionType.MultipleValue:
                            var keyValuePairs = commandOption.Value().Split(',');
                            var subDictionary = new Dictionary<string, object>();
                            foreach (var keyValuePair in keyValuePairs)
                            {
                                var subKeys = keyValuePair.Split(':');
                                subDictionary[subKeys[0]] = ParseTypedArgument(subKeys.Length > 1 ? subKeys[1] : "");
                            }

                            optionsObject[optionKey] = subDictionary;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                return 0;
            });

            application.Execute(args);
            if (noArgsShowHelp && args.Length == 0)
            {
                application.ShowHelp();
            }
            return optionsObject;
        }

        private static object ParseTypedArgument(string value)
        {
            if (value == "true" || value == "false")
            {
                return value == "true";
            }

            double numericValue;
            if (double.TryParse(value, out numericValue))
            {
                return numericValue;
            }

            return value;
        }


        /// <summary>
        /// Parses toolbox configuration file and composes a dictionary from it
        /// taking as keys only those properties that have neither empty nor null values.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Dictionary<string, object> ParseConfigurationFile(string path)
        {
            var optionsObject = new Dictionary<string, object>();
            try
            {
                // Read the text from json and fill that in a specifal object that
                // will hold the values corresponding to json file contents
                var toolBoxOptionsConfig = JsonConvert.DeserializeObject<ToolBoxOptionsConfiguration>(File.ReadAllText(path));

                // iterate over the properties using reflection and check for non empty values to pupulate optionsObject.
                foreach (var prop in toolBoxOptionsConfig.GetType().GetProperties())
                {
                    if (prop.PropertyType == typeof(string))
                    {
                        // most of our values are strings, get value, if null or empty then continue; populate otherwise..
                        var valueAsString = (string) prop.GetValue(toolBoxOptionsConfig);
                        if (string.IsNullOrEmpty(valueAsString))
                            continue;

                        // be cautios of System.InvalidOperationException can happen here if there is no such attribute
                        var key = GetThePropertyNameFromJsonAttribute(prop);
                        optionsObject[key] = valueAsString;
                    }
                    else if(prop.PropertyType == typeof(string[]))
                    {
                        // if we have reached here we are most probably working with 'tickers' array
                        var valueAsStringArray = (string[])prop.GetValue(toolBoxOptionsConfig);

                        // if null or empty then continue
                        if (valueAsStringArray == null || valueAsStringArray.Length == 0)
                            continue;

                        var key = GetThePropertyNameFromJsonAttribute(prop);

                        // I fully copy here a piece of code from above Parse() method - a part that handles the
                        // case CommandOptionType.MultipleValue - for the full compatibility
                        var keyValuePairs = valueAsStringArray;
                        var subDictionary = new Dictionary<string, object>();
                        foreach (var keyValuePair in keyValuePairs)
                        {
                            var subKeys = keyValuePair.Split(':');
                            subDictionary[subKeys[0]] = ParseTypedArgument(subKeys.Length > 1 ? subKeys[1] : "");
                        }

                        optionsObject[key] = subDictionary;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return optionsObject;
        }

        // Reads the property name in JsonProperty attribute
        private static string GetThePropertyNameFromJsonAttribute(PropertyInfo prop)
        {
            return prop.GetCustomAttributes(true)
                .Select(x => x as JsonPropertyAttribute)
                .Where(x => x != null)
                .Select(x => x.PropertyName).First();
        }
    }
}
