using System.Diagnostics;

namespace ImagePipeline.Memory
{
    /**
    * A simple 'counter' that keeps track of the number of items (Count) as well as the byte
    * mCount for the number of items
    * WARNING: this class is not synchronized - the caller must ensure the appropriate
    * synchronization
    */
    class Counter
    {
        public int Count { get; set; }

        public int NumBytes { get; set; }

        /**
         * Add a new item to the counter
         * @param numBytes size of the item in bytes
         */
        public void Increment(int numBytes)
        {
            Count++;
            NumBytes += numBytes;
        }

        /**
         * 'Decrement' an item from the counter
         * @param numBytes size of the item in bytes
         */
        public void Decrement(int numBytes)
        {
            if (NumBytes >= numBytes && Count > 0)
            {
                Count--;
                NumBytes -= numBytes;
            }
            else
            {
                Debug.WriteLine($"Unexpected decrement of { numBytes }. Current numBytes = { NumBytes }, count = { Count }");
            }
        }

        /**
         * Reset the counter
         */
        public void Reset()
        {
            Count = 0;
            NumBytes = 0;
        }
    }
}
