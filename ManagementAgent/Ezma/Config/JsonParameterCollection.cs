using System.Collections.Generic;
using Microsoft.MetadirectoryServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Vigo.Bas.ManagementAgent.Ezma.Config
{
    internal class JsonParameterCollection
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ConfigParameterPage Page { get; set; }
        public List<JsonConfigParameter> Parameters { get; set; }
    }
}