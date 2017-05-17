using System;
using System.Threading;
using System.Threading.Tasks;
using EventPipe;

namespace task_stress
{
    class Program
    {
        static void Main(string[] args)
        {
            int numTasks = 10;
            if(args.Length > 0)
            {
                numTasks = Convert.ToInt32(args[0]);
            }
            int currentIteration = 1;
            while(true)
            {
                Console.WriteLine("Running iteration {0}", currentIteration++);

                // Start tracing.
                TraceControl.EnableDefault();

                try
                {
                // Start tasks
                Task[] tasks = GenerateTasks(numTasks);
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
                    break;
                }

                // Stop tracing.
                TraceControl.Disable();
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
            new Action(Lock)
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

        private static object s_lock = new object();
        public static void Lock()
        {
            lock(s_lock)
            {
                Allocate();
            }
        }
    }
}
