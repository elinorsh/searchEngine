using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SearchEngineApp
{
    /// <summary>
    /// The class that united the process that indexing a corpus
    /// </summary>
    class PreTextProcess : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        int size = 5;
        ReadFile readFile;
        Parse parsing;
        Indexer indexer;
        public bool withStemmer { get; set; }
        HashSet<Document> hashSetDocs;
        public double avgLengthOfAllDocs { get; set; }
        HashSet<string> languagesSet;
        string originalSavePath;
        bool isFinish;
        public double sumHeaderLength;
        public bool FinishIndexting
        {
            get { return isFinish; }
            set
            {
                isFinish = value;
                NotifyPropertyChanged("isFinish");
            }
        }
        public void NotifyPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        /// <summary>
        /// init the data stucture
        /// </summary>
        public PreTextProcess()
        {
            indexer = new Indexer();
            withStemmer = false;
            hashSetDocs = new HashSet<Document>();
            sumHeaderLength = 0;
        }

        /// <summary>
        /// going through 'size' files each time and parse each documnet in them and then 
        /// writing a posting file. after that merge all the posting files into one file for each letter
        /// and saving the dictionary on the disk as a text file
        /// </summary>
        /// <param name="savePath">the path to save the files to</param>
        /// <param name="loadPath">the path to load the files from</param>
        public void StartIndexing(string savePath, string loadPath)
        {
            readFile = new ReadFile(loadPath);
            parsing = new Parse(loadPath + "\\" + "stop_words.txt");
            originalSavePath = savePath;
            if (withStemmer)
                savePath = savePath + "\\" + "WithStemmer";
            else
                savePath = savePath + "\\" + "NoStemmer";
            Directory.CreateDirectory(savePath);
            savePath += "\\";

            int amount = readFile.GetNumberOfFiles();
            int sum = 0;
            int counter = 0;

            while (!readFile.isFinish())
            {
                readFile.ReadDirectory(size);
                while (readFile.docs.Count != 0) //calls 'size' times
                {
                    Document doc = readFile.docs.Dequeue();
                    List<string> textDoc = parsing.GetTerms(doc.text, false);
                    indexer.Index(doc, textDoc, withStemmer); //for inverted index
                    hashSetDocs.Add(doc);
                    doc.header = ConvertDocHeader(parsing.GetTerms(new StringBuilder(doc.header), withStemmer));
                    sumHeaderLength += doc.header.Length;
                    sum += doc.sumOfWords;
                }
                indexer.LoadToMemory(savePath, counter);
                counter++;
            }
            avgLengthOfAllDocs = sum / hashSetDocs.Count;
            indexer.CalculateTopFiveWords();
            languagesSet = readFile.languagesSet;
            int numOfFiles = (amount + size - 1) / size;
            int mergeCounter = (numOfFiles + size - 1) / size; ;
            string prefix = "";
            bool isLastTime = false;
            while (numOfFiles > 1)
            {
                if (numOfFiles <= size)
                    isLastTime = true;
                for (int i = 0; i < mergeCounter; i++)
                {
                    indexer.MergeSizeFiles(i, size, savePath, prefix, isLastTime, readFile.docsAmount);
                }
                prefix += "[Merged]";
                mergeCounter = (mergeCounter + size - 1) / size;
                numOfFiles = (numOfFiles + size - 1) / size;
            }
            indexer.SaveDictionary(originalSavePath, withStemmer);

            FinishIndexting = true;
        }

        /// <summary>
        /// add to the documents the vector
        /// </summary>
        /// <param name="loadPath"></param>
        /// <param name="savePath"></param>
        /// <param name="stemmer"></param>
        public void IndexingDocuments(string loadPath, string savePath, bool stemmer)
        {
            //calc the vector of each doc for the cosSim
            CalcCosineSimilarity(loadPath);
            SaveDocs(savePath, stemmer);
        }

        /// <summary>
        /// returning all the languages from all the documents
        /// </summary>
        /// <returns></returns>
        public HashSet<string> GetLanguages()
        {
            return languagesSet;
        }

        /// <summary>
        /// clearing all the data sturcture and deleting all the files
        /// </summary>
        public void ClearAll()
        {
            string path = originalSavePath;
            string postingPathStemmer = path + "\\WithStemmer\\";
            string postingPathNoStemmer = path + "\\NoStemmer\\";
            string dictionaryStemmerPath = path + "\\StemmerDictionary.txt";
            string dictionaryNoStemmerPath = path + "\\NoStemmerDictionary.txt";
            string docsStemmerPath = path + "\\StemmerDocs.txt";
            string docsNoStemmerPath = path + "\\NoStemmerDocs.txt";
            string[] allFiles;
            if (Directory.Exists(postingPathNoStemmer))
            {
                allFiles = Directory.GetFiles(postingPathNoStemmer);
                for (int i = 0; i < allFiles.Length; i++)
                {
                    File.Delete(allFiles[i]);
                }
                Directory.Delete(postingPathNoStemmer);
            }
            if (Directory.Exists(postingPathStemmer))
            {
                allFiles = Directory.GetFiles(postingPathStemmer);
                for (int i = 0; i < allFiles.Length; i++)
                {
                    File.Delete(allFiles[i]);
                }
                Directory.Delete(postingPathStemmer);
            }

            File.Delete(dictionaryNoStemmerPath);
            File.Delete(dictionaryStemmerPath);
            File.Delete(docsNoStemmerPath);
            File.Delete(docsStemmerPath);
            if (hashSetDocs != null)
                hashSetDocs.Clear();
            if (languagesSet != null)
                languagesSet.Clear();
            withStemmer = false;
            if (indexer != null)
                indexer.Clear();
            if (readFile != null)
                readFile.Clear();
            if (parsing != null)
                parsing.Clear();
        }

        /// <summary>
        /// load the stop words to the memory
        /// </summary>
        /// <param name="path"></param>
        public void LoadStopWords(string path)
        {
            parsing = new Parse(path);
            //parsing.LoadStopWords(path);
        }

        #region Documents

        /// <summary>
        /// calculating the vector of each document
        /// </summary>
        /// <param name="loadPath">the path of the corpus</param>
        public void CalcCosineSimilarity(string loadPath)
        {
            readFile = new ReadFile(loadPath);
            parsing = new Parse(loadPath + "\\" + "stop_words.txt");

            int amount = readFile.GetNumberOfFiles();
            int counter = 0;
            double vector;
            string docID;
            Dictionary<string, double> docsVec = new Dictionary<string, double>(); //docID, vector
            while (!readFile.isFinish())
            {
                readFile.ReadDirectory(size);
                while (readFile.docs.Count != 0) //calls 'size' times
                {
                    Document doc = readFile.docs.Dequeue();
                    List<string> textDoc = parsing.GetTerms(doc.text, false);
                    docID = indexer.CalcDocVector(doc, textDoc, withStemmer, out vector);
                    docsVec.Add(docID, vector);
                }
                counter++;
            }
            foreach (Document doc in hashSetDocs)
            {
                doc.docVector = docsVec[doc.docID];
            }
        }

        /// <summary>
        /// convert the document header from list with empty strings to one string
        /// </summary>
        /// <param name="arr"></param>
        /// <returns></returns>
        private string ConvertDocHeader(List<string> arr)
        {
            string ans = "";
            for (int i = 0; i < arr.Count; i++)
            {
                if (arr[i] != "")
                    ans += arr[i] + " ";
            }
            return ans;
        }

        /// <summary>
        /// saving the dictionary to the disk as a text file
        /// </summary>
        /// <param name="path"></param>
        private void SaveDocs(string path, bool stemmer)
        {
            if (stemmer)
                path = path + "\\StemmerDocs.txt";
            else
                path = path + "\\NoStemmerDocs.txt";
            FileStream fs = new FileStream(path, FileMode.OpenOrCreate);
            StreamWriter writer = new StreamWriter(fs);
            string info = "<DOCNO> <Max_TF> <Unique Words> <Sum Of Words> <Languages> <Vector> <Header>";
            writer.WriteLine(info);
            foreach (Document item in hashSetDocs)
            {
                string lang = item.language;
                if (lang == "") //doc with no language
                    lang = " ";
                info = item.docID + "," + item.max_tf + "," + item.numberOfdistinguishWords + "," + item.sumOfWords +
                    "," + lang + "," + item.docVector + "," + item.header;
                writer.WriteLine(info);
            }

            writer.Flush();
            writer.Close();
            fs.Close();
        }

        /// <summary>
        /// get the average of all the documents length
        /// </summary>
        /// <returns>average length</returns>
        public double GetAvgHeaderLength()
        {
            return sumHeaderLength / hashSetDocs.Count;
        }

        /// <summary>
        /// return the hash set of all the documents
        /// </summary>
        /// <returns></returns>
        public HashSet<Document> GetDocuments()
        {
            return hashSetDocs;
        }

        /// <summary>
        /// load all the information of the documents to the memory
        /// </summary>
        /// <param name="path">the path where the documents file is located</param>
        /// <returns></returns>
        public bool LoadDocs2(string path)
        {
            IEnumerable<string> lines = File.ReadLines(path);
            bool firstLine = true;
            languagesSet = new HashSet<string>();
            int sum = 0;
            foreach (string line in lines)
            {
                if (firstLine)
                {
                    firstLine = false;
                    continue;
                }
                else
                {
                    try
                    {
                        string[] splitted = line.Split(',');
                        Document doc = new Document();
                        doc.docID = splitted[0];
                        doc.max_tf = Int32.Parse(splitted[1]);
                        doc.numberOfdistinguishWords = Int32.Parse(splitted[2]);
                        doc.sumOfWords = Int32.Parse(splitted[3]);
                        string lan = splitted[4];
                        if (lan.Split(' ').Length > 1)
                            lan = lan.Split(' ')[0];
                        if (lan.Contains("language"))
                        {
                            int index = lan.IndexOf("language");
                            lan = lan.Substring(0, index);
                        }
                        if (lan.Equals("rusian") || lan.Equals("russia"))
                            lan = "russian";
                        else if (lan.Equals("span") || lan.Equals("spansih"))
                            lan = "spanish";
                        else if (lan.Equals("tigrigna") || lan.Equals("trigrigna") || lan.Equals("trigrignya"))
                            lan = "tigrinya";
                        else if (lan.Equals("engligh") || lan.Equals("enhglish") || lan.Equals("enlgish") || lan.Equals("eng"))
                            lan = "english";
                        doc.language = lan;
                        doc.docVector = double.Parse(splitted[5]);
                        sum += doc.sumOfWords;
                        languagesSet.Add(doc.language);
                        for (int i = 6; i < splitted.Length - 1; i++)
                        {
                            doc.header += splitted[i] + ",";
                        }
                        doc.header += splitted[splitted.Length - 1];
                        hashSetDocs.Add(doc);

                    }
                    catch { return false; }
                }
            }
            avgLengthOfAllDocs = sum / hashSetDocs.Count;
            return true;
        }

        #endregion

        #region Dictionary

        /// <summary>
        /// getting all the information from the indexing process to present to the user
        /// </summary>
        /// <param name="numOfDocs"></param>
        /// <param name="numOfWord"></param>
        public void infoAfterFinish(out int numOfDocs, out int numOfWord)
        {
            numOfDocs = readFile.docsAmount;
            numOfWord = indexer.numOfDistinquishWords();
        }

        /// <summary>
        /// load a dictionary from a text file
        /// </summary>
        /// <returns></returns>
        public bool LoadDictionary(string path)
        {
            return indexer.LoadDictionary(path);
        }

        /// <summary>
        /// taking the string that represent the dictionary from the indexer and sending it to the main window
        /// </summary>
        /// <param name="dic"></param>
        public void ShowDictionary(ref List<string> list)
        {
            indexer.ShowDictionary(ref list);
        }

        /// <summary>
        /// return the dictionary
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, List<string>> GetDictinary()
        {
            return indexer.dictionary;
        }

        #endregion

    }
}
