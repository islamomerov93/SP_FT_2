using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System_Programming_Final_Task.ViewModels
{
    public class SmartHealthVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string o)
        {
            PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(o));
        }

        public SmartHealthVM()
        {
            SearchedWords = new ObservableCollection<string>();
            Report = new ObservableCollection<string>();
            injuredFilesCount = 0;
        }

        private ObservableCollection<string> searchedWords;

        public ObservableCollection<string> SearchedWords
        {
            get { return searchedWords; }
            set { searchedWords = value; OnPropertyChanged("SearchedWords"); }
        }

        private int injuredFilesCount;

        public int InjuredFilesCount
        {
            get { return injuredFilesCount; }
            set { injuredFilesCount = value; OnPropertyChanged("InjuredFilesCount"); }
        }

        private ObservableCollection<string> report;

        public ObservableCollection<string> Report
        {
            get { return report; }
            set { report = value;  OnPropertyChanged("Report"); }
        }

    }
}
