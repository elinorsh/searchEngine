using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngineApp
{
    /// <summary>
    /// take a file and extracting the text and the relevant information 
    /// </summary>
    class ReadFile
    {
        public string myPath { get; private set; }
        public Queue<Document> docs { get; set; }
        string[] allFiles;
        int counter;
        int length;
        public int docsAmount { get; private set; }
        //public HashSet<string> languagesSet { get; private set; }
        public HashSet<string> languagesSet { get; private set; }

        /// <summary>
        /// init all the data member of the class
        /// </summary>
        /// <param name="path"></param>
        public ReadFile(string path)
        {
            myPath = path;
            docs = new Queue<Document>();
            allFiles = Directory.GetFiles(myPath);
            counter = 0;
            length = allFiles.Length;
            docsAmount = 0;
            languagesSet = new HashSet<string>();

        }

        /// <summary>
        /// getting the sum numbers of files
        /// </summary>
        /// <returns></returns>
        public int GetNumberOfFiles()
        {
            return length - 1;
        }

        /// <summary>
        /// reading 'size' files and putting the text from them in the Queue
        /// and saving the relevant information for the document
        /// </summary>
        /// <param name="size"></param>
        public void ReadDirectory(int size)
        {
            double num;
            Stopwatch timer = new Stopwatch();
            timer.Restart();

            if (counter < length)
            {
                for (int i = 0; i < size && counter < length; i++)
                {
                    if (allFiles[counter].Contains("stop_words"))
                    {
                        counter++;
                        continue;
                    }
                    IEnumerable<string> lines = File.ReadLines(allFiles[counter]);
                    Document doc = new Document();
                    bool isText = false;
                    bool isSpace = false;
                    string lowerLine = "";
                    string docField = "";
                    foreach (string line in lines)
                    {
                        if (!isText && line != "<TEXT>")
                        {
                            if (line.Contains("<DOCNO>"))
                            {
                                docField = line.Trim("<DOCNO>".ToCharArray());
                                docField = line.Trim("</DOCNO>".ToCharArray());
                                doc.docID = docField.Replace(" ", string.Empty);
                                docsAmount++;
                                continue;
                            }
                            if (line.Contains("<TI>"))
                            {
                                docField = line.Trim("<H3> <TI>".ToCharArray());
                                docField = docField.Trim("</TI></H3>".ToCharArray());
                                doc.header = docField.ToLower();
                                continue;
                            }
                            if (line.Contains("<F P=105>")) //also can appear in the <TEXT> scope!
                            {
                                docField = Languages(line);
                                if (!Double.TryParse(docField, out num) && docField != "")
                                {
                                    doc.language = docField;
                                }
                                continue;
                            }
                        }

                        if (line == "<TEXT>")
                        {
                            isText = true;
                            isSpace = false;

                            continue;
                        }
                        if (isText && !isSpace)
                        {
                            if (line.Contains("<F P=105>"))
                            {
                                docField = Languages(line);
                                if (!Double.TryParse(docField, out num) && docField != "")
                                {
                                    doc.language = docField;
                                }
                                continue;
                            }
                            if (line.Contains("Language: <F") || line.Contains("Article Type:"))
                                continue;
                            else
                                isSpace = true;
                        }
                        if (line == "</TEXT>")
                        {
                            isText = false;
                            docs.Enqueue(doc);
                            doc = new Document();
                            continue;
                        }
                        if (isText)
                        {
                            if (line != "")
                            {
                                lowerLine = line.ToLower();
                                doc.text.Append(" " + lowerLine);
                            }
                        }
                    }
                    counter++;
                }
            }
            timer.Stop();
            Console.WriteLine("the time it took for readFile of " + size + " docs: " + timer.Elapsed);
        }

        /// <summary>
        /// extracting from a line the languages if exist
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private string Languages(string line)
        {
            string docField;
            docField = line.Replace("Language: <F P=105>", "");
            docField = docField.Replace("<F P=105>", "");
            docField = docField.Replace(" </F>", "");
            if (docField[0] == ' ')
                docField = docField.Substring(1);
            if (docField[0] == ' ')
                docField = docField.Substring(1);
            if (docField[0] == ' ')
                docField = docField.Substring(1);
            docField = docField.Replace("<F P=105>", "");
            if (!char.IsLetter(docField[docField.Length - 1]))
                docField = docField.Remove(docField.Length - 1);
            docField = docField.ToLower();
            languagesSet.Add(docField);
            return docField;
        }

        /// <summary>
        /// returning true when we finish reading all the files
        /// and false otherwise
        /// </summary>
        /// <returns></returns>
        public bool isFinish() //true when done
        {
            return !(counter < length);
        }

        /// <summary>
        /// clearing all the data structures
        /// </summary>
        public void Clear()
        {
            docs.Clear();
            allFiles = null;
            if (languagesSet != null)
                languagesSet.Clear();


        }
    }
}
