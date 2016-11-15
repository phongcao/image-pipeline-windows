using System;

namespace FBCore.Common.Util
{
    /// <summary>
    /// Schemes for URIs
    /// </summary>
    public class UriUtil
    {
        /// <summary>
        /// http scheme for URIs
        /// </summary>
        public const string HTTP_SCHEME = "http";

        /// <summary>
        /// https scheme for URIs
        /// </summary>
        public const string HTTPS_SCHEME = "https";

        /// <summary>
        /// App package scheme for URIs
        /// </summary>
        public const string APP_PACKAGE_SCHEME = "ms-appx";

        /// <summary>
        /// App package web scheme for URIs
        /// </summary>
        public const string APP_PACKAGE_WEB_SCHEME = "ms-appx-web";

        /// <summary>
        /// App data scheme for URIs
        /// </summary>
        public static string APP_DATA_SCHEME = "ms-appdata";

        /// <summary>
        /// Resource scheme for URIs
        /// </summary>
        public const string APP_RESOURCE_SCHEME = "ms-resource";

        ///  Data scheme for URIs 
        public const string DATA_SCHEME = "data";

        /// <summary>
        /// Check if uri represents network resource
        ///
        /// <param name="uri">uri to check</param>
        /// @return true if uri's scheme is equal to "http" or "https"
        /// </summary>
        public static bool IsNetworkUri(Uri uri)
        {
            string scheme = GetSchemeOrNull(uri);
            return HTTPS_SCHEME.Equals(scheme) || HTTP_SCHEME.Equals(scheme);
        }

        /// <summary>
        /// Check if uri represents app package
        ///
        /// <param name="uri">uri to check</param>
        /// @return true if uri's scheme is equal to "file"
        /// </summary>
        public static bool IsAppPackageUri(Uri uri)
        {
            string scheme = GetSchemeOrNull(uri);
            return (APP_PACKAGE_SCHEME.Equals(scheme) || APP_PACKAGE_WEB_SCHEME.Equals(scheme));
        }

        /// <summary>
        /// Check if uri represents app data
        ///
        /// <param name="uri">uri to check</param>
        /// @return true if uri's scheme is equal to "content"
        /// </summary>
        public static bool IsAppDataUri(Uri uri)
        {
            string scheme = GetSchemeOrNull(uri);
            return APP_DATA_SCHEME.Equals(scheme);
        }

        /// <summary>
        /// Check if uri represents app resource
        ///
        /// <param name="uri">uri to check</param>
        /// @return true if uri's scheme is equal to "asset"
        /// </summary>
        public static bool IsAppResourceUri(Uri uri)
        {
            string scheme = GetSchemeOrNull(uri);
            return APP_RESOURCE_SCHEME.Equals(scheme);
        }

        /// <summary>
        /// Check if the uri is a data uri
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static bool IsDataUri(Uri uri)
        {
            return DATA_SCHEME.Equals(GetSchemeOrNull(uri));
        }

        /// <summary>
        /// <param name="uri">uri to extract scheme from, possibly null</param>
        /// @return null if uri is null, result of uri.Scheme otherwise
        /// </summary>
        public static string GetSchemeOrNull(Uri uri)
        {
            return uri == null ? null : uri.Scheme;
        }

        /// <summary>
        /// A wrapper around Uri.TryCreate that returns null if the input is null.
        ///
        /// <param name="uriAsString">the uri as a string</param>
        /// @return the parsed Uri or null if the input was null
        /// </summary>
        public static Uri ParseUriOrNull(string uriAsString)
        {
            Uri uri = null;
            if (uriAsString != null)
            {
                Uri.TryCreate(uriAsString, UriKind.RelativeOrAbsolute, out uri);
            }

            return uri;
        }
    }
}
