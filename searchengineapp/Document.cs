using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngineApp
{
    class Document
    {
        public string docID { set; get; }
        public StringBuilder text { set; get; }
        public int max_tf { set; get; }
        public int sumOfWords { set; get; }
        public int numberOfdistinguishWords { set; get; }
        public string language { set; get; }
        public string header { get; set; }
        public Dictionary<string, double> queryFreq { set; get; }
        public double docVector { set; get; }

        /// <summary>
        /// represent a document in the corpus
        /// </summary>
        public Document()
        {
            //text = new List<StringBuilder>();
            text = new StringBuilder();
            docID = "";
            language = "";
            sumOfWords = 0;
            queryFreq = new Dictionary<string, double>();
        }

        /// <summary>
        /// overrides the function equals
        /// </summary>
        /// <param name="obj">the othe object to compare with</param>
        /// <returns>true if they are equal anf false otherwise</returns>
        public override bool Equals(object obj)
        {
            Document other = (Document)obj;
            return other.docID == this.docID;
        }


        /// <summary>
        /// overrides the function hash code
        /// </summary>
        /// <returns>hash of the docID</returns>
        public override int GetHashCode()
        {
            return this.docID.GetHashCode();
        }

    }
}
