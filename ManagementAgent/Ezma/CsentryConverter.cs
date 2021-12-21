using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.MetadirectoryServices;
using Newtonsoft.Json;
using Vigo.Bas.ManagementAgent.Ezma.Config;
using Vigo.Bas.ManagementAgent.Log;

namespace Vigo.Bas.ManagementAgent.Ezma
{
    public static class CsentryConverter
    {
        public static CSEntryChange GetCsentry(object entity)
        {
            //TODO: need to mock csentrychange
            CSEntryChange csentry = CSEntryChange.Create();
            csentry.ObjectModificationType = ObjectModificationType.Add;
            csentry.ObjectType = entity.GetType().Name;

            var attributes = GetCsentryAttributes(entity);
            attributes.ForEach(change => csentry.AttributeChanges.Add(change));

            return csentry;
        }

        /// <summary>
        /// Will try to map all the properties of the given entity to an AttributeChange
        /// </summary>
        /// <param name="entity">Any kinds of object</param>
        /// <returns></returns>
        [Obsolete("This method can be dangerous if not all the attributes of the entity type is used in the schema")]
        public static List<AttributeChange> GetCsentryAttributes(object entity)
        {
            PropertyInfo[] properties = entity.GetType().GetProperties();
            List<string> usedAttributes = properties.Select(info => info.Name).ToList();
            
            return GetCsentryAttributes(entity, usedAttributes);
        }

        internal static List<AttributeChange> GetCsentryAttributes(object entity, List<string> usedAttributes)
        {
            var attributes = new List<AttributeChange>();
            PropertyInfo[] properties = entity.GetType().GetProperties().Where(info => usedAttributes.Contains(info.Name))
                .ToArray();

            foreach (PropertyInfo propertyInfo in properties)
            {
                var type = propertyInfo.PropertyType;
                string propertyName = propertyInfo.Name;
                object value = propertyInfo.GetValue(entity, null);

                if (value == null)
                    continue;

                //if (propertyInfo.GetCustomAttributes(false).Any(o => o is EzmaReferenceValueAttribute))
                //{
                //    //TODO: do something about value attributes...
                //    attributes.Add
                //}
                if (TypeHelper.IsNativeType(type))
                    attributes.Add(AttributeChange.CreateAttributeAdd(propertyName, value));
                else if (TypeHelper.IsIEnumerableOrArray(type) && TypeHelper.ArrayTypeIsNativeType(type))
                {
                    IList<object> listValue = new List<object>();

                    foreach (object obj in ((IEnumerable)(value)))
                    {
                        listValue.Add(obj);
                    }

                    attributes.Add(AttributeChange.CreateAttributeAdd(propertyName, listValue));
                }
                else if (type == typeof (DateTime))
                {
                    attributes.Add(AttributeChange.CreateAttributeAdd(propertyName, value.ToString()));
                }
                else
                {
                    string jsonValue = JsonConvert.SerializeObject(value,
                            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.None });
                    attributes.Add(AttributeChange.CreateAttributeAdd(propertyName, jsonValue));
                }
            }

            return attributes;
        }

        public static AttributeChange GetCsentryAttribute(object value, string propertyName)
        {
            var type = value.GetType();


            if (TypeHelper.IsNativeType(type))
                return AttributeChange.CreateAttributeAdd(propertyName, value);
            else if (TypeHelper.IsIEnumerableOrArray(type))
            {
                //TODO: multivalue attributes - ... or does this actually handle it?
                return AttributeChange.CreateAttributeAdd(propertyName, value);
            }
            else if (type == typeof(DateTime))
            {
                return AttributeChange.CreateAttributeAdd(propertyName, value.ToString());
            }
            else
            {
                string jsonValue = JsonConvert.SerializeObject(value);
                return AttributeChange.CreateAttributeAdd(propertyName, jsonValue);
            }
        }

        //TODO: constrain T to empty constructor classes
        public static T ConvertFromCsentry<T>(CSEntryChange csentry)
        {
            T obj = (T)Activator.CreateInstance(typeof(T));
            Type entityType = typeof(T);


            if (csentry.DN != null)
            {
                var anchor = csentry.AnchorAttributes.FirstOrDefault();

                var property = entityType.GetProperty(anchor.Name);

                if (property != null)
                    property.SetValue(obj, anchor.Value);
            }

            //TODO: refactor this
            foreach (var attributeChange in csentry.AttributeChanges)
            {
                string name = attributeChange.Name;

                object value = attributeChange.ValueChanges[0].Value;
                var entityProperty = entityType.GetProperty(name);
                Type propertyType = entityProperty.PropertyType;
                
                //tries to catch any reference types that are not arrays or standard (string, decimal) and assumes they are a json string that can be deserialized
                //TODO: this might be slow if it fails
                if (!TypeHelper.IsIEnumerableOrArray(propertyType) &&
                    !TypeHelper.IsNativeType(propertyType)
                    && propertyType != typeof(DateTime)
                    )
                { 
                    value = JsonConvert.DeserializeObject(value.ToString(), propertyType, 
                            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.None });
                }

                if (TypeHelper.IsIEnumerableOrArray(propertyType))
                {
                    var changes = attributeChange.ValueChanges.Select(change => change.Value).ToArray();
                    
                    var genericList = typeof (List<>);
                    var constructedListType = genericList.MakeGenericType(propertyType.GetElementType());
                    var list = Activator.CreateInstance(constructedListType);

                    foreach (object change in changes)
                    {
                        dynamic newVal = Convert.ChangeType(change, typeof(string));
                        constructedListType.GetMethod("Add").Invoke(list, new object[] { newVal });
                    }

                    if (propertyType.IsArray)
                    {
                        var array = list.GetType().GetMethod("ToArray").Invoke(list, null);
                        entityProperty.SetValue(obj, array);
                    }
                    else
                    {
                        entityProperty.SetValue(obj, list);
                    }
                    
                    continue;
                }

                if (entityProperty == typeof(DateTime))
                {
                    DateTime dt = DateTime.Parse(value.ToString());
                    entityProperty.SetValue(obj, dt);
                }
                else
                    entityProperty.SetValue(obj, value);
            }

            return obj;
        }
    }
}
