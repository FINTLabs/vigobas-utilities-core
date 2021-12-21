using System;
using System.Collections.Generic;

namespace Vigo.Bas.ManagementAgent.Ezma
{
    public interface IBaseRepo : IDisposable
    {
        List<object> GetAllNative();
        void Update(object entity);
        void Add(object entity);
        void Delete(object entity);
    }
}