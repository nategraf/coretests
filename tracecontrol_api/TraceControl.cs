using System;
using System.Reflection;

namespace EventPipe
{
    public static class TraceControl
    {
        private static MethodInfo m_enableMethod;
        private static MethodInfo m_disableMethod;

        public static void Enable(TraceConfiguration traceConfig)
        {
            m_enableMethod.Invoke(
                null,
                new object[]
                {
                    traceConfig.ConfigurationObject
                });
        }

        public static void Disable()
        {
            m_disableMethod.Invoke(
                null,
                null);
        }

        static TraceControl()
        {
            if(!Initialize())
            {
                throw new InvalidOperationException("Reflection failed.");
            }
        }

        private static bool Initialize()
        {
           Assembly SPC = typeof(System.Diagnostics.Tracing.EventSource).Assembly;
           if(SPC == null)
           {
               Console.WriteLine("System.Private.CoreLib assembly == null");
               return false;
           }
           Type eventPipeType = SPC.GetType("System.Diagnostics.Tracing.EventPipe");
           if(eventPipeType == null)
           {
               Console.WriteLine("System.Diagnostics.Tracing.EventPipe type == null");
               return false;
           }
           m_enableMethod = eventPipeType.GetMethod("Enable", BindingFlags.NonPublic | BindingFlags.Static);
           if(m_enableMethod == null)
           {
               Console.WriteLine("EventPipe.Enable method == null");
               return false;
           }
           m_disableMethod = eventPipeType.GetMethod("Disable", BindingFlags.NonPublic | BindingFlags.Static);
           if(m_disableMethod == null)
           {
               Console.WriteLine("EventPipe.Disable method == null");
               return false;
           }

           return true;
        }

    }
}
