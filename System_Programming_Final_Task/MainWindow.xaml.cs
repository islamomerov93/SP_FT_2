using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System_Programming_Final_Task.Helper;
using System_Programming_Final_Task.ViewModels;
using Microsoft.WindowsAPICodePack.Dialogs;

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
            notification.ShowNotification("Smart Scanner", "\"Smart Scanner\" app is starting", System.Windows.Forms.ToolTipIcon.Info);
            notification = new Notification();
            SmartHealthVM = new SmartHealthVM();
            DataContext = SmartHealthVM;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (WB.CanGoBack) WB.GoBack();
        }

        private void StartBtnClicked(object sender, RoutedEventArgs e)
        {
            _ = Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                Report = new ObservableCollection<string>();
                SmartHealthVM.Report = null;
                SmartHealthVM.InjuredFilesCount = 0;
            }));
            Task task = new Task(GetAllFiles);
            Tasks.Add(task);
            task.Start();
            TaskFactory.ContinueWhenAll(Tasks.ToArray(), (taskF) => notification.ShowNotification("Scan Completed", $"{SmartHealthVM.InjuredFilesCount} injured files are found !", System.Windows.Forms.ToolTipIcon.Warning, 5000));
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
                    if (!directoryInfo.GetAccessControl().AreAccessRulesProtected) continue;
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
                                File.Copy(file, Destination1);
                                if (!File.Exists(Destination2))
                                {
                                    ++SmartHealthVM.InjuredFilesCount;
                                    File.Copy(file, Destination2);
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

        private async void GetAllFiles()
        {
            await DirSearch(FolderForStartSearc, FolderForSaveInjuredFiles);

            _ = Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                new Action(() =>
                {
                    WB.Source = new Uri(FolderForSaveInjuredFiles);
                }));
        }

        private async Task replaceString(String filename, ObservableCollection<String> searchedWords, String replace)
        {
            StreamReader sr = new StreamReader(filename);
            String[] rows = Regex.Split(sr.ReadToEnd(), "\r\n");
            sr.Close();
            foreach (var searchedWord in searchedWords)
            {
                StreamWriter sw = new StreamWriter(filename);
                for (int i = 0; i < rows.Length; i++)
                {
                    if (rows[i].Contains(searchedWord)) rows[i] = rows[i].Replace(searchedWord, replace);
                    sw.WriteLine(rows[i]);
                }
                sw.Close();
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
                    if (rows[i].Contains(searchedWord))
                    {
                        ++swsCounts[searchedWord];
                        IsInjured = true;
                    }
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
                SmartHealthVM.SearchedWords = null;
                SmartHealthVM.SearchedWords = sws;
            }

        }
    }
}
