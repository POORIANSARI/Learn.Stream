using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Learn.Stream.Multithreading;

public class Learn
{
    /// <summary>
    /// Thread-static field that maintains separate instances per thread.
    /// Each thread has its own copy, avoiding race conditions.
    /// Useful for thread-local state without explicit ThreadLocal wrapper.
    /// </summary>
    [ThreadStatic] static int counter;

    /// <summary>
    /// Demonstrates basic thread creation, starting, and synchronous waiting.
    /// Shows fundamental thread lifecycle management with Join().
    /// Essential pattern for coordinating thread completion.
    /// </summary>
    public void Execute()
    {
        var t = new Thread(param => Console.WriteLine(param));
        t.Start();
        t.Join(); // Wait for completion 
    }

    /// <summary>
    /// Shows thread sleeping to pause execution for specified duration.
    /// Demonstrates cooperative multitasking and thread scheduling.
    /// Sleep releases CPU to other threads during wait period.
    /// </summary>
    public void Sleep()
    {
        var t = new Thread(param => Console.WriteLine(param));
        t.Start();
        Thread.Sleep(1000); // Pause for 1 second
        t.Join(); // Wait for completion 
    }

    /// <summary>
    /// Illustrates Join() method for thread synchronization.
    /// Calling thread blocks until target thread completes.
    /// Critical for ensuring thread completion before proceeding.
    /// </summary>
    public void Join()
    {
        var t = new Thread(param => Console.WriteLine(param));
        t.Start();
        Thread.Sleep(1000); // Pause for 1 second
        t.Join(); // Wait for completion 
    }

    /// <summary>
    /// Demonstrates background threads that terminate with main thread.
    /// Background threads don't prevent application shutdown.
    /// Useful for cleanup tasks or monitoring threads.
    /// </summary>
    public void KillAfterMain()
    {
        var t = new Thread(param => Console.WriteLine(param));
        t.IsBackground = true; // Thread dies when main ends
        t.Start();
        t.Join(); // Wait for completion 
    }

    /// <summary>
    /// Shows thread identification using Name and ManagedThreadId properties.
    /// Essential for debugging and logging in multi-threaded applications.
    /// CurrentThread provides access to executing thread's properties.
    /// </summary>
    public void PrintThreadName()
    {
        var t = new Thread(param => Console.WriteLine(Thread.CurrentThread.Name!, param));
        //Thread.CurrentThread.ManagedThreadId
        t.Start();
        t.Join(); // Wait for completion 
    }

    /// <summary>
    /// Demonstrates thread priority setting for scheduling influence.
    /// Higher priority threads get more CPU time allocation.
    /// Use sparingly as OS ultimately controls thread scheduling.
    /// </summary>
    public void SetPriority()
    {
        var t = new Thread(param => Console.WriteLine(Thread.CurrentThread.Name!, param));
        t.Priority = ThreadPriority.Highest;
        t.Start();
        t.Join(); // Wait for completion 
    }

    /// <summary>
    /// Shows deprecated Abort() method - DO NOT USE in production.
    /// Abort can leave shared state in inconsistent condition.
    /// Modern alternative is CancellationToken for cooperative cancellation.
    /// </summary>
    public void Abort()
    {
        Thread t = new Thread(() =>
        {
            while (true)
            {
            }
        });
        t.Start();
        t.Abort(); // Unsafe, can crash app
    }

    /// <summary>
    /// Demonstrates ThreadPool usage for lightweight background work.
    /// ThreadPool reuses threads, reducing creation overhead.
    /// Ideal for short-lived, CPU-bound operations.
    /// </summary>
    public void UseThreadPool()
    {
        ThreadPool.QueueUserWorkItem(_ => Console.WriteLine("ThreadPool work"));
    }

    /// <summary>
    /// Shows yielding execution to allow other threads to run.
    /// Sleep(0) triggers thread scheduler to switch contexts.
    /// Useful in tight loops to prevent thread starvation.
    /// </summary>
    public void YieldExecToOtherThreads()
    {
        Thread.Sleep(0); // Yields execution to other threads
    }

    /// <summary>
    /// Demonstrates indefinite waiting using Timeout.Infinite.
    /// Thread blocks until interrupted or application terminates.
    /// Commonly used in service applications or daemon threads.
    /// </summary>
    public void WaitForEver()
    {
        Thread.Sleep(Timeout.Infinite); // Wait indefinitely
    }

    /// <summary>
    /// Shows thread interruption mechanism for breaking wait states.
    /// Interrupt() throws ThreadInterruptedException in waiting thread.
    /// Allows graceful cancellation of blocking operations.
    /// </summary>
    void Interrupt()
    {
        Thread t = new Thread(() =>
        {
            try
            {
                Thread.Sleep(5000);
            }
            catch (ThreadInterruptedException)
            {
                Console.WriteLine("Interrupted!");
            }
        });
        t.Start();
        t.Interrupt();
    }

    /// <summary>
    /// Demonstrates checking thread execution status.
    /// IsAlive returns true while thread is running.
    /// Essential for thread lifecycle monitoring and debugging.
    /// </summary>
    void CheckingIfThreadIsAlive()
    {
        Thread t = new Thread(() => { });
        t.Start();
        Console.WriteLine(t.IsAlive);
    }

    /// <summary>
    /// Shows ThreadStatic field usage for per-thread storage.
    /// Each thread maintains separate counter instance.
    /// Eliminates need for synchronization in thread-local scenarios.
    /// </summary>
    void SetThreadSafeCounter()
    {
        counter = 5; // Each thread has its own counter
    }

    /// <summary>
    /// Demonstrates basic Task creation and execution.
    /// Task.Run executes work on ThreadPool thread.
    /// Wait() blocks until task completes synchronously.
    /// </summary>
    void CreateTask()
    {
        Task t = Task.Run(() => Console.WriteLine("Hello Task"));
        t.Wait();
    }

    /// <summary>
    /// Shows Task with return value using generic Task<T>.
    /// Result property blocks until task completes and returns value.
    /// Preferred over Thread for operations needing return values.
    /// </summary>
    void CreateTaskWithReturnValue()
    {
        Task<int> t = Task.Run(() => 42);
        Console.WriteLine(t.Result);
    }

    /// <summary>
    /// Demonstrates waiting for multiple tasks to complete.
    /// WaitAll blocks until all specified tasks finish.
    /// Essential for coordinating parallel operations.
    /// </summary>
    void WaitAll()
    {
        Task t1 = Task.Run(() => Thread.Sleep(1000));
        Task t2 = Task.Run(() => Thread.Sleep(500));
        Task.WaitAll(t1, t2);
    }

    /// <summary>
    /// Shows waiting for first task completion among multiple tasks.
    /// WaitAny returns index of first completed task.
    /// Useful for timeout scenarios or race conditions.
    /// </summary>
    void WaitAny()
    {
        Task t1 = Task.Run(() => Thread.Sleep(1000));
        Task t2 = Task.Run(() => Thread.Sleep(500));
        int finished = Task.WaitAny(t1, t2);
        Console.WriteLine($"Task {finished} finished first");
    }

    /// <summary>
    /// Demonstrates async/await pattern with Task.Delay.
    /// Delay provides non-blocking wait unlike Thread.Sleep.
    /// Essential for responsive async programming.
    /// </summary>
    async void Delay()
    {
        try
        {
            await Task.Delay(1000); // Non-blocking wait
        }
        catch (Exception e)
        {
            throw; // TODO handle exception
        }
    }

    /// <summary>
    /// Shows task continuation pattern for chaining operations.
    /// ContinueWith executes when antecedent task completes.
    /// Allows building complex async workflows.
    /// </summary>
    void ContinueWith()
    {
        Task.Run(() => 1)
            .ContinueWith(t => Console.WriteLine($"Result: {t.Result}"));
    }

    /// <summary>
    /// Demonstrates composite task creation waiting for all completions.
    /// WhenAll returns task that completes when all input tasks finish.
    /// Preferred over WaitAll in async contexts.
    /// </summary>
    void WhenAll()
    {
        Task.WhenAll(Task.Delay(1000), Task.Delay(500));
    }

    /// <summary>
    /// Shows composite task creation waiting for first completion.
    /// WhenAny returns task that completes when first input task finishes.
    /// Useful for timeout patterns and competitive scenarios.
    /// </summary>
    void WhenAny()
    {
        var first = Task.WhenAny(Task.Delay(1000), Task.Delay(500));
    }

    /// <summary>
    /// Demonstrates cooperative cancellation using CancellationToken.
    /// CancellationTokenSource provides cancellation control mechanism.
    /// Essential pattern for graceful operation termination.
    /// </summary>
    void CancellationTokenSourceCreate()
    {
        var cts = new CancellationTokenSource();
        var t = Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
            }
        }, cts.Token);
        cts.Cancel();
    }

    /// <summary>
    /// Shows TaskFactory usage for advanced task creation options.
    /// LongRunning hint suggests dedicated thread allocation.
    /// Provides more control over task creation behavior.
    /// </summary>
    void TaskFactoryCreate()
    {
        Task t = Task.Factory.StartNew(() => Thread.Sleep(1000), TaskCreationOptions.LongRunning);
        t.Wait();
    }

    /// <summary>
    /// Demonstrates task status monitoring during execution.
    /// Status property reflects current task lifecycle state.
    /// Essential for debugging and progress tracking.
    /// </summary>
    void TaskStatus()
    {
        Task t = Task.Run(() => Thread.Sleep(100));
        Console.WriteLine(t.Status); // Running, RanToCompletion, Faulted
    }

    /// <summary>
    /// Shows alternative task completion checking using IsCompleted.
    /// IsCompleted returns true when task finishes (success or failure).
    /// Non-blocking way to check task completion status.
    /// </summary>
    void TaskStatus2()
    {
        Task t = Task.Run(() => Thread.Sleep(100));
        Console.WriteLine(t.IsCompleted);
    }

    /// <summary>
    /// Demonstrates blocking behavior of Task.Result property.
    /// Result blocks calling thread until task completes.
    /// Can cause deadlocks in UI or ASP.NET contexts.
    /// </summary>
    void TaskResultBlocks()
    {
        Task<int> t = Task.Run(() => 10);
        int result = t.Result; // Blocks until done
    }

    /// <summary>
    /// Shows parallel execution of multiple independent operations.
    /// Parallel.Invoke runs actions concurrently on available threads.
    /// Simplifies parallel execution of heterogeneous work.
    /// </summary>
    void ParallelInvoke()
    {
        Parallel.Invoke(
            () => Console.WriteLine("Task1"),
            () => Console.WriteLine("Task2")
        );
    }

    /// <summary>
    /// Demonstrates parallel iteration with controlled concurrency.
    /// MaxDegreeOfParallelism limits number of concurrent threads.
    /// Essential for resource-bounded parallel processing.
    /// </summary>
    void ParallelFor()
    {
        Parallel.For(0, 10, new ParallelOptions { MaxDegreeOfParallelism = 2 }, i => Console.WriteLine(i));
    }

    /// <summary>
    /// Shows parallel iteration over collection elements.
    /// ForEach automatically partitions work across available threads.
    /// Ideal for CPU-intensive operations on collections.
    /// </summary>
    void ParallelForEach()
    {
        Parallel.ForEach(new[] { 1, 2, 3 }, i => Console.WriteLine(i));
    }

    /// <summary>
    /// Demonstrates PLINQ (Parallel LINQ) for query parallelization.
    /// AsParallel() enables automatic parallelization of LINQ operations.
    /// Combines functional programming with parallel execution.
    /// </summary>
    void PLinq()
    {
        var result = (from i in Enumerable.Range(1, 10).AsParallel()
            select i * i).ToList();
    }

    /// <summary>
    /// Shows task waiting with timeout specification.
    /// Wait(timeout) returns false if task doesn't complete in time.
    /// Prevents indefinite blocking in unreliable scenarios.
    /// </summary>
    void TaskWaitTimeout()
    {
        var t = Task.Delay(2000);
        bool finished = t.Wait(1000); // false if timeout
    }

    /// <summary>
    /// Demonstrates continuation with custom TaskScheduler.
    /// TaskScheduler controls where continuation executes.
    /// Important for UI thread marshalling and context control.
    /// </summary>
    void TaskContinueWithWithScheduler()
    {
        Task.Run(() => Console.WriteLine("A"))
            .ContinueWith(t => Console.WriteLine("B"), TaskScheduler.Current);
    }

    /// <summary>
    /// Shows proper exception handling in Task-based operations.
    /// AggregateException wraps exceptions from faulted tasks.
    /// Essential for robust error handling in parallel code.
    /// </summary>
    void ExceptionThrow()
    {
        Task t = Task.Run(() => throw new InvalidOperationException());
        try
        {
            t.Wait();
        }
        catch (AggregateException ex)
        {
            Console.WriteLine(ex.InnerException.Message);
        }
    }

    /// <summary>
    /// Demonstrates async method returning Task with value.
    /// Async methods enable non-blocking asynchronous programming.
    /// Foundation of modern .NET asynchronous patterns.
    /// </summary>
    async Task<int> GetDataAsync()
    {
        await Task.Delay(1000);
        return 42;
    }

    /// <summary>
    /// Shows async lambda expression usage in delegates.
    /// Async lambdas enable asynchronous operations in functional contexts.
    /// Useful for event handlers and callback scenarios.
    /// </summary>
    async Task AsyncLambda()
    {
        Func<Task> asyncAction = async () => await Task.Delay(500);
        await asyncAction();
    }

    /// <summary>
    /// Demonstrates ConfigureAwait(false) for library code optimization.
    /// Avoids capturing synchronization context, improving performance.
    /// Critical pattern for avoiding deadlocks in library methods.
    /// </summary>
    async Task ConfigureAwait()
    {
        await Task.Delay(500).ConfigureAwait(false); // Avoid context capture
    }

    /// <summary>
    /// Shows fire-and-forget task pattern using discard operator.
    /// Starts task without waiting for completion or result.
    /// Useful for background operations that don't need coordination.
    /// </summary>
    void FireAndForget()
    {
        _ = Task.Run(async () => await Task.Delay(1000));
    }

    /// <summary>
    /// Demonstrates avoiding deadlocks by using await instead of blocking.
    /// Wait() can deadlock in UI/ASP.NET contexts with sync context.
    /// Await preserves async nature and prevents context deadlocks.
    /// </summary>
    async Task AvoidBlockingAsync()
    {
        // Bad
        Task.Delay(1000).Wait(); // Can deadlock
        // Good
        await Task.Delay(1000);
    }

    /// <summary>
    /// Shows thread-safe counter increment using Interlocked operations.
    /// Interlocked provides atomic operations without explicit locking.
    /// Guarantees thread safety with minimal performance overhead.
    /// </summary>
    void ThreadSafeCounterWithInterlocked()
    {
        int counter = 0;
        Parallel.For(0, 1000, i => Interlocked.Increment(ref counter));
        Console.WriteLine(counter); // Always 1000
    }

    /// <summary>
    /// Demonstrates atomic compare-and-swap operation.
    /// CompareExchange atomically compares and conditionally updates value.
    /// Foundation of lock-free programming algorithms.
    /// </summary>
    void InterlockedCompareExchange()
    {
        int value = 0;
        Interlocked.CompareExchange(ref value, 1, 0); // Sets value to 1 if currently 0
    }

    /// <summary>
    /// Shows atomic addition operation using Interlocked.Add.
    /// Add atomically adds value to variable without race conditions.
    /// More efficient than lock-based arithmetic operations.
    /// </summary>
    void InterlockedAdd()
    {
        int total = 0;
        Interlocked.Add(ref total, 5);
    }

    /// <summary>
    /// Demonstrates proper loop variable capture in async contexts.
    /// Local copy prevents closure capture issues in loops.
    /// Critical pattern for avoiding unexpected behavior in parallel code.
    /// </summary>
    void AvoidCapturingLoopVariables()
    {
        for (int i = 0; i < 5; i++)
        {
            int copy = i;
            Task.Run(() => Console.WriteLine(copy));
        }
    }

    /// <summary>
    /// Shows thread-safe lazy initialization pattern.
    /// Lazy<T> ensures single initialization across multiple threads.
    /// Eliminates race conditions in singleton and expensive object creation.
    /// </summary>
    void ThreadSafeLazyInitialization()
    {
        Lazy<MyClass> lazy = new Lazy<MyClass>(() => new MyClass(), true);
    }

    /// <summary>
    /// Demonstrates deadlock prevention in async methods.
    /// Never block on async methods using .Result or .Wait().
    /// Always use await to maintain async chain integrity.
    /// </summary>
    async Task AvoidDeadlocksWithAsync()
    {
        // Never do:
        Task.Run(() => asyncMethod().Result);

        // Instead:
        await asyncMethod2();
    }

    /// <summary>
    /// Helper method returning completed task for demonstration.
    /// FromResult creates already-completed task with specified result.
    /// Useful for implementing async interfaces synchronously.
    /// </summary>
    Task<int> asyncMethod()
    {
        return Task.FromResult(42);
    }

    /// <summary>
    /// Helper async method demonstrating proper async implementation.
    /// Uses Task.Run for CPU-bound work in async context.
    /// Shows proper async/await pattern usage.
    /// </summary>
    async Task<int> asyncMethod2()
    {
        return await Task.Run(() => 42);
    }

    /// <summary>
    /// Demonstrates manual task completion control using TaskCompletionSource.
    /// Allows creating tasks completed by external events or conditions.
    /// Essential for bridging callback-based APIs with Task-based async.
    /// </summary>
    async Task TaskCompletionSource()
    {
        var tcs = new TaskCompletionSource<int>();
        Task.Run(() => tcs.SetResult(42));
        var result = await tcs.Task;
    }

    /// <summary>
    /// Shows async-compatible locking using SemaphoreSlim.
    /// WaitAsync() allows non-blocking acquisition of semaphore.
    /// Only async-compatible synchronization primitive in .NET.
    /// </summary>
    async Task AsyncLockWithSemaphoreSlim()
    {
        var sem = new SemaphoreSlim(1, 1);
        await sem.WaitAsync();
        try
        {
            /* Critical section */
        }
        finally
        {
            sem.Release();
        }
    }

    /// <summary>
    /// Demonstrates thread-safe dictionary operations.
    /// ConcurrentDictionary provides atomic operations without external locking.
    /// TryAdd ensures thread-safe insertion with existence checking.
    /// </summary>
    void ThreadSafeDictionary()
    {
        var dict = new ConcurrentDictionary<int, string>();
        dict.TryAdd(1, "A");
    }

    /// <summary>
    /// Shows ValueTask usage for high-performance async scenarios.
    /// ValueTask reduces allocation overhead for frequently-called async methods.
    /// Especially beneficial when result is often available synchronously.
    /// </summary>
    async ValueTask<int> UsingValueTaskForPerformance()
    {
        await Task.Delay(100);
        return 10;
    }

    /// <summary>
    /// Demonstrates cancellation token usage in async methods.
    /// CancellationToken enables cooperative cancellation of async operations.
    /// Essential for responsive cancellation in long-running async work.
    /// </summary>
    async Task CancellationTokenInAsync()
    {
        var cts = new CancellationTokenSource();
        await Task.Delay(1000, cts.Token);
    }

    /// <summary>
    /// Shows async enumerable for streaming asynchronous data.
    /// IAsyncEnumerable enables async iteration over data sequences.
    /// Ideal for processing streaming data or paged API results.
    /// </summary>
    async IAsyncEnumerable<int> GetNumbersAsync()
    {
        for (int i = 0; i < 5; i++)
        {
            await Task.Delay(100);
            yield return i;
        }
    }

    /// <summary>
    /// Demonstrates thread-local Random instance for thread safety.
    /// ThreadLocal<T> provides per-thread storage without static issues.
    /// Random class is not thread-safe, requiring thread-local instances.
    /// </summary>
    void ThreadSafeRandom()
    {
        ThreadLocal<Random> rnd = new ThreadLocal<Random>(() => new Random());
        Console.WriteLine(rnd.Value.Next());
    }

    /// <summary>
    /// Shows basic lock statement for mutual exclusion.
    /// Lock provides exclusive access to shared resources.
    /// Syntactic sugar over Monitor.Enter/Exit with exception safety.
    /// </summary>
    Task SimpleLock()
    {
        object _lock = new object();
        lock (_lock)
        {
            // Critical section
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Demonstrates explicit Monitor usage for advanced locking scenarios.
    /// Monitor.Enter/Exit provides lower-level lock control.
    /// Allows timeout specifications and advanced synchronization patterns.
    /// </summary>
    void MonitorEnterExit()
    {
        object _lock = new object();

        Monitor.Enter(_lock);
        try
        {
            /* Critical section */
        }
        finally
        {
            Monitor.Exit(_lock);
        }
    }

    /// <summary>
    /// Shows non-blocking lock acquisition with timeout.
    /// TryEnter attempts lock acquisition without indefinite blocking.
    /// Essential for avoiding deadlocks and implementing timeout logic.
    /// </summary>
    void MonitorTryEnter()
    {
        object _lock = new object();

        if (Monitor.TryEnter(_lock, 1000))
        {
            try
            {
                /* Critical section */
            }
            finally
            {
                Monitor.Exit(_lock);
            }
        }
    }

    /// <summary>
    /// Demonstrates reader-writer locking for performance optimization.
    /// ReaderWriterLockSlim allows concurrent readers or exclusive writers.
    /// Improves performance in read-heavy, infrequent-write scenarios.
    /// </summary>
    void ReaderWriterLockSlim()
    {
        ReaderWriterLockSlim rw = new ReaderWriterLockSlim();
        rw.EnterReadLock();
        try
        {
            /* Read */
        }
        finally
        {
            rw.ExitReadLock();
        }
    }

    /// <summary>
    /// Shows upgradeable read lock for conditional write scenarios.
    /// UpgradeableReadLock allows upgrading to write lock when needed.
    /// Prevents common reader-writer deadlock scenarios.
    /// </summary>
    void UpgradeableReadLock()
    {
        ReaderWriterLockSlim rw = new ReaderWriterLockSlim();
        rw.EnterUpgradeableReadLock();
        try
        {
            rw.EnterWriteLock();
            rw.ExitWriteLock();
        }
        finally
        {
            rw.ExitUpgradeableReadLock();
        }
    }

    /// <summary>
    /// Demonstrates lock-free programming using atomic operations.
    /// Exchange atomically sets value and returns original.
    /// Eliminates locking overhead in simple update scenarios.
    /// </summary>
    void LockFreeProgrammingWithInterlocked()
    {
        int value = 0;
        Interlocked.Exchange(ref value, 5);
    }

    /// <summary>
    /// Shows Mutex usage for process-wide synchronization.
    /// Named mutexes provide synchronization across application instances.
    /// Essential for single-instance applications and shared resources.
    /// </summary>
    void Mutex()
    {
        //named
        //Mutex m = new Mutex(false, "MyAppMutex");

        using (Mutex m = new Mutex(false, "GlobalMutex"))
        {
            m.WaitOne();
            // Critical section
            m.ReleaseMutex();
        }
    }

    /// <summary>
    /// Demonstrates SpinLock for short critical sections.
    /// SpinLock busy-waits instead of blocking for very short waits.
    /// More efficient than traditional locks for brief operations.
    /// </summary>
    void SpinLock()
    {
        SpinLock spin = new SpinLock();
        bool lockTaken = false;
        try
        {
            spin.Enter(ref lockTaken);
        }
        finally
        {
            if (lockTaken) spin.Exit();
        }
    }

    /// <summary>
    /// Shows double-checked locking pattern for lazy singleton initialization.
    /// Reduces lock contention by checking condition before acquiring lock.
    /// Classic optimization pattern but error-prone without proper implementation.
    /// </summary>
    void DoubleCheckedLocking()
    {
        // object _lock = new object();
        //
        // if(instance==null) {
        //     lock(_lock) {
        //         if(instance==null)
        //             instance = new MyClass();
        //     }
    }

    /// <summary>
    /// Demonstrates potential deadlock scenario with nested locking.
    /// Multiple locks acquired in different order can cause deadlocks.
    /// Always acquire locks in consistent order to prevent deadlocks.
    /// </summary>
    void CauseDeadlocks()
    {
        object obj1 = new object();
        object obj2 = new object();

        lock (obj1)
        {
            lock (obj2)
            {
            }
        } // Can deadlock if another thread locks in reverse order
    }

    /// <summary>
    /// Shows Monitor.Wait/Pulse for thread coordination.
    /// Wait releases lock and blocks until Pulse notification.
    /// Classical producer-consumer synchronization mechanism.
    /// </summary>
    void MonitorWaitPulse()
    {
        object _lock = new object();

        lock (_lock)
        {
            Monitor.Wait(_lock); // Wait for signal
            Monitor.Pulse(_lock); // Signal waiting thread
        }
    }

    /// <summary>
    /// Demonstrates thread-safe FIFO queue operations.
    /// ConcurrentQueue provides lock-free enqueue/dequeue operations.
    /// Ideal for producer-consumer scenarios with ordered processing.
    /// </summary>
    void ConcurrentQueue()
    {
        ConcurrentQueue<int> q = new ConcurrentQueue<int>();
        q.Enqueue(1);
        q.TryDequeue(out int val);
    }

    /// <summary>
    /// Shows thread-safe LIFO stack operations.
    /// ConcurrentStack provides lock-free push/pop operations.
    /// Useful for work-stealing algorithms and temporary storage.
    /// </summary>
    void ConcurrentStack()
    {
        ConcurrentStack<int> stack = new ConcurrentStack<int>();
        stack.Push(1);
        stack.TryPop(out int val);
    }

    /// <summary>
    /// Demonstrates unordered thread-safe collection.
    /// ConcurrentBag optimized for scenarios where same thread produces and consumes.
    /// Provides excellent performance for work-stealing patterns.
    /// </summary>
    void ConcurrentBag()
    {
        ConcurrentBag<int> bag = new ConcurrentBag<int>();
        bag.Add(1);
        bag.TryTake(out int val);
    }

    /// <summary>
    /// Shows thread-safe dictionary with atomic operations.
    /// ConcurrentDictionary provides lock-free key-value operations.
    /// TryUpdate ensures atomic conditional updates.
    /// </summary>
    void ConcurrentDictionary()
    {
        ConcurrentDictionary<int, string> dict = new ConcurrentDictionary<int, string>();
        dict.TryAdd(1, "A");
        dict.TryUpdate(1, "B", "A");
    }

    /// <summary>
    /// Demonstrates blocking collection for producer-consumer patterns.
    /// BlockingCollection provides blocking enumeration with completion signaling.
    /// Essential for bounded producer-consumer scenarios.
    /// </summary>
    void BlockingCollection()
    {
        BlockingCollection<int> bc = new BlockingCollection<int>();
        Task.Run(() =>
        {
            foreach (var item in bc.GetConsumingEnumerable())
                Console.WriteLine(item);
        });
        bc.Add(1);
        bc.CompleteAdding();
    }

    /// <summary>
    /// Shows immutable collections for thread-safe shared state.
    /// Immutable collections eliminate need for synchronization.
    /// Changes create new instances, preserving thread safety.
    /// </summary>
    void ImmutableCollections()
    {
        var list = ImmutableList<int>.Empty.Add(1);
    }

    /// <summary>
    /// Demonstrates custom partitioning for Parallel.ForEach optimization.
    /// Partitioner.Create provides custom work distribution strategies.
    /// Improves load balancing and reduces overhead.
    /// </summary>
    void PartitionersForParallelForEach()
    {
        var rangePartitioner = Partitioner.Create(0, 1000);
        Parallel.ForEach(rangePartitioner, (range) => { });
    }

    /// <summary>
    /// Shows practical concurrent queue usage in parallel scenarios.
    /// Combines parallel execution with thread-safe collection operations.
    /// Demonstrates real-world concurrent programming patterns.
    /// </summary>
    void ThreadSafeQueueExample()
    {
        ConcurrentQueue<int> queue = new ConcurrentQueue<int>();
        Parallel.For(0, 1000, i => queue.Enqueue(i));
    }

    // Additional methods for missing topics:

    /// <summary>
    /// Demonstrates producer-consumer pattern using BlockingCollection.
    /// Producer adds items while consumer processes them concurrently.
    /// Essential pattern for decoupling production from consumption.
    /// </summary>
    void ProducerConsumerPattern()
    {
        var bc = new BlockingCollection<int>(boundedCapacity: 100);
        
        // Producer
        Task.Run(() =>
        {
            for (int i = 0; i < 10; i++)
            {
                bc.Add(i);
                Thread.Sleep(100);
            }
            bc.CompleteAdding();
        });

        // Consumer
        Task.Run(() =>
        {
            foreach (var item in bc.GetConsumingEnumerable())
            {
                Console.WriteLine($"Consumed: {item}");
            }
        });
    }

    /// <summary>
    /// Shows automatic cancellation after specified timeout.
    /// CancelAfter provides time-based cancellation without manual timer.
    /// Useful for implementing operation timeouts automatically.
    /// </summary>
    void AutoCancellationWithTimeout()
    {
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(5)); // auto-cancel after 5s
        
        Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                await Task.Delay(100, cts.Token);
            }
        }, cts.Token);
    }

    /// <summary>
    /// Demonstrates limiting parallel execution to prevent resource exhaustion.
    /// MaxDegreeOfParallelism controls maximum concurrent thread usage.
    /// Essential for managing system resource consumption.
    /// </summary>
    void LimitParallelism()
    {
        ParallelOptions po = new ParallelOptions { MaxDegreeOfParallelism = 4 };
        Parallel.For(0, 100, po, i => 
        {
            Console.WriteLine($"Processing {i} on thread {Thread.CurrentThread.ManagedThreadId}");
            Thread.Sleep(100);
        });
    }

    /// <summary>
    /// Shows ThreadLocal storage for per-thread data isolation.
    /// ThreadLocal provides thread-specific instances without static limitations.
    /// Eliminates race conditions in thread-specific state management.
    /// </summary>
    void ThreadLocalStorage()
    {
        ThreadLocal<int> localData = new ThreadLocal<int>(() => Thread.CurrentThread.ManagedThreadId);
        
        Parallel.For(0, 5, i =>
        {
            Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: {localData.Value}");
        });
        
        localData.Dispose(); // Important to dispose ThreadLocal
    }

    /// <summary>
    /// Demonstrates async work offloading to prevent UI thread blocking.
    /// Task.Run moves CPU-intensive work to background threads.
    /// Critical pattern for maintaining responsive user interfaces.
    /// </summary>
    async Task AvoidBlockingUIThread()
    {
        // Instead of blocking UI thread with CPU work
        var result = await Task.Run(() =>
        {
            // CPU-intensive work
            return ExpensiveComputation();
        });
        
        // Update UI with result
        Console.WriteLine($"Result: {result}");
    }

    /// <summary>
    /// Helper method simulating expensive computation.
    /// Represents CPU-bound work that should run on background thread.
    /// Demonstrates work suitable for Task.Run offloading.
    /// </summary>
    int ExpensiveComputation()
    {
        Thread.Sleep(2000); // Simulate heavy computation
        return 42;
    }

    /// <summary>
    /// Shows SynchronizationContext usage for UI thread marshalling.
    /// SynchronizationContext.Post schedules work on original context.
    /// Essential for updating UI from background threads safely.
    /// </summary>
    void UseSynchronizationContext()
    {
        var context = SynchronizationContext.Current;
        
        Task.Run(() =>
        {
            // Background work
            var result = "Background work completed";
            
            // Marshal back to UI thread
            context?.Post(_ =>
            {
                // Update UI here
                Console.WriteLine($"UI Update: {result}");
            }, null);
        });
    }

    /// <summary>
    /// Demonstrates keeping critical sections minimal for performance.
    /// Short lock duration reduces contention and improves throughput.
    /// Shows preparation outside lock and minimal work inside.
    /// </summary>
    void AvoidLockContention()
    {
        object _lock = new object();
        var data = new List<string>();
        
        // Prepare work outside lock
        string newItem = "Prepared item";
        
        // Keep critical section short
        lock (_lock)
        {
            data.Add(newItem); // Minimal work in lock
        }
    }

    /// <summary>
    /// Shows thread-safe lazy singleton implementation pattern.
    /// Lazy<T> with thread-safety ensures single instance creation.
    /// Eliminates double-checked locking complexity and errors.
    /// </summary>
    void ThreadSafeLazySingleton()
    {
        // Thread-safe singleton using Lazy<T>
        var instance = MySingleton.Instance;
        Console.WriteLine($"Singleton ID: {instance.Id}");
    }

    /// <summary>
    /// Demonstrates exception handling in parallel operations.
    /// Parallel operations can throw AggregateException with multiple inner exceptions.
    /// Proper handling prevents silent failures in parallel code.
    /// </summary>
    void ExceptionHandlingInParallel()
    {
        try
        {
            Parallel.For(0, 10, i =>
            {
                if (i == 5) throw new InvalidOperationException($"Error at {i}");
                Console.WriteLine(i);
            });
        }
        catch (AggregateException ae)
        {
            foreach (var ex in ae.InnerExceptions)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Shows structured concurrency with linked cancellation tokens.
    /// CreateLinkedTokenSource combines multiple cancellation sources.
    /// Enables hierarchical cancellation in complex async operations.
    /// </summary>
    async Task LinkedCancellationTokens()
    {
        var parentCts = new CancellationTokenSource();
        var childCts = new CancellationTokenSource();
        
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            parentCts.Token, childCts.Token);
        
        try
        {
            await Task.Delay(5000, linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Operation was cancelled");
        }
    }

    /// <summary>
    /// Demonstrates async enumerable consumption with cancellation.
    /// Combines IAsyncEnumerable with CancellationToken for responsive iteration.
    /// Shows modern async streaming pattern with cancellation support.
    /// </summary>
    async Task ConsumeAsyncEnumerable()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        
        await foreach (var number in GetNumbersAsync().WithCancellation(cts.Token))
        {
            Console.WriteLine($"Received: {number}");
        }
    }

    /// <summary>
    /// Shows custom TaskScheduler for specialized execution contexts.
    /// TaskScheduler controls where and how tasks execute.
    /// Useful for UI thread scheduling or custom execution policies.
    /// </summary>
    void CustomTaskScheduler()
    {
        var task = Task.Factory.StartNew(() =>
        {
            Console.WriteLine($"Running on thread: {Thread.CurrentThread.ManagedThreadId}");
        }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
        
        task.Wait();
    }

    /// <summary>
    /// Demonstrates safe async disposal pattern with IAsyncDisposable.
    /// DisposeAsync enables proper cleanup of async resources.
    /// Critical for preventing resource leaks in async scenarios.
    /// </summary>
    async Task AsyncDisposablePattern()
    {
        await using var resource = new AsyncResource();
        // Use resource
        await resource.DoWorkAsync();
        // Automatic async disposal
    }

    /// <summary>
    /// Shows volatile keyword for preventing compiler optimizations.
    /// Volatile ensures reads/writes aren't optimized away or reordered.
    /// Important for lock-free algorithms and memory barriers.
    /// </summary>
    void VolatileKeyword()
    {
        // This would typically be a field
        volatile bool isComplete = false;
        
        Task.Run(() =>
        {
            Thread.Sleep(1000);
            isComplete = true; // Volatile write
        });
        
        // Volatile read in loop
        while (!isComplete)
        {
            Thread.Sleep(10);
        }
    }

    /// <summary>
    /// Demonstrates memory barriers for advanced synchronization scenarios.
    /// Thread.MemoryBarrier prevents instruction reordering across barrier.
    /// Low-level synchronization primitive for lock-free algorithms.
    /// </summary>
    void MemoryBarriers()
    {
        int data = 0;
        bool ready = false;
        
        Task.Run(() =>
        {
            data = 42;
            Thread.MemoryBarrier(); // Ensure data write completes before ready
            ready = true;
        });
        
        while (!ready)
        {
            Thread.Sleep(1);
        }
        Thread.MemoryBarrier(); // Ensure ready read completes before data read
        Console.WriteLine(data);
    }

    /// <summary>
    /// Shows AsyncLocal for async context flow across await boundaries.
    /// AsyncLocal maintains context through async call chains.
    /// Useful for correlation IDs, security context, or tracing data.
    /// </summary>
    async Task AsyncLocalContext()
    {
        var asyncLocal = new AsyncLocal<string>();
        asyncLocal.Value = "Context Data";
        
        await Task.Run(async () =>
        {
            Console.WriteLine($"Context: {asyncLocal.Value}"); // Still available
            await Task.Delay(100);
            Console.WriteLine($"After delay: {asyncLocal.Value}"); // Still preserved
        });
    }
}

/// <summary>
/// Thread-safe singleton implementation using Lazy<T>.
/// Demonstrates proper singleton pattern with thread safety.
/// Eliminates double-checked locking complexity.
/// </summary>
public class MySingleton
{
    private static readonly Lazy<MySingleton> _instance = 
        new Lazy<MySingleton>(() => new MySingleton());
    
    public static MySingleton Instance => _instance.Value;
    public string Id { get; } = Guid.NewGuid().ToString();
    
    private MySingleton() { } // Private constructor
}

/// <summary>
/// Example async disposable resource for demonstrating async disposal.
/// Shows proper implementation of IAsyncDisposable pattern.
/// Critical for resources requiring async cleanup operations.
/// </summary>
public class AsyncResource : IAsyncDisposable
{
    public async Task DoWorkAsync()
    {
        await Task.Delay(100);
        Console.WriteLine("Async work completed");
    }
    
    public async ValueTask DisposeAsync()
    {
        await Task.Delay(50); // Simulate async cleanup
        Console.WriteLine("Async resource disposed");
    }
}

/// <summary>
/// Placeholder class for demonstrations.
/// Used in various examples throughout the concurrency learning module.
/// Represents typical business objects in real applications.
/// </summary>
internal class MyClass
{
    public string Name { get; set; } = "Example";
}