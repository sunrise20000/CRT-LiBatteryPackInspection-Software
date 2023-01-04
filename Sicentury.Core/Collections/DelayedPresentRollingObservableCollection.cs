using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Sicentury.Core.Collections
{
    /// <summary>
    /// The collection is thread-safe.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DelayedPresentRollingObservableCollection<T> : IEnumerable<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        #region Variables

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private readonly List<T> _list;
        private readonly List<T> _cache;

        private readonly object _lockList;
        private readonly object _lockCache;

        private readonly SemaphoreSlim _semListEnumLocker;

        private readonly int _presentDelayMillisec = 200; // the default delay is 200ms

        #endregion

        #region Constructors

        public DelayedPresentRollingObservableCollection(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentException("the capacity must be greate then 0.", nameof(capacity));

            _lockList = new object();
            _lockCache = new object();
            _semListEnumLocker = new SemaphoreSlim(0, 1);

            Capacity = capacity;

            lock (_lockList)
            {
                _list = new List<T>();
            }

            lock (_lockCache)
            {
                _cache = new List<T>();
            }

            _semListEnumLocker.Release();

            _ = Task.Run(async () =>
            {
                Thread.Sleep(2000);

                while (true)
                {
                    try
                    {
                        Present();
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine($"Unable to present the items, {e.Message}");
                        Debugger.Break();
                    }
                    finally
                    {
                        // present the data every 200ms
                        await Task.Delay(_presentDelayMillisec);
                    }
                }
            });
        }

        public DelayedPresentRollingObservableCollection(int capacity, int presentDelayMillisec) : this(capacity)
        {
            _presentDelayMillisec = presentDelayMillisec < 50 ? 50 : presentDelayMillisec;
        }

        #endregion

        #region Properties

        public int Capacity { get; }

        public int Count
        {
            get
            {
                lock (_lockList)
                {
                    return _list?.Count ?? 0;
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Copy the items from the cache to the presented list and update the UI.
        /// </summary>
        private void Present()
        {
            var itemsChanged = new List<T>();

            lock (_lockCache)
            {
                if (_cache.Count <= 0)
                    return;

                itemsChanged.AddRange(_cache);
                _cache.Clear();
            }

            var notifyAction = NotifyCollectionChangedAction.Add;
            lock (_lockList)
            {
                _list.AddRange(itemsChanged);

                if (_list.Count >= Capacity)
                {
                    var cntToRemain = Capacity / 2;
                    _list.RemoveRange(0, _list.Count - cntToRemain);

                    notifyAction = NotifyCollectionChangedAction.Reset;
                }
            }

            // raise the collection changed event.
            SynchronizationContext.SetSynchronizationContext(
                new DispatcherSynchronizationContext(Application.Current.Dispatcher));

            SynchronizationContext.Current.Post(p1 =>
            {
                CollectionChanged?.Invoke(
                    this,
                    new NotifyCollectionChangedEventArgs(notifyAction,
                        notifyAction == NotifyCollectionChangedAction.Add ? itemsChanged : null));
            }, null);

            SynchronizationContext.Current.Post(p1 =>
            {
                OnPropertyChanged(nameof(Count));
            }, null);
        }

        public void Add(T newItem)
        {
            lock (_lockCache)
            {
                _cache.Add(newItem);
            }
        }

        public void Clear()
        {
            lock (_lockCache)
            {
                _cache.Clear();
            }

            lock (_lockList)
            {
                _list.Clear();
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            lock (_lockList)
            {
                _semListEnumLocker.Wait();
                return new SafeEnumerator<T>(_list.GetEnumerator(), _semListEnumLocker);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            lock (_lockList)
            {
                _semListEnumLocker.Wait();
                return new SafeEnumerator<T>(_list.GetEnumerator(), _semListEnumLocker);
            }
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException();

                lock (_lockList)
                {
                    return _list[index];
                }
            }
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}