using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;
using System.IO;

namespace PGO_BinarySearch
{
    class Program
    {
        public const string CleanReproPath = @"c:\work\clean_repro";
        public const string WritableReproPath = @"c:\work\repro_results";
        public const string TestBedPath = @"";
        public const string DebugLogFileName = "_DebugLog.txt";
        public const string RunParamsFileName = "_RunParams.txt";
        public const string SuccessMarkerFileName = "_Result_Success.txt";
        public const string FailMarkerFileName = "_Result_Fail.txt";
        public static int CurrentRunIndex = 0;
        public static string CurrentReproDirectory = null;

        static void Main(string[] args)
        {
            // Build the module list.
            Log("Start: Build Module List");
            string[] modules = Directory.GetFiles(CleanReproPath, "*.obj");
            ModuleList moduleList = new ModuleList(modules);
            Log("End: Build Module List");

            // Find the latest run.
            CurrentRunIndex = GetLastRunIndex() + 1;
            Console.WriteLine("Set current run index.");

            // If last run index != 1, get the serialized args.
            if(CurrentRunIndex > 1)
            {
                string lastReproDirectory = Path.Combine(WritableReproPath, (CurrentRunIndex - 1).ToString());
                bool success = false;
                if(Directory.Exists(lastReproDirectory))
                {
                    string runParamsPath = Path.Combine(lastReproDirectory, RunParamsFileName);
                    if(File.Exists(runParamsPath))
                    {
                        moduleList.InitializeStartPointFrom(runParamsPath);
                    }
                }
                if(!success)
                {
                    throw new Exception("Unable to restart from previous run.");
                }
            }

            while (true)
            {
                // Make a copy of the repro and give it a unique name (monotonically increasing).
                CurrentReproDirectory = Path.Combine(WritableReproPath, CurrentRunIndex.ToString());
                Log("Start: Create new repro directory '{0}'.", CurrentReproDirectory);
                foreach (string srcFile in Directory.GetFiles(CleanReproPath))
                {
                    File.Copy(srcFile, Path.Combine(CurrentReproDirectory, Path.GetFileName(srcFile)));
                }
                Log("End: Create new repro directory '{0}'.", CurrentReproDirectory);

                // Dump the run params.
                string runParamsPath = Path.Combine(WritableReproPath, RunParamsFileName);
                Log("Start: Save run parameters.");
                moduleList.WriteState(runParamsPath);
                Log("End: Save run parameters.");

                // Dump the list of files that were compiled.
                string moduleListLogPath = Path.Combine(WritableReproPath, DebugLogFileName);
                Log("Start: Write module list to '{0}'.", moduleListLogPath);
                moduleList.WriteDebugLog(moduleListLogPath);
                Log("End: Write module list to '{0}'.", moduleListLogPath);

                // Decide which files to compile using LTCG.
                IEnumerable<string> ltcgFiles = moduleList.LTCGModules;

                // Compile the LTCG set.
                Log("Start: Compile LTCG modules.");
                foreach(string ltcgFile in ltcgFiles)
                {
                    CompileWithLTCG(ltcgFile);
                }
                Log("End: Compile LTCG modules.");

                // Compile and Link the rest of the binary with PGO.
                Log("Start: Compile the remaining modules with PGO.");
                ProcessStartInfo linkStartInfo = new ProcessStartInfo("link.exe")
                {
                    Arguments = @"@link.rsp /UseProfile:PGD=coreclr.pgd",
                    WorkingDirectory = WritableReproPath
                };
                Execute(linkStartInfo);
                Log("End: Compile the remaining modules with PGO.");

                // Copy coreclr.dll into the test bed.
                Log("Start: Update coreclr.dll in testbed.");
                File.Copy(Path.Combine(WritableReproPath, "coreclr.dll"), Path.Combine(TestBedPath, "coreclr.dll"));
                Log("End: Update coreclr.dll in testbed.");

                // Run the test two times - if neither fails, consider it a pass.
                Log("Start: Execute tests.");
                bool testsPassed = ExecuteTests();
                Log("End: Execute tests.");

                // Write the result marker file.
                Log("Start: Write marker file.");
                WriteMarker(testsPassed);
                Log("End: Write marker file.");

                // Restart the loop, moving the line between LTCG and PGO until we only have 1 obj file in LTCG and it doesn't fail.
                if(testsPassed)
                {
                    Log("Mark success.");
                    moduleList.MarkPass();
                }
                else
                {
                    Log("Mark failure.");
                    moduleList.MarkFail();
                }
            }
        }

        private static int GetLastRunIndex()
        {
            string[] directories = Directory.GetDirectories(WritableReproPath);
            if(directories.Length <= 0)
            {
                return 0;
            }

            Array.Sort(directories);
            string lastPath = directories[directories.Length - 1];
            return Convert.ToInt32(lastPath);
        }

        private static void CompileWithLTCG(string filePath)
        {
            // link.exe /cvtcil {srcObj} /out:{destObj}
            string destFilePath = Path.Combine(Path.GetDirectoryName(filePath), "temp.obj");
            ProcessStartInfo linkStartInfo = new ProcessStartInfo("link.exe")
            {
                Arguments = string.Format("/cvtcil {0}, /out:{1}", filePath, destFilePath),
                WorkingDirectory = WritableReproPath
            };
            Execute(linkStartInfo);

            // move {destObj} {srcObj}
            File.Copy(destFilePath, filePath, true);

            // del {destObj}
            File.Delete(destFilePath);
        }

        private static void Execute(ProcessStartInfo startInfo)
        {
            Console.WriteLine("Executing {0} {1}", startInfo.FileName, startInfo.Arguments);
            using (Process p = Process.Start(startInfo))
            {
                p.WaitForExit();
            }
        }

        private static bool ExecuteTests()
        {
            int numTasks = 5;
            Task[] testTasks = new Task[numTasks];
            for (int i = 0; i < numTasks; i++)
            {
                testTasks[i] = new Task(TestWorker);
                testTasks[i].Start();
            }
            Task.WaitAll(testTasks);

            for (int i = 0; i < numTasks; i++)
            {
                if (testTasks[i].IsFaulted)
                {
                    return false;
                }
            }

            return true;
        }

        private static void TestWorker()
        {
            // TODO
        }

        private static void Log(string message, params object[] args)
        {
            string formattedMessage = string.Format(message, args);
            Console.WriteLine("[{0}](Index = {1}) {2}", DateTime.Now, CurrentRunIndex, formattedMessage);
        }

        private static void WriteMarker(bool success)
        {
            string fileName = success ? SuccessMarkerFileName : FailMarkerFileName;
            string filePath = Path.Combine(WritableReproPath, fileName);
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.Write("\n");
            }
        }
    }
}
