using FBCore.Common.Util;
using System.Collections.Generic;
using System.Text;

namespace Cache.Common
{
    /// <summary>
    /// Cache key util
    /// </summary>
    public sealed class CacheKeyUtil
    {
        /// <summary>
        /// Get a list of possible resourceIds from MultiCacheKey or get single resourceId from CacheKey.
        /// </summary>
        public static IList<string> GetResourceIds(ICacheKey key)
        {
            try
            {
                IList<string> ids;
                if (key.GetType() == typeof(MultiCacheKey))
                {
                    IList<ICacheKey> keys = ((MultiCacheKey)key).CacheKeys;
                    ids = new List<string>(keys.Count);
                    foreach (var entry in keys)
                    {
                        ids.Add(SecureHashKey(entry));
                    }
                }
                else
                {
                    ids = new List<string>(1);
                    ids.Add(SecureHashKey(key));
                }

                return ids;
            }
            catch (EncoderFallbackException)
            {
                // This should never happen. All VMs support UTF-8
                throw;
            }
        }

        /// <summary>
        /// Get the resourceId from the first key in MultiCacheKey or get single resourceId from CacheKey.
        /// </summary>
        public static string GetFirstResourceId(ICacheKey key)
        {
            try
            {
                if (key.GetType() == typeof(MultiCacheKey))
                {
                    IList<ICacheKey> keys = ((MultiCacheKey)key).CacheKeys;
                    return SecureHashKey(keys[0]);
                }
                else
                {
                    return SecureHashKey(key);
                }
            }
            catch (EncoderFallbackException)
            {
                // This should never happen. All VMs support UTF-8
                throw;
            }
        }

        private static string SecureHashKey(ICacheKey key)
        {
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(key.ToString());
            return SecureHashUtil.MakeSHA1HashBase64(utf8Bytes);
        }
    }
}
