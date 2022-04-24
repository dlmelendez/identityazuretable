using Azure.Data.Tables;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ElCamino.AspNetCore.Identity.AzureTable.Helpers
{
    public static class EntityMapHelper
    {
        private readonly static ConcurrentDictionary<string, PropertyInfo[]> TypeProperties = new ConcurrentDictionary<string, PropertyInfo[]>();

        private static PropertyInfo[] GetProperties(Type type)
        {
            return TypeProperties.GetOrAdd(type.FullName,
                (name) => type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty)
                .Where(w => w.GetCustomAttribute(typeof(IgnoreDataMemberAttribute)) == null)
                .ToArray());
        }

        public static T MapTableEntity<T>(this TableEntity dte) where T : ITableEntity, new()
        {
            T t = new();
            var properties = GetProperties(typeof(T));
            foreach (var prop in properties)
            {             
                prop.SetValue(t, dte[prop.Name]);
            }
            return t;
        }
    }
}
