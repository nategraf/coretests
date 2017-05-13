using System;
using System.Threading.Tasks;

namespace ConsoleApplication
{
    public class Program
    {
        static int NumThreads = 2;

        public static void Main(string[] args)
        {
            if(args.Length >= 1)
            {
                NumThreads = Convert.ToInt32(args[0]);
            }
            Console.WriteLine($"Starting {NumThreads} threads.");

            Task[] workers = new Task[NumThreads];
            for(int i=0; i<NumThreads; i++)
            {
                workers[i] = new Task(() => AllocTest.Allocator());
                workers[i].Start();
            }

            Console.WriteLine("Waiting for all workers to complete.");
            Task.WaitAll(workers);
        }
    }

    public static class AllocTest
    {
        public static void Allocator()
        {
            Console.WriteLine($"Started new thread with id {System.Threading.Thread.CurrentThread.ManagedThreadId}.");

            while(true)
            {
                for(int i=0; i<10000; i++)
                {
                    object o = new object();
                    GC.KeepAlive(o);
                }
                System.Threading.Thread.Sleep(0);
            }
        }
    }
}

