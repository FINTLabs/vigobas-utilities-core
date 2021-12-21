using Microsoft.MetadirectoryServices;

namespace Vigo.Bas.ManagementAgent.Ezma.Config
{
    internal class JsonConfigParameter
    {
        public ConfigParameterType ParameterType { get; set; }
        public string Name { get; set; }
        public object DefaultValue { get; set; }
        public string[] Values { get; set; }
    }
}