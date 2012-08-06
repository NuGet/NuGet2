using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace NuGet.Commands
{
    [Command(typeof(NuGetCommand), "config", "ConfigCommandDesc", MaxArgs = 1,
            UsageSummaryResourceName = "ConfigCommandSummary", UsageExampleResourceName = "ConfigCommandExamples")]
    public class ConfigCommand : Command
    {
        private const string HttpPasswordKey = "http_proxy.password";
        private readonly Dictionary<string, string> _setValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly ISettings _settings;

        [ImportingConstructor]
        public ConfigCommand(ISettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }
            _settings = settings;
        }

        [Option(typeof(NuGetCommand), "ConfigCommandSetDesc")]
        public Dictionary<string, string> Set
        {
            get { return _setValues; }
        }

        public override void ExecuteCommand()
        {
            string getKey = Arguments.FirstOrDefault();
            if (Set.Any())
            {
                foreach (var property in Set)
                {
                    if (String.IsNullOrEmpty(property.Value))
                    {
                        _settings.DeleteConfigValue(property.Key);
                    }
                    else
                    {
                        // Hack: Need a nicer way for the user to say encrypt this.
                        bool encrypt = HttpPasswordKey.Equals(property.Key, StringComparison.OrdinalIgnoreCase);
                        _settings.SetConfigValue(property.Key, property.Value, encrypt);
                    }
                }
            }
            else if (!String.IsNullOrEmpty(getKey))
            {
                string value = _settings.GetConfigValue(getKey);
                if (String.IsNullOrEmpty(value))
                {
                    Console.WriteWarning(NuGetResources.ConfigCommandKeyNotFound, getKey);
                }
                else
                {
                    Console.WriteLine(value);
                }
            }
        }
    }
}
