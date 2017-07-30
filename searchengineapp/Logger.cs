using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngineApp
{
    class Logger
    {
        
        public static void MyLogger(string line)
        {
            string filename = @"D:\A\files\logger.txt";
            FileStream fs;
            if (!File.Exists(filename))
                fs = new FileStream(filename, FileMode.Create);
            else
                fs = new FileStream(filename, FileMode.Append);
            StreamWriter writer = new StreamWriter(fs);
            writer.WriteLine(line);
            writer.Flush();
            writer.Close();
            fs.Close();
        }
    }
}
