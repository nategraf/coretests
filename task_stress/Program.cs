using System;
using System.Threading;
using System.Threading.Tasks;
using EventPipe;

namespace task_stress
{
    class Program
    {
        private static bool s_errorOccurred = false;
        private static int s_numTasks = 10;

        static void Main(string[] args)
        {
            if(args.Length > 0)
            {
                s_numTasks = Convert.ToInt32(args[0]);
            }
            int currentIteration = 1;

            Thread taskThread = new Thread(new ThreadStart(TaskThreadProc));
            taskThread.Start();

            while(!s_errorOccurred)
            {
                Console.WriteLine("Running iteration {0}", currentIteration++);

                // Start tracing.
                TraceControl.EnableDefault();

                // Sleep for 5 seconds to fill the trace buffer.
                Thread.Sleep(TimeSpan.FromSeconds(5));

                // Stop tracing.
                TraceControl.Disable();
            }
        }

        private static void TaskThreadProc()
        {
            while(!s_errorOccurred)
            {
                try
                {
                    // Start tasks
                    Task[] tasks = GenerateTasks(s_numTasks);
                    for(int i=0; i<tasks.Length; i++)
                    {
                        tasks[i].Start();
                    }

                    // Wait for completion.
                    Task.WaitAll(tasks);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    s_errorOccurred = true;
                }
            }
        }

        private static Task[] GenerateTasks(int numTasks)
        {
            Task[] tasks = new Task[numTasks];
            int numTasksPerAction = numTasks / TaskActions.Actions.Length;
            int taskIndex = 0;
            foreach(Action action in TaskActions.Actions)
            {
                for(int i=0; i<numTasksPerAction; i++)
                {
                    tasks[taskIndex++] = new Task(action);
                }
            }

            Random r = new Random();
            while(taskIndex < numTasks)
            {
                tasks[taskIndex++] = new Task(TaskActions.Actions[r.Next(TaskActions.Actions.Length)]);
            }

            return tasks;
        }
    }

    public static class TaskActions
    {
        public static Action[] Actions = new Action[]
        {
            new Action(Allocate),
            new Action(Spin),
        };

        public static void Allocate()
        {
            for(int i=0; i<100000; i++)
            {
                GC.KeepAlive(new object());
            }
        }

        public static void Spin()
        {
            SpinWait s = new SpinWait();
            for(int i=0; i<1000; i++)
            {
                s.SpinOnce();
            }
        }
    }
}
