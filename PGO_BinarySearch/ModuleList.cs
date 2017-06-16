using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace PGO_BinarySearch
{
    public class ModuleList
    {
        private string[] _sortedModules;
        private int _startIndex;
        private int _endIndex;
        private int _testIndex;

        public ModuleList(string[] modules)
        {
            _sortedModules = new string[modules.Length];
            Array.Copy(modules, _sortedModules, _sortedModules.Length);
            _startIndex = 0;
            _endIndex = _sortedModules.Length - 1;
            _testIndex = (_startIndex + _endIndex) / 2;
        }

        public IEnumerable<string> LTCGModules
        {
            get
            {
                for (int i = _startIndex; i < _testIndex; i++)
                {
                    yield return _sortedModules[i];
                }
            }
        }

        public IEnumerable<string> PGUModules
        {
            get
            {
                for (int i = 0; i < _startIndex; i++)
                {
                    yield return _sortedModules[i];
                }
                for (int i = _testIndex; i < _sortedModules.Length; i++)
                {
                    yield return _sortedModules[i];
                }
            }
        }

        public void MarkPass()
        {
            _endIndex = _testIndex;
            Next();
        }

        public void MarkFail()
        {
            _startIndex = _testIndex;
            Next();
        }

        private void Next()
        {
            if (_startIndex > _endIndex)
            {
                throw new Exception("Search operation complete!");
            }
            _testIndex = (_startIndex + _endIndex) / 2;
        }

        public void WriteDebugLog(string pathToDebugLog)
        {
            using (StreamWriter writer = new StreamWriter(pathToDebugLog))
            {
                WriteDebugLog(writer);
            }
        }

        public void WriteDebugLog(TextWriter writer)
        {
            writer.WriteLine("LTCG modules:");
            foreach (string module in LTCGModules)
            {
                writer.WriteLine("\t" + module);
            }

            writer.WriteLine("\n\nPGU modules:");
            foreach (string module in PGUModules)
            {
                writer.WriteLine("\t" + module);
            }
        }

        public void WriteState(string pathToStateFile)
        {
            using (StreamWriter writer = new StreamWriter(pathToStateFile))
            {
                writer.WriteLine(_startIndex.ToString());
                writer.WriteLine(_endIndex.ToString());
                writer.WriteLine(_testIndex.ToString());
            }
        }

        public void InitializeStartPointFrom(string pathToStateFile)
        {
            using (StreamReader reader = new StreamReader(pathToStateFile))
            {
                string line = reader.ReadLine();
                _startIndex = Convert.ToInt32(line);
                line = reader.ReadLine();
                _endIndex = Convert.ToInt32(line);
                line = reader.ReadLine();
                _testIndex = Convert.ToInt32(line);
            }
        }
    }
}
