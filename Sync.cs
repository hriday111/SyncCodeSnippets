using System;
using System.Threading;
using System.Threading.Tasks;

public class MySynchronization
{
    private readonly object _lockHandle = new object();
    private int _counter = 0;
    private static SemaphoreSlim _semaphore = new SemaphoreSlim(2, 2);
    private static Mutex _mutex = new Mutex();

    public void UseLock()
    {
        lock (_lockHandle)
        {
            _counter++;
        }
    }

    public void UseMonitor()
    {
        bool lockTaken = false;
        try
        {
            Monitor.TryEnter(_lockHandle, TimeSpan.FromMilliseconds(100), ref lockTaken);
            if (lockTaken)
            {
                _counter++;
            }
        }
        finally
        {
            if (lockTaken) Monitor.Exit(_lockHandle);
        }
    }

    public void UseInterlocked()
    {
        Interlocked.Increment(ref _counter);
    }

    public async Task UseSemaphore()
    {
        await _semaphore.WaitAsync();
        try
        {
            await Task.Delay(10);
            _counter++;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void UseMutex()
    {
        if (_mutex.WaitOne(1000))
        {
            try
            {
                _counter++;
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }
    }
}
