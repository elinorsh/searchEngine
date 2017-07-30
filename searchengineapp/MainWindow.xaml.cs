using CSharpStringSort;
using Microsoft.Win32;
using NHunspell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SearchEngineApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool withStemmer;
        string loadPath;
        string savePath;
        PreTextProcess preTextProc;
        Stopwatch timer;
        HashSet<string> selectedLan;

        bool SaveToFile = false;
        string postingFolderPath;
        string dataFolderPath;
        public enum Status { Indexer, Loaded, NotReady };
        string saveToFilePath;
        Status status;

        /// <summary>
        /// init the main window
        /// </summary>
        public MainWindow()
        {
            withStemmer = false;
            loadPath = "";
            savePath = "";
            saveToFilePath = "";
            InitializeComponent();
            DataContext = preTextProc;
            timer = new Stopwatch();
            selectedLan = new HashSet<string>();
            preTextProc = new PreTextProcess();
            postingFolderPath = "";
            dataFolderPath = "";
            status = Status.NotReady;
            Directory.GetCurrentDirectory();

        }

        /// <summary>
        /// the event IsFinish calling that function to reable all the buttons on screen
        /// </summary>
        private void FinishIndexing()
        {
            int numOfWords;
            int numOfDocs;
            preTextProc.infoAfterFinish(out numOfDocs, out numOfWords);
            timer.Stop();
            System.Windows.MessageBox.Show(numOfDocs + " Docs were indexed\n" + numOfWords + " Unique term were found\n" + "In " + timer.Elapsed + " ms");
            EnablesAllButtoms(true);
            this.Dispatcher.Invoke(() =>
            {
                languagesList.Visibility = Visibility.Visible;
            });
        }

        /// <summary>
        /// enable or disable all the buttoms in the window
        /// </summary>
        /// <param name="isEnable"></param>
        private void EnablesAllButtoms(bool isEnable)
        {
            this.Dispatcher.Invoke(() =>
            {
                Go_Buttom.IsEnabled = isEnable;
                DictionaryShow_Buttom.IsEnabled = isEnable;
                BrowseFromButton.IsEnabled = isEnable;
                BrowseToButton.IsEnabled = isEnable;
                Clear_Buttom.IsEnabled = isEnable;
                StemmerCheckBox.IsEnabled = isEnable;
                LoadData_Buttom.IsEnabled = isEnable;
                PostingPath_Buttom.IsEnabled = isEnable;
                QueryButtom.IsEnabled = isEnable;
                QueryInput.IsEnabled = isEnable;
                SaveToFileCheckBox.IsEnabled = isEnable;
                LoadQureisFile.IsEnabled = isEnable;
                if (languagesList.Visibility == Visibility.Visible)
                    languagesList.IsEnabled = isEnable;
            });
        }

        /// <summary>
        /// to make sure that other windows can appear above the main window
        /// </summary>
        /// <param name="sender">press on other window</param>
        /// <param name="e">the other window</param>
        private void Window_Activated(object sender, EventArgs e)
        {
            var window = sender as MainWindow;

            window.Topmost = false;
        }

        #region Buttoms
        //***************Part1*************//

        /// <summary>
        /// choosing loading path to load the corpus and the stop words list from.
        /// if the folder does not containe a stop_words.txt file, error
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e) //load 
        {
            FolderBrowserDialog browser = new FolderBrowserDialog();
            browser.ShowDialog();
            browser.ShowNewFolderButton = false;
            if (File.Exists(browser.SelectedPath + "\\" + "stop_words.txt"))
            {
                textBoxLoad.Text = browser.SelectedPath;
                loadPath = browser.SelectedPath;
            }
            else
            {
                if (browser.SelectedPath != "")
                    System.Windows.MessageBox.Show("please choose a folder that contains 'stop_words.txt' file");
            }
        }

        /// <summary>
        /// choosing saving path to save the dictionary and posting files.
        /// </summary>
        private void Button_Click_1(object sender, RoutedEventArgs e) //save
        {
            FolderBrowserDialog browser = new FolderBrowserDialog();
            browser.ShowDialog();
            browser.ShowNewFolderButton = true;
            textBoxSave.Text = browser.SelectedPath;
            savePath = browser.SelectedPath;
        }

        /// <summary>
        /// WithStemmer = true if the checkbox is checked
        /// </summary>
        /// <param name="sender">checkbox cheked</param>
        /// <param name="e"></param>
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            withStemmer = true;
        }

        /// <summary>
        /// WithStemmer = false if the checkbox is unchecked
        /// </summary>
        /// <param name="sender">checkbox uncheked</param>
        /// <param name="e"></param>
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            withStemmer = false;
        }

        /// <summary>
        /// clearing all the database and the files
        /// </summary>
        /// <param name="sender">clear buttom</param>
        /// <param name="e">Clear_Buttom</param>
        private void Button_Click_2(object sender, RoutedEventArgs e) //clear
        {
            if (preTextProc == null)
            {
                System.Windows.MessageBox.Show("Please press 'Go!' first");
                return;
            }
            Clear_Buttom.IsEnabled = false;
            textBoxLoad.Text = "";
            textBoxSave.Text = "";
            postingFolderPath = "";
            loadPath = "";
            savePath = "";
            File.Delete(saveToFilePath + @"\results.txt");
            saveToFilePath = "";
            SaveToFileCheckBox.IsChecked = false;
            if (preTextProc != null)
                preTextProc.ClearAll();
            Clear_Buttom.IsEnabled = true;
            languagesList.Visibility = Visibility.Hidden;
            status = Status.NotReady;
            AnswerTextList.ItemsSource = null;
            QueryInput.Clear();
            System.Windows.MessageBox.Show("All Clear!");
        }

        /// <summary>
        /// starting the process of indexing.
        /// allowed only after the user choose load and save paths
        /// </summary>
        /// <param name="sender">press "Go!"</param>
        /// <param name="e">Go_Buttom</param>
        private void Button_Click_3(object sender, RoutedEventArgs e) //"GO!"
        {
            if (loadPath != "" && savePath != "")
            {
                Go_Buttom.IsEnabled = false;

                timer.Restart();
                preTextProc.PropertyChanged += delegate (Object sender1, PropertyChangedEventArgs e1)
                {
                    FinishIndexing();
                };
                preTextProc.withStemmer = withStemmer;
                if (withStemmer)
                    postingFolderPath = savePath + "\\" + "WithStemmer";
                else
                    postingFolderPath = savePath + "\\" + "NoStemmer";
                Thread t = new Thread(() => preTextProc.StartIndexing(savePath, loadPath));
                EnablesAllButtoms(false);
                status = Status.Indexer;
                t.Start();
            }
            else
            {
                System.Windows.MessageBox.Show("Please choose load and save path");
            }
            //timerText
        }

        /// <summary>
        /// taking a string that represeting the dictionary and showing it to the user
        /// </summary>
        /// <param name="sender">press "Show Dictionary"</param>
        /// <param name="e">DictionaryShow_Buttom</param>
        private void DictionaryShow_Buttom_Click(object sender, RoutedEventArgs e) //Show Dictionary
        {
            if (preTextProc == null)
            {
                System.Windows.MessageBox.Show("Please press 'Go!' first");
                return;
            }
            List<string> list = new List<string>();
            preTextProc.ShowDictionary(ref list);
            if (list.Count > 0)
            {
                DictionaryShow_Buttom.IsEnabled = false;
                AnswerTextList.ItemsSource = list;
                AnswerTextList.Visibility = Visibility.Visible;
                DictionaryShow_Buttom.IsEnabled = true;
            }
            else
            {
                System.Windows.MessageBox.Show("Please load dictionary");
                return;
            }
        }

        /// <summary>
        /// updating the languages set and opening a new window to present them to the user
        /// </summary>
        /// <param name="sender">press "Choose Languages"</param>
        /// <param name="e">languagesList_Click</param>
        private void languagesList_Click(object sender, RoutedEventArgs e)
        {
            HashSet<string> set = preTextProc.GetLanguages();
            WindowLanguagesList window = new WindowLanguagesList(set);
            window.Show();
            selectedLan = window.selectedLan;
        }

        //***************Part2*************//

        /// <summary>
        /// get the input query, check that all the data is in the memory
        /// and call the function that execute the query
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void QueryButtom_Click(object sender, RoutedEventArgs e) //query
        {
            if (QueryInput.Text == null || QueryInput.Text == "")
            {
                System.Windows.MessageBox.Show("Please write a query");
                return;
            }
            if (status == Status.NotReady)
            {
                System.Windows.MessageBox.Show("Error, please load or create all the relevant data");
                return;
            }
            EnablesAllButtoms(false);
            AnswerTextList.ItemsSource = null;
            List<string> list = GetQueryResults(QueryInput.Text, null);
            if (list != null)
            {
                AnswerTextList.ItemsSource = list;
                AnswerTextList.Visibility = Visibility.Visible;
            }
            EnablesAllButtoms(true);
        }

        /// <summary>
        /// get the a path from the user of the posting folder
        /// and chech that this folder is valid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PostingPath_Buttom_Click(object sender, RoutedEventArgs e) //posting
        {
            FolderBrowserDialog browser = new FolderBrowserDialog();
            browser.ShowDialog();
            browser.ShowNewFolderButton = false;
            if (isValidPostingFolder(browser.SelectedPath))
            {
                postingFolderPath = browser.SelectedPath;
                if (dataFolderPath != "" && (withStemmer && postingFolderPath.Contains("WithStemmer")) || (!withStemmer) && postingFolderPath.Contains("NoStemmer"))
                {
                    status = Status.Loaded;
                    System.Windows.MessageBox.Show("Posting file chosen!");
                }
                else
                    System.Windows.MessageBox.Show("please choose a folder that contains all the posting files according to your Stemmer choice");
            }
            else
            {
                if (browser.SelectedPath != "")
                    System.Windows.MessageBox.Show("please choose a folder that contains all the posting files");
            }
        }

        /// <summary>
        /// get a folder from the user and load the data:
        /// stop_words, dicionary and documents
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadData_Buttomm_Click(object sender, RoutedEventArgs e) //load data
        {
            EnablesAllButtoms(false);
            FolderBrowserDialog browser = new FolderBrowserDialog();
            browser.ShowDialog();
            browser.ShowNewFolderButton = false;
            string docsPath, dicPath;
            bool ans;
            try
            {
                //stopwords
                string selectedPath = browser.SelectedPath;
                string stopwordsPath = selectedPath + "\\stop_words.txt";
                preTextProc.LoadStopWords(stopwordsPath);

                //dictionary
                if (File.Exists(selectedPath + "\\NoStemmerDictionary.txt") && !withStemmer)
                {
                    dicPath = selectedPath + "\\NoStemmerDictionary.txt";
                }
                else if (File.Exists(selectedPath + "\\StemmerDictionary.txt") && withStemmer)
                {
                    dicPath = selectedPath + "\\StemmerDictionary.txt";
                }
                else
                {
                    System.Windows.MessageBox.Show("Please choose a valid Dictionary file");
                    EnablesAllButtoms(true);
                    return;
                }
                ans = preTextProc.LoadDictionary(dicPath);
                if (!ans)
                {
                    System.Windows.MessageBox.Show("Please choose a valid Dictionary file");
                    EnablesAllButtoms(true);
                    return;
                }

                //documents
                if (File.Exists(selectedPath + "\\NoStemmerDocs.txt") && !withStemmer)
                {
                    docsPath = selectedPath + "\\NoStemmerDocs.txt";

                }
                else if (File.Exists(selectedPath + "\\StemmerDocs.txt") && withStemmer)
                {
                    docsPath = selectedPath + "\\StemmerDocs.txt";
                }
                else
                {
                    System.Windows.MessageBox.Show("Please choose a valid document file");
                    EnablesAllButtoms(true);
                    return;
                }
                ans = preTextProc.LoadDocs2(docsPath);
                if (ans)
                {
                    languagesList.Visibility = Visibility.Visible;
                }
                else
                {
                    System.Windows.MessageBox.Show("Please choose a valid document file");
                    EnablesAllButtoms(true);
                    return;
                }
                dataFolderPath = selectedPath;
                System.Windows.MessageBox.Show("All data Loaded!");
                if (postingFolderPath != "")
                    status = Status.Loaded;
                //preTextProc.IndexingDocuments(loadPath, savePath, withStemmer);
            }
            catch
            {
                System.Windows.MessageBox.Show("please choose a folder that contains valid stopwords, dictionary and docs files");

            }
            EnablesAllButtoms(true);

        }

        /// <summary>
        /// load a file with queris and execute them one by one
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadQureisFile_Buttom_Click(object sender, RoutedEventArgs e)
        {
            if (status == Status.NotReady)
            {
                System.Windows.MessageBox.Show("Error, please load or create all the relevant data first");
                return;
            }
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "Text files (*.txt)|*.txt|All files(*.*)|*.*";
            if (dialog.ShowDialog() == true)
            {              
                EnablesAllButtoms(false);
                AnswerTextList.ItemsSource = null;
                List<string> results = new List<string>();
                string filename = dialog.FileName;
                IEnumerable<string> lines = File.ReadLines(filename);
                foreach (string queryLine in lines)
                {
                    string[] splitted = queryLine.Split('\t');
                    string queryID = splitted[0];
                    string query = splitted[1];
                    AnswerTextList.ItemsSource = null;
                    List<string> queryResults = GetQueryResults(query, Int32.Parse(queryID));
                    if (queryResults != null)
                        results.AddRange(queryResults);
                }
                AnswerTextList.ItemsSource = results;
                AnswerTextList.Visibility = Visibility.Visible;
                if (SaveToFile)
                {
                    WriteResToFile(results);
                }
                EnablesAllButtoms(true);
            }
        }

        /// <summary>
        /// get a path from the user where to save the results
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveToFile_Checked(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog browser = new FolderBrowserDialog();
            browser.ShowDialog();
            browser.ShowNewFolderButton = false;
            if (browser.SelectedPath != "")
            {
                saveToFilePath = browser.SelectedPath;
                SaveToFile = true;
            }
        }

        /// <summary>
        /// uncheck the option of save to file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveToFile_Unchecked(object sender, RoutedEventArgs e)
        {
            saveToFilePath = "";
            SaveToFile = false;
        }

        #endregion

        #region QueryCalc

        /// <summary>
        /// compare between 2 words with N-Gram (N=2).
        /// and return true if the threshold value is bigger than "madad".
        /// </summary>
        /// <param name="word1">first word to compare</param>
        /// <param name="word2">second word to compare</param>
        /// <param name="madad">threshold</param>
        /// <returns></returns>
        private bool NGramComparison(string word1, string word2, double madad)
        {
            if (word1 == "program")
                Console.WriteLine();
            HashSet<string> set1 = GetNGram(word1);
            HashSet<string> set2 = GetNGram(word2);
            List<string> united = set1.Union<string>(set2).ToList<string>();
            List<string> intersect = set2.Intersect(set1).ToList<string>();
            if (intersect.Count == 0)
                return false;
            double inter = intersect.Count;
            double unit = united.Count;
            madad = (double)(inter / unit);

            return madad >= 0.71;

        }

        /// <summary>
        /// return a 2-Gram substrings of the word
        /// </summary>
        /// <param name="word">the word to do NGram on</param>
        /// <returns>hashSet that conatins all the substrings</returns>
        private HashSet<string> GetNGram(string word) //2-Gram-
        {
            HashSet<string> set = new HashSet<string>();
            for (int i = 0; i < word.Length - 1; i++)
            {
                string gram = word[i] + "" + word[i + 1];
                set.Add(gram);
            }

            return set;
        }
        
        /// <summary>
        /// return a list of strings, when each string is a line of result to the user.
        /// the line contains the rank and the document ID.
        /// </summary>
        /// <param name="queryInput">the query</param>
        /// <param name="qID">id if exist</param>
        /// <returns></returns>
        private List<string> GetQueryResults(string queryInput, int? qID)
        {
            AnswerTextList.ItemsSource = null;
            StringBuilder text = new StringBuilder(queryInput.ToLower());
            string parsePath;
            if (status == Status.Indexer)
                parsePath = loadPath;
            else
                parsePath = dataFolderPath;
            HashSet<string> importantWords = new HashSet<string>();
            Parse parser = new Parse(parsePath + "\\" + "stop_words.txt");
            List<string> orgQuery = parser.GetTerms(text, false);
            Dictionary<string, List<string>> synonymsDic = GetAllSynonyms(orgQuery, ref importantWords);
            if(withStemmer)
            orgQuery = parser.GetTerms(text, withStemmer);
            orgQuery = RemoveDupWords(orgQuery);
            Dictionary<double, HashSet<string>> dicResults = new Dictionary<double, HashSet<string>>();
            Dictionary<double, HashSet<string>> dicResultsFinal = new Dictionary<double, HashSet<string>>();
            List<string> listResults = new List<string>();
            Search search = new Search(orgQuery, preTextProc.GetDocuments(), postingFolderPath, preTextProc.GetDictinary(), selectedLan, qID, importantWords);
            search.withStemmer = withStemmer;
            search.SetAvgLengthOfAllDocs(preTextProc.avgLengthOfAllDocs);
            for (int i = 0; i < orgQuery.Count; i++)
            {
                string currentWord = orgQuery[i];
                foreach (string word in synonymsDic[currentWord])
                {
                    List<string> newQuery = new List<string>(orgQuery);
                    newQuery[i] = word;
                    search.SetNewQuery(newQuery);
                    search.SetAvgLengthOfAllDocs(preTextProc.avgLengthOfAllDocs);
                    
                    if (search.FilterDocuments())
                    {
                        dicResults = search.RankerRelevantDocs();
                        foreach (double rank in dicResults.Keys)
                        {
                            if (!dicResultsFinal.ContainsKey(rank))
                            {
                                dicResultsFinal.Add(rank, dicResults[rank]);
                            }
                            else
                            {
                                HashSet<string> temp = MergeHashSet(dicResults[rank], dicResultsFinal[rank]);
                                dicResultsFinal[rank] = temp;
                            }
                        }
                    }
                }
            }

            foreach (string importantWord in importantWords)
            {
                List<string> list = new List<string>();
                list.Add(importantWord);
                search.SetNewQuery(list);
                search.SetAvgLengthOfAllDocs(preTextProc.avgLengthOfAllDocs);
                if (search.FilterDocuments())
                {
                    dicResults = search.RankerRelevantDocs();
                    foreach (double rank in dicResults.Keys)
                    {
                        if (!dicResultsFinal.ContainsKey(rank))
                        {
                            dicResultsFinal.Add(rank, dicResults[rank]);
                        }
                        else
                        {
                            HashSet<string> temp = MergeHashSet(dicResults[rank], dicResultsFinal[rank]);
                            dicResultsFinal[rank] = temp;
                        }
                    }
                }
            }

            listResults = GetFinalListResults(dicResultsFinal);
            if (listResults.Count == 0)
            {
                List<string> errorlist = new List<string>();
                errorlist.Add("No documents found for this query :(");
                AnswerTextList.Visibility = Visibility.Visible;
                AnswerTextList.ItemsSource = errorlist;
                EnablesAllButtoms(true);
                return null;
            }
            AnswerTextList.ItemsSource = listResults;
            AnswerTextList.Visibility = Visibility.Visible;

            if (SaveToFile)
            {
                WriteResToFile(listResults);
            }

            return listResults;
        }

        /// <summary>
        /// return a dictionary that each key is a word in the query and the value
        /// is a list of another words that similiar.
        /// updating the importantWords set.
        /// </summary>
        /// <param name="orgQuery">query</param>
        /// <param name="importantWords">refrences of hashSet of the important words</param>
        /// <returns></returns>
        private Dictionary<string, List<string>> GetAllSynonyms(List<string> orgQuery, ref HashSet<string> importantWords)
        {
            Dictionary<string, List<string>> synonymsDic = new Dictionary<string, List<string>>();
            MyThes thes = new MyThes(Directory.GetCurrentDirectory() + @"\th_en_us_new.dat");
            Stemmer stem = new Stemmer();
            string[] toSort = preTextProc.GetDictinary().Keys.ToArray<string>();
            string[] sorted = Sedgewick.Sort(toSort);
            string currentWordStem;
            for (int i = 0; i < orgQuery.Count; i++)
            {
                List<string> list = new List<string>();
                string currentWord = orgQuery[i];
                if(withStemmer)
                    currentWordStem = stem.stemTerm(currentWord);
                else
                    currentWordStem=currentWord;
                list.Add(currentWordStem);
                synonymsDic.Add(currentWordStem, list);
                using (Hunspell hunspell = new Hunspell("en_us.aff", "en_us.dic"))
                {
                    ThesResult tr = thes.Lookup(currentWord, hunspell);
                    if (tr == null)
                    {
                        importantWords.Add(currentWord);
                    }
                    else
                    {
                        ThesMeaning meanings = tr.Meanings[0];
                        foreach (string synonym in meanings.Synonyms)
                        {
                            if (!NGramComparison(currentWord, synonym.ToLower(), 0.75))
                                continue;
                        }
                    }
                }
                if (withStemmer)
                    continue;
                int loc = Array.IndexOf(sorted, currentWord);
                try
                {
                    if (NGramComparison(currentWord, sorted[loc + 1], 0.8))
                        synonymsDic[currentWord].Add(sorted[loc + 1]);

                    if (NGramComparison(currentWord, sorted[loc + 2], 0.8))
                        synonymsDic[currentWord].Add(sorted[loc + 2]);

                    if (NGramComparison(currentWord, sorted[loc - 1], 0.8))
                        synonymsDic[currentWord].Add(sorted[loc - 1]);

                    if (NGramComparison(currentWord, sorted[loc - 2], 0.8))
                        synonymsDic[currentWord].Add(sorted[loc - 2]);
                }
                catch { }
                string stemWord = stem.stemTerm(currentWord);
                if(preTextProc.GetDictinary().ContainsKey(stemWord))
                    synonymsDic[currentWord].Add(stemWord);
            }



            return synonymsDic;
        }

        /// <summary>
        /// helper function that get a dictionary of ranks and dicionaryID and return the
        /// 50 documents with the highest rank
        /// </summary>
        /// <param name="dicResultsFinal"></param>
        /// <returns></returns>
        private List<string> GetFinalListResults(Dictionary<double, HashSet<string>> dicResultsFinal)
        {
            List<string> listResults = new List<string>();
            HashSet<string> docs = new HashSet<string>();
            double[] sorted = dicResultsFinal.Keys.ToArray<double>();
            Array.Sort(sorted);
            int counter = 0;
            for (int i = 0; counter < 50 && i < sorted.Length; i++)
            {
                foreach (string docID in dicResultsFinal[sorted[sorted.Length - 1 - i]])
                {
                    if (counter < 50)
                    {
                        if (docs.Add(docID))
                        {
                            listResults.Add(Search.queryID + " 0 " + docID + " " + sorted[sorted.Length - 1 - i] + " 1 3");
                            counter++;
                        }
                    }
                }
            }
            return listResults;
        }

        /// <summary>
        /// write a list of results into a file
        /// </summary>
        /// <param name="list">the list of the results</param>
        private void WriteResToFile(List<string> list)
        {
            string filename;
            //if (withStemmer)
            //    filename = saveToFilePath + "\\resultsWithStemmer.txt";
            //else
            //    filename = saveToFilePath + "\\resultsNoStemmer.txt";
            filename = saveToFilePath + "\\results.txt";

            if (File.Exists(filename))
                File.Delete(filename);
            FileStream fs = new FileStream(filename, FileMode.Create);
            StreamWriter wrtier = new StreamWriter(fs);
            foreach (string line in list)
            {
                wrtier.WriteLine(line);
            }
            wrtier.Flush();
            wrtier.Close();
            fs.Close();
        }

        /// <summary>
        /// an event that check when the user finish writing the first word in the query text box
        /// and a space, to present him the autocomplete
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        #endregion

        private void QueryInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (QueryInput.Text.Length > 0)
                {
                    string[] splitted = QueryInput.Text.Split(' ');
                    if (splitted.Length == 2)
                    {
                        if (splitted[1] == "")
                        {
                            string word = splitted[0];
                            AnswerTextList.ItemsSource = GetTop5Words(word);
                            AnswerTextList.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            AnswerTextList.ItemsSource = null;
                        }
                    }
                    else
                        AnswerTextList.ItemsSource = null;
                }
            }
            catch { }
        }

        #region helper functions

        /// <summary>
        /// helper function that merge 2 hashSets to one
        /// </summary>
        /// <param name="set1"></param>
        /// <param name="set2"></param>
        /// <returns>one hashSet</returns>
        private HashSet<string> MergeHashSet(HashSet<string> set1, HashSet<string> set2)
        {
            foreach (string item in set1)
            {
                set2.Add(item);
            }
            return set2;
        }

        /// <summary>
        /// check if a folder have all the files that necessary to be a valid posting folder
        /// </summary>
        /// <param name="folderPath"></param>
        /// <returns>true if valid</returns>
        private bool isValidPostingFolder(string folderPath)
        {
            string names = "#abcdefghijklmnopqrstuvwxyz";
            for (int i = 0; i < names.Length; i++)
            {
                string path = folderPath + "\\" + "[Posting]-" + names[i] + ".txt";
                if (!File.Exists(path))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// remove duplicates words in a query
        /// </summary>
        /// <param name="query"></param>
        /// <returns>the query after removing the duplicates</returns>
        private List<string> RemoveDupWords(List<string> query)
        {
            List<string> ans = new List<string>();
            HashSet<string> set = new HashSet<string>();
            foreach (var item in query)
            {
                if (set.Add(item))
                {
                    ans.Add(item);
                }
            }
            return ans;
        }

        /// <summary>
        /// get the top 5 words for a specific word
        /// </summary>
        /// <param name="word"></param>
        /// <returns>the top 5 words</returns>
        private List<string> GetTop5Words(string word)
        {
            List<string> list = new List<string>();
            List<string> ans = new List<string>();
            ans.Add("Can we offer you one of these words?");
            try
            {
                list = preTextProc.GetDictinary()[word];
                int numWords = list.Count - 4;
                for (int i = 0; i < numWords; i++)
                {
                    ans.Add(list[i + 4]);
                }
            }
            catch { }
            if (ans.Count == 1)
                return null;
            return ans;
        }

        //private 
        #endregion
    }
}


/*
        private List<string> GetQueryResults2(string queryInput, int? qID)
        {
            timer.Restart();
            StringBuilder text = new StringBuilder(queryInput.ToLower());
            string parsePath;
            if (status == Status.Indexer)
                parsePath = loadPath;
            else
                parsePath = dataFolderPath;
            Parse parser = new Parse(parsePath + "\\" + "stop_words.txt");
            List<string> queryTemp = parser.GetTerms(text, false);

            HashSet<string> importantWords = new HashSet<string>();
            MyThes thes = new MyThes(Directory.GetCurrentDirectory() + @"\th_en_us_new.dat");

            for (int i = 0; i < queryTemp.Count; i++) //words (such as names) that have no Synonyms 
            {
                using (Hunspell hunspell = new Hunspell("en_us.aff", "en_us.dic"))
                {
                    ThesResult tr = thes.Lookup(queryTemp[i], hunspell);
                    if (tr == null)
                        importantWords.Add(queryTemp[i]);
                }
            }


            List<string> orgQuery = parser.GetTerms(text, withStemmer);
            orgQuery = RemoveDupWords(orgQuery);
            List<string> newQuery = new List<string>(orgQuery);
            Dictionary<double, HashSet<string>> dicResults = new Dictionary<double, HashSet<string>>();
            Dictionary<double, HashSet<string>> dicResultsFinal = new Dictionary<double, HashSet<string>>();
            List<string> listResults = new List<string>();

            Search search = new Search(newQuery, preTextProc.GetDocuments(), postingFolderPath, preTextProc.GetDictinary(), selectedLan, qID, importantWords);
            search.SetAvgLengthOfAllDocs(preTextProc.avgLengthOfAllDocs);
            search.SetAvgHeaderLength(preTextProc.GetAvgHeaderLength());
            bool isFirstTime = true;
            //creating new queries with the SYNONYMS
            for (int i = 0; i < orgQuery.Count; i++)
            {
                using (Hunspell hunspell = new Hunspell("en_us.aff", "en_us.dic"))
                {
                    ThesResult tr = thes.Lookup(orgQuery[i], hunspell);
                    if (tr == null)
                    {
                        isFirstTime = false;
                        continue;
                    }

                    ThesMeaning meanings = tr.Meanings[0];

                    if (isFirstTime)
                    {
                        isFirstTime = false;
                        meanings.Synonyms.Add(orgQuery[0]);
                    }


                    foreach (string synonym in meanings.Synonyms)
                    {
                        if (!NGramComparison(newQuery[i], synonym.ToLower()))
                            continue;
                        if (timer.ElapsedMilliseconds > 50000)
                            break;
                        newQuery = new List<string>(orgQuery);
                        newQuery[i] = synonym.ToLower();
                        search.SetNewQuery(newQuery);
                        search.SetAvgLengthOfAllDocs(preTextProc.avgLengthOfAllDocs);
                        search.SetAvgHeaderLength(preTextProc.GetAvgHeaderLength());
                        try
                        {
                            if (!search.FilterDocuments())
                                continue;
                        }
                        catch
                        {
                            System.Windows.MessageBox.Show("Sorry! An error occurred :(");
                            return null;
                        }
                        dicResults = search.RankerRelevantDocs();
                        foreach (double rank in dicResults.Keys)
                        {
                            if (!dicResultsFinal.ContainsKey(rank))
                            {
                                dicResultsFinal.Add(rank, dicResults[rank]);
                            }
                            else
                            {
                                HashSet<string> temp = MergeHashSet(dicResults[rank], dicResultsFinal[rank]);
                                dicResultsFinal[rank] = temp;
                            }
                        }
                    }

                }
            }
            //creating new one word queries with the IMPORTANT words
            foreach (string importantWord in importantWords)
            {
                List<string> list = new List<string>();
                list.Add(importantWord);
                search.SetNewQuery(list);
                search.SetAvgLengthOfAllDocs(preTextProc.avgLengthOfAllDocs);
                search.SetAvgHeaderLength(preTextProc.GetAvgHeaderLength());
                if (search.FilterDocuments())
                {
                    dicResults = search.RankerRelevantDocs();
                    foreach (double rank in dicResults.Keys)
                    {
                        if (!dicResultsFinal.ContainsKey(rank))
                        {
                            dicResultsFinal.Add(rank, dicResults[rank]);
                        }
                        else
                        {
                            HashSet<string> temp = MergeHashSet(dicResults[rank], dicResultsFinal[rank]);
                            dicResultsFinal[rank] = temp;
                        }
                    }
                }
            }
            //all the word in query has no synonym
            if (dicResultsFinal.Count == 0)
            {
                search.SetNewQuery(orgQuery);
                search.SetAvgLengthOfAllDocs(preTextProc.avgLengthOfAllDocs);
                search.SetAvgHeaderLength(preTextProc.GetAvgHeaderLength());
                if (search.FilterDocuments())
                {
                    dicResults = search.RankerRelevantDocs();
                    foreach (double rank in dicResults.Keys)
                    {
                        if (!dicResultsFinal.ContainsKey(rank))
                        {
                            dicResultsFinal.Add(rank, dicResults[rank]);
                        }
                        else
                        {
                            HashSet<string> temp = MergeHashSet(dicResults[rank], dicResultsFinal[rank]);
                            dicResultsFinal[rank] = temp;
                        }
                    }
                }
            }
            listResults = GetFinalListResults(dicResultsFinal);
            if (listResults.Count == 0)
            {
                List<string> errorlist = new List<string>();
                errorlist.Add("No documents found for this query :(");
                AnswerTextList.Visibility = Visibility.Visible;
                AnswerTextList.ItemsSource = errorlist;
                EnablesAllButtoms(true);
                return null;
            }
            AnswerTextList.ItemsSource = listResults;
            AnswerTextList.Visibility = Visibility.Visible;

            if (SaveToFile)
            {
                WriteResToFile(listResults);
            }

            return listResults;
        }


*/