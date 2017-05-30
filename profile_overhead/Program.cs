using System;
using System.Diagnostics;
using System.Threading;
using EventPipe;

namespace profile_overhead
{
    class Program
    {
        private static int NumThreads = 20;

        static void Main(string[] args)
        {
            // Setup worker args.
            WorkerArgs workerArgs = new WorkerArgs(TimeSpan.FromMilliseconds(10));

            // Warm-up.
            Console.WriteLine("Start: Warm-up");
            TimeSpan warmup = Run(false, workerArgs);
            Console.WriteLine("\tElapsed time: {0} milliseconds", warmup.TotalMilliseconds);
            Console.WriteLine("Stop: Warm-up");

            int index = 0;
            while(true)
            {
                bool runWithout = false;
                bool runWith = false;

                if(index % 2 == 0)
                {
                    goto trace;
                }

noTrace:
            // Run without tracing enabled.
            Console.WriteLine("Start: EnableTracing = false");
            TimeSpan withoutTracing = Run(false, workerArgs);
            Console.WriteLine("\tElapsed time: {0} milliseconds", withoutTracing.TotalMilliseconds);
            Console.WriteLine("Stop: EnableTracing = false");
            runWithout = true;

            if(runWith)
            {
                goto print;
            }

trace:
            // Run with tracing enabled.
            Console.WriteLine("Start: EnableTracing = true");
            TimeSpan withTracing = Run(true, workerArgs);
            Console.WriteLine("\tElapsed time: {0} milliseconds", withTracing.TotalMilliseconds);
            Console.WriteLine("Stop: EnableTracing = true");
            runWith = true;

            if(!runWithout)
            {
                goto noTrace;
            }

print:
            // Print the overhead percentage.
            double msWithTracing = withTracing.TotalMilliseconds;
            double msWithoutTracing = withoutTracing.TotalMilliseconds;
            double overhead = (msWithTracing - msWithoutTracing) / msWithoutTracing;
            double percentage = overhead * 100;

            Console.WriteLine("\nOverhead = {0:F4} = {1:F4}%", overhead, percentage);
            Console.WriteLine("\n");
            index++;
            }

        }

        static TimeSpan Run(bool enableTracing, WorkerArgs workerArgs)
        {
            // Create threads.
            Thread[] workers = new Thread[NumThreads];
            for(int i=0; i<NumThreads; i++)
            {
                workers[i] = new Thread(WorkerThreadProc);
            }

            // Start tracing.
            if(enableTracing)
            {
                TraceControl.EnableDefault(TimeSpan.FromMilliseconds(1));
            }

            // Start time measurement.
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            // Start threads.
            for(int i=0; i<NumThreads; i++)
            {
                workers[i].Start(workerArgs);

                // Insert some delay between threads so that they don't all try to take the CPU at the same time.
                Thread.Sleep(1);
            }

            // Wait for threads to run to completion.
            for(int i=0; i<NumThreads; i++)
            {
                workers[i].Join();
            }

            // Stop time measurement.
            stopWatch.Stop();

            // Stop tracing.
            if(enableTracing)
            {
                Console.WriteLine("\tStart: Rundown/Write");
                TraceControl.Disable();
                Console.WriteLine("\tStop: Rundown/Write");
            }

            // Perform a full blocking GC to allow threads to be cleaned up.
            workers = null;
            GC.Collect(2, GCCollectionMode.Forced);

            return stopWatch.Elapsed;
        }

        private static void WorkerThreadProc(object data)
        {
            WorkerArgs workerArgs = (WorkerArgs)data;

            for(int i=0; i<1000; i++)
            {
                for(int j=0; j<100; j++)
                {
                    GC.KeepAlive(new object());
                }

                Thread.Sleep((int)workerArgs.SleepTime.TotalMilliseconds);
            }
        }
    }

    public sealed class WorkerArgs
    {
        public WorkerArgs(TimeSpan sleepTime)
        {
            SleepTime = sleepTime;
        }

        public TimeSpan SleepTime
        {
            get;
            set;
        }
    }
}
