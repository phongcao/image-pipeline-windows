using System;

namespace FBCore.Common.Util
{
    /// <summary>
    /// Schemes for URIs.
    /// </summary>
    public class UriUtil
    {
        /// <summary>
        /// http scheme for URIs.
        /// </summary>
        public const string HTTP_SCHEME = "http";

        /// <summary>
        /// https scheme for URIs.
        /// </summary>
        public const string HTTPS_SCHEME = "https";

        /// <summary>
        /// App package scheme for URIs.
        /// </summary>
        public const string APP_PACKAGE_SCHEME = "ms-appx";

        /// <summary>
        /// App package web scheme for URIs.
        /// </summary>
        public const string APP_PACKAGE_WEB_SCHEME = "ms-appx-web";

        /// <summary>
        /// App data scheme for URIs
        /// </summary>
        public static string APP_DATA_SCHEME = "ms-appdata";

        /// <summary>
        /// Resource scheme for URIs.
        /// </summary>
        public const string APP_RESOURCE_SCHEME = "ms-resource";

        /// <summary>
        /// Data scheme for URIs.
        /// </summary>
        public const string DATA_SCHEME = "data";

        /// <summary>
        /// File scheme for URIs.
        /// </summary>
        public const string FILE_SCHEME = "file";

        /// <summary>
        /// FutureAccessList scheme for URIs.
        /// </summary>
        public const string FUTURE_ACCESS_LIST_SCHEME = "urn:future-access-list:";

        /// <summary>
        /// Check if uri represents network resource.
        ///
        /// <param name="uri">uri to check.</param>
        /// <returns>true if uri's scheme is equal to "http" or "https".</returns>
        /// </summary>
        public static bool IsNetworkUri(Uri uri)
        {
            string scheme = GetSchemeOrNull(uri);
            return HTTPS_SCHEME.Equals(scheme) || HTTP_SCHEME.Equals(scheme);
        }

        /// <summary>
        /// Check if uri represents app package.
        /// </summary>
        /// <param name="uri">uri to check.</param>
        /// <returns>true if uri's scheme is equal to "file".</returns>
        public static bool IsAppPackageUri(Uri uri)
        {
            string scheme = GetSchemeOrNull(uri);
            return (APP_PACKAGE_SCHEME.Equals(scheme) || APP_PACKAGE_WEB_SCHEME.Equals(scheme));
        }

        /// <summary>
        /// Check if uri represents app data.
        /// </summary>
        /// <param name="uri">uri to check.</param>
        /// <returns>true if uri's scheme is equal to "content".</returns>
        public static bool IsAppDataUri(Uri uri)
        {
            string scheme = GetSchemeOrNull(uri);
            return APP_DATA_SCHEME.Equals(scheme);
        }

        /// <summary>
        /// Check if uri represents app resource.
        /// </summary>
        /// <param name="uri">uri to check.</param>
        /// <returns>true if uri's scheme is equal to "asset".</returns>
        public static bool IsAppResourceUri(Uri uri)
        {
            string scheme = GetSchemeOrNull(uri);
            return APP_RESOURCE_SCHEME.Equals(scheme);
        }

        /// <summary>
        /// Check if the uri is a data uri.
        /// </summary>
        /// <param name="uri">uri to check.</param>
        public static bool IsDataUri(Uri uri)
        {
            string scheme = GetSchemeOrNull(uri);
            return DATA_SCHEME.Equals(scheme);
        }

        /// <summary>
        /// Check if the uri is a file uri.
        /// </summary>
        /// <param name="uri">uri to check.</param>
        public static bool IsFileUri(Uri uri)
        {
            string scheme = GetSchemeOrNull(uri);
            return FILE_SCHEME.Equals(scheme);
        }

        /// <summary>
        /// Check if the uri is a FutureAccessList uri.
        /// </summary>
        /// <param name="uri">uri to check.</param>
        public static bool IsFutureAccessListUri(Uri uri)
        {
            return (uri == null) ? false : uri.OriginalString.StartsWith(
                FUTURE_ACCESS_LIST_SCHEME);
        }

        /// <summary>
        /// Gets uri scheme.
        /// </summary>
        /// <param name="uri">uri to extract scheme from, possibly null.</param>
        /// <returns>null if uri is null, result of uri.Scheme otherwise.</returns>
        public static string GetSchemeOrNull(Uri uri)
        {
            return uri == null ? null : uri.Scheme;
        }

        /// <summary>
        /// A wrapper around Uri.TryCreate that returns null if the input is null.
        /// </summary>
        /// <param name="uriAsString">The uri as a string.</param>
        /// <returns>The parsed Uri or null if the input was null.</returns>
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
