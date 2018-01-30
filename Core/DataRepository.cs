using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public class DataRepository
    {
        private string _filePath;
        public DataRepository(string filePath)
        {
            _filePath = filePath;
        }

        public IEnumerable<string> GetPuns()
        {
            using (var reader = new StreamReader(_filePath + "\\" + "puns.txt"))
            {
                while(true)
                {
                    var line = reader.ReadLine();

                    if (line == null)
                        break;

                    yield return line;
                }
            }
        }

        public IEnumerable<string> GetStopWords()
        {
            using (var reader = new StreamReader(_filePath + "\\" + "stopwords.txt"))
            {
                while (true)
                {
                    var line = reader.ReadLine();

                    if (line == null)
                        break;

                    yield return line;
                }
            }
        }
    }
}
