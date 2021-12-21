using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Vigo.Bas.ManagementAgent.Ezma.Config
{
    public static class TypeHelper
    {
            private static List<Type> _nonPrimitiveNativeTypes;
            public static List<Type> NonPrimitiveNativeTypes
            {
                get
                {
                    if (_nonPrimitiveNativeTypes == null)
                    {
                        //_systemTypes = Assembly.GetExecutingAssembly().GetType().Module.Assembly.GetExportedTypes().ToList();
                        _nonPrimitiveNativeTypes = new List<Type>()
                        {
                            typeof(string), typeof(decimal)
                        };
                    }
                    return _nonPrimitiveNativeTypes;
                }
            }


        public static bool IsNativeType(Type type)
        {
            return  type.IsPrimitive ||
                    NonPrimitiveNativeTypes.Any(sysType => sysType == type);
        }

        public static bool IsIEnumerableOrArray(Type type)
        {
            return typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string);
        }

        public static bool ArrayTypeIsNativeType(Type arrayType)
        {
            //TODO: this doesnt get membertype of lsits
            Type memberType = arrayType.GetElementType();
            if (memberType == null)
                memberType = arrayType.GetGenericArguments()[0];

            return IsNativeType(memberType);
        }

    }
}
