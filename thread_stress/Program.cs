using System;
using System.Threading;

namespace thread_stress
{
    class Program
    {
        static void Main(string[] args)
        {
            int count = 1;
            while(true)
            {
                if(count % 100 == 0)
                {
                    GC.Collect();
                }

                Thread t = new Thread(new ThreadStart(Worker));
                t.Start();
                t.Join();
            }
        }

        private static void Worker()
        {
            Console.WriteLine("Thread ID: {0}", Thread.CurrentThread.ManagedThreadId);
        }
    }
}
