using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;

namespace Vigo.Bas.Common
{
    public class WebserviceUtil
    {
        public static Binding ResolveBinding(string name, string filePath)
        {
            BindingsSection section = GetBindingsSection(filePath);

            foreach (var bindingCollection in section.BindingCollections)
            {
                if (bindingCollection.ConfiguredBindings.Count > 0)
                {
                    foreach (IBindingConfigurationElement configuredBinding in bindingCollection.ConfiguredBindings)
                    {
                        if (configuredBinding.Name == name)
                        {
                            var bindingElement = configuredBinding;
                            var binding = (Binding)Activator.CreateInstance(bindingCollection.BindingType);
                            binding.Name = bindingElement.Name;
                            bindingElement.ApplyConfiguration(binding);

                            return binding;
                        }
                    }
                }
            }

            return null;
        }

        private static ChannelEndpointElement GetEndpoint(string filePath, string name)
        {
            System.Configuration.Configuration config =
             System.Configuration.ConfigurationManager.OpenMappedExeConfiguration(
                 new System.Configuration.ExeConfigurationFileMap() { ExeConfigFilename = filePath },
                   System.Configuration.ConfigurationUserLevel.None);

            var serviceModel = ServiceModelSectionGroup.GetSectionGroup(config);

            foreach (ChannelEndpointElement endpoint in serviceModel.Client.Endpoints)
            {
                if (endpoint.Name == name)
                {
                    return endpoint;
                }
            }

            return null;
        }

        public static EndpointAddress GetEndpointAddress(string endpointName, string filePath)
        {
            var endpoint = GetEndpoint(filePath, endpointName);

            if (endpoint != null)
                return new EndpointAddress(endpoint.Address);
            else
                return null;
        }

        public static List<IEndpointBehavior> ResolveEndpointBehavior(string name, string filePath)
        {
            BehaviorsSection section = GetBehaviorsSection(filePath);
            List<IEndpointBehavior> endpointBehaviors = new List<IEndpointBehavior>();

            if (section.EndpointBehaviors.Count > 0
          && section.EndpointBehaviors[0].Name == name)
            {
                var behaviorCollectionElement = section.EndpointBehaviors[0];

                foreach (BehaviorExtensionElement behaviorExtension in behaviorCollectionElement)
                {
                    object extension = behaviorExtension.GetType().InvokeMember("CreateBehavior",
                          BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance,
                          null, behaviorExtension, null);

                    endpointBehaviors.Add((IEndpointBehavior)extension);
                }

                return endpointBehaviors;
            }

            return null;
        }

        private static BindingsSection GetBindingsSection(string filePath)
        {
            System.Configuration.Configuration config =
          System.Configuration.ConfigurationManager.OpenMappedExeConfiguration(
              new System.Configuration.ExeConfigurationFileMap() { ExeConfigFilename = filePath },
                System.Configuration.ConfigurationUserLevel.None);

            var serviceModel = ServiceModelSectionGroup.GetSectionGroup(config);
            return serviceModel.Bindings;
        }

        private static BehaviorsSection GetBehaviorsSection(string path)
        {
            System.Configuration.Configuration config =
          System.Configuration.ConfigurationManager.OpenMappedExeConfiguration(
                 new System.Configuration.ExeConfigurationFileMap() { ExeConfigFilename = path },
                    System.Configuration.ConfigurationUserLevel.None);

            var serviceModel = ServiceModelSectionGroup.GetSectionGroup(config);
            return serviceModel.Behaviors;
        }
    }
}
