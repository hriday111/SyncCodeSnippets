
using System;
using System.Collections.Generic;
using System.Threading;
using System.Collections.Concurrent;

namespace SyncCheatSheet
{
    class Program
    {
        static void Main()
        {
        }
    }

    class LockExample
    {
        private readonly object _lock = new object();
        private int _counter;

        void Increment()
        {
            lock (_lock)
            {
                _counter++;
            }
        }
    }

    class MonitorExample
    {
        private readonly object _lock = new object();
        private bool _ready = false;

        void Waiter()
        {
            lock (_lock)
            {
                while (!_ready)         
                    Monitor.Wait(_lock);
            }
        }

        void Signaler()
        {
            lock (_lock)
            {
                _ready = true;
                Monitor.PulseAll(_lock); // wake all waiters
            }
        }
    }

    class ProducerConsumer
    {
        private readonly Queue<int> _queue = new Queue<int>();
        private readonly object _lock = new object();
        private const int Capacity = 5;

        void Producer()
        {
            for (int i = 0; i < 100; i++)
            {
                lock (_lock)
                {
                    while (_queue.Count == Capacity)
                        Monitor.Wait(_lock);

                    _queue.Enqueue(i);
                    Monitor.PulseAll(_lock);
                }
            }
        }

        void Consumer()
        {
            while (true)
            {
                int item;
                lock (_lock)
                {
                    while (_queue.Count == 0)
                        Monitor.Wait(_lock);

                    item = _queue.Dequeue();
                    Monitor.PulseAll(_lock);
                }
            }
        }
    }

    class InterlockedExample
    {
        private int _count;

        void SafeIncrement()
        {
            Interlocked.Increment(ref _count);
        }

        void CompareAndSwap()
        {
            int expected = 0;
            int desired = 1;
            Interlocked.CompareExchange(ref _count, desired, expected);
        }
    }

    class VolatileExample
    {
        private volatile bool _stop;

        void Worker()
        {
            while (!_stop)
            {
                // do work
            }
        }

        void Stop() => _stop = true;
    }

    // ========================================================
    // 6) SemaphoreSlim — LIMIT CONCURRENCY (IN-PROCESS)
    // ========================================================
    class SemaphoreSlimExample
    {
        private readonly SemaphoreSlim _sem = new SemaphoreSlim(2, 2);

        void UseResource()
        {
            _sem.Wait();
            try
            {
                // at most 2 threads here
            }
            finally
            {
                _sem.Release();
            }
        }
    }

    // ========================================================
    // 7) Semaphore — CROSS-PROCESS
    // ========================================================
    class SemaphoreExample
    {
        void CrossProcess()
        {
            using var sem = new Semaphore(1, 1, "Global\\LabSemaphore");
            sem.WaitOne();
            try
            {
                // shared across processes
            }
            finally
            {
                sem.Release();
            }
        }
    }

    class MutexExample
    {
        private readonly Mutex _mutex = new Mutex();

        void Critical()
        {
            _mutex.WaitOne();
            try
            {
                // exclusive access
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }
    }

    class ReaderWriterExample
    {
        private readonly ReaderWriterLockSlim _rw = new ReaderWriterLockSlim();
        private int _data;

        int Read()
        {
            _rw.EnterReadLock();
            try { return _data; }
            finally { _rw.ExitReadLock(); }
        }

        void Write(int value)
        {
            _rw.EnterWriteLock();
            try { _data = value; }
            finally { _rw.ExitWriteLock(); }
        }
    }

    class BarrierExample
    {
        private readonly Barrier _barrier = new Barrier(3, _ =>
        {
            Console.WriteLine("All team members arrived");
        });

        void Worker()
        {
            // work
            _barrier.SignalAndWait(); // wait for team
            // next phase
        }
    }

    class CountdownExample
    {
        private readonly CountdownEvent _countdown = new CountdownEvent(3);

        void Worker()
        {
            // init work
            _countdown.Signal();
        }

        void MainThread()
        {
            _countdown.Wait(); // waits until 3 signals
        }
    }

    class ManualResetEventExample
    {
        private readonly ManualResetEventSlim _evt = new ManualResetEventSlim(false);

        void Waiter() => _evt.Wait();

        void SignalAll() => _evt.Set();

        void Reset() => _evt.Reset();
    }

    class AutoResetEventExample
    {
        private readonly AutoResetEvent _evt = new AutoResetEvent(false);

        void Waiter()
        {
            _evt.WaitOne(); // only one released
        }

        void Signal() => _evt.Set();
    }
    class ConcurrentCollectionsExample
    {
        private readonly ConcurrentQueue<int> _queue = new ConcurrentQueue<int>();

        void Producer() => _queue.Enqueue(1);

        void Consumer()
        {
            if (_queue.TryDequeue(out int item))
            {
                // use item
            }
        }
    }

    class CancellationExample
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        void Worker()
        {
            try
            {
                while (true)
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    // work
                }
            }
            catch (OperationCanceledException)
            {
                // clean exit
            }
        }

        void Stop() => _cts.Cancel();
    }
}
