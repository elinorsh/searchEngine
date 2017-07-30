using CSharpStringSort;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SearchEngineApp
{
    /// <summary>
    /// Interaction logic for WindowLanguagesList.xaml
    /// </summary>
    public partial class WindowLanguagesList : Window
    {
        public HashSet<string> selectedLan { get; set; }
        private HashSet<string> set;
        public WindowLanguagesList(HashSet<string> s)
        {
            set = s;
            InitializeComponent();
            selectedLan = new HashSet<string>();
            list.Items.Clear();
            string[] toSort = set.ToArray<string>();
            string[] sorted = Sedgewick.Sort(toSort);
            for (int i = 0; i < sorted.Length; i++)
            {

                ListBoxItem boxItem = new ListBoxItem();    
                boxItem.Content = sorted[i];
                list.Items.Add(boxItem);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e) //Cancel
        {
            selectedLan.Clear();
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e) //OK
        {
            if (list.SelectedItems.Count > 0)
            {
                foreach (ListBoxItem lan in list.SelectedItems)
                {
                    string lang = lan.Content.ToString();
                    selectedLan.Add(lang);
                }
            }
            this.Close();
        }


    }
}
