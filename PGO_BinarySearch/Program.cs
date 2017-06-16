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
        public const string TestBedPath = @"c:\work\test_bed";
        public static readonly string CoreRootPath = Path.Combine(TestBedPath, "tests\\core_root");
        public const string DebugLogFileName = "_DebugLog.txt";
        public const string RunParamsFileName = "_RunParams.txt";
        public const string SuccessMarkerFileName = "_Result_Success.txt";
        public const string FailMarkerFileName = "_Result_Fail.txt";
        public static int CurrentRunIndex = 0;
        public static string CurrentReproDirectory = null;

        public const string VCVarsAll = "C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\VC\\vcvarsall.bat";
        public const string VCArch = "x86_amd64";

        static void Main(string[] args)
        {
            // Build the module list.
            Log("Start: Build Module List");
            ModuleList moduleList = new ModuleList(CleanReproPath);
            Log("End: Build Module List");

            // Find the latest run.
            CurrentRunIndex = 1;
            Log($"Set current run index to {CurrentRunIndex}.");

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
                Directory.CreateDirectory(CurrentReproDirectory);
                foreach (string srcFile in Directory.GetFiles(CleanReproPath))
                {
                    File.Copy(srcFile, Path.Combine(CurrentReproDirectory, Path.GetFileName(srcFile)));
                }
                Log("End: Create new repro directory '{0}'.", CurrentReproDirectory);

                // Dump the run params.
                string runParamsPath = Path.Combine(CurrentReproDirectory, RunParamsFileName);
                Log("Start: Save run parameters.");
                moduleList.WriteState(runParamsPath);
                Log("End: Save run parameters.");

                // Dump the list of files that will be compiled.
                string moduleListLogPath = Path.Combine(CurrentReproDirectory, DebugLogFileName);
                Log("Start: Write module list to '{0}'.", moduleListLogPath);
                moduleList.WriteDebugLog(CurrentReproDirectory, moduleListLogPath);
                Log("End: Write module list to '{0}'.", moduleListLogPath);

                // Decide which files to compile using LTCG.
                IEnumerable<string> ltcgFiles = moduleList.LTCGModules;

                // Compile the LTCG set.
                Log("Start: Compile LTCG modules.");
                CompileWithLTCG(ltcgFiles.ToArray());
                Log("End: Compile LTCG modules.");

                // Compile and Link the rest of the binary with PGO.
                Log("Start: Compile the remaining modules with PGO.");
                ProcessStartInfo linkStartInfo = CreateProcessStartInfo();
                linkStartInfo.FileName = "cmd.exe";
                linkStartInfo.Arguments = $"/s /c \" \"{VCVarsAll}\" {VCArch} &&  \"link.exe\" @link.rsp /UseProfile:PGD=coreclr.pgd\"";

                int exitCode;
                if ((exitCode = Execute(linkStartInfo)) != 0)
                {
                    throw new InvalidOperationException($"Link command failed.  Exit code: {exitCode}");
                }
                Log("End: Compile the remaining modules with PGO.");

                // Copy coreclr.dll into the test bed.
                Log("Start: Update coreclr.dll in testbed.");
                File.Copy(Path.Combine(CurrentReproDirectory, "coreclr.dll"), Path.Combine(CoreRootPath, "coreclr.dll"), true);
                Log("End: Update coreclr.dll in testbed.");

                // Run the test multiple times.
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

                CurrentRunIndex++;
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

        private static void CompileWithLTCG(string[] fileNames)
        {

            StringBuilder builder = new StringBuilder();
            for(int i=0; i<fileNames.Length; i++)
            {
                // Prepend the full path.
                string filePath = Path.Combine(CurrentReproDirectory, fileNames[i]);

                // link.exe /cvtcil {srcObj} /out:{destObj}
                string destFilePath = Path.Combine(Path.GetDirectoryName(filePath), "temp.obj");

                builder.Append($"\"link.exe\" /cvtcil \"{filePath}\" /out:\"{destFilePath}\" && copy /Y \"{destFilePath}\" \"{filePath}\" && del \"{destFilePath}\" && ");

                if((i%20 == 0) || ((i+1) == fileNames.Length))
                {
                    builder.Append("echo \"Batch complete.\"");

                    ProcessStartInfo linkStartInfo = CreateProcessStartInfo();
                    linkStartInfo.FileName = "cmd.exe";
                    linkStartInfo.Arguments = $"/s /c \" \"{VCVarsAll}\" {VCArch} && {builder.ToString()}";

                    int exitCode;
                    if ((exitCode = Execute(linkStartInfo)) != 0)
                    {
                        throw new InvalidOperationException($"Link command failed.  Exit code: {exitCode}");
                    }

                    builder.Clear();
                }
            }
        }

        private static int Execute(ProcessStartInfo startInfo)
        {
            Log("Executing {0} {1}", startInfo.FileName, startInfo.Arguments);
            Stopwatch s = Stopwatch.StartNew();
            int exitCode;
            using (Process p = Process.Start(startInfo))
            {
                p.WaitForExit();
                exitCode = p.ExitCode;
            }
            s.Stop();
            Log($"Completed in {s.Elapsed.ToString()}");
            return exitCode;
        }

        private static bool ExecuteTests()
        {
            bool testPassed = true;
            for (int i = 0; i < 10; i++)
            {
                Log($"Running test iteration {i}.");

                // Run the test.
                TestWorker();

                // Check for dumps.
                string[] crashDumps = Directory.GetFiles("c:\\crashes");
                testPassed = (crashDumps.Length == 0);
                foreach (string dump in crashDumps)
                {
                    File.Copy(dump, Path.Combine(CurrentReproDirectory, Path.GetFileName(dump)));
                    File.Delete(dump);
                }

                if(!testPassed)
                {
                    break;
                }
            }

            return testPassed;
        }

        private static void TestWorker()
        {
            ProcessStartInfo startInfo = CreateProcessStartInfo();
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = $"/s /c \" c:\\work\\procdump.exe -ma -n 1 -e 1 -g -f C0000005.ACCESS_VIOLATION -x c:\\crashes c:\\work\\test_bed\\tests\\core_root\\corerun.exe C:\\Work\\test_bed\\GC\\Features\\SustainedLowLatency\\sustainedlowlatency_race\\sustainedlowlatency_race.exe \"";

            Execute(startInfo);

        }

        private static void Log(string message, params object[] args)
        {
            string formattedMessage = string.Format(message, args);
            Console.WriteLine("[{0}](Index = {1}) {2}", DateTime.Now, CurrentRunIndex, formattedMessage);
        }

        private static void WriteMarker(bool success)
        {
            string fileName = success ? SuccessMarkerFileName : FailMarkerFileName;
            string filePath = Path.Combine(CurrentReproDirectory, fileName);
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.Write("\n");
            }
        }

        private static ProcessStartInfo CreateProcessStartInfo()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = false;
            startInfo.WorkingDirectory = CurrentReproDirectory;

            return startInfo;
        }
    }
}
