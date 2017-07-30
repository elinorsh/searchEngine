using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngineApp
{
    /// <summary>
    /// parsing a text with specific set of rules
    /// </summary>
    class Parse
    {
        string[] textDoc;
        HashSet<string> StopWordsHash;
        Dictionary<string, int> monthsName;
        HashSet<double> numsTerm;
        List<string> toReturn;
        Stemmer stemmer;
        bool isStemmer;

        /// <summary>
        /// constructor. init the data structure and loading the stop words from a text file.
        /// als, init the month names for the parsing of dates
        /// </summary>
        /// <param name="loadPath">the path to load files from</param>
        public Parse(string loadPath)
        {
            LoadStopWords(loadPath);

            monthsName = new Dictionary<string, int>();
            SetMonthsNames();
            numsTerm = new HashSet<double>();
            stemmer = new Stemmer();
            isStemmer = false;
            toReturn = new List<string>();
        }


        /// <summary>
        /// turning a text to an array that each cell represent a different term
        /// </summary>
        /// <param name="text">the text</param>
        /// <param name="toStem">parsing with or without stemmer</param>
        /// <returns></returns>
        public List<string> GetTerms(StringBuilder text, bool toStem)
        {
            toReturn.Clear();
            textDoc = text.ToString().Split(' ');
            int length = textDoc.Length;
            int loc = 0;
            double num;
            isStemmer = toStem;
            while (loc < length)
            {
                if (textDoc[loc] == "")
                {
                    loc++;
                    continue;
                }
                //new
                if ((!textDoc[loc].Any(char.IsLetterOrDigit)))//removing junk like ##, ^, ! and so on
                {
                    textDoc[loc] = "";
                    loc++;
                    continue;
                }
                //if (Double.TryParse(textDoc[loc], out numTerm))
                //{
                //    numsTerm.Add(numTerm);
                //}
                RemoveTagit(loc);
                RemovePunctuation(loc);
                string currentWord = textDoc[loc];
                if (Double.TryParse(currentWord, out num))
                {
                    //textDoc[loc] = OverMillion(num);
                    toReturn.Add(OverMillion(num));
                }
                if (monthsName.ContainsKey(currentWord))
                    Dates(loc);
                else if (currentWord.Any(char.IsDigit)) //if the term includes number
                {
                    if (!WordContainsNumbers(loc))
                        if (currentWord.Contains('/'))
                            WordIsFraction(loc);
                }
                else if (currentWord.Any(char.IsPunctuation))
                {
                    RemovePunc(loc);
                }
                else if (!CheckSpecialWord(loc))
                {
                    if (currentWord != "" && currentWord != "\n")
                    {
                        if (isStemmer)
                            currentWord = stemmer.stemTerm(currentWord);
                        if (!StopWordsHash.Contains(currentWord))
                            toReturn.Add(currentWord);
                        else
                            textDoc[loc] = "";
                    }
                    else
                        textDoc[loc] = "";
                }
                loc++;
            }
            return toReturn;
        }

        /// <summary>
        /// loading the stop words from a text file to HashSet
        /// </summary>
        /// <param name="path">the path to load file from</param>
        public void LoadStopWords(string path)
        {
            StopWordsHash = new HashSet<string>();
            //path = path + "\\" + "stop_words.txt";
            IEnumerable<string> lines = File.ReadLines(path);
            foreach (string line in lines)
            {
                StopWordsHash.Add(line);
            }
        }

        /// <summary>
        /// clearing the memory
        /// </summary>
        public void Clear()
        {
            textDoc = null;
        }

        #region SpecialWords

        /// <summary>
        /// checking and handling when words has special meanings, considering the parsing rules
        /// </summary>
        /// <param name="loc">the location of the word in the array</param>
        /// <returns>true if the word has been parsed and false otherwise</returns>
        private bool CheckSpecialWord(int loc)
        {
            if (PercentLaw(loc))
                return true;
            else if (BigNumberMaybeDollarLaw(loc))
                return true;
            else if (WordIsDollar(loc))
                return true;
            else if (WordIsBetween(loc))
                return true;
            else if (WordIsBigNumber(loc))
                return true;
            else if (WordIsKilometer(loc))
                return true;
            else if (WordIsAMPM(loc))
                return true;
            return false;
        }

        /// <summary>
        /// checking and handling when words are fractions.
        /// meaning with 'X/Y' pattern 
        /// </summary>
        /// <param name="loc">the location of the word in the array</param>
        private void WordIsFraction(int loc)
        {
            int num;
            string currentWord = textDoc[loc];
            if (loc > 0 && Int32.TryParse(textDoc[loc - 1], out num))
            {
                currentWord = num + " " + currentWord;
                textDoc[loc - 1] = "";
                toReturn.RemoveAt(toReturn.Count - 1);
            }
            string[] split = currentWord.Split(new char[] { ' ', '/' });

            if (split.Length == 2 || split.Length == 3)
            {
                int a, b;

                if (int.TryParse(split[0], out a) && int.TryParse(split[1], out b))
                {
                    if (split.Length == 2)
                    {
                        textDoc[loc] = Math.Round(((double)a / b), 2) + "";
                        toReturn.Add(textDoc[loc]);
                        return;
                    }

                    int c;

                    if (int.TryParse(split[2], out c))
                    {
                        textDoc[loc] = Math.Round((a + (double)b / c), 2) + "";
                        toReturn.Add(textDoc[loc]);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// checking and handling when words are numbers
        /// </summary>
        /// <param name="num">the number</param>
        /// <returns>the number as string after parse</returns>
        private string WordIsNumber(int num)
        {
            if (num >= 1000000)
                return OverMillion(num);

            return "";

        }

        /// <summary>
        /// checking and handling words that are 'PM' or 'AM'
        /// </summary>
        /// <param name="loc"the location of the word in the array></param>
        /// <returns>true if the word has been parsed and false otherwise</returns>
        private bool WordIsAMPM(int loc)
        {
            bool isPM;
            int num;
            string currentWord = textDoc[loc];
            if (loc > 0 && (currentWord == "pm" || currentWord == "am"))
            {
                string prevWord = textDoc[loc - 1];
                string newWord = "";
                if (currentWord == "pm")
                    isPM = true;
                else
                    isPM = false;

                if (prevWord.Contains(":"))
                    newWord = DoubleDot(prevWord, isPM);
                else if (prevWord.Contains("."))
                    newWord = DoubleDot(prevWord.Replace(".", ":"), isPM);
                else if (Int32.TryParse(prevWord, out num))
                    newWord = OnlyNumber(num, isPM);

                if (newWord != "")
                {
                    //if(toReturn.Count>0)
                    //    toReturn.RemoveAt(toReturn.Count - 1);
                    toReturn.Add(newWord);
                    textDoc[loc - 1] = "";
                    textDoc[loc] = newWord;
                    return true;
                }

            }
            return false;
        }

        /// <summary>
        /// handaling times that is X:X like 16:00 pm
        /// </summary>
        /// <param name="word">the word to parse</param>
        /// <param name="isPM">indicates if its PM format or not</param>
        /// <returns>true if the word has been parsed and false otherwise</returns>
        private string DoubleDot(string word, bool isPM)// 16:00 pm
        {
            int num1, num2;
            string[] splitted = word.Split(':');
            string ans = "";
            if (splitted.Length == 2)
            {
                if (Int32.TryParse(splitted[0], out num1) && Int32.TryParse(splitted[1], out num2))
                {
                    if (num2 >= 0 && num2 <= 60)//minutes
                    {
                        if (isPM)
                        {
                            if (num1 < 12)
                                num1 += 12;
                            if (num2 / 10 == 0)
                                ans = num1 + ":" + num2 + "0";
                            else
                                ans = num1 + ":" + num2;
                        }
                        else
                            ans = num1 + ":" + num2;
                    }
                }
            }
            return ans;
        }

        /// <summary>
        /// handaling times that is numbers like 5 pm
        /// </summary>
        /// <param name="num1"></param>
        /// <param name="isPM"></param>
        /// <returns>true if the word has been parsed and false otherwise</returns>
        private string OnlyNumber(int num1, bool isPM) //5 pm
        {
            string ans = "";
            if (num1 <= 12)
            {
                if (isPM) //5 pm = 17:00 or 5 am = 5:00
                {
                    num1 += 12;
                }
                ans = num1 + ":" + "00";
            }
            else if (num1 <= 24 && isPM) //19 pm = 19:00
            {
                ans = num1 + ":" + "00";
            }

            return ans;
        }

        /// <summary>
        /// handaling when words are kilometers or km
        /// </summary>
        /// <param name="loc"the location of the word in the array></param>
        /// <returns>true if the word has been parsed and false otherwise</returns>
        private bool WordIsKilometer(int loc)
        {
            string currentWord = textDoc[loc];
            if (loc > 0 && (currentWord == "kilometers" || currentWord == "km"))
            {
                double num;
                if (Double.TryParse(textDoc[loc - 1], out num))
                {
                    toReturn.RemoveAt(toReturn.Count - 1);
                    textDoc[loc - 1] = "";
                    toReturn.Add(num + " " + "km");
                    textDoc[loc] = num + " " + "km";
                    return true;
                }

            }
            return false;

        }

        /// <summary>
        /// handaling when words are with big numbers
        /// like million billion or trillion
        /// </summary>
        /// <param name="loc"the location of the word in the array></param>
        /// <returns>true if the word has been parsed and false otherwise</returns>
        private bool WordIsBigNumber(int loc)
        {
            if (loc > 1)
            {
                string prevWord = textDoc[loc - 1];
                string currentWord = textDoc[loc];
                string[] arr = prevWord.Split(' ');
                if (arr.Length == 2 && (currentWord == "million" || currentWord == "billion"))
                {
                    if (arr[1] == "m")
                    {
                        //toReturn.RemoveAt(toReturn.Count - 1);
                        textDoc[loc] = "";
                        return true;
                    }

                }
            }
            return false;

        }

        /// <summary>
        /// handaling when word is between
        /// </summary>
        /// <param name="loc"the location of the word in the array></param>
        /// <returns>true if the word has been parsed and false otherwise</returns>
        private bool WordIsBetween(int loc)
        {
            string currentWord = textDoc[loc];
            if (currentWord == "between")
            {
                if (loc < textDoc.Length - 3)
                {
                    string next1Word = textDoc[loc + 1];
                    string next2Word = textDoc[loc + 2];
                    string next3Word = textDoc[loc + 3];

                    int numBefore, numAfter;
                    if (Int32.TryParse(next1Word, out numBefore))
                        if (Int32.TryParse(next3Word, out numAfter))
                            if (next2Word == "and")
                            {
                                textDoc[loc + 1] = "";
                                textDoc[loc + 2] = "";
                                textDoc[loc + 3] = "";
                                textDoc[loc] = numBefore + "-" + numAfter;
                                toReturn.Add(textDoc[loc]);
                            }
                    return true;

                }

                return false;
            }
            else
                return false;
        }

        /// <summary>
        /// handaling when word is big number, that may have dollars in it
        /// </summary>
        /// <param name="loc"the location of the word in the array></param>
        /// <returns>true if the word has been parsed and false otherwise</returns>
        private bool BigNumberMaybeDollarLaw(int loc)
        {
            double isBigNum = BigNumbersLaw(textDoc[loc]);
            if (isBigNum != 1 && loc > 0)
            {
                double num;
                string prevWord = textDoc[loc - 1];
                if (Double.TryParse(prevWord, out num))
                {
                    textDoc[loc - 1] = "";
                    textDoc[loc] = (OverMillion(isBigNum * num));
                    if (loc < textDoc.Length - 1 &&
                        (textDoc[loc + 1] == "million" ||
                        textDoc[loc + 1] == "billion" || textDoc[loc + 1] == "trillion"))

                        textDoc[loc + 1] = "";
                }
                else if (prevWord.Contains("dollars"))
                {
                    prevWord = prevWord.Trim(" dollars".ToCharArray());
                    if (Double.TryParse(prevWord, out num))
                    {
                        //textDoc[loc - 2] = "";
                        textDoc[loc - 1] = "";
                        toReturn.RemoveAt(toReturn.Count - 1);
                        textDoc[loc] = OverMillion(isBigNum * num) + " dollars";
                        toReturn.Add(textDoc[loc]);
                    }
                }

                return true;
            }
            return false;
        }

        /// <summary>
        /// handaling when word is dollar
        /// </summary>
        /// <param name="loc"the location of the word in the array></param>
        /// <returns>true if the word has been parsed and false otherwise</returns>
        private bool WordIsDollar(int loc)
        {
            string currentWord = textDoc[loc];
            if (currentWord == "dollars")
            {
                if (loc > 1)
                {
                    string prevWord = textDoc[loc - 1];
                    if (prevWord == "u.s")
                    {
                        string prevPrevWord = textDoc[loc - 2];
                        double num;
                        if (Double.TryParse(prevPrevWord, out num))
                        {
                            textDoc[loc - 1] = "";
                            textDoc[loc - 2] = "";
                            toReturn.RemoveAt(toReturn.Count - 1);
                            toReturn.RemoveAt(toReturn.Count - 1);
                            if (!prevPrevWord.Contains("dollars"))
                            {
                                textDoc[loc] = prevPrevWord + " dollars";
                                toReturn.Add(prevPrevWord + " dollars");
                            }

                            else
                            {
                                textDoc[loc] = prevPrevWord;
                                toReturn.Add(prevPrevWord);
                            }
                            return true;
                        }
                        char s = ' ';
                        string[] tempArr = prevPrevWord.Split(s);
                        if ((Double.TryParse(tempArr[0], out num) && tempArr[1] == "m"))
                        {
                            toReturn.RemoveAt(toReturn.Count - 1);
                            toReturn.RemoveAt(toReturn.Count - 1);
                            textDoc[loc - 1] = "";
                            textDoc[loc - 2] = "";
                            if (!prevPrevWord.Contains("dollars"))
                            {
                                textDoc[loc] = prevPrevWord + " dollars";
                                toReturn.Add(prevPrevWord + " dollars");
                            }
                            else
                            {
                                textDoc[loc] = prevPrevWord;
                                toReturn.Add(prevPrevWord);
                            }
                            return true;
                        }
                    }
                    else if (textDoc[loc - 1].Any(Char.IsDigit))
                    {
                        textDoc[loc - 1] = "";
                        toReturn.RemoveAt(toReturn.Count - 1);
                        if (!prevWord.Contains("dollars"))
                        {
                            textDoc[loc] = prevWord + " dollars";
                            toReturn.Add(prevWord + " dollars");
                        }
                        else
                        {
                            textDoc[loc] = prevWord;
                            toReturn.Add(prevWord);
                        }
                    }
                    else
                        toReturn.Add(currentWord);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// turning word that represent big number like million into this number
        /// </summary>
        /// <param name="word"></param>
        /// <returns>true if the word has been parsed and false otherwise</returns>
        private double BigNumbersLaw(string word)
        {
            if (word == "million")
                return 1000000;
            if (word == "billion")
                return 1000000000;
            if (word == "trillion")
                return 1000000000000;
            return 1;
        }

        /// <summary>
        /// handaling when word is precent or percantage
        /// </summary>
        /// <param name="loc"the location of the word in the array></param>
        /// <returns>true if the word has been parsed and false otherwise</returns>
        private bool PercentLaw(int loc)
        {
            double num;
            string currentWord = textDoc[loc];
            if (currentWord == "percent" || currentWord == "percentage")
            {
                if (loc > 0)
                {
                    string prevWord = textDoc[loc - 1];
                    if (Double.TryParse(prevWord, out num))
                    {
                        toReturn.RemoveAt(toReturn.Count - 1);
                        textDoc[loc - 1] = "";
                        textDoc[loc] = num + "%";
                        toReturn.Add(num + "%");
                    }
                }
                return true;
            }
            return false;
        }


        #endregion

        #region RemovingPrefixAndSuffix

        /// <summary>
        /// also stemming the word
        /// </summary>
        /// <param name="loc"></param>
        private void RemovePunc(int loc)
        {
            string currentWord = textDoc[loc];
            StringBuilder puncs = new StringBuilder();
            int count = 0;
            for (int i = 0; i < currentWord.Length; i++) //find the first sequence of punc in the word
            {
                if (char.IsPunctuation(currentWord[i]))
                {
                    puncs.Append(currentWord[i]);
                    count++;
                }
                else if (count > 0)
                    break;
            }
            int firstIndex = currentWord.IndexOf(puncs.ToString());
            int lastIndex = currentWord.LastIndexOf(puncs.ToString());
            if (firstIndex != lastIndex)
            {
                toReturn.Add(currentWord);
                return;
            }
            string[] splitted = currentWord.Split(puncs.ToString().ToCharArray()); //remove this sequence
            if (splitted[0].Any(char.IsLetter) && splitted[splitted.Length - 1].Any(char.IsLetter)) // that contains only letters
            {
                if (puncs.Equals("-"))
                    toReturn.Add(splitted[0] + "-" + splitted[splitted.Length - 1]); //add them to text doc as one word
                else
                    textDoc[loc] = "";
                if (!StopWordsHash.Contains(splitted[0]))
                {
                    if (isStemmer)
                        splitted[0] = stemmer.stemTerm(splitted[0]);
                    splitted[0] = RemoveFirstOrLastChar(splitted[0], true);
                    splitted[0] = RemoveFirstOrLastChar(splitted[0], false);
                    toReturn.Add(splitted[0]); //add it to the stringBuilder
                }
                if (!StopWordsHash.Contains(splitted[splitted.Length - 1]))
                {
                    if (isStemmer)
                        splitted[splitted.Length - 1] = stemmer.stemTerm(splitted[splitted.Length - 1]);
                    splitted[splitted.Length - 1] = RemoveFirstOrLastChar(splitted[splitted.Length - 1], true);
                    splitted[splitted.Length - 1] = RemoveFirstOrLastChar(splitted[splitted.Length - 1], false);
                    toReturn.Add(splitted[splitted.Length - 1]);
                }
            }

        }

        /// <summary>
        /// removing tags from the term
        /// </summary>
        /// <param name="loc"><the location of the word in the array/param>
        private void RemoveTagit(int loc)
        {
            string currentWord = textDoc[loc];
            if (currentWord.Length > 0)
            {
                if (currentWord[0] == '<' && currentWord[currentWord.Length - 1] == '>')
                    textDoc[loc] = "";
                if (currentWord.Equals("[text]"))
                {
                    textDoc[loc] = "";
                }
                if (currentWord == "<f") //<F p=106> 
                {
                    textDoc[loc + 1] = ""; //remove the "p=106>" from text
                    textDoc[loc] = "";
                }
            }
        }

        /// <summary>
        /// removing puctuations
        /// </summary>
        /// <param name="loc">the location of the word in the array</param>
        private void RemovePunctuation(int loc)
        {
            string currentWord = textDoc[loc];
            if (currentWord != "")
            {
                currentWord = RemoveFirstOrLastChar(currentWord, true);
                currentWord = RemoveFirstOrLastChar(currentWord, false);
                textDoc[loc] = currentWord;
                //toReturn.Add(currentWord);
            }

        }

        /// <summary>
        /// recursivly removing the first or the last char of the term,
        /// if that char is not a digit and not a letter
        /// </summary>
        /// <param name="currentWord">the word to parse</param>
        /// <param name="isLast">removing from the last chars or from the first</param>
        /// <returns>the word after parse</returns>
        private string RemoveFirstOrLastChar(string currentWord, bool isLast)
        {
            if (currentWord != "")
            {
                int length = currentWord.Length - 1;
                char c;
                if (isLast)
                    c = currentWord[length];
                else
                    c = currentWord[0];
                if (length != -1)
                    if (!char.IsLetterOrDigit(c) && c != '%' && c != '#' && c != '$')
                    {
                        if (isLast)
                            currentWord = currentWord.Substring(0, length);

                        else
                            currentWord = currentWord.Substring(1);
                        //Console.WriteLine("word is: " + currentWord);
                        currentWord = RemoveFirstOrLastChar(currentWord, isLast);
                    }
            }
            return currentWord;
        }

        #endregion

        #region Number

        /// <summary>
        /// checking and handling when words have digits in them
        /// </summary>
        /// <param name="loc">the location of the word in the array</param>
        /// <returns>true if the word contain number and falsr otherwise</returns>
        private bool WordContainsNumbers(int loc)
        {
            double num;
            string currentWord = textDoc[loc];
            if (currentWord.Contains("$"))
            {
                currentWord = currentWord.Trim('$');
                if (Double.TryParse(currentWord, out num))
                {
                    textDoc[loc] = OverMillion(num) + " dollars";
                    toReturn.Add(textDoc[loc]);
                }
                return true;
            }
            else return BigNumberSuffixLaw(currentWord, loc);
        }

        /// <summary>
        /// checking and handling when words have digits in them
        /// </summary>
        /// <param name="currentWord">the word to parse</param>
        /// <param name="loc">the location of the word in the array</param>
        /// <returns>true if the word has been parsed and false otherwise</returns>
        private bool BigNumberSuffixLaw(string currentWord, int loc)
        {
            double num;
            if (currentWord.Contains("bn"))
            {
                currentWord = currentWord.Trim("bn".ToCharArray());
                if (Double.TryParse(currentWord, out num))
                {
                    textDoc[loc] = OverMillion(num * (BigNumbersLaw("billion")));
                    toReturn.Add(textDoc[loc]);
                    return true;

                }
            }
            else if (currentWord.EndsWith("m"))
            {
                return true;
            }
            else if (currentWord.Contains("/"))
            {
                DateToMekaf('/', loc);

            }
            else if (currentWord.Contains("."))
            {
                DateToMekaf('.', loc);
            }

            return false;
        }


        /// <summary>
        /// turning numbers to their right form with the 'm' rule
        /// </summary>
        /// <param name="num"></param>
        /// <returns>true if the word has been parsed and false otherwise</returns>
        private string OverMillion(double num)
        {
            string ans = num + "";
            if (num >= 1000000)
            {
                if (num > 1000000)
                    while (num > 1000000)
                    {
                        num = num / 1000000 + (num % 1000000);
                    }
                else if (num == 1000000)
                    num = 1;
                ans = num + " m";
            }
            return ans;
        }

        #endregion

        #region Dates

        /// <summary>
        /// turning dates 1.8.15 to 2015-08-01
        /// </summary>
        /// <param name="c">the seperator char</param>
        /// <param name="loc">the location of the word in the array</param>
        /// <returns>trueif the word has been parsed and false otherwise</returns>
        private bool DateToMekaf(char c, int loc)
        {
            string currentWord = textDoc[loc];
            int first = currentWord.IndexOf(c);
            int last = currentWord.LastIndexOf(c);
            if (first != last)
            {
                string[] splitted = currentWord.Split(c);
                if (splitted.Length == 3)
                {
                    int num1, num2, num3;
                    Int32.TryParse(splitted[0], out num1);
                    Int32.TryParse(splitted[1], out num2);
                    Int32.TryParse(splitted[2], out num3);
                    if (num1 > 0 && num2 > 0 && num3 > 0)
                    {
                        if (CheckValidDate(num1, num2, num3, loc))
                            return true;
                        if (CheckValidDate(num2, num1, num3, loc))
                            return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// check if three numbers represent a valid date
        /// </summary>
        /// <param name="num1">first word to check</param>
        /// <param name="num2">second word to check</param>
        /// <param name="num3">third word to check</param>
        /// <param name="loc">the location of the word in the array</param>
        /// <returns>true if the words have been parsed and false otherwise</returns>
        private bool CheckValidDate(int num1, int num2, int num3, int loc)
        {
            if (DayOfMonth(num1 + "") != "-1") //day
                if (num2 > 0 && num2 < 13) //month
                {
                    string parsedYear = "";
                    if (num3 > 999 && num3 < 10000)
                        parsedYear = num3 + "";
                    else if (num3 > 00 && num3 <= 09) //year is 2000 and beyond
                        parsedYear = "200" + num3;
                    else if (num3 > 09 && num3 <= 20) //year is 2000 and beyond
                        parsedYear = "20" + num3;
                    else
                        parsedYear = "19" + num3; //year is 1900 and beyond
                    if (parsedYear != "")
                    {
                        textDoc[loc] = parsedYear + "-" + num2 + "-" + num1;
                        toReturn.Add(textDoc[loc]);
                        return true;
                    }
                }
            return false;
        }

        /// <summary>
        /// turning dates to thier right form according to the rules 
        /// </summary>
        /// <param name="loc"the location of the word in the array></param>
        private void Dates(int loc)
        {
            int numAfter = 0;
            int numBefore = 0;
            string dayTH = "";
            bool tryParseAfter = false;
            bool tryParseBefore = false;
            string parsedDay = "";
            string parsedYear = "";
            bool isYear = false;
            bool isPsik = false;
            int month = 0;
            int length = textDoc.Length;
            string monthString = "";
            bool nextExist = false, prevExist = false;

            if (loc < length - 1)
                nextExist = true;
            if (loc > 1)
                prevExist = true;
            if (nextExist)
            {
                month = textDoc[loc + 1].Length;
                if (textDoc[loc + 1] != "" && textDoc[loc + 1][month - 1] == ',')// format of Month DD, YYYY
                    isPsik = true;
                RemovePunctuation(loc + 1); //cases like november 1993.
                monthsName.TryGetValue(textDoc[loc], out month);
                //month = DateTime.ParseExact(textDoc[loc], "MMMM", CultureInfo.InvariantCulture).Month; 

                monthString = month + "";
                if (month > 0 && month < 10)
                    monthString = "0" + month;

                if (textDoc.Length > loc + 1)
                    tryParseAfter = Int32.TryParse(textDoc[loc + 1], out numAfter); //year is always after the month name
            }
            if (prevExist)
            {
                tryParseBefore = Int32.TryParse(textDoc[loc - 1], out numBefore);
                if (!tryParseBefore)
                    dayTH = DayOfMonth(textDoc[loc - 1]);
            }
            if (numAfter >= 1000 && numAfter <= 2025)
            {
                isYear = true;
                parsedYear = numAfter + "";
            }
            else if ((tryParseAfter && tryParseBefore) || (dayTH != "-1" && tryParseAfter)) //the two numbers represent a year
            {
                isYear = true;
                if (numAfter > 00 && numAfter <= 09) //year is 2000 and beyond
                    parsedYear = "200" + numAfter;
                else if (numAfter > 09 && numAfter <= 20) //year is 2000 and beyond
                    parsedYear = "20" + numAfter;
                else
                    parsedYear = "19" + numAfter; //year is 1900 and beyond
            }
            if (isYear && nextExist && prevExist)
            {
                parsedDay = DayOfMonth(textDoc[loc - 1]);
                if (parsedDay != "-1") //year&day
                {
                    textDoc[loc] = parsedYear + "-" + monthString + "-" + parsedDay;
                    textDoc[loc - 1] = "";
                    textDoc[loc + 1] = "";
                    toReturn.RemoveAt(toReturn.Count - 1);
                    toReturn.Add(textDoc[loc]);

                    return;
                }
                else //year only
                {
                    textDoc[loc] = parsedYear + "-" + monthString;
                    toReturn.Add(textDoc[loc]);
                    textDoc[loc + 1] = "";
                    return;
                }
            }
            //one number next to month name-> the month day
            if (prevExist)
            {
                parsedDay = DayOfMonth(textDoc[loc - 1]);
                if (!isYear && parsedDay != "-1")
                {
                    textDoc[loc] = monthString + "-" + parsedDay;
                    toReturn.Add(textDoc[loc]);
                    textDoc[loc - 1] = "";
                    return;
                }
            }
            if (nextExist)
            {
                parsedDay = DayOfMonth(textDoc[loc + 1]);
                if (!isYear && tryParseAfter && parsedDay != "-1")
                {
                    if (isPsik && textDoc.Length > loc + 2) //format of Month DD, YYYY
                    {
                        int numAfter2 = 0;
                        Int32.TryParse(textDoc[loc + 2], out numAfter2);
                        if (numAfter2 >= 1000 && numAfter2 <= 2025)
                        {
                            isYear = true;
                            parsedYear = numAfter2 + "";
                            textDoc[loc] = parsedYear + "-" + monthString + "-" + parsedDay;
                            toReturn.Add(textDoc[loc]);
                            textDoc[loc + 1] = "";
                            textDoc[loc + 2] = "";
                            return;
                        }
                    }
                    else
                    {
                        textDoc[loc] = monthString + "-" + parsedDay;
                        toReturn.Add(textDoc[loc]);
                        textDoc[loc + 1] = "";
                        return;
                    }
                }
            }

        }

        /// <summary>
        /// turning dates to thier right form according to the rules 
        /// </summary>
        /// <param name="day"></param>
        /// <returns>the day of month as a string</returns>
        private string DayOfMonth(string day)
        {
            int num;
            int length = day.Length;
            if (length > 2 && ((day[length - 2] == 't' && day[length - 1] == 'h') || (day[length - 2] == 's' && day[length - 1] == 't') || (day[length - 2] == 'n' && day[length - 1] == 'd') || (day[length - 2] == 'r' && day[length - 1] == 'd'))) //16th january
            {
                day = day.Substring(0, length - 2);
            }
            if (day != "")
            {
                Int32.TryParse(day, out num);
                if (num >= 1 && num <= 31)
                {
                    if (num > 0 && num < 10)
                        return "0" + num;
                    return num + "";
                }

            }
            return "-1";
        }

        /// <summary>
        /// set the hashSet with the month names
        /// </summary>
        private void SetMonthsNames()
        {
            monthsName.Add("january", 1);
            monthsName.Add("jan", 1);
            monthsName.Add("february", 2);
            monthsName.Add("feb", 2);
            monthsName.Add("march", 3);
            monthsName.Add("mar", 3);
            monthsName.Add("april", 4);
            monthsName.Add("apr", 4);
            monthsName.Add("may", 5);
            monthsName.Add("june", 6);
            monthsName.Add("jun", 6);
            monthsName.Add("july", 7);
            monthsName.Add("jul", 7);
            monthsName.Add("august", 8);
            monthsName.Add("aug", 8);
            monthsName.Add("september", 9);
            monthsName.Add("sep", 9);
            monthsName.Add("october", 10);
            monthsName.Add("oct", 10);
            monthsName.Add("november", 11);
            monthsName.Add("nov", 11);
            monthsName.Add("december", 12);
            monthsName.Add("dec", 12);
        }


        #endregion




    }
}
