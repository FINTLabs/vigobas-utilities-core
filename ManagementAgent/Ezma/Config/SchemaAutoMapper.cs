using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using log4net.Core;
using Microsoft.MetadirectoryServices;
using Vigo.Bas.ManagementAgent.Log;

namespace Vigo.Bas.ManagementAgent.Ezma.Config
{
    /// <summary>
    /// Provides auto-mapping attempt for simple object types. Only takes account for properties and not fields, etc.
    /// </summary>
    public static class SchemaAutoMapper
    {
        //TODO: Consider removing this method as it allows for bad decisions
        /// <summary>
        /// This text will be used for attempts to auto map the anchor attribute
        /// </summary>
        private const string TextMappingToAnchorAttribute = "id";

        /// <summary>
        /// Automatically tries to create the schematype based on the properties of the given type.
        /// Since this overload lacks typeName and anchorAttribute it will be inferred (first attribute with name containing  <see cref="TestClass"/> will be chosen as anchor
        /// It is recommended to use overload that lets you explicity overload
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static SchemaType GetSchema(Type type)
        {
            return GetSchema(type, null, type.Name);
        }
        
        public static SchemaType GetSchema(Type type, string anchorAttribute)
        {
            return GetSchema(type, null, type.Name);
        }

        /// <summary>
        /// Automatically tries to create the schematype based on the properties of the given type
        /// </summary>
        /// <param name="type"></param>
        /// <param name="typeName">The name of the object type in the MA</param>
        /// <param name="anchorAttribute">The property of the type that will be set at anchor attribute</param>
        /// <returns></returns>
        public static SchemaType GetSchema(Type type, string anchorAttribute, string typeName)
        {
            PropertyInfo[] properties = type.GetProperties();
            SchemaType schemaType = SchemaType.Create(typeName, false);
            bool isAnchorSet = false;

            foreach (PropertyInfo propertyInfo in properties)
            {
                SchemaAttribute attribute = null;

                if (!isAnchorSet)
                {
                    bool setAsAnchor = PropertyMatchesAnchorCriteria(propertyInfo, anchorAttribute);

                    if (setAsAnchor) { 
                        attribute = GetAnchorAttributeFromProperty(propertyInfo);
                        isAnchorSet = true;
                    }
                }

                if (attribute == null)
                    attribute = GetAttributeFromProperty(propertyInfo);

                if (attribute != null )
                    schemaType.Attributes.Add(attribute);
            }
            
            return schemaType;
        }

        private static bool PropertyMatchesAnchorCriteria(PropertyInfo propertyInfo, string anchorAttributeName)
        {
            if (String.IsNullOrEmpty(anchorAttributeName))
            {
                //searching for id 
                if (propertyInfo.Name.IndexOf(TextMappingToAnchorAttribute, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                return false;
            }

            //TODO: make sure invalid types wont be attempted to be set as anchor
            if (propertyInfo.Name == anchorAttributeName && (propertyInfo.PropertyType == typeof(string) || propertyInfo.PropertyType == typeof(int)))
                return true;
            return false;
        }

        public static SchemaAttribute GetAttributeFromProperty(PropertyInfo propertyInfo)
        {
            DeterminedAttributeType determinedType = GetAttributeType(propertyInfo);

            string propertyName = propertyInfo.Name;
            var attribute = GetSchemaAttributeFromDeterminedType(determinedType, propertyName);
            return attribute;
        }
        
        private static SchemaAttribute GetAnchorAttributeFromProperty(PropertyInfo propertyInfo)
        {

            DeterminedAttributeType determinedType = GetAttributeType(propertyInfo);
            
            if (determinedType.IsMultivalued && determinedType.AttributeType == AttributeType.Boolean)
                throw new NotSupportedException("IEnumerable and bool types cant be set as anchor in schema");

            string propertyName = propertyInfo.Name;
            var attribute = GetAnchorAttributeFromDeterminedType(determinedType, propertyName);
            return attribute;
        }

        private static SchemaAttribute GetSchemaAttributeFromDeterminedType(DeterminedAttributeType determinedType, string name)
        {
            return !determinedType.IsMultivalued 
                ? SchemaAttribute.CreateSingleValuedAttribute(name, determinedType.AttributeType) 
                : SchemaAttribute.CreateMultiValuedAttribute(name, determinedType.AttributeType);
        }

        private static SchemaAttribute GetAnchorAttributeFromDeterminedType(DeterminedAttributeType determinedType, string name)
        {
            if (determinedType.IsMultivalued)
                throw new Exception("Multivalued anchors are not allowed");

            return SchemaAttribute.CreateAnchorAttribute(name, determinedType.AttributeType);
        }

        public static DeterminedAttributeType GetAttributeType(PropertyInfo propInfo)
        {
            var customAttribs =  propInfo.GetCustomAttributes(false);
            var type = propInfo.PropertyType;

            if (customAttribs.Any(attrib => attrib is EzmaReferenceValueAttribute))
            {
                bool multiValued = TypeHelper.IsIEnumerableOrArray(type) && TypeHelper.ArrayTypeIsNativeType(type);
                return new DeterminedAttributeType(multiValued, AttributeType.Reference);
            }

            if (type == typeof(string) || type == typeof(DateTime))
                return new DeterminedAttributeType(false, AttributeType.String);

            if (type == typeof (int))
                return new DeterminedAttributeType(false, AttributeType.Integer);

            if (type == typeof(bool))
                return new DeterminedAttributeType(false, AttributeType.Boolean);

            if (TypeHelper.IsIEnumerableOrArray(type) && TypeHelper.ArrayTypeIsNativeType(type))
            {
                if (typeof(IEnumerable<string>).IsAssignableFrom(type))
                    return new DeterminedAttributeType(true, AttributeType.String);

                if (typeof(IEnumerable<int>).IsAssignableFrom(type))
                    return new DeterminedAttributeType(true, AttributeType.Integer);
            }
            
            //else we go figure its a reference type or a array/list of object of a reference type
            return new DeterminedAttributeType(false, AttributeType.String);
        }
    }
}
