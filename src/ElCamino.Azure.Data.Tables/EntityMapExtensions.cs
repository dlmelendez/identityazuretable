// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Serialization;

namespace Azure.Data.Tables
{
    /// <summary>
    /// Extensions for mapping <see cref="TableEntity"/> to a class that implements <see cref="ITableEntity"/>
    /// </summary>
    public static class EntityMapExtensions
    {
        private static readonly ConcurrentDictionary<string, PropertyInfo[]> TypeProperties = new();

        /// <summary>
        /// Threadsafe caching PropertyInfo[] because the check for the IgnoreDataMemberAttribute slows property lookup
        /// </summary>
        /// <param name="type">Type that implements ITableEntity and new() </param>
        /// <returns><seealso cref="PropertyInfo"/> array</returns>
        private static PropertyInfo[] GetProperties(Type type)
        {
            if (type.FullName is not null)
            {
                return TypeProperties.GetOrAdd(type.FullName,
                    (name) => type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty)
                    .Where(w => w.GetCustomAttribute(typeof(IgnoreDataMemberAttribute)) == null)
                    .ToArray());
            }
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty)
                    .Where(w => w.GetCustomAttribute(typeof(IgnoreDataMemberAttribute)) == null)
                    .ToArray();
        }

        /// <summary>
        /// Maps <see cref="TableEntity"/> to a class that implements <see cref="ITableEntity"/>
        /// </summary>
        /// <typeparam name="T"><see cref="ITableEntity"/> class, new()</typeparam>
        /// <param name="dte"></param>
        /// <returns>Returns T <see cref="ITableEntity"/> class, new()</returns>
        public static T MapTableEntity<T>(this TableEntity dte) where T : ITableEntity, new()
        {
            T t = new();
            foreach (var prop in GetProperties(typeof(T)))
            {
                if (dte.TryGetValue(prop.Name, out object obj))
                {
                    prop.SetValue(t, obj);
                }
            }

            return t;
        }
    }
}
