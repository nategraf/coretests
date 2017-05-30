using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Diagnostics;

namespace eventsource_test
{
    class Program
    {
        const int RecursionDepth = 3;

        static void Main(string[] args)
        {
            Console.WriteLine("Press enter to continue.");
            Console.ReadKey();

            // Create a new listener.
            TestEventListener listener = new TestEventListener();
            listener.EnableEvents(TestEventSource.Log, EventLevel.Verbose);

            // Create activities and write events.
            Recurse(0);

            // Disable the listener and write another event.
            listener.DisableEvents(TestEventSource.Log);

            // Write a couple of events outside of an activity.
            TestEventSource.Log.TestEvent(5, "Test String 5");
            TestEventSource.Log.TestEvent(6, "Test String 6");

            // Test dispose.
            listener.Dispose();
            TestEventSource.Log.Dispose();
        }

        private static void Recurse(int currentDepth)
        {
            if(currentDepth > RecursionDepth)
            {
                return;
            }

            // Start a new operation.
            Console.WriteLine("\tStartOperation - Depth = {0}", currentDepth);
            TestEventSource.Log.OperationStart();

            // Write test events to the event source.
            TestEventSource.Log.TestEvent(1, "Test String");
            TestEventSource.Log.TestEvent(2, "Test String 2");

            // Recurse.
            Recurse(currentDepth + 1);

            // Stop the operation.
            Console.WriteLine("\tStopOperation - Depth = {0}", currentDepth);
            TestEventSource.Log.OperationStop();

            // Write test events to the event source.
            TestEventSource.Log.TestEvent(3, "Test String 3");
            TestEventSource.Log.TestEvent(4, "Test String 4");
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

        [Event(2, ActivityOptions = EventActivityOptions.Recursive)]
        public void OperationStart()
        {
            WriteEvent(2);
        }

        [Event(3, ActivityOptions = EventActivityOptions.Recursive)]
        public void OperationStop()
        {
            WriteEvent(3);
        }
    }

    public class TestEventListener : EventListener
    {
        private Stack<Guid> m_activityIdStack = new Stack<Guid>();

        public TestEventListener()
        {
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            Console.WriteLine("\t\tOnEventWritten:");
            Console.WriteLine("\t\t\tEventName: {0}", eventData.EventName);
            Console.WriteLine("\t\t\tActivityId: {0}", eventData.ActivityId);
            Console.WriteLine("\t\t\tRelatedActivityId: {0}", eventData.RelatedActivityId);

            for(int i=0; i<eventData.PayloadNames.Count; i++)
            {
                Console.WriteLine("\t\t\t{0}: {1}", eventData.PayloadNames[i], eventData.Payload[i]);
            }
            Console.WriteLine();

            if (eventData.EventName.Equals("OperationStart"))
            {
                m_activityIdStack.Push(eventData.ActivityId);
                ValidateStartActivity(eventData.ActivityId, eventData.RelatedActivityId);
            }
            else if (eventData.EventName.Equals("OperationStop"))
            {
                m_activityIdStack.Pop();
                ValidateActivityId(eventData.ActivityId);
            }
            else
            {
                ValidateActivityId(eventData.ActivityId);
            }


        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            Console.WriteLine("OnEventSourceCreated: {0}", eventSource.GetType().FullName);
        }

        private void ValidateStartActivity(Guid activityId, Guid relatedActivityId)
        {
            // Validate the activity id.
            ValidateActivityId(activityId);

            // Validate the related activity id.
            // NOTE: This is not thread safe (but neither is this test).
            Guid savedActivityId = m_activityIdStack.Pop();
            ValidateActivityId(relatedActivityId);
            m_activityIdStack.Push(savedActivityId);
        }

        private void ValidateActivityId(Guid activityId)
        {
            if(m_activityIdStack.Count == 0)
            {
                Debug.Assert(activityId == Guid.Empty);
            }
            else
            {
                Debug.Assert(m_activityIdStack.Peek() == activityId);
            }
        }
    }
}
