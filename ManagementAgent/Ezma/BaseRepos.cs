using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Vigo.Bas.ManagementAgent.Ezma
{
    public abstract class BaseRepo<T> : IBaseRepo
    {
        public abstract void Dispose();

        public abstract void Update(T entity);

        public abstract void Add(T entity);

        public abstract void Delete(T entity);

        public abstract List<T> GetAll();

        public List<object> GetAllNative()
        {
            return (List<object>)
                GetMethod("GetAll", true)
                .MakeGenericMethod(new[] { typeof(T) })
                .Invoke(this, null);
        }

        public void Update(object entity)
        {
            GetMethod("Update", true)
                .Invoke(this, new[] { entity });
        }

        public void Add(object entity)
        {
            GetMethod("Add", true)
                .Invoke(this, new[] { entity });
        }

        public void Delete(object entity)
        {
            GetMethod("Delete", true)
                .Invoke(this, new[] { entity });
        }

        public MethodInfo GetMethod(string name, bool generic)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }
            return GetType().GetMethods()
                .FirstOrDefault(method => method.Name == name & method.DeclaringType == GetType());
        }
    }
}
