using System;

namespace hello_world
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Start: Allocation");
            DateTime endTime = DateTime.Now + TimeSpan.FromSeconds(10);
            while(DateTime.Now < endTime)
            {
                for(int i=0; i<1000000; i++)
                {
                    GC.KeepAlive(new object());
                }
            }
            Console.WriteLine("Stop: Allocation");
        }
    }
}
