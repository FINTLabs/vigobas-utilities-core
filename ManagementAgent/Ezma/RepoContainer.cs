using System;
using System.Collections.Generic;
using System.Linq;

namespace Vigo.Bas.ManagementAgent.Ezma
{
    public class RepoContainer
    {
        public IBaseRepo Repository { get; set; }
        public string ObjectTypeName { get; set; }
        public Type RepoGenericType { get; set; }
        public List<object> ImportedEntities { get; set; }

        public RepoContainer(IBaseRepo repository) 
        {
            var genericType = repository.GetType().GetGenericArguments().FirstOrDefault();

            if (genericType == null)
                genericType = repository.GetType().BaseType.GetGenericArguments().FirstOrDefault();

            ObjectTypeName = genericType.Name;
            RepoGenericType = genericType;
            Repository = repository;
        }

    }
}
