using System;

namespace Cache.Common
{
    /// <summary>
    /// A categorizaton of different cache and storage related errors.
    /// </summary>
    public enum CacheErrorCategory
    {
        /// <summary>
        /// READ_DECODE error
        /// </summary>
        READ_DECODE,

        /// <summary>
        /// READ_FILE error
        /// </summary>
        READ_FILE,

        /// <summary>
        /// READ_FILE_NOT_FOUND error
        /// </summary>
        READ_FILE_NOT_FOUND,

        /// <summary>
        /// READ_INVALID_ENTRY error
        /// </summary>
        READ_INVALID_ENTRY,

        /// <summary>
        /// WRITE_ENCODE error
        /// </summary>
        WRITE_ENCODE,

        /// <summary>
        /// WRITE_CREATE_TEMPFILE error
        /// </summary>
        WRITE_CREATE_TEMPFILE,

        /// <summary>
        /// WRITE_UPDATE_FILE_NOT_FOUND error
        /// </summary>
        WRITE_UPDATE_FILE_NOT_FOUND,

        /// <summary>
        /// WRITE_RENAME_FILE_TEMPFILE_NOT_FOUND error
        /// </summary>
        WRITE_RENAME_FILE_TEMPFILE_NOT_FOUND,

        /// <summary>
        /// WRITE_RENAME_FILE_TEMPFILE_PARENT_NOT_FOUND error
        /// </summary>
        WRITE_RENAME_FILE_TEMPFILE_PARENT_NOT_FOUND,

        /// <summary>
        /// WRITE_RENAME_FILE_OTHER error
        /// </summary>
        WRITE_RENAME_FILE_OTHER,

        /// <summary>
        /// WRITE_CREATE_DIR error
        /// </summary>
        WRITE_CREATE_DIR,

        /// <summary>
        /// WRITE_CALLBACK_ERROR error
        /// </summary>
        WRITE_CALLBACK_ERROR,

        /// <summary>
        /// WRITE_INVALID_ENTRY error
        /// </summary>
        WRITE_INVALID_ENTRY,

        /// <summary>
        /// DELETE_FILE error
        /// </summary>
        DELETE_FILE,

        /// <summary>
        /// EVICTION error
        /// </summary>
        EVICTION,

        /// <summary>
        /// GENERIC_IO error
        /// </summary>
        GENERIC_IO,

        /// <summary>
        /// OTHER error
        /// </summary>
        OTHER
    }

    /// <summary>
    /// An interface for logging various cache errors.
    /// </summary>
    public interface ICacheErrorLogger
    {
        /// <summary>
        /// Log an error of the specified category.
        /// <param name="category">Error category</param>
        /// <param name="clazz">Class reporting the error</param>
        /// <param name="message">An optional error message</param>
        /// </summary>
        void LogError(
            CacheErrorCategory category,
            Type clazz,
            string message);
    }
}
