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
        public static Queue<file> FilesQueue;
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
            Thread.CurrentThread.Name = "main";

            filefinder.Name = "FileFinder";
            filefinder.IsBackground = true;
            filefinder.Priority = ThreadPriority.Highest;
            hashcalc.Priority = ThreadPriority.BelowNormal;
            icongeter.Priority = ThreadPriority.BelowNormal;
            hashcalc.Name = "hashcalc";
            hashcalc.IsBackground = true;

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
            const string root = @"E:\OneDrive\Документи";
            // Data structure to hold names of subfolders to be
            // examined for files.
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
                // An UnauthorizedAccessException exception will be thrown if we do not have
                // discovery permission on a folder or file. It may or may not be acceptable 
                // to ignore the exception and continue enumerating the remaining files and 
                // folders. It is also possible (but unlikely) that a DirectoryNotFound exception 
                // will be raised. This will happen if currentDir has been deleted by
                // another application or thread after our call to Directory.Exists. The 
                // choice of which exceptions to catch depends entirely on the specific task 
                // you are intending to perform and also on how much you know with certainty 
                // about the systems on which this code will run.
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
                    foreach (FileInfo i in dirinfo)
                    {
                        lock (FilesQueue)
                        {
                            FilesQueue.Enqueue(new file(i.Name, string.Empty, null, i.FullName));
                            Files.Add(new file(i.Name, string.Empty, null, i.FullName));
                        }
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
                // Perform the required action on each file here.
                // Modify this block to perform your required task.
                // Push the subdirectories onto the stack for traversal.
                // This could also be done before handing the files.
                foreach (string str in subDirs)
                    dirs.Push(str);
            }
        }
        public void SetMD5HashFromFile()
        {
            int i = 0;
            foreach (file t in FilesQueue)
            {
                lock (FilesQueue)
                {
                    file temp = FilesQueue.ElementAt(i);
                    using (var md5 = MD5.Create())
                    {
                        using (var stream = File.OpenRead(temp.Path))
                        {
                            Files[i].Hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty);
                            ++i;
                        }
                    }
                }
            }
        }
        public void SetIcon()
        {
            int i = 0;
            lock (FilesQueue.ElementAt(i))
            {
                foreach (file f in FilesQueue)
                {
                    lock (FilesQueue)
                    {
                        file temp = FilesQueue.ElementAt(i);
                        var sysicon = System.Drawing.Icon.ExtractAssociatedIcon(temp.Path);
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
        }
    }
}
