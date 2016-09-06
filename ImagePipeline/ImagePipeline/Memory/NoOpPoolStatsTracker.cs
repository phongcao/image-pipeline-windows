using ImagePipeline.Memory;

namespace ImagePipeline.Memory
{
    /**
     * Empty implementation of PoolStatsTracker that does not perform any tracking.
     */
    public class NoOpPoolStatsTracker : PoolStatsTracker
    {
        // Init lock
        private static readonly object _instanceGate = new object();

        private static NoOpPoolStatsTracker _instance = null;

        private NoOpPoolStatsTracker()
        {
        }

        public static NoOpPoolStatsTracker GetInstance()
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

        public override void SetBasePool<T>(BasePool<T> basePool)
        {
        }

        public override void OnValueReuse(int bucketedSize)
        {
        }

        public override void OnSoftCapReached()
        {
        }

        public override void OnHardCapReached()
        {
        }

        public override void OnAlloc(int size)
        {
        }

        public override void OnFree(int sizeInBytes)
        {
        }

        public override void OnValueRelease(int sizeInBytes)
        {
        }   
    }
}
