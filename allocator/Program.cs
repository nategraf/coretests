using System;
using System.Threading.Tasks;

namespace ConsoleApplication
{
    public class Program
    {
        const int NumThreads = 2;

        public static void Main(string[] args)
        {
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
                object o = new object();
                GC.KeepAlive(o);
            }
        }
    }
}

