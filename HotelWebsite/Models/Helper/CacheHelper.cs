using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;

namespace MyBlog.Model
{

    /// <summary>
    /// Cache Helper
    /// </summary>
    public static class CacheHelper
    {
        /// <summary>
        /// Adds the specified key.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="cacheTimeSeconds">The cache time seconds.</param>
        public static void Add<T>(string key, T value, double cacheTimeSeconds)
        {
            if (value != null)
            {
                HttpContext.Current.Cache.Add(
                    key,
                    value,
                    null,
                    DateTime.Now.AddSeconds(cacheTimeSeconds),
                    System.Web.Caching.Cache.NoSlidingExpiration,
                    CacheItemPriority.Normal, null);
            }
        }

        /// <summary>
        /// Remove item from cache
        /// </summary>
        /// <param name="key">Name of cached item</param>
        public static void Clear(string key)
        {
            HttpContext.Current.Cache.Remove(key);
        }

        /// <summary>
        /// Check for item in cache
        /// </summary>
        /// <param name="key">Name of cached item</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool Exists(string key)
        {
            return HttpContext.Current.Cache[key] != null;
        }

        /// <summary>
        /// Retrieve cached item
        /// </summary>
        /// <typeparam name="T">Type of cached item</typeparam>
        /// <param name="key">Name of cached item</param>
        /// <param name="value">Cached value. Default(T) if
        /// item doesn't exist.</param>
        /// <returns>Cached item as type</returns>
        public static bool Get<T>(string key, out T value)
        {
            if (Exists(key))
            {
                value = (T)HttpContext.Current.Cache[key];
                return true;
            }

            value = default(T);
            return false;
        }

        public static T Get<T>(string key)
        {
            T value;
            if (Exists(key))
            {
                value = (T)HttpContext.Current.Cache[key];
                return value;
            }

            value = default(T);
            return value;
        }

        /// <summary>
        /// Gets the or add.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key.</param>
        /// <param name="getter">The getter.</param>
        /// <param name="cacheTimeSeconds">The cache time seconds.</param>
        /// <returns>Cached item as type</returns>
        public static async Task<T> GetOrAdd<T>(string key, Func<Task<T>> getter, double cacheTimeSeconds)
        {
            T value;
            if (Exists(key))
            {
                Get<T>(key, out value);
            }
            else
            {
                value = await getter();
                Add(key, value, cacheTimeSeconds);
            }

            return value;
        }

        public static T ClearOrAdd<T>(string key, T value, double cacheTimeSeconds)
        {
            if (Exists(key))
            {
                Clear(key);
                Add(key, value, cacheTimeSeconds);
            }
            else
            {
                Add(key, value, cacheTimeSeconds);
            }

            return value;
        }
    }
}
