using System;

namespace FBCore.Common.References
{
    /// <summary>
    /// To eliminate the possibility of some of our objects causing an OutOfMemoryError
    /// when they are not used, we reference them via SoftReferences.
    /// What is a SoftReference?
    /// <a href="http://developer.android.com/reference/java/lang/ref/SoftReference.html"></a>
    /// <a href="http://docs.oracle.com/javase/7/docs/api/java/lang/ref/SoftReference.html"></a>
    /// A Soft Reference is a reference that is cleared when its referent is not strongly
    /// reachable and there is memory pressure. SoftReferences as implemented by Dalvik
    /// blindly treat every second SoftReference as a WeakReference every time a garbage
    /// collection happens, - i.e. clear it unless there is something else referring to it:
    /// <a href="https://goo.gl/Pe6aS7">dalvik</a>
    /// <a href="https://goo.gl/BYaUZE">art</a>
    /// It will however clear every SoftReference if we don't have enough memory to
    /// satisfy an allocation after a garbage collection.
    ///
    /// This means that as long as one of the soft references stays alive, they all
    /// stay alive. If we have two SoftReferences next to each other on the heap,
    /// both pointing to the same object, then we are guaranteed that neither will
    /// be cleared until we otherwise would have thrown an OutOfMemoryError.
    /// Since we can't strictly guarantee the location of objects on the heap, we use
    /// 3 just to be on the safe side.
    /// TLDR: It's a reference that's cleared if and only if we otherwise would have
    /// encountered an OOM.
    /// </summary>
    public class OOMSoftReference<T> where T : class
    {
        WeakReference<T> softRef1;
        WeakReference<T> softRef2;
        WeakReference<T> softRef3;

        /// <summary>
        /// Instantiates the <see cref="OOMSoftReference{T}"/>.
        /// </summary>
        public OOMSoftReference()
        {
            softRef1 = null;
            softRef2 = null;
            softRef3 = null;
        }

        /// <summary>
        /// Set 3 weak references just to be on the safe side.
        /// </summary>
        /// <param name="hardReference">The hard reference.</param>
        public void Set(T hardReference)
        {
            softRef1 = new WeakReference<T>(hardReference);
            softRef2 = new WeakReference<T>(hardReference);
            softRef3 = new WeakReference<T>(hardReference);
        }

        /// <summary>
        /// Try getting the reference from the weak reference.
        /// </summary>
        /// <returns>T</returns>
        public T Get()
        {
            T reference = default(T);
            if (softRef1 != null)
            {
                softRef1.TryGetTarget(out reference);
            }

            return reference;
        }

        /// <summary>
        /// Clears all the weak references.
        /// </summary>
        public void Clear()
        {
            if (softRef1 != null)
            {
                softRef1 = null;
            }

            if (softRef2 != null)
            {
                softRef2 = null;
            }

            if (softRef3 != null)
            {
                softRef3 = null;
            }
        }
    }
}
