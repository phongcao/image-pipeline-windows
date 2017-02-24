using System.Diagnostics;

namespace ImagePipeline.Memory
{
    /// <summary>
   /// A simple 'counter' that keeps track of the number of items (Count)
   /// as well as the byte _count for the number of items.
   /// WARNING: this class is not synchronized - the caller must ensure
   /// the appropriate synchronization.
   /// </summary>
    class Counter
    {
        public int Count { get; set; }

        public int NumBytes { get; set; }

        /// <summary>
        /// Add a new item to the counter.
        /// </summary>
        /// <param name="numBytes">
        /// Size of the item in bytes.
        /// </param>
        public void Increment(int numBytes)
        {
            Count++;
            NumBytes += numBytes;
        }

        /// <summary>
        /// 'Decrement' an item from the counter.
        /// </summary>
        /// <param name="numBytes">
        /// Size of the item in bytes.
        /// </param>
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

        /// <summary>
        /// Reset the counter.
        /// </summary>
        public void Reset()
        {
            Count = 0;
            NumBytes = 0;
        }
    }
}
