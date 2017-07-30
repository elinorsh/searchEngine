using CSharpStringSort;
using NHunspell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngineApp
{
    class Search
    {
        public static int queryID = 100; //random quryID
        Ranker ranker;
        List<string> query; //the query
        string postingPath;
        Dictionary<string, List<string>> dictionary; //the dictionary that currently in the memory
        Dictionary<string, Document> allDocsDic; //all the corpus
        HashSet<string> languages; //the selected languages OR empty
        Dictionary<string, Document> filteredDocs; //the documents that will be send to the ranker
        HashSet<string> importantWords; //specials important words in the query
        public bool withStemmer { get; set; }
        /// <summary>
        /// constructor. give an ID if necessary, init a ranker and set the IDF for each word in the query
        /// </summary>
        /// <param name="q">the query</param>
        /// <param name="docs">all the corpus</param>
        /// <param name="p">the path of the posting folder</param>
        /// <param name="dic">dictionary</param>
        /// <param name="lan">a set with all the languages the user chose. if empty: no language selected</param>
        /// <param name="qID">query ID if exist</param>
        /// <param name="iw">set of the important words</param>
        public Search(List<string> q, HashSet<Document> docs, string p, Dictionary<string, List<string>> dic, HashSet<string> lan, int? qID, HashSet<string> iw)
        {
            dictionary = dic;
            postingPath = p;
            query = q;
            filteredDocs = new Dictionary<string, Document>();
            allDocsDic = ConvertDocDictionary(docs);
            languages = lan;
            if (qID != null)
                queryID = (int)qID;
            else
                queryID++;

            importantWords = iw;
            ranker = new Ranker(query, importantWords, withStemmer);
            ranker.queryTermIdf = SetIdfForQuery();
        }

        /// <summary>
        /// call the ranker to rank all the filter documents
        /// </summary>
        /// <returns></returns>
        public Dictionary<double, HashSet<string>> RankerRelevantDocs()
        {
            return ranker.RankerRelevantDocs();
        }

        /// <summary>
        /// set a new quer.
        /// clear the filtered documents, init a new ranker and find the IDF of the query
        /// </summary>
        /// <param name="q"></param>
        public void SetNewQuery(List<string> q)
        {
            query = q;
            filteredDocs.Clear();
            ranker = new Ranker(query, importantWords, withStemmer);
            ranker.queryTermIdf = SetIdfForQuery();

        }

        /// <summary>
        /// filter the corpus. takes all the documents that at least one of the words from the query is in them.
        /// if its important word, takes only the documents that that word is in.
        /// </summary>
        /// <returns></returns>
        public bool FilterDocuments()
        {
            bool filterlanguages = (languages.Count > 0);
            ranker.N = allDocsDic.Keys.Count;
            List<string> list = GetQueryPostingInfo();
            if (list == null)
                return false;
            foreach (string word in list)
            {
                int index = word.IndexOf("FBIS");
                string currentWord = word.Substring(0, index - 1);
                HashSet<string> wordDocs = GetAllDocsFromPosting(word);
                foreach (string doc in wordDocs)
                {
                    if (!filteredDocs.ContainsKey(doc))
                    {
                        //foreach (string importantWord in importantWords)
                        //{
                        //    if (!allDocsDic[doc].queryFreq.ContainsKey(importantWord) || allDocsDic[doc].queryFreq[importantWord] == 0)
                        //        continue;
                        //}
                        if (!filterlanguages)
                            filteredDocs.Add(doc, allDocsDic[doc]);
                        else
                        {
                            if (languages.Contains(allDocsDic[doc].language))
                                filteredDocs.Add(doc, allDocsDic[doc]);
                        }
                    }
                }
            }
            ranker.corpusDocs = filteredDocs;

            return true;
        }

        /// <summary>
        /// helper function
        /// convert hashSet of documents to dictionary of DocID,Document
        /// </summary>
        /// <param name="docs"></param>
        /// <returns></returns>
        private Dictionary<string, Document> ConvertDocDictionary(HashSet<Document> docs)
        {
            Dictionary<string, Document> allDocsDic = new Dictionary<string, Document>();
            foreach (Document doc in docs)
            {
                allDocsDic.Add(doc.docID, doc);
            }
            return allDocsDic;
        }

        #region posting

        /// <summary>
        /// return all the documents from a single line in the posting
        /// </summary>
        /// <param name="postingInfo"></param>
        /// <returns>the set of all the documents</returns>
        private HashSet<string> GetAllDocsFromPosting(string postingInfo)
        {
            HashSet<string> ans = new HashSet<string>();
            int index = postingInfo.IndexOf("FBIS");
            string currentWord = postingInfo.Substring(0, index - 1);
            ranker.docsPerTerm.Add(currentWord, 0);
            string[] termInfo = postingInfo.Substring(index).Split(',');
            string[] temp;

            for (int i = 0; i < termInfo.Length - 1; i++)
            {
                temp = termInfo[i].Split(' ');
                string doc = temp[0];
                if (!allDocsDic[doc].queryFreq.ContainsKey(currentWord))
                    allDocsDic[doc].queryFreq.Add(currentWord, Int32.Parse(temp[1]));
                ans.Add(doc);
                ranker.docsPerTerm[currentWord]++;
            }

            return ans;

        }

        /// <summary>
        /// get the posting line for each word in the query
        /// </summary>
        /// <returns>list of all these lines</returns>
        private List<string> GetQueryPostingInfo() //the posting info for each word
        {
            List<string> info = new List<string>();
            Dictionary<char, List<string>> queryDic = NumberOfUniqueChar();
            int length = queryDic.Keys.Count;
            FileStream[] fileStreamArr = new FileStream[length];
            StreamReader[] readerArr = new StreamReader[length];
            string filePath;
            int i = 0;
            try
            {
                foreach (char c in queryDic.Keys)
                {
                    filePath = postingPath + "\\[Posting]-" + c + ".txt";
                    fileStreamArr[i] = new FileStream(filePath, FileMode.Open);
                    readerArr[i] = new StreamReader(fileStreamArr[i]);
                    string[] toSort = queryDic[c].ToArray<string>();
                    string[] sorted = Sedgewick.Sort(toSort);
                    string currentWord;
                    int[] locations = new int[sorted.Length];
                    int lineLoc = 0;
                    for (int j = 0; j < locations.Length; j++)
                    {
                        currentWord = sorted[j];
                        if (!dictionary.ContainsKey(currentWord))
                            continue;
                        locations[j] = Int32.Parse(dictionary[currentWord][3]);
                        while (lineLoc < locations[j])
                        {
                            readerArr[i].ReadLine();
                            lineLoc++;
                        }
                        info.Add(currentWord + " " + readerArr[i].ReadLine());
                        lineLoc++;
                    }
                    readerArr[i].Close();
                    fileStreamArr[i].Close();
                }
            }
            catch { }
            if (info.Count == 0)
                return null;
            try
            {
                GetWordsPositions(info);
            }
            catch { return null; }
            return info;
        }

        /// <summary>
        /// hepfer function that return the unique chars of the first char of each word in the query
        /// </summary>
        /// <returns>return a dictionary: char, all the words that starts with that char </char></returns>
        private Dictionary<char, List<string>> NumberOfUniqueChar()
        {
            char c;
            Dictionary<char, List<string>> queryDic = new Dictionary<char, List<string>>();
            foreach (string word in query)
            {
                if (word == "")
                    continue;
                c = word[0];
                if (!Char.IsLetter(c))
                    c = '#';
                if (!queryDic.ContainsKey(c))
                {
                    List<string> list = new List<string>();
                    list.Add(word);
                    queryDic.Add(c, list);
                }
                else
                {
                    queryDic[c].Add(word);
                }

            }
            return queryDic;
        }

        #endregion

        #region ranker setters

        /// <summary>
        /// update the postingPositionsDicin the ranker.
        /// get all the positions from the postring info
        /// </summary>
        /// <param name="list">list of the lines from the posting, one for each word in the query</param>
        private void GetWordsPositions(List<string> list)
        {
            Dictionary<string, Dictionary<string, List<int>>> postingInfoDic = new Dictionary<string, Dictionary<string, List<int>>>();
            foreach (string line in list)
            {
                int index = line.IndexOf("FBIS");
                string currentWord = line.Substring(0, index - 1);
                string[] splitted1 = line.Substring(index).Split(',');
                for (int i = 0; i < splitted1.Length; i++) //FBIS3-6905 3 16 75 92,FBIS4-44996 1 123
                {
                    List<int> positionsList = new List<int>();
                    string currentDocLine = splitted1[i];
                    string[] splitted2 = currentDocLine.Split(' ');
                    string docID = splitted2[0];
                    for (int j = 2; j < splitted2.Length; j++)
                    {
                        positionsList.Add(Int32.Parse(splitted2[j]));
                    }
                    //wordPositions.Add(currentWord, positionsList);
                    if (!postingInfoDic.ContainsKey(docID))
                    {
                        Dictionary<string, List<int>> wordPositions = new Dictionary<string, List<int>>();
                        wordPositions.Add(currentWord, positionsList);
                        postingInfoDic.Add(docID, wordPositions);

                    }
                    else
                    {
                        Dictionary<string, List<int>> wordPositions = postingInfoDic[docID]; //<word, list of positions>
                        wordPositions.Add(currentWord, positionsList);
                    }
                }
            }
            ranker.postingPositionsDic = postingInfoDic;
        }

        /// <summary>
        /// set the average length of all the documents in the ranker
        /// </summary>
        /// <param name="avg"></param>
        public void SetAvgLengthOfAllDocs(double avg)
        {
            ranker.avgLengthOfAllDocs = avg;
        }

        /// <summary>
        /// create a dictionary with each word from the query and its IDF that was already calculated in the dictionary
        /// </summary>
        /// <returns>dictionary <word, IDF> </word></returns>
        private Dictionary<string, double> SetIdfForQuery()
        {
            Dictionary<string, double> idfDic = new Dictionary<string, double>();
            foreach (string word in query)
            {
                if (!idfDic.ContainsKey(word))
                    if (dictionary.ContainsKey(word))
                        idfDic.Add(word, Double.Parse(dictionary[word][2]));
                    else
                        idfDic.Add(word, 0);
            }
            return idfDic;
        }

        #endregion

        /* 
         private Dictionary<double, List<string>> CalcFiDocDic(string term, bool filterlanguages)
         {
             Dictionary<double, List<string>> tfDocsDic = new Dictionary<double, List<string>>();
             int index = term.IndexOf("FBIS");
             string currentWord = term.Substring(0, index - 1);
             ranker.docsPerTerm.Add(currentWord, 0);
             string[] termInfo = term.Substring(index).Split(',');
             string[] temp;
             for (int i = 0; i < termInfo.Length - 1; i++)
             {
                 temp = termInfo[i].Split(' ');
                 string doc = temp[0];

                 double amount = Int32.Parse(temp[1]);
                 if (filterlanguages && !languages.ContainsKey(allDocsDic[doc].language))
                     continue;
                 amount = Math.Round(amount / allDocsDic[doc].max_tf, 5);
                 if (!tfDocsDic.ContainsKey(amount))
                 {
                     List<string> docList = new List<string>();
                     docList.Add(doc);
                     tfDocsDic.Add(amount, docList);
                 }
                 else
                 {
                     tfDocsDic[amount].Add(doc);
                 }
             }
             return tfDocsDic;
         }
         */ //calcFiDocDic
    }
}
