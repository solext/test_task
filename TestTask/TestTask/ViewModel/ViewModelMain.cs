using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using TestTask.Model;
using System.Threading;
using System.Windows.Input;
using System.Security.Cryptography;

namespace TestTask.ViewModel
{
    public class ViewModelMain : ViewModelBase
    {
        ObservableCollection<file> _Files;
        ICommand _command;
        public Queue<file> FilesQueue;
        Thread filefinder;
        Thread hashcalc;
        Thread icongeter;
        public ViewModelMain()
        {
            Files = new ObservableCollection<file>();
            FilesQueue = new Queue<file>();
            filefinder = new Thread(TraverseTree);
            hashcalc = new Thread(SetMD5HashFromFile);
            icongeter = new Thread(SetIcon);

            filefinder.Name = "FileFinder";
            filefinder.IsBackground = true;
            hashcalc.Name = "hashcalc";
            hashcalc.IsBackground = true;
            filefinder.Priority = ThreadPriority.Highest;
            icongeter.Name = "icongeter";
            icongeter.IsBackground = true;
            filefinder.Start();
            hashcalc.Start();
            icongeter.Start();


        }
        public ObservableCollection<file> Files
        {
            get
            {
                return _Files;
            }
            set
            {
                _Files = value;
                OnPropertyChanged("Files");
            }
        }

        public void TraverseTree()
        {
            const string root = @"C:\";
            Stack<string> dirs = new Stack<string>();
            if (!System.IO.Directory.Exists(root))
            {
                throw new ArgumentException();
            }
            dirs.Push(root);

            while (dirs.Count > 0)
            {
                string currentDir = dirs.Pop();
                string[] subDirs;
                try
                {
                    subDirs = System.IO.Directory.GetDirectories(currentDir);
                }
                catch (UnauthorizedAccessException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
                catch (System.IO.DirectoryNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
                try
                {
                    FileInfo[] dirinfo = new DirectoryInfo(currentDir).GetFiles();
                    int i = 0;
                    while (i < dirinfo.Length)
                    {
                        FilesQueue.Enqueue(new file(dirinfo[i].Name, string.Empty, null, dirinfo[i].FullName));
                        Files.Add(new file(dirinfo[i].Name, string.Empty, null, dirinfo[i].FullName));
                        ++i;

                    }
                }

                catch (UnauthorizedAccessException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
                catch (System.IO.DirectoryNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
                catch (Exception)
                {
                    continue;
                }
                foreach (string str in subDirs)
                    dirs.Push(str);
            }
        }

        public void SetMD5HashFromFile()
        {
            int i = 0;
            while (i < FilesQueue.Count)
            {

                try
                {
                    lock (FilesQueue)
                    {
                        file temp = FilesQueue.ElementAt(i);
                        var md5 = MD5.Create();
                        var stream = File.OpenRead(temp.Path);
                        Files[i].Hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty);
                        ++i;
                    }
                }
                catch (System.IO.IOException e)
                {
                    Files[i].Hash = "has not access";
                    ++i;
                    continue;
                }
                catch (System.UnauthorizedAccessException e)
                {
                    Files[i].Hash = "Access deny";
                    ++i;
                    continue;
                }
                catch (Exception)
                {
                    Files[i].Hash = "Access deny";
                    ++i;
                    continue;
                }


            }
        }
        public void SetIcon()
        {
            int i = 0;
            while (i < FilesQueue.Count)
            {
                lock (Files[i])
                {
                    var sysicon = System.Drawing.Icon.ExtractAssociatedIcon(Files[i].Path);
                    var bmpSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                                sysicon.Handle,
                                System.Windows.Int32Rect.Empty,
                                System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                    bmpSrc.Freeze();
                    Files[i].Icon = bmpSrc;
                    Files[i].Icon.Freeze();
                    ++i;

                }
            }
        }

        public ICommand StopScan
        {
            get
            {
                if (_command == null)
                {
                    _command = new file.DelegateCommand(CanExecute, Execute);
                }
                return _command;
            }
        }
        bool isStop = false;
        private void Execute(object parameter)
        {
            if (!isStop)
            {
                filefinder.Abort();
                hashcalc.Abort();
                icongeter.Abort();
            }
            else
            {
                filefinder.Start();
                hashcalc.Start();
                icongeter.Start();
            }
        }

        private bool CanExecute(object parameter)
        {
            return true;
        }
    }
}
