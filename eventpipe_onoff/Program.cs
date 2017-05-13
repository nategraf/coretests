using System;
using System.Reflection;
using System.Threading.Tasks;

namespace eventpipe_onoff
{
    class Program
    {
        static void Main(string[] args)
        {
            Assembly SPC = typeof(System.Diagnostics.Tracing.EventSource).Assembly;
            if(SPC == null)
            {
                Console.WriteLine("System.Private.CoreLib assembly == null");
                return;
            }
            Type eventPipeType = SPC.GetType("System.Diagnostics.Tracing.EventPipe");
            if(eventPipeType == null)
            {
                Console.WriteLine("System.Diagnostics.Tracing.EventPipe type == null");
                return;
            }
            MethodInfo enableMethod = eventPipeType.GetMethod("Enable", BindingFlags.NonPublic | BindingFlags.Static);
            if(enableMethod == null)
            {
                Console.WriteLine("EventPipe.Enable method == null");
                return;
            }
            MethodInfo disableMethod = eventPipeType.GetMethod("Disable", BindingFlags.NonPublic | BindingFlags.Static);
            if(disableMethod == null)
            {
                Console.WriteLine("EventPipe.Disable method == null");
                return;
            }

            // Start the allocator thread.
            Task t = new Task(new Action(Allocator));
            t.Start();

            int iteration = 1;
            while(true)
            {
                string outputFile = string.Format("/home/brianrob/cli/config_api/file-{0}.netperf", iteration);
                object configurationObject = CreateConfiguration(SPC, outputFile);
                if(configurationObject == null)
                {
                    Console.WriteLine("configurationObject == null");
                    return;
                }

                Console.WriteLine("Iteration {0}:", iteration++);
                Console.WriteLine("\tStart: Enable tracing.");
                enableMethod.Invoke(null, new object[] { configurationObject });
                Console.WriteLine("\tEnd: Enable tracing.\n");

                Console.WriteLine("\tStart: Allocation.");
                // Allocate for 1000000 iterations.
                for(int i=0; i<1000000; i++)
                {
                    GC.KeepAlive(new object());
                }
                Console.WriteLine("\tEnd: Allocation.\n");

                Console.WriteLine("\tStart: Disable tracing.");
                disableMethod.Invoke(null, null);
                Console.WriteLine("\tEnd: Disable tracing.\n");

                System.IO.File.Delete(outputFile);
            }
        }

        private static void Allocator()
        {
            while(true)
            {
                GC.KeepAlive(new object());
            }
        }

        private static object CreateConfiguration(Assembly SPC, string outputFile)
        {
            // Get the EventPipeConfiguration type.
            Type configurationType = SPC.GetType("System.Diagnostics.Tracing.EventPipeConfiguration");
            if(configurationType == null)
            {
                Console.WriteLine("configurationType == null");
                return null;
            }

            // Get the EventPipeConfiguration ctor.
            ConstructorInfo configurationCtor = configurationType.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new Type[] { typeof(string), typeof(uint) },
                null);
            if(configurationCtor == null)
            {
                Console.WriteLine("configurationCtor == null");
                return null;
            }

            // Setup the configuration values.
            uint circularBufferMB = 1024; // 1 GB
            uint level = 5; // Verbose

            // Create a new instance of EventPipeConfiguration.
            object config = configurationCtor.Invoke(new object[] { outputFile, circularBufferMB });
            if(config == null)
            {
                Console.WriteLine("config == null.");
                return null;
            }

            // Get the enable provider method.
            MethodInfo enableProviderMethod = configurationType.GetMethod(
                "EnableProvider",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if(enableProviderMethod == null)
            {
                Console.WriteLine("enableProviderMethod == null");
                return null;
            }

            // Setup the provider values.
            // Public provider.
            string providerName = "e13c0d23-ccbc-4e12-931b-d9cc2eee27e4";
            UInt64 keywords = 0x4c14fccbd;

            // Enable the provider.
            enableProviderMethod.Invoke(config, new object[] { providerName, keywords, level });

            // Private provider.
            providerName = "763fd754-7086-4dfe-95eb-c01a46faf4ca";
            keywords = 0x4002000b;

            // Enable the provider.
            enableProviderMethod.Invoke(config, new object[] { providerName, keywords, level });

            // Sample profiler.
            providerName = "3c530d44-97ae-513a-1e6d-783e8f8e03a9";
            keywords = 0x0;

            // Enable the provider.
            enableProviderMethod.Invoke(config, new object[] { providerName, keywords, level });

            return config;
        }
    }
}
