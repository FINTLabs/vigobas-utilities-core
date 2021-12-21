using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.MetadirectoryServices;
using Newtonsoft.Json;

namespace Vigo.Bas.ManagementAgent.Ezma.Config
{
    public static class ConfigParameterReader
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="jsonFile">Full or relative path to the json file</param>
        /// <param name="page"></param>
        /// <returns></returns>
        public static List<ConfigParameterDefinition> GetConfigParameters(string jsonFile, ConfigParameterPage page)
        {
            List<ConfigParameterDefinition> configParametersDefinitions = new List<ConfigParameterDefinition>();

            List<JsonConfigParameter> jsonConfigParameters = GetJsonConfiguration(jsonFile, page);

            foreach (var jsonConfigParameter in jsonConfigParameters)
            {
                ConfigParameterDefinition configParameter = GetConfigParameter(jsonConfigParameter);
                configParametersDefinitions.Add(configParameter);
            }
            
            return configParametersDefinitions;
        }

        private static ConfigParameterDefinition GetConfigParameter(JsonConfigParameter jsonParameter)
        {
            switch (jsonParameter.ParameterType)
            {
                case ConfigParameterType.String:
                    return ConfigParameterDefinition.CreateStringParameter(jsonParameter.Name, "", (string) jsonParameter.DefaultValue);
                case ConfigParameterType.Divider:
                    return ConfigParameterDefinition.CreateDividerParameter();
                case ConfigParameterType.EncryptedString:
                    return ConfigParameterDefinition.CreateEncryptedStringParameter(jsonParameter.Name, "", (string)jsonParameter.DefaultValue);
                case ConfigParameterType.CheckBox:
                    return ConfigParameterDefinition.CreateCheckBoxParameter(jsonParameter.Name, (bool) jsonParameter.DefaultValue);
                case ConfigParameterType.DropDown:
                    //TODO: determine what to do with extensible parameter
                    return ConfigParameterDefinition.CreateDropDownParameter(jsonParameter.Name, jsonParameter.Values, true);
                default:
                    throw new NotImplementedException("Does not support " + jsonParameter.ParameterType);
            }
        }

        private static List<JsonConfigParameter> GetJsonConfiguration(string file, ConfigParameterPage page)
        {
            string jsonText = File.ReadAllText(file);
            List<JsonParameterCollection> collections = JsonConvert.DeserializeObject<List<JsonParameterCollection>>(jsonText);

            if (collections.All(collection => collection.Page != page))
                return new List<JsonConfigParameter>();

            JsonParameterCollection pageConfigCollection = collections.FirstOrDefault(collection => collection.Page == page);
            List<JsonConfigParameter> parameters = pageConfigCollection.Parameters;

            foreach (var jsonConfigParameter in parameters)
            {
                if (jsonConfigParameter.ParameterType == ConfigParameterType.CheckBox)
                    jsonConfigParameter.DefaultValue = jsonConfigParameter.DefaultValue == null ? false : jsonConfigParameter.DefaultValue;
            }

            return parameters;
        }
    }
}
