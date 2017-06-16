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
        private int _lastRoundIndex = -1;

        public ModuleList(string reproDirectoryPath)
        {
            _sortedModules = Directory.GetFiles(reproDirectoryPath, "*.obj");
            for(int i=0; i<_sortedModules.Length; i++)
            {
                _sortedModules[i] = Path.GetFileName(_sortedModules[i]);
            }
            Array.Sort(_sortedModules);
            _startIndex = 0;
            _endIndex = _sortedModules.Length - 1;
            _testIndex = (_startIndex + _endIndex) / 2;
        }

        public IEnumerable<string> LTCGModules
        {
            get
            {
                if (_startIndex == _testIndex)
                {
                    yield return _sortedModules[_startIndex];
                }
                else
                {
                    for (int i = _startIndex; i < _testIndex; i++)
                    {
                        yield return _sortedModules[i];
                    }
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
                if (_startIndex != _testIndex)
                {
                    yield return _sortedModules[_testIndex];
                }
                for (int i = _testIndex + 1; i < _sortedModules.Length; i++)
                {
                    yield return _sortedModules[i];
                }
            }
        }

        public void MarkPass()
        {
            CheckBlame(true);
            Next();
        }

        public void MarkFail()
        {
            CheckBlame(false);
            Next();
        }

        private void Next()
        {
            _testIndex = (_startIndex + _endIndex) / 2;
        }

        private void CheckBlame(bool prevSuccess)
        {
            if (_lastRoundIndex != -1)
            {
                int badIndex = 0;
                if(prevSuccess)
                {
                    badIndex = _startIndex;
                }
                else
                {
                    badIndex = _lastRoundIndex;
                }
                throw new Exception(string.Format("Bad module: {0}", _sortedModules[badIndex]));
            }
            else
            {
                if ((_startIndex == _testIndex) && prevSuccess)
                {
                    throw new Exception(string.Format("Bad module: {0}", _sortedModules[_startIndex]));
                }
                else if ((_startIndex == _testIndex) && !prevSuccess)
                {
                    _lastRoundIndex = _startIndex;
                    _startIndex = _endIndex;
                }
                else
                {
                    if(prevSuccess)
                    {
                        _endIndex = _testIndex;
                    }
                    else
                    {
                        _startIndex = _testIndex;
                    }
                }
            }
        }

        public void WriteDebugLog(string currentReproPath, string pathToDebugLog)
        {
            using (StreamWriter writer = new StreamWriter(pathToDebugLog))
            {
                WriteDebugLog(currentReproPath, writer);
            }
        }

        public void WriteDebugLog(string currentReproPath, TextWriter writer)
        {
            writer.WriteLine("LTCG modules:");
            foreach (string module in LTCGModules)
            {
                writer.WriteLine("\t" + Path.Combine(currentReproPath, module));
            }

            writer.WriteLine("\n\nPGU modules:");
            foreach (string module in PGUModules)
            {
                writer.WriteLine("\t" + Path.Combine(currentReproPath, module));
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
