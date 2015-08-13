using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Threading;
using System.Runtime.CompilerServices;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Command;
using Microsoft.Practices.ServiceLocation;
using newRBS.ViewModelUtils;
using Microsoft.Win32;
using System.IO;


namespace newRBS.ViewModels
{
    public class ImportSpectraViewModel : ViewModelBase
    {
        public ICommand OpenFileCommand { get; set; }

        private bool? _DialogResult;
        public bool? DialogResult
        { get { return _DialogResult; } set { _DialogResult = value; RaisePropertyChanged(); } }

        private string _SelectedPath;
        public string SelectedPath
        {
            get { return _SelectedPath; }
            set
            {
                _SelectedPath = value;
                RaisePropertyChanged("SelectedPath");
            }
        }

        private string _FileContent;
        public string FileContent
        {
            get { return _FileContent; }
            set
            {
                _FileContent = value;
                RaisePropertyChanged("FileContent");
            }
        }

        public ImportSpectraViewModel()
        {
            OpenFileCommand = new RelayCommand(() => _OpenFileCommand(), () => true);
        }

        public void _OpenFileCommand()
        {
            var dialog = new OpenFileDialog { };
            dialog.ShowDialog();

            SelectedPath = dialog.FileName;

            TextReader textReader = new StreamReader(SelectedPath);

            string line;
            while ((line = textReader.ReadLine()) != null)
            {
                string[] splitString = line.Split('\t');
                switch (splitString[0].Replace(" ", string.Empty))
                {
                    case "Channel":
                        {
                            Console.WriteLine("Channel");
                            break;
                        }
                }
                  
            }

            FileContent = line;
            Console.WriteLine(line);

            textReader.Close();
        }
    }
}
