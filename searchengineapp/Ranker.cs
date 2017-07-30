using CSharpStringSort;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngineApp
{
    /// <summary>
    /// get a qurey and return a list of relevant documents
    /// </summary>
    class Ranker
    {
        public string postingPath { get; set; } //the path of the posting folder
        public Dictionary<string, Document> corpusDocs { get; set; } //docID, Document
        public double avgLengthOfAllDocs { get; set; }
        public double avgHeaderLength { get; set; }
        public int N { get; set; } //total # of docs
        public Dictionary<string, int> docsPerTerm { get; set; } //term, #docs -> ni
        public Dictionary<string, Dictionary<string, List<int>>> postingPositionsDic { get; set; } // docID, <word, list of positions>

        HashSet<string> importantWords;
        bool withStemmer;
        public Dictionary<string, double> queryTermIdf { get; set; } //term, idf
        List<string> query;

        public Ranker(List<string> q, HashSet<string> iw, bool stem) //path include stemmer/no stemmer folder
        {
            N = 0;
            docsPerTerm = new Dictionary<string, int>();
            queryTermIdf = new Dictionary<string, double>();
            query = q;
            postingPositionsDic = new Dictionary<string, Dictionary<string, List<int>>>();
            importantWords = iw;
            withStemmer = stem;
        }

        /// <summary>
        /// rank each document from the filtered corpus from Search class according to the query
        /// a query with one word only won't be ranked with proximity search
        /// </summary>
        /// <returns>Dictionary of <rank, hashSet with all the documents with the same rank> </returns>
        public Dictionary<double, HashSet<string>> RankerRelevantDocs()
        {
            double rank = 0;
            //get rank for all docs
            Dictionary<double, HashSet<string>> ranks = new Dictionary<double, HashSet<string>>(); //rank, list of docs in the same rank
            foreach (string docID in corpusDocs.Keys)
            {

                rank = ScoreBM25(corpusDocs[docID]);
                double norm=0.5;
                if (withStemmer)
                    norm = 1;
                rank += CosSim(docID)*norm;
                
                if (query.Count > 1)
                    rank += ProximitySearch(corpusDocs[docID]);

                rank = Math.Round(rank, 3);
                if (!ranks.ContainsKey(rank))
                {
                    HashSet<string> docsRankSet = new HashSet<string>();
                    docsRankSet.Add(docID);
                    ranks.Add(rank, docsRankSet);
                }
                else
                {
                    ranks[rank].Add(docID);
                }
            }
            return ranks;
        }

        /// <summary>
        /// calculating the rank of a document considering the locations of the words from the query in the document
        /// </summary>
        /// <param name="doc">the current doc</param>
        /// <returns>rank of the document</returns>
        private double ProximitySearch(Document doc)
        {
            double ans = 0, dl = doc.sumOfWords, b = 0.5, k1 = 1.2;
            double K = k1 * ((1 - b) + (b * dl) / avgLengthOfAllDocs);
            Dictionary<Tuple<string, string>, List<Tuple<int, int>>> megaDic = new Dictionary<Tuple<string, string>, List<Tuple<int, int>>>(); //Qd(q)
            Dictionary<Tuple<string, string>, List<Tuple<int, int>>> finalDic = new Dictionary<Tuple<string, string>, List<Tuple<int, int>>>(); //Ad(q)
            HashSet<int> locationsSet = new HashSet<int>(); //Pd(q)
            for (int i = 0; i < query.Count; i++)
            {
                for (int j = 0; j < query.Count && j != i; j++) //query[i] X query[j]
                {
                    if (postingPositionsDic[doc.docID].ContainsKey(query[i]) && postingPositionsDic[doc.docID].ContainsKey(query[j]))
                    {
                        Tuple<string, string> pair = new Tuple<string, string>(query[j], query[i]);
                        CartesianProduct(doc.docID, pair, ref megaDic, ref locationsSet);
                    }
                    else
                        break;
                }
            }
            int[] locArr = locationsSet.ToArray<int>(); //Pd(q)
            Array.Sort(locArr);
            bool isExist;
            for (int i = 0; i < locArr.Length - 1; i++)
            {
                isExist = false;
                Tuple<int, int> loc = new Tuple<int, int>(locArr[i], locArr[i + 1]);
                foreach (Tuple<string, string> wordsPair in megaDic.Keys)
                {
                    foreach (Tuple<int, int> item in megaDic[wordsPair])
                    {
                        if (item.Equals(loc))
                        {
                            if (!finalDic.ContainsKey(wordsPair))
                            {
                                List<Tuple<int, int>> list = new List<Tuple<int, int>>();
                                list.Add(item);
                                finalDic.Add(wordsPair, list);
                            }
                            else
                            {
                                finalDic[wordsPair].Add(item);
                            }
                            isExist = true;
                            break;
                        }
                    }
                    if (isExist)
                        break;
                }

            }
            double acc = 0;
            foreach (string term in query)
            {
                foreach (Tuple<string, string> wordsPair in finalDic.Keys)
                {
                    if (term.Equals(wordsPair.Item1) || term.Equals(wordsPair.Item2))
                    {
                        foreach (Tuple<int, int> locPair in finalDic[wordsPair])
                        {
                            double idf1 = queryTermIdf[wordsPair.Item1];
                            double idf2 = queryTermIdf[wordsPair.Item2];
                            double mechane = Math.Pow(locPair.Item1 - locPair.Item2, 2);
                            acc += ((idf1 + idf2) / mechane);
                        }
                    }
                }
                double temp = acc * (k1 + 1) / (acc + K);
                ans += Math.Min(1, queryTermIdf[term]) * temp;
            }
            return ans;
        }

        /// <summary>
        /// calculating cartesian product of all the locations of 2 words (pair)
        /// </summary>
        /// <param name="docID">current docID</param>
        /// <param name="pair">the pair of the words to do cartesian product on</param>
        /// <param name="megaDic">the dictionary of tuples and list of tuples</param>
        /// <param name="locationsSet">hashSet that save all the locations of all the words</param>
        private void CartesianProduct(string docID, Tuple<string, string> pair, ref Dictionary<Tuple<string, string>, List<Tuple<int, int>>> megaDic, ref HashSet<int> locationsSet)
        {
            List<Tuple<int, int>> tupleList = new List<Tuple<int, int>>();
            List<int> list1 = postingPositionsDic[docID][pair.Item1];
            List<int> list2 = postingPositionsDic[docID][pair.Item2];
            for (int i = 0; i < list1.Count; i++)
            {
                for (int j = 0; j < list2.Count; j++)
                {
                    if (list1[i] < list2[j])
                    {
                        tupleList.Add(new Tuple<int, int>(list1[i], list2[j]));
                        locationsSet.Add(list1[i]);
                        locationsSet.Add(list2[j]);
                    }
                }
            }
            megaDic[pair] = tupleList;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="doc">the current doc</param>
        /// <returns>rank of the document</returns>
        private double ScoreBM25(Document doc)
        {
            double ans = 0, dl = doc.sumOfWords, tmp = 0;
            int ni = 0, qfi = 1;
            double b = 0.75, k1 = 1.2;
            int k2 = 1000;
            double beforeLog1, part2, part3, fi = 0;
            double K = k1 * ((1 - b) + ((b * dl) / avgLengthOfAllDocs));
            foreach (string word in query)
            {
                if (doc.queryFreq.ContainsKey(word))
                    fi = doc.queryFreq[word];
                else
                    fi = 0;
                if (docsPerTerm.ContainsKey(word))
                    ni = docsPerTerm[word];
                else
                    ni = 0;
                beforeLog1 = (1 / ((ni + 0.5) / (N - ni + 0.5)));
                part2 = ((k1 + 1) * fi) / (K + fi);
                part3 = ((k2 + 1) * qfi) / (k2 + qfi);
                tmp = Math.Log(beforeLog1, 2);
                ans += tmp * part2 * part3;
                //Logger.MyLogger("docID: " + doc.docID + " ans: " + ans + " tmp: " + tmp);           
            }
            return ans;
        }

        /// <summary>
        /// calculating Cosine Similarty
        /// </summary>
        /// <param name="docID"></param>
        /// <returns>rank</returns>
        private double CosSim(string docID)
        {
            double cosSim = 0;
            int queryAmount = 0;
            foreach (string word in query)
            {
                if (importantWords.Contains(word))
                    queryAmount = queryAmount + 4; //2^2
                else
                    queryAmount = queryAmount + 1; //1^2
            }
            double docVec = (corpusDocs[docID].docVector) * Math.Sqrt(queryAmount);
            cosSim = (InnerProduct(docID)) / docVec;
            return cosSim;
        }

        /// <summary>
        /// Calculating inner product
        /// for the numerator in the cosine
        /// </summary>
        /// <param name="docID"></param>
        /// <returns></returns>
        private double InnerProduct(string docID)
        {
            double ans = 0;
            double idf, tf, normalize;
            foreach (string word in query)
            {
                if (queryTermIdf.ContainsKey(word))
                    idf = queryTermIdf[word];
                else
                    idf = 0;
                if (corpusDocs[docID].queryFreq.ContainsKey(word))
                    tf = (double)corpusDocs[docID].queryFreq[word]/corpusDocs[docID].sumOfWords;
                else
                    tf = 0;
                if (importantWords.Contains(word))
                    normalize = 2;
                else
                    normalize = 1;
                ans += (tf * idf*normalize);
            }

            return ans;
        }

        #region Unused

        private double RankByHeader(Document doc)
        {

            double ans = 0, wc = 0.3, factorHtf;
            string header = doc.header;
            foreach (string word in query)
            {
                int htf = header.Select((c, j) => header.Substring(j)).Count(sub => sub.StartsWith(word)); //maybe count the sum of appearences in the header
                factorHtf = htf * wc;
                double tfHeader = htf / (1 + (header.Length / avgHeaderLength - 1));
                ans += tfHeader;
            }
            //double numOfWordsInQeury = query.Count;
            //foreach (string word in query)
            //{
            //        if (doc.header.Contains(word))
            //        {
            //            ans += (1 / numOfWordsInQeury);
            //            break;
            //        }
            //    
            //}
            //Logger.MyLogger("for " + doc.docID + " ans is: " + ans);
            return ans;
        }

        private double ScoreBM25F(Document doc)
        {
            double ans = 0, temp, k1 = 1.2, wc = 0.4;
            double btf, factorHtf, factorBtf, idf;
            string header = doc.header;

            foreach (string word in query)
            {

                //header
                int htf = header.Select((c, j) => header.Substring(j)).Count(sub => sub.StartsWith(word)); //maybe count the sum of appearences in the header
                factorHtf = htf * wc;
                double tfHeader = htf / (1 + (header.Length / avgHeaderLength - 1));
                //body
                if (doc.queryFreq.ContainsKey(word))
                    btf = doc.queryFreq[word];
                else
                    btf = 0;
                factorBtf = btf * (1 - wc);
                double tfBody = btf / (1 + (doc.sumOfWords / avgLengthOfAllDocs - 1));
                temp = tfBody * factorBtf + tfHeader * factorHtf;
                idf = Math.Log(N / queryTermIdf[word], 2);
                ans += (temp / (k1 + temp)) * idf;
            }
            return ans;
        }

        #endregion

    }
}
