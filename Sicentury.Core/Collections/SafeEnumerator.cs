using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Sicentury.Core.Collections
{
    public sealed class SafeEnumerator<T> : IEnumerator<T>

    {
        #region variables

        // this is the (thread-unsafe)
        // enumerator of the underlying collection
        private readonly IEnumerator<T> _enumeratorInner;

        // this is the object we shall lock on. 
        private readonly SemaphoreSlim _semLocker;

        #endregion

        public SafeEnumerator(IEnumerator<T> inner, SemaphoreSlim semLocker)
        {
            // entering lock in constructor
            //semLocker.Wait();
            _semLocker = semLocker;

            _enumeratorInner = inner;
        }

        #region Implementation of IDisposable

        public void Dispose()
        {
            _semLocker.Release();
        }

        #endregion

        #region Implementation of IEnumerator

        // we just delegate actual implementation
        // to the inner enumerator, that actually iterates
        // over some collection

        public bool MoveNext()
        {
            return _enumeratorInner.MoveNext();
        }

        public void Reset()
        {
            _enumeratorInner.Reset();
        }

        public T Current => _enumeratorInner.Current;

        object IEnumerator.Current => Current;

        #endregion
    }
}

