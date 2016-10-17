using Windows.Storage;

namespace FBCore.Common.Statfs
{
    /// <summary>
    /// Mock the <see cref="StatFsHelper"/> class for unit test
    /// </summary>
    class MockStatFsHelper : StatFsHelper
    {
        private static MockStatFsHelper _mockStatsFsHelper;

        /// <summary>
        /// Returns MockStatFsHelper singleton
        /// </summary>
        internal static new MockStatFsHelper Instance
        {
            get
            {
                if (_mockStatsFsHelper == null)
                {
                    _mockStatsFsHelper = new MockStatFsHelper();
                }

                return _mockStatsFsHelper;
            }
        }

        internal void SetInternalPath(StorageFolder internalPath)
        {
            _internalPath = internalPath;
        }

        internal void SetExternalPath(StorageFolder externalPath)
        {
            _externalPath = externalPath;
        }

        internal void SetInternalStatFs(ulong? statFs)
        {
            _internalStatFs = statFs;
        }

        internal void SetExternalStatFs(ulong? statFs)
        {
            _externalStatFs = statFs;
        }

        internal override ulong? UpdateStatsHelper(StorageFolder dir)
        {
            if (dir == null || _internalStatFs == null || _externalStatFs == null)
            {
                return 0;
            }

            return dir.Path.Equals(_internalPath.Path) ? _internalStatFs : _externalStatFs;
        }
    }
}
