using System;
using System.Collections.Generic;
using System.Threading;

namespace OMEGALIDL
{
    class Program
    {
        static void Main(string[] args)
        {
            int M = 20; 
            int N = 6;  
            int K = 2;  

            if (args.Length >= 3)
            {
                int.TryParse(args[0], out M);
                int.TryParse(args[1], out N);
                int.TryParse(args[2], out K);
            }

            if (M < 20 || M > 100) return;
            if (N < 3 || N > 12 || N % 3 != 0) return;
            if (K < 2 || K > 5) return;

            ShopSimulation sim = new ShopSimulation(M, N, K);
            sim.Run();
        }
    }

    public class ShopSimulation
    {
        private readonly int _mCustomers;
        private readonly int _nWorkers;
        private readonly int _kTerminals;

        private readonly SemaphoreSlim _shopCapacity;
        private readonly SemaphoreSlim _paymentTerminals;

        private readonly Barrier[] _teamBarriers;
        private int _totalDeliveries = 0;
        private int _lidlomixCount = 0;
        private readonly object _stockLock = new object();

        private readonly private readonly CancellationTokenSource _cts;


        public ShopSimulation(int m, int n, int k)
        {
            _mCustomers = m;
            _nWorkers = n;
            _kTerminals = k;

            _shopCapacity = new SemaphoreSlim(8, 8);
            _paymentTerminals = new SemaphoreSlim(_kTerminals, _kTerminals);
            
            _cts = new CancellationTokenSource();

            int numberOfTeams = _nWorkers / 3;
            _teamBarriers = new Barrier[numberOfTeams];

            for (int i = 0; i < numberOfTeams; i++)
            {
                int teamId = i;
                _teamBarriers[i] = new Barrier(3, (b) =>
                {
                    if (_cts.Token.IsCancellationRequested) return;

                    Console.WriteLine($"TEAM {teamId}: Delivering stock");
                    Thread.Sleep(400); 

                    lock (_stockLock)
                    {
                        _totalDeliveries++;
                        _lidlomixCount++;
                        Console.WriteLine($"TEAM {teamId}: Lidlomix delivered");
                        Monitor.PulseAll(_stockLock);
                    }
                });
            }
        }

        public void Run()
        {
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                _cts.Cancel();
                
                lock (_stockLock)
                {
                    Monitor.PulseAll(_stockLock);
                }
            };

            List<Thread> threads = new List<Thread>();

            for (int i = 0; i < _nWorkers; i++)
            {
                int workerId = i;
                Thread t = new Thread(() => WorkerRoutine(workerId));
                threads.Add(t);
                t.Start();
            }

            for (int i = 0; i < _mCustomers; i++)
            {
                int customerId = i;
                Thread t = new Thread(() => CustomerRoutine(customerId));
                threads.Add(t);
                t.Start();
            }

            foreach (var t in threads)
            {
                t.Join();
            }
        }

        private void CustomerRoutine(int id)
        {
            try
            {
                _shopCapacity.Wait(_cts.Token);
                Console.WriteLine($"CUSTOMER {id}: Entered the shop");
                Thread.Sleep(200);

                lock (_stockLock)
                {
                    while (_lidlomixCount == 0)
                    {
                        if (_cts.Token.IsCancellationRequested) 
                        {
                            _shopCapacity.Release();
                            return;
                        }
                        
                        Monitor.Wait(_stockLock);
                    }

                    if (_cts.Token.IsCancellationRequested)
                    {
                        _shopCapacity.Release();
                        return;
                    }

                    _lidlomixCount--;
                    Console.WriteLine($"CUSTOMER {id}: Picked up Lidlomix");
                }

                _paymentTerminals.Wait(_cts.Token);
                Console.WriteLine($"CUSTOMER {id}: Using payment terminal");
                Thread.Sleep(300);
                
                Console.WriteLine($"CUSTOMER {id}: Paid and leaving");
                _paymentTerminals.Release();
                _shopCapacity.Release();
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void WorkerRoutine(int id)
        {
            int teamId = id / 3;
            Barrier myTeamBarrier = _teamBarriers[teamId];

            while (!_cts.Token.IsCancellationRequested)
            {
                lock (_stockLock)
                {
                    if (_totalDeliveries >= _mCustomers) break;
                }

                try
                {
                    myTeamBarrier.SignalAndWait(_cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (BarrierPostPhaseException)
                {
                    break;
                }
            }
        }
    }
}
