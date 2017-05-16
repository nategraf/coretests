using System;
using System.Diagnostics.Tracing;

namespace eventsource_test
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Press enter to continue.");
            Console.ReadKey();

            // Create a new listener.
            TestEventListener listener = new TestEventListener();
            listener.EnableEvents(TestEventSource.Log, EventLevel.Verbose);

            // Write test events to the event source.
            Console.WriteLine("Start/WriteTestEvent\n");
            TestEventSource.Log.TestEvent(1, "Test String");
            TestEventSource.Log.TestEvent(2, "Test String 2");
            Console.WriteLine("Stop/WriteTestEvent");

            // Disable the listener and write another event.
            listener.DisableEvents(TestEventSource.Log);
            TestEventSource.Log.TestEvent(3, "Test String 3");

            // Test dispose.
            listener.Dispose();
            TestEventSource.Log.Dispose();
        }
    }

    [EventSource]
    public class TestEventSource : EventSource
    {
        public static TestEventSource Log = new TestEventSource();

        private TestEventSource() : base(true)
        {
        }

        [Event(1)]
        public void TestEvent(int intArg, string strArg)
        {
            WriteEvent(1, intArg, strArg);
        }
    }

    public class TestEventListener : EventListener
    {
        public TestEventListener()
        {
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            Console.WriteLine("OnEventWritten:");
            Console.WriteLine("\tEventName: {0}", eventData.EventName);
            for(int i=0; i<eventData.PayloadNames.Count; i++)
            {
                Console.WriteLine("\t{0}: {1}", eventData.PayloadNames[i], eventData.Payload[i]);
            }
            Console.WriteLine();
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            Console.WriteLine("OnEventSourceCreated: {0}", eventSource.GetType().FullName);
        }
    }
}
