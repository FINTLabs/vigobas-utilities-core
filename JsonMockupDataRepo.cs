using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.MetadirectoryServices;
using Newtonsoft.Json;
using Vigo.Bas.ManagementAgent.Ezma;
using Vigo.Bas.ManagementAgent.Log;

namespace Vigo.Bas
{
    public class JsonMockupDataRepo<T> : BaseRepo<T>
    {
        private readonly string _anchorAttribute;
        private const string EmptyJsonArray = "[]";
        private readonly string _fileName;
        private string _folderPath;
        private string FullFilePath { get { return Path.Combine(_folderPath, _fileName); } }
        private Encoding _encodingUsed = Encoding.Default;
        private List<T> _allEntities;

        /// <summary>
        /// Uses the MAData/MA directory as base folder
        /// </summary>
        /// <param name="anchorAttribute"></param>
        public JsonMockupDataRepo(string anchorAttribute)
        {
            _folderPath = MAUtils.MAFolder;
            _anchorAttribute = anchorAttribute;
            _fileName = typeof (T).Name + ".json";
            SetupDataFile();
        }

        public JsonMockupDataRepo(string anchorAttribute, string folderPath)
        {
            if (folderPath == null)
                folderPath = "";

            _anchorAttribute = anchorAttribute;
            _folderPath = folderPath;
            _fileName = typeof(T).Name + ".json";
            SetupDataFile();
        }

        private void SetupDataFile()
        {
            if (!File.Exists(FullFilePath))
            {
                File.Create(FullFilePath).Close();
                //initializing with empty array
                File.WriteAllText(FullFilePath, EmptyJsonArray);
            }

            string objectsJson = File.ReadAllText(FullFilePath, _encodingUsed);
            _allEntities = JsonConvert.DeserializeObject<List<T>>(objectsJson);
            Logger.Log.ErrorFormat("Loaded a total of {0} json entites into repository", _allEntities.Count);
        }

        public override List<T> GetAll()
        {
            return _allEntities;
        }

        public override void Update(T entity)
        {
            T toUpdate = default(T);

            PropertyInfo anchorInfo = typeof (T).GetProperty(_anchorAttribute);
            string updatingEntityId = (string) anchorInfo.GetValue(entity);

            foreach (var existingEntity in _allEntities)
            {
                //TODO: assumes anchor attribute is a string valued proeprty - is this ok?
                string entityId = (string) anchorInfo.GetValue(existingEntity);

                if (entityId == updatingEntityId)
                    toUpdate = existingEntity;
            }
            
            if (toUpdate != null)
            {
                _allEntities.Remove(toUpdate);
                _allEntities.Add(entity);
            }
        }

        public override void Add(T entity)
        {
            //TODO: dont allow duplicates
            _allEntities.Add(entity);
        }

        public override void Delete(T entity)
        {
            //T toDelete = _allEntities.FirstOrDefault(contact => contact.ExternalId == entity.ExternalId);
            T toDelete = default(T);

            PropertyInfo anchorInfo = typeof(T).GetProperty(_anchorAttribute);
            string deletingEntityId = (string)anchorInfo.GetValue(entity);

            foreach (var existingEntity in _allEntities)
            {
                //TODO: assumes anchor attribute is a string valued proeprty - is this ok?
                string entityId = (string)anchorInfo.GetValue(existingEntity);

                if (entityId == deletingEntityId)
                    toDelete = existingEntity;
            }

            if (toDelete != null)
            {
                Logger.Log.DebugFormat("Entity count before deletion: {0}", _allEntities.Count);
                bool success = _allEntities.Remove(toDelete);
                if (!success)
                    Logger.Log.ErrorFormat("Failed removing/deleting entity with ExternalId {0}", deletingEntityId);

                Logger.Log.DebugFormat("Entity count after deletion: {0}", _allEntities.Count);
            }
            else 
                Logger.Log.ErrorFormat("Couldnt find entity with ExternalId {0}", deletingEntityId);
        }

        public override void Dispose()
        {
            string entitiesJson = JsonConvert.SerializeObject(_allEntities, Formatting.Indented, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            File.WriteAllText(FullFilePath, entitiesJson, _encodingUsed);
            Logger.Log.DebugFormat("Saved {0} entities to {1}", _allEntities.Count, FullFilePath);
        }
    }
}