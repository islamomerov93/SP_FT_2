using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System_Programming_Final_Task.Helper;
using System_Programming_Final_Task.ViewModels;

namespace System_Programming_Final_Task
{
    public partial class MainWindow : Window
    {
        Notification notification = new Notification();
        SmartHealthVM SmartHealthVM;
        ObservableCollection<string> FilePaths = new ObservableCollection<string>();
        ObservableCollection<string> sws = new ObservableCollection<string>();
        Dictionary<string, int> swsCounts = new Dictionary<string, int>();
        ObservableCollection<string> Report = new ObservableCollection<string>();
        List<Task> Tasks = new List<Task>();
        TaskFactory TaskFactory = new TaskFactory();
        static string FolderForStartSearc = @"C:\";
        static string FolderForSaveInjuredFiles = FolderForStartSearc + @"InjuredFileScanner";
        public MainWindow()
        {
            InitializeComponent();
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
            notification.ShowNotification("Smart Scanner", "\"Smart Scanner\" app is starting", System.Windows.Forms.ToolTipIcon.Info);
            notification = new Notification();
            SmartHealthVM = new SmartHealthVM();
            DataContext = SmartHealthVM;
        }

        void OnProcessExit(object sender, EventArgs e)
        {
            File.WriteAllLines("Report.txt", Report);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (WB.CanGoBack) WB.GoBack();
        }

        private async void StartBtnClicked(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                List<string> Keys = new List<string>(swsCounts.Keys);
                foreach (var item in Keys)
                {
                    swsCounts[item] = 0;
                }
                _ = Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    Report = new ObservableCollection<string>();
                    SmartHealthVM.Report = null;
                    SmartHealthVM.InjuredFilesCount = 0;
                    SmartHealthVM.SwsCounts = null;
                }));
            }).Wait();

            Task task = new Task(DoScan);
            Tasks.Add(task);
            task.Start();
            _ = TaskFactory.ContinueWhenAll(Tasks.ToArray(), (taskF) =>
            {
                notification.ShowNotification("Scan Completed", $"{SmartHealthVM.InjuredFilesCount} injured files are found !", System.Windows.Forms.ToolTipIcon.Warning, 5000);
                if (!File.Exists("Report.txt")) File.Create("Report.txt");
            });
            notification = new Notification();
        }

        bool open = false;
        private void ViewBtnClicked(object sender, RoutedEventArgs e)
        {
            if (!open) MW.Width = 1000;
            else MW.Width = 550;
            open = !open;
        }

        private void OpenFolderDialogBtnClick(object sender, RoutedEventArgs e)
        {
            using (var fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = fbd.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                { FolderForStartSearc = fbd.SelectedPath; }
            }
        }

        async Task DirSearch(string FolderForStartSearc, string FolderForSaveInjuredFiles)
        {
            try
            {
                foreach (string d in Directory.GetDirectories(FolderForStartSearc))
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(d);
                    if (directoryInfo.GetAccessControl().AreAccessRulesProtected) continue;
                    foreach (string file in Directory.GetFiles(d))
                    {
                        FileInfo fileInfo = new FileInfo(file);
                        if (fileInfo.GetAccessControl().AreAccessRulesProtected) continue;
                        FilePaths.Add(file);
                        string Destination1 = file.ToString().Replace(d, FolderForSaveInjuredFiles + @"\InjuredFiles");
                        string Destination2 = file.ToString().Replace(d, FolderForSaveInjuredFiles + @"\InjuredFiles_Modified");

                        if (!Directory.Exists(FolderForSaveInjuredFiles + @"\InjuredFiles"))
                            Directory.CreateDirectory(FolderForSaveInjuredFiles + @"\InjuredFiles");

                        if (!Directory.Exists(FolderForSaveInjuredFiles + @"\InjuredFiles_Modified"))
                            Directory.CreateDirectory(FolderForSaveInjuredFiles + @"\InjuredFiles_Modified");

                        if (!File.Exists(Destination1))
                        {
                            bool isInjured = await CheckForForbiddenWords(file, SmartHealthVM.SearchedWords, "*******");
                            if (isInjured)
                            {
                                lock (Destination1)
                                {
                                    File.Copy(file, Destination1);
                                }
                                if (!File.Exists(Destination2))
                                {
                                    lock (Destination2)
                                    {
                                        ++SmartHealthVM.InjuredFilesCount;
                                        File.Copy(file, Destination2);
                                    }
                                    await replaceString(Destination2, SmartHealthVM.SearchedWords, "*******");
                                }
                            }
                        }
                    }
                    await DirSearch(d, FolderForSaveInjuredFiles);
                }
            }
            catch (System.Exception excpt)
            {
                MessageBox.Show(excpt.Message);
            }
        }

        private async void DoScan()
        {
            await DirSearch(FolderForStartSearc, FolderForSaveInjuredFiles);

            _ = Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                new Action(() =>
                {
                    WB.Source = new Uri(FolderForSaveInjuredFiles);
                }));
            SmartHealthVM.SwsCounts = null;
            SmartHealthVM.SwsCounts = swsCounts;
        }

        private async Task replaceString(String filename, ObservableCollection<String> searchedWords, String replace)
        {
            StreamReader sr = new StreamReader(filename);
            String[] rows = Regex.Split(sr.ReadToEnd(), "\r\n");
            sr.Close();
            foreach (var searchedWord in searchedWords)
            {
                lock (filename)
                {
                    StreamWriter sw = new StreamWriter(filename);
                    for (int i = 0; i < rows.Length; i++)
                    {
                        if (rows[i].Contains(searchedWord))
                        {
                            rows[i] = rows[i].Replace(searchedWord, replace);
                            ++swsCounts[searchedWord];
                        }
                        sw.WriteLine(rows[i]);
                    }
                    sw.Close();
                }
            }
        }

        private async Task<bool> CheckForForbiddenWords(String filename, ObservableCollection<String> searchedWords, String replace)
        {
            bool IsInjured = false;
            StreamReader sr = new StreamReader(filename);
            String[] rows = Regex.Split(sr.ReadToEnd(), "\r\n");
            sr.Close();
            foreach (var searchedWord in searchedWords)
            {
                for (int i = 0; i < rows.Length; i++)
                {
                    if (rows[i].Contains(searchedWord)) IsInjured = true;
                }
            }
            if (IsInjured)
            {
                _ = Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    Report.Add(filename);
                    SmartHealthVM.Report = null;
                    SmartHealthVM.Report = Report;
                }));
            }
            return IsInjured;
        }

        private void AddWordToForbiddenWordsList(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(InjuredWordLbl.Text))
            {
                sws.Add(InjuredWordLbl.Text);
                swsCounts.Add(InjuredWordLbl.Text, 0);
                SmartHealthVM.SwsCounts = null;
                SmartHealthVM.SwsCounts = swsCounts;
                SmartHealthVM.SearchedWords = null;
                SmartHealthVM.SearchedWords = sws;
                InjuredWordLbl.Text = null;
            }
        }

        private void DeleteWordFromForbiddenWordsList(object sender, RoutedEventArgs e)
        {
            if (LB.SelectedItem != null)
            {
                string selected = LB.SelectedItem.ToString();
                sws.Remove(selected);
                swsCounts.Remove(selected);
                SmartHealthVM.SwsCounts = null;
                SmartHealthVM.SwsCounts = swsCounts;
                SmartHealthVM.SearchedWords = null;
                SmartHealthVM.SearchedWords = sws;
                InjuredWordLbl.Text = null;
            }
        }
    }
}
