//------------------------------------------------------------------------------
// <copyright file="WeakHashtable.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

// This was ripped from the BCL's WeakHashtable and the namespace was changed.
namespace ExtendedGrid.Microsoft.Windows.Controls
{
    using System;
    using System.Collections;
    using System.Security.Permissions;

    /// <devdoc>
    ///     This is a hashtable that stores object keys as weak references.  
    ///     It monitors memory usage and will periodically scavenge the
    ///     hash table to clean out dead references.
    /// </devdoc>
    [HostProtection(SharedState = true)]
    internal sealed class WeakHashtable : Hashtable
    {
        private static IEqualityComparer _comparer = new WeakKeyComparer();

        private long _lastGlobalMem;
        private int _lastHashCount;

        internal WeakHashtable()
            : base(_comparer)
        {
        }

        /// <devdoc>
        ///     Override of clear that performs a scavenge.
        /// </devdoc>
        public override void Clear()
        {
            base.Clear();
        }

        /// <devdoc>
        ///     Override of remove that performs a scavenge.
        /// </devdoc>
        public override void Remove(object key)
        {
            base.Remove(key);
        }

        /// <devdoc>
        ///     Override of Item that wraps a weak reference around the
        ///     key and performs a scavenge.
        /// </devdoc>
        public void SetWeak(object key, object value)
        {
            ScavengeKeys();
            this[new EqualityWeakReference(key)] = value;
        }

        /// <devdoc>
        ///     This method checks to see if it is necessary to
        ///     scavenge keys, and if it is it performs a scan
        ///     of all keys to see which ones are no longer valid.
        ///     To determine if we need to scavenge keys we need to
        ///     try to track the current GC memory.  Our rule of
        ///     thumb is that if GC memory is decreasing and our
        ///     key count is constant we need to scavenge.  We
        ///     will need to see if this is too often for extreme
        ///     use cases like the CompactFramework (they add
        ///     custom type data for every object at design time).
        /// </devdoc>
        private void ScavengeKeys()
        {
            int hashCount = Count;

            if (hashCount == 0)
            {
                return;
            }

            if (_lastHashCount == 0)
            {
                _lastHashCount = hashCount;
                return;
            }

            long globalMem = GC.GetTotalMemory(false);

            if (_lastGlobalMem == 0)
            {
                _lastGlobalMem = globalMem;
                return;
            }

            float memDelta = (float)(globalMem - _lastGlobalMem) / (float)_lastGlobalMem;
            float hashDelta = (float)(hashCount - _lastHashCount) / (float)_lastHashCount;

            if (memDelta < 0 && hashDelta >= 0)
            {
                // Perform a scavenge through our keys, looking
                // for dead references.                
                ArrayList cleanupList = null;
                foreach (object o in Keys)
                {
                    EqualityWeakReference wr = o as EqualityWeakReference;
                    if (wr != null && !wr.IsAlive)
                    {
                        if (cleanupList == null)
                        {
                            cleanupList = new ArrayList();
                        }

                        cleanupList.Add(wr);
                    }
                }

                if (cleanupList != null)
                {
                    foreach (object o in cleanupList)
                    {
                        Remove(o);
                    }
                }
            }

            _lastGlobalMem = globalMem;
            _lastHashCount = hashCount;
        }

        private class WeakKeyComparer : IEqualityComparer
        {
            bool IEqualityComparer.Equals(object x, object y)
            {
                if (x == null)
                {
                    return y == null;
                }

                if (y != null && x.GetHashCode() == y.GetHashCode())
                {
                    EqualityWeakReference wX = x as EqualityWeakReference;
                    EqualityWeakReference wY = y as EqualityWeakReference;

                    if (wX != null)
                    {
                        if (!wX.IsAlive)
                        {
                            return false;
                        }

                        x = wX.Target;
                    }

                    if (wY != null)
                    {
                        if (!wY.IsAlive)
                        {
                            return false;
                        }

                        y = wY.Target;
                    }

                    return object.ReferenceEquals(x, y);
                }

                return false;
            }

            int IEqualityComparer.GetHashCode(object obj)
            {
                return obj.GetHashCode();
            }
        }

        /// <devdoc>
        ///     A wrapper of WeakReference that overrides GetHashCode and
        ///     Equals so that the weak reference returns the same equality
        ///     semantics as the object it wraps.  This will always return
        ///     the object's hash code and will return True for a Equals
        ///     comparison of the object it is wrapping.  If the object
        ///     it is wrapping has finalized, Equals always returns false.
        /// </devdoc>
        private sealed class EqualityWeakReference
        {
            private int _hashCode;
            private WeakReference _weakRef;

            internal EqualityWeakReference(object o)
            {
                _weakRef = new WeakReference(o);
                _hashCode = o.GetHashCode();
            }

            public bool IsAlive
            {
                get { return _weakRef.IsAlive; }
            }

            public object Target
            {
                get { return _weakRef.Target; }
            }

            public override bool Equals(object o)
            {
                if (o == null)
                {
                    return false;
                }

                if (o.GetHashCode() != _hashCode)
                {
                    return false;
                }

                if (o == this || (IsAlive && object.ReferenceEquals(o, Target)))
                {
                    return true;
                }

                return false;
            }

            public override int GetHashCode()
            {
                return _hashCode;
            }
        }
    }
}
