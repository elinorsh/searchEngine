using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CSharpStringSort;
using System.Diagnostics;

namespace SearchEngineApp
{
    /// <summary>
    /// The class the index the corpus. 
    /// his input its the terms and output is posting files and dictionary.
    /// </summary>
    class Indexer
    {
        //List<int>: [0] number of docs, [1] sum of appearance [2]idf, [3] pointer to the posting
        public Dictionary<string, List<string>> dictionary { get; set; }
        Dictionary<string, Dictionary<string, int>> autocompleteDic; //<docID, <word, sum of appeaences> >
        //key=term, Dictionary<string, List<string>>: key id DocID, List: # appearances of word in doc, positions of the word in doc
        Dictionary<string, Dictionary<string, List<string>>> prePosting { get; set; }

        /// <summary>
        /// Constructor, init the dictionaries
        /// </summary>
        public Indexer()
        {
            dictionary = new Dictionary<string, List<string>>();
            prePosting = new Dictionary<string, Dictionary<string, List<string>>>();
            autocompleteDic = new Dictionary<string, Dictionary<string, int>>();
        }

        /// <summary>
        /// put each term into the dictionary and prePosting dictioary.
        /// also update the document fields: number of unique words in the document.
        /// and number of docs for each term
        /// and sum of appearences in the document for each term
        /// </summary>
        /// <param name="doc">the document to update</param>
        /// <param name="textDoc">the text in this document</param>
        public void Index(Document doc, List<string> textDoc, bool withStem)
        {
            Stemmer stem = new Stemmer();
            string currentWord;
            Dictionary<string, int> maxTFDic = new Dictionary<string, int>();
            int length = textDoc.Count;
            int amount = 0;
            string noStemWord;
            for (int i = 0; i < length; i++)
            {
                noStemWord = textDoc[i];
                if (withStem)
                    currentWord = stem.stemTerm(textDoc[i]);
                else
                    currentWord = textDoc[i];
                if (!currentWord.Equals(""))
                {
                    if (!maxTFDic.ContainsKey(currentWord)) //first time the word appeard in this doc
                    {
                        List<string> list = new List<string>();
                        if (!dictionary.ContainsKey(currentWord)) //also not in the dictionary
                        {
                            list.Add("0"); //[0]number of docs
                            list.Add("0"); //[1]sum of appearance
                            list.Add("0"); //[2]idf
                            list.Add("0"); //[3]pointer to posting
                            dictionary.Add(currentWord, list);

                            if (i < length - 2) //not the last word
                            {
                                string nextWord = textDoc[i + 1];
                                Dictionary<string, int> insideAutocompleteDic = new Dictionary<string, int>();
                                insideAutocompleteDic.Add(nextWord, 0);
                                autocompleteDic.Add(currentWord, insideAutocompleteDic);
                            }
                        }
                        Int32.TryParse(dictionary[currentWord][0], out amount);
                        amount++;
                        dictionary[currentWord][0] = amount + "";
                        doc.numberOfdistinguishWords++;
                        maxTFDic.Add(currentWord, 1);
                        List<string> postingInfo = new List<string>();
                        postingInfo.Add("1");//number of appereances in doc
                        postingInfo.Add(i + "");//index of the word in doc
                        if (!prePosting.ContainsKey(currentWord))
                        {
                            Dictionary<string, List<string>> postingDic = new Dictionary<string, List<string>>();
                            postingDic.Add(doc.docID, postingInfo);
                            prePosting.Add(currentWord, postingDic);
                        }
                        else
                        {
                            Dictionary<string, List<string>> postingDic = prePosting[currentWord];
                            postingDic.Add(doc.docID, postingInfo);
                        }
                    }
                    else
                    {
                        maxTFDic[currentWord]++;
                        Int32.TryParse((prePosting[currentWord][doc.docID][0]), out amount);
                        prePosting[currentWord][doc.docID][0] = amount + 1 + "";
                        prePosting[currentWord][doc.docID].Add(i + "");
                    }
                    Int32.TryParse(dictionary[currentWord][1], out amount);
                    if (amount == 0)
                        Console.WriteLine();
                    amount++;
                    dictionary[currentWord][1] = amount + "";
                    doc.sumOfWords++;

                    if (i < length - 2)//not the last word
                    {
                        if (autocompleteDic.ContainsKey(currentWord))
                        {
                            //string nextWord = textDoc[j + 1];
                            string nextWord = textDoc[i + 1];
                            if (autocompleteDic[currentWord].ContainsKey(nextWord))
                            {
                                autocompleteDic[currentWord][nextWord]++;
                            }
                            else
                            {
                                autocompleteDic[currentWord].Add(nextWord, 1);
                            }
                        }
                        else //first time the word appeard was the last word in the doc.
                        {
                            //string nextWord = textDoc[j + 1];
                            string nextWord = textDoc[i + 1];
                            Dictionary<string, int> insideAutocompleteDic = new Dictionary<string, int>();
                            insideAutocompleteDic.Add(nextWord, 1);
                            autocompleteDic.Add(currentWord, insideAutocompleteDic);
                        }
                    }
                }
            }
            //calculate the max_tf of current document
            int max = 0;
            foreach (KeyValuePair<string, int> item in maxTFDic)
            {
                max = Math.Max(max, item.Value);
            }
            maxTFDic.Clear();
            doc.max_tf = max;
        }

        #region ForPart2
        /// <summary>
        /// calculating the top five words for each word in the dictionary
        /// </summary>
        public void CalculateTopFiveWords()
        {
            string[] maxArr;
            foreach (string key in autocompleteDic.Keys)
            {
                int count = autocompleteDic[key].Keys.Count;
                maxArr = new string[5];
                if (count <= 5)
                {
                    foreach (string nextWord in autocompleteDic[key].Keys)
                    {
                        dictionary[key].Add(nextWord);
                    }
                    continue;
                }
                else  //filling the first five nextWords in maxArr
                {
                    foreach (string nextWord in autocompleteDic[key].Keys)
                    {
                        FindMaxInFive(maxArr, key, nextWord);
                    }
                }
                for (int i = 0; i < 5; i++)
                {
                    dictionary[key].Add(maxArr[i]);
                }
            }
        }

        /// <summary>
        /// helper function that find the maximum value in an array of five
        /// </summary>
        /// <param name="maxArr">the array</param>
        /// <param name="key">the docID</param>
        /// <param name="nextWord">the word we want to find for the top five next words</param>
        /// <returns></returns>
        private bool FindMaxInFive(string[] maxArr, string key, string nextWord)
        {
            int i;
            for (i = 0; i < 5; i++)
            {
                if (maxArr[i] == null || maxArr[i] == "")
                {
                    maxArr[i] = nextWord;
                    return true;
                }
            }
            int minValue = autocompleteDic[key][maxArr[0]], minPlace = -1, num;
            for (i = 0; i < 5; i++) //maxArr is full
            {
                num = autocompleteDic[key][maxArr[i]];
                minValue = Math.Min(minValue, num);
                if (num == minValue)
                {
                    minPlace = i;
                }

            }
            if (autocompleteDic[key][nextWord] > autocompleteDic[key][maxArr[minPlace]])
            {
                maxArr[minPlace] = nextWord;
                return true;
            }
            else
                return false;

        }

        /// <summary>
        /// calculating the vector of each document
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="textDoc"></param>
        /// <param name="withStem"></param>
        public string CalcDocVector(Document doc, List<string> textDoc, bool withStem, out double vector)
        {
            Stemmer stem = new Stemmer();
            string currentWord;
            Dictionary<string, int> wordsFreq = new Dictionary<string, int>(); //<word, #of appearences>
            int length = textDoc.Count;
            int sumOfWords = 0; ;
            //counting the number of apperances of each word = tf
            for (int i = 0; i < length; i++)
            {
                if (textDoc[i] != "")
                {
                    if (withStem)
                        currentWord = stem.stemTerm(textDoc[i]);
                    else
                        currentWord = textDoc[i];
                    if (!wordsFreq.ContainsKey(currentWord))
                    {
                        wordsFreq.Add(currentWord, 1);
                    }
                    else
                    {
                        wordsFreq[currentWord]++;
                    }
                    sumOfWords++;
                }
            }
            //calculating the vector doc
            doc.docVector = 0;
            double idf, tf;
            foreach (string word in wordsFreq.Keys)
            {
                if (dictionary.ContainsKey(word))
                    idf = double.Parse(dictionary[word][2]);
                else
                    idf = 0;
                if (wordsFreq.ContainsKey(word))
                    tf = (double)wordsFreq[word]/sumOfWords;
                else
                    tf = 0;
                doc.docVector += Math.Pow(idf * tf, 2);
            }
            doc.docVector = Math.Sqrt(doc.docVector);
            vector = Math.Round(doc.docVector,4);
            return doc.docID;
        }

        #endregion

        /// <summary>
        /// for each letter in the abc we take 'size' files and merge them simultaneously. 
        /// </summary>
        /// <param name="counter">counters of the merges we did until now</param>
        /// <param name="size">how many files we merge simultaneously</param>
        /// <param name="dirPath">the path for the posting files to merge</param>
        /// <param name="prefixFileName">the prefix to add to the new file that will be created</param>
        /// <param name="isLastTime">indicates if this is the large merge</param>
        /// <param name="N">number of documents, for the idf calculation</param>
        public void MergeSizeFiles(int counter, int size, string dirPath, string prefixFileName, bool isLastTime, int N)
        {
            if (size == 1)
                return;
            string s = "#abcdefghijklmnopqrstuvwxyz";
            StreamReader[] readerArr = new StreamReader[size];
            FileStream[] fileStreamArr = new FileStream[size];
            string filePath;
            bool[] isEndOfFile = new bool[size]; //init to false
            string[] lines = new string[size];
            bool isAllFinish = false;
            int minLinePlace;
            StringBuilder allDocsNumbers = new StringBuilder();
            string currentTerm;
            int finalLineCounter = 0;
            for (int i = 0; i < s.Length; i++) //for all the letters in the #abc
            {
                isAllFinish = false;
                isEndOfFile = new bool[size];
                finalLineCounter = 0;
                if (!isLastTime)
                    filePath = dirPath + "[Merged]" + prefixFileName + s[i] + (int)(counter) + ".txt";
                else
                    filePath = dirPath + "[Posting]-" + s[i] + ".txt";
                File.Delete(filePath);
                FileStream fs = new FileStream(filePath, FileMode.Create);
                StreamWriter writer = new StreamWriter(fs);

                for (int j = 0; j < size; j++) //for all the size files
                {
                    filePath = dirPath + prefixFileName + s[i] + (int)(j + counter * size) + ".txt";
                    if (File.Exists(filePath))
                    {
                        fileStreamArr[j] = new FileStream(filePath, FileMode.OpenOrCreate);
                        readerArr[j] = new StreamReader(fileStreamArr[j]);
                    }
                    else
                    {
                        isEndOfFile[j] = true;
                        lines[j] = "";
                    }
                }
                for (int j = 0; j < size; j++) //read line from each file
                {
                    if (!isEndOfFile[j])
                    {
                        if (readerArr[j].EndOfStream)
                        {
                            isEndOfFile[j] = true;
                            lines[j] = "";
                        }
                        else
                        {
                            lines[j] = readerArr[j].ReadLine();
                        }
                    }
                }
                while (!isAllFinish)
                {
                    minLinePlace = MinLine(lines);
                    if (minLinePlace == -1)
                        break;

                    for (int k = 0; k < size; k++)
                    {
                        isAllFinish = true;
                        if (!isEndOfFile[k])
                        {
                            isAllFinish = false;
                            break;
                        }
                    }
                    if (!isLastTime)
                    {
                        currentTerm = lines[minLinePlace];
                        allDocsNumbers = new StringBuilder(readerArr[minLinePlace].ReadLine());
                        //string tmp = "";
                        for (int k = 0; k < size; k++)
                        {
                            //if (currentTerm == "space")
                            //{
                            //tmp += lines[k] + " , ";
                            //}

                            if (lines[k].Equals(currentTerm) && k != minLinePlace) //2 same terms in different files
                            {
                                allDocsNumbers.Append(readerArr[k].ReadLine());
                                lines[k] = readerArr[k].ReadLine();
                                if (readerArr[k].EndOfStream)
                                {
                                    isEndOfFile[k] = true;
                                    lines[k] = "";
                                }
                            }
                        }
                        writer.WriteLine(currentTerm);
                        writer.WriteLine(allDocsNumbers);
                    }
                    else //at last merge we write the lines without the term
                    {
                        currentTerm = lines[minLinePlace];
                        allDocsNumbers = new StringBuilder(readerArr[minLinePlace].ReadLine());
                        for (int k = 0; k < size; k++)
                        {
                            if (lines[k].Equals(currentTerm) && k != minLinePlace) //2 same terms in different files
                            {
                                string line = readerArr[k].ReadLine();
                                allDocsNumbers.Append(line);
                                lines[k] = readerArr[k].ReadLine();
                                if (readerArr[k].EndOfStream)
                                {
                                    isEndOfFile[k] = true;
                                    lines[k] = "";
                                }
                            }
                        }

                        if (Char.IsWhiteSpace(allDocsNumbers[0])) //removing the white space in the begining of the docs line
                            allDocsNumbers.Remove(1, allDocsNumbers.Length - 1);
                        if (Char.IsWhiteSpace(currentTerm[currentTerm.Length - 1])) //removing the white space in the end of the term
                            currentTerm = currentTerm.Remove(currentTerm.Length - 1);
                        //writer.WriteLine(currentTerm);
                        writer.WriteLine(allDocsNumbers);
                        int df;
                        Int32.TryParse(dictionary[currentTerm][0], out df);
                        double idf = Math.Log((N / df), 2);
                        idf = Math.Round(idf, 2);
                        dictionary[currentTerm][2] = idf + "";
                        dictionary[currentTerm][3] = finalLineCounter + "";

                        finalLineCounter++;
                    }
                    if (readerArr[minLinePlace].EndOfStream)
                    {
                        isEndOfFile[minLinePlace] = true;
                        lines[minLinePlace] = "";
                        filePath = dirPath + prefixFileName + s[i] + (int)(minLinePlace + counter * size) + ".txt";
                        readerArr[minLinePlace].Close();
                        fileStreamArr[minLinePlace].Close();
                        File.Delete(filePath);
                    }
                    else
                    {
                        lines[minLinePlace] = readerArr[minLinePlace].ReadLine();
                    }
                }
                writer.Flush();
                writer.Close();
                fs.Close();
                for (int j = 0; j < size; j++) //for all the size files
                {
                    filePath = dirPath + prefixFileName + s[i] + (int)(j + counter * size) + ".txt";
                    if (readerArr[j] != null)
                    {
                        readerArr[j].Close();
                        fileStreamArr[j].Close();
                    }

                    File.Delete(filePath);
                }
            }
            //if (!isLastTime)
            //    Console.WriteLine("is the file sorted: " + isFileSorted(dirPath + "[Merged]a0.txt"));
            //else
            //    Console.WriteLine("is the file sorted: " + isFileSorted(dirPath + "[Posting]-a.txt"));
            Console.WriteLine("Finish!");
        }

        /// <summary>
        /// loading part of the posting to text file.
        /// loading the 'prePosting' dictionary to file and then clearing it
        /// </summary>
        /// <param name="dirPath">a path to write the files to</param>
        /// <param name="num">the iteration number</param>
        public void LoadToMemory(string dirPath, int num)
        {
            Stopwatch sortTimer = new Stopwatch();
            sortTimer.Restart();
            string[] toSort = prePosting.Keys.ToArray<string>();
            string[] sorted = Sedgewick.Sort(toSort);
            toSort = null;
            sortTimer.Stop();
            //Console.WriteLine("the time it took to sort prePosting for the " + num + " time is: " + sortTimer.Elapsed);

            int length = sorted.Length;
            createAllPostingFiles(dirPath, num);
            //Console.WriteLine("the first file is not sorted in: " + isFileSorted(dirPath + "a0.txt"));
            string filePath = dirPath + "#" + num + ".txt";
            File.Delete(filePath);
            FileStream fs = new FileStream(filePath, FileMode.Create);
            StreamWriter writer = new StreamWriter(fs);
            char c = '#';
            int amount;
            string s, currentWord, index;
            StringBuilder termLine = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                currentWord = sorted[i];
                if (isNewChar(currentWord, c))
                {
                    writer.Flush();
                    writer.Close();
                    fs.Close();
                    c = currentWord[0];
                    filePath = dirPath + c + num + ".txt";
                    fs = new FileStream(filePath, FileMode.Append);
                    writer = new StreamWriter(fs);
                }
                termLine.Clear();
                writer.WriteLine(currentWord);
                foreach (KeyValuePair<string, List<string>> item in prePosting[sorted[i]])
                {
                    Int32.TryParse(item.Value[0], out amount);
                    index = "";
                    for (int j = 1; j < item.Value.Count; j++) //all the locations of the currentWord in the doc
                        index += " " + item.Value[j];
                    //s = item.Key + " " + amount + ",";
                    s = item.Key + " " + amount + index + ",";
                    termLine.Append(s);
                }
                writer.WriteLine(termLine);
            }
            writer.Flush();
            writer.Close();
            fs.Close();
            prePosting.Clear();
        }

        /// <summary>
        /// number of unique words = dictionary size
        /// </summary>
        /// <returns>number of unique words</returns>
        public int numOfDistinquishWords()
        {
            return dictionary.Keys.Count;
        }

        /// <summary>
        /// clearing all the memory and deleting all files
        /// </summary>
        public void Clear()
        {
            if (dictionary != null)
                dictionary.Clear();
            if (prePosting != null)
                prePosting.Clear();
            if (autocompleteDic != null)
                autocompleteDic.Clear();
        }

        #region HelperFunctuins

        /// <summary>
        /// checking if a word is as same letter like a specific char
        /// </summary>
        /// <param name="key">the word we comparing</param>
        /// <param name="c">the char</param>
        /// <returns>true or false</returns>
        private bool isNewChar(string key, char c)
        {
            if (!char.IsLetter(key[0]))
                return false;
            if (key[0] == c)
                return false;
            return true;
        }

        /// <summary>
        /// creating the posting files, each file is for a letter in the abc
        /// and each name is the letter + num parameters
        /// </summary>
        /// <param name="path">a path to write the files to</param>
        /// <param name="num">the number to add to the files name</param>
        private void createAllPostingFiles(string path, int num)
        {
            string s = "#abcdefghijklmnopqrstuvwxyz";
            Directory.CreateDirectory(path);
            for (int i = 0; i < s.Length; i++)
            {
                string filePath = path + s[i] + num + ".txt";
                File.Delete(filePath);
                File.Create(filePath).Dispose();
            }
        }

        /// <summary>
        /// function that compare between 2 words with comparing between each char        /// 
        /// </summary>
        /// <param name="s1">the first string for comparation</param>
        /// <param name="s2">the second string for comparation</param>
        /// <returns>if s1>s2 then return 1. if s1==s2 return 0 and if s2>s1 return 0</returns>
        private int OurCompareTo(string s1, string s2)
        {
            if (s1 == null)
                return -1;
            if (s2 == null)
                return 1;
            int length = Math.Min(s1.Length, s2.Length);
            if (s1.Equals(s2))
                return 0;
            for (int i = 0; i < length; i++)
            {
                if (s1[i].CompareTo(s2[i]) > 0)
                    return 1;
                else if (s1[i].CompareTo(s2[i]) < 0)
                    return -1;
            }
            if (s1.Length < s2.Length && s1.Length > 0)
                return -1;
            else
                return 1;
        }

        /// <summary>
        /// helper function that return the location of the min line in an array
        /// </summary>
        /// <param name="lines">the array of all the lines that needs to be checked</param>
        /// <returns>the index of the min line in the array</returns>
        private int MinLine(string[] lines)
        {
            string minLine = "";
            int i, j;
            int place = -1;
            for (i = 0; i < lines.Length; i++)
            {
                if (lines[i] != "")
                {
                    minLine = lines[i];
                    place = i;
                    break;
                }
            }
            for (j = i; j < lines.Length; j++)
            {
                if (lines[j] != null && lines[j] != "")
                {
                    int compare = OurCompareTo(minLine, lines[j]);
                    //int compare = minLine.CompareTo(lines[j]);
                    if (compare == 1) //minLine > lines[j]
                    {
                        minLine = lines[j];
                        place = j;
                    }
                }
            }
            return place;
        }
        #endregion

        #region Dictionary

        /// <summary>
        /// take the term and number of appreance for each term and appeand
        /// it to a single string
        /// </summary>
        /// <param name="info">the variable for the dictionary</param>
        public void ShowDictionary(ref List<string> info)
        {
            if (dictionary.Count == 0)
            {
                return;
            }
            string[] toSort = dictionary.Keys.ToArray<string>();
            string[] sorted = Sedgewick.Sort(toSort);
            toSort = null;
            string currentWord;
            //int counter = 0;
            info.Add("Number and Symbols");
            for (int i = 0; i < sorted.Length - 1; i++)
            {
                currentWord = sorted[i];
                info.Add(currentWord + " : " + dictionary[currentWord][1]);
                if (currentWord[0] != sorted[i + 1][0]) //different first letter
                {
                    info.Add(sorted[i + 1][0].ToString().ToUpper());
                }
                else //same letter
                {
                    if (i == sorted.Length - 1)
                        info.Add(sorted[i + 1] + " : " + dictionary[sorted[i + 1]][1]);
                }
            }
        }

        /// <summary>
        /// save the dictionary to txt file
        /// </summary>
        /// <param name="path">the path the user chose to save his files</param>
        /// <param name="withStemmer">indicates if the user chose with or without stemmer</param>
        public void SaveDictionary(string path, bool withStemmer)
        {
            if (withStemmer)
                path = path + "\\StemmerDictionary.txt";
            else
                path = path + "\\NoStemmerDictionary.txt";
            File.Delete(path);
            FileStream fs = new FileStream(path, FileMode.Create);
            StreamWriter writer = new StreamWriter(fs);
            string currentWord, currentInfo = "";
            string wordAndLength;
            string[] toSort = dictionary.Keys.ToArray<string>();
            string[] sorted = Sedgewick.Sort(toSort);
            toSort = null;
            for (int i = 0; i < sorted.Length; i++)
            {
                currentWord = sorted[i];
                wordAndLength = currentWord.Length + " " + currentWord;
                int count = dictionary[currentWord].Count;
                for (int j = 0; j < count - 1; j++)
                {
                    currentInfo += dictionary[currentWord][j] + " ";
                }
                currentInfo += dictionary[currentWord][count - 1];
                writer.WriteLine(wordAndLength + " " + currentInfo);
                writer.Flush();
                currentInfo = "";
            }
            writer.Close();
            fs.Close();
        }

        /// <summary>
        /// the dictionary txt file is where the user choose to save it
        /// </summary>
        /// <param name="path">the path the user chose to save his files</param>
        /// <param name="withStemmer">indicates if the user chose with or without stemmer</param>
        /// <returns>true if the load was successful and false otherwise</returns>
        public bool LoadDictionary(string path)
        {
            if (!File.Exists(path))
                return false;
            FileStream fs = new FileStream(path, FileMode.OpenOrCreate);
            StreamReader reader = new StreamReader(fs);
            dictionary = new Dictionary<string, List<string>>();
            string line, currentTerm;
            string[] splitted;
            string[] values;
            List<string> list;
            int wordLength;
            char[] seperator = (" ").ToCharArray();
            try
            {
                while (!reader.EndOfStream)
                {
                    currentTerm = "";
                    line = reader.ReadLine();
                    splitted = line.Split(seperator);
                    wordLength = Int32.Parse(splitted[0]);
                    currentTerm = line.Substring(splitted[0].Length, wordLength + 1);
                    if (currentTerm[0] == ' ')
                    {
                        currentTerm = currentTerm.Substring(1);
                    }
                    values = line.Substring(splitted[0].Length + wordLength + 1).Split(' ');
                    //values = splitted[splitted.Length - 1].Split(' ');
                    list = new List<string>();
                    for (int i = 0; i < values.Length; i++)
                    {
                        if (values[i] != "")
                            list.Add(values[i]);
                    }
                    dictionary.Add(currentTerm, list);

                }
                reader.Close();
                fs.Close();
                return true;
            }
            catch { }
            return false;
        }
        #endregion
    }

}
