using FBCore.Common.References;
using System.Collections.Generic;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// A Bucket that uses OOMSoftReferences to store its free list.
    /// </summary>
    class OOMSoftReferenceBucket<T> : Bucket<T> where T : class
    {
        private Queue<OOMSoftReference<T>>_spareReferences;

        public OOMSoftReferenceBucket(int itemSize, int maxLength, int inUseLength) : 
            base(itemSize, maxLength, inUseLength)
        {
            _spareReferences = new Queue<OOMSoftReference<T>>();
        }

        public override T Pop()
        {
            OOMSoftReference<T> reference = (OOMSoftReference<T>)_freeList.Dequeue();
            T value = reference.Get();
            reference.Clear();
            _spareReferences.Enqueue(reference);
            return value;
        }

        protected override void AddToFreeList(T value)
        {
            OOMSoftReference<T> reference = default(OOMSoftReference<T>);
            if (_spareReferences.Count != 0)
            {
                reference = _spareReferences.Dequeue();
            }
            else
            {
                reference = new OOMSoftReference<T>();
            }

            reference.Set(value);
            _freeList.Enqueue(reference);
        }
    }
}
