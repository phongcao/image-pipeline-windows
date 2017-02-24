namespace ImagePipeline.Memory
{
    /// <summary>
    /// Empty implementation of PoolStatsTracker that does not
    /// perform any tracking.
    /// </summary>
    public class NoOpPoolStatsTracker : PoolStatsTracker
    {
        private static readonly object _instanceGate = new object();

        private static NoOpPoolStatsTracker _instance = null;

        private NoOpPoolStatsTracker()
        {
        }

        /// <summary>
        /// Singleton.
        /// </summary>
        public static NoOpPoolStatsTracker Instance
        {
            get
            {
                lock (_instanceGate)
                {
                    if (_instance == null)
                    {
                        _instance = new NoOpPoolStatsTracker();
                    }

                    return _instance;
                }
            }
        }

        /// <summary>
        /// Ignore tracking.
        /// </summary>
        public override void SetBasePool<T>(BasePool<T> basePool)
        {
        }

        /// <summary>
        /// Ignore tracking.
        /// </summary>
        public override void OnValueReuse(int bucketedSize)
        {
        }

        /// <summary>
        /// Ignore tracking.
        /// </summary>
        public override void OnSoftCapReached()
        {
        }

        /// <summary>
        /// Ignore tracking.
        /// </summary>
        public override void OnHardCapReached()
        {
        }

        /// <summary>
        /// Ignore tracking.
        /// </summary>
        public override void OnAlloc(int size)
        {
        }

        /// <summary>
        /// Ignore tracking.
        /// </summary>
        public override void OnFree(int sizeInBytes)
        {
        }

        /// <summary>
        /// Ignore tracking.
        /// </summary>
        public override void OnValueRelease(int sizeInBytes)
        {
        }   
    }
}
