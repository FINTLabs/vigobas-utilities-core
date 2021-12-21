using System.IO;
using Microsoft.MetadirectoryServices;
using Newtonsoft.Json;

namespace Vigo.Bas.ManagementAgent.Ezma.Config
{
    public class CapabilityLoader
    {
        public static MACapabilities LoadCapabilities(string filename)
        {
            string jsonData = File.ReadAllText(filename);
            return JsonConvert.DeserializeObject<MACapabilities>(jsonData);
        }
    }
}
