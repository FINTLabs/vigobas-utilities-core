using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.MetadirectoryServices;
using Vigo.Bas.Common;
using Vigo.Bas.ManagementAgent.Ezma.Config;
using Vigo.Bas.ManagementAgent.Log;

namespace Vigo.Bas.ManagementAgent.Ezma
{
    public abstract class BaseAgent :
        IMAExtensible2CallExport,
        IMAExtensible2CallImport,
        IMAExtensible2GetSchema,
        IMAExtensible2GetCapabilities,
        IMAExtensible2GetParameters
    {
        public int ExportDefaultPageSize
        {
            get { return 12; }
        }

        public int ExportMaxPageSize
        {
            get { return 50; }
        }

        public int ImportDefaultPageSize
        {
            get { return 50; }
        }

        public int ImportMaxPageSize
        {
            get { return 50; }
        }

        public MACapabilities Capabilities
        {
            get { return GetMaCapabilities(); }
        }


        private const int MinImportPageSize = 5;

        public virtual int ImportPageSize
        {
            get { return _setImportPageSize.Clamp(MinImportPageSize, ImportMaxPageSize); }
        }
        
        protected List<RepoContainer> _repoContainers;
        private int _entitesImportedCount;
        private int _setImportPageSize;
        private int _currentRepoIndex;
        private Schema _schemaTypes;

        public void OpenExportConnection(KeyedCollection<string, ConfigParameter> configParameters, Schema types,
            OpenExportConnectionRunStep exportRunStep)
        {
            _schemaTypes = types;
            _repoContainers = GetRepositoryContainers(configParameters);

            if (configParameters["Use mockup data"].Value == "1")
            {
                ReplaceReposWithMockups(types);
            }

            Logger.Log.DebugFormat("Starting export");
        }

        private void ReplaceReposWithMockups(Schema schema)
        {
            var mockupContainers = new List<RepoContainer>();

            foreach (var repoContainer in _repoContainers)
            {
                var mockType = typeof (JsonMockupDataRepo<>);
                Type genericType = mockType.MakeGenericType(repoContainer.RepoGenericType);
                
                //TODO: really need to test this
                string anchorAttribute = schema.Types[repoContainer.RepoGenericType.Name]
                    .AnchorAttributes.FirstOrDefault().Name;

                IBaseRepo repo = (IBaseRepo) Activator.CreateInstance(genericType, anchorAttribute);
                
                var mockupRepoContainer = new RepoContainer(repo);
                mockupContainers.Add(mockupRepoContainer);
            }

            _repoContainers = mockupContainers;
        }

        public PutExportEntriesResults PutExportEntries(IList<CSEntryChange> csentries)
        {
            var results = new PutExportEntriesResults();

            foreach (var csentry in csentries)
            {
                try
                {
                    Logger.Log.DebugFormat("Exporting csentry {0} with modificationType {1}", csentry.DN,
                              csentry.ObjectModificationType);
                    //Generic type will always be named the same as the csentry objecttype
                    string typeName = csentry.ObjectType;
                    RepoContainer repoContainer = _repoContainers.FirstOrDefault(container => container.ObjectTypeName == typeName);

                    if (repoContainer == null)
                        throw new Exception("Couldnt find RepoContainer for type " + typeName);

                    object entity = typeof(CsentryConverter)
                        .GetMethod("ConvertFromCsentry")
                        .MakeGenericMethod(new Type[] { repoContainer.RepoGenericType })
                        .Invoke(null, new[] { csentry });
                    
                    if (entity == null)
                    {
                        string errorMsg = String.Format("Was not able to convert CSEntry to {0}",
                            repoContainer.RepoGenericType);

                        throw new Exception(errorMsg);
                    }

                    switch (csentry.ObjectModificationType)
                    {
                        case ObjectModificationType.Add:
                            repoContainer.Repository.Add(entity);
                            break;
                        case ObjectModificationType.Delete:
                            repoContainer.Repository.Delete(entity);
                            break;
                        case ObjectModificationType.Replace:
                        case ObjectModificationType.Update:
                            repoContainer.Repository.Update(entity);
                            break;
                        case ObjectModificationType.Unconfigured:
                            break;
                        case ObjectModificationType.None:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    var changeResult = CSEntryChangeResult.Create(csentry.Identifier, null,
                        MAExportError.ExportErrorCustomContinueRun
                        , "script-error", ex.ToString());
                    results.CSEntryChangeResults.Add(changeResult);
                    Logger.Log.Error(ex);
                }
            }

            return results;
        }

        public void CloseExportConnection(CloseExportConnectionRunStep exportRunStep)
        {
            foreach (var repoContainer in _repoContainers)
            {
                repoContainer.Repository.Dispose();
            }

            Logger.Log.DebugFormat("Export ended");
        }

        public OpenImportConnectionResults OpenImportConnection(
            KeyedCollection<string, ConfigParameter> configParameters, Schema types,
            OpenImportConnectionRunStep importRunStep)
        {
            _schemaTypes = types;
            _repoContainers = GetRepositoryContainers(configParameters);

            Logger.Log.DebugFormat("Starting import");

            if (configParameters["Use mockup data"].Value == "1")
            {
                ReplaceReposWithMockups(types);
            }

            _setImportPageSize = importRunStep.PageSize > 0 ? importRunStep.PageSize : ImportDefaultPageSize;
            Logger.Log.DebugFormat(
                "Trying to set import page size to {0}. Validated by Max and Min sizes, the current import size is {1}",
                _setImportPageSize, ImportPageSize);
            
            var importResults = new OpenImportConnectionResults();

            foreach (RepoContainer repoContainer in _repoContainers)
            {
                try
                {
                    repoContainer.ImportedEntities = ((IList) repoContainer.Repository
                        .GetType()
                        .GetMethod("GetAll")
                        .Invoke(repoContainer.Repository, null))
                        .Cast<object>().ToList();
                }
                catch (Exception ex)
                {
                    Logger.Log.Error(ex);
                    throw;
                }
            }

            return importResults;
        }

        public GetImportEntriesResults GetImportEntries(GetImportEntriesRunStep importRunStep)
        {
            var repo = _repoContainers[_currentRepoIndex]; 
            
            List<object> currentImportBatchEntities = repo.ImportedEntities
                .Skip(_entitesImportedCount)
                .Take(ImportPageSize)
                .ToList();

            _entitesImportedCount += currentImportBatchEntities.Count;

            var csentries = ConvertToCsentries(currentImportBatchEntities);
            var importInfo = new GetImportEntriesResults {MoreToImport = true};

            if (_entitesImportedCount >= repo.ImportedEntities.Count)
                importInfo.MoreToImport = false;

            //switching to next repository
            if (importInfo.MoreToImport == false)
            {
                if (_repoContainers.Count > _currentRepoIndex + 1)
                {
                    _currentRepoIndex++;
                    importInfo.MoreToImport = true;
                    _entitesImportedCount = 0;
                    Logger.Log.Debug("Switching to next repository");
                }
            }

            importInfo.CSEntries = csentries;
            Logger.Log.InfoFormat("Imported {0} objects", csentries.Count);
            return importInfo;
        }

        private List<CSEntryChange> ConvertToCsentries(List<object> currentImportBatchEntities)
        {
            //TODO: throw error or warning?
            if (currentImportBatchEntities.Count == 0)
                return new List<CSEntryChange>();

            var contactCsentries = new List<CSEntryChange>();
            string typeName = currentImportBatchEntities.First().GetType().Name;

            List<string> usedAttributes = _schemaTypes.Types[typeName].Attributes
                .Select(attribute => attribute.Name).ToList();

            if (_schemaTypes.Types[typeName].AnchorAttributes.Any())
            {
                usedAttributes.AddRange(_schemaTypes.Types[typeName].AnchorAttributes.Select(attribute => attribute.Name));
            }

            foreach (object contact in currentImportBatchEntities)
            {
                CSEntryChange csentry = CSEntryChange.Create();
                //TODO: change csentryconverter to automatically map name from type name. Setting of ObjectType can then be removed
                csentry.ObjectType = typeName;
                csentry.ObjectModificationType = ObjectModificationType.Add;

                try
                {
                    //List<AttributeChange> attributes = CsentryConverter.GetCsentryAttributes(contact);
                    List<AttributeChange> attributes = CsentryConverter.GetCsentryAttributes(contact, usedAttributes);

                    attributes.ForEach(atr => csentry.AttributeChanges.Add(atr));
                    contactCsentries.Add(csentry);
                }
                catch (Exception ex)
                {
                    csentry.ErrorCodeImport = MAImportError.ImportErrorCustomContinueRun;
                    csentry.ErrorDetail = ex.ToString();
                    Logger.Log.Error(ex);
                }
            }

            return contactCsentries;
        }

        public virtual CloseImportConnectionResults CloseImportConnection(CloseImportConnectionRunStep importRunStep)
        {
            foreach (var repoContainer in _repoContainers)
            {
                repoContainer.Repository.Dispose();
            }

            Logger.Log.DebugFormat("Import ended");

            return new CloseImportConnectionResults();
        }

        public virtual Schema GetSchema(KeyedCollection<string, ConfigParameter> configParameters)
        {
            //TODO: need to pass configparameters in here
            _repoContainers = GetRepositoryContainers(configParameters);
            Schema schema = Schema.Create();

            foreach (var repoContainer in _repoContainers)
            {
                SchemaType schemaType = SchemaAutoMapper.GetSchema(repoContainer.RepoGenericType);
                schema.Types.Add(schemaType);
            }

            return schema;
        }

        public IList<ConfigParameterDefinition> GetConfigParameters(
            KeyedCollection<string, ConfigParameter> configParameters, ConfigParameterPage page)
        {
            var configParametersDefinitions = new List<ConfigParameterDefinition>();

            if (page == ConfigParameterPage.Global)
                configParametersDefinitions.Add(ConfigParameterDefinition.CreateCheckBoxParameter("Use mockup data"));

            IList<ConfigParameterDefinition> customParameters = GetEzmaConfigParameters(configParameters, page);

            if (customParameters != null)
                configParametersDefinitions.AddRange(customParameters);

            return configParametersDefinitions;
        }

        public abstract IList<ConfigParameterDefinition> GetEzmaConfigParameters(
            KeyedCollection<string, ConfigParameter> configParameters, ConfigParameterPage page);

        public virtual ParameterValidationResult ValidateConfigParameters(
            KeyedCollection<string, ConfigParameter> configParameters, ConfigParameterPage page)
        {
            return new ParameterValidationResult();
        }
        
        //private MACapabilities GetMaCapabilities()
        //{
        //    return CapabilityLoader.LoadCapabilities(Path.Combine(MaConfigLocation, MaCapabilitiesFilename));
        //}

        protected abstract List<RepoContainer> GetRepositoryContainers(KeyedCollection<string, ConfigParameter> configParameters);

        protected virtual MACapabilities GetMaCapabilities()
        {
            return new MACapabilities()
            {
                ConcurrentOperation = true,
                DeleteAddAsReplace = false,
                DeltaImport = false,
                DistinguishedNameStyle = MADistinguishedNameStyle.None,
                ExportType = MAExportType.ObjectReplace,
                NoReferenceValuesInFirstExport = true,
                ObjectRename = false
            };
        }
    }
}
