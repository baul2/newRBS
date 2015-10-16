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
using newRBS.ViewModels.Utils;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Annotations;
using OxyPlot.Series;
using MathNet.Numerics;
using newRBS.Database;

namespace newRBS.ViewModels
{
    public class Position : ViewModelBase
    {
        private string _Name;
        public string Name { get { return _Name; } set { _Name = value; RaisePropertyChanged(); } }

        private double _Translation;
        public double Translation { get { return _Translation; } set { _Translation = value; RaisePropertyChanged(); } }

        private double _HorizontalTilt;
        public double HorizontalTilt { get { return _HorizontalTilt; } set { _HorizontalTilt = value; RaisePropertyChanged(); } }

        private double _VerticalTilt;
        public double VerticalTilt { get { return _VerticalTilt; } set { _VerticalTilt = value; RaisePropertyChanged(); } }

        private double _Rotation;
        public double Rotation { get { return _Rotation; } set { _Rotation = value; RaisePropertyChanged(); } }
    }

    public class GoniometerViewModel : ViewModelBase
    {
        private bool? _DialogResult;
        public bool? DialogResult
        { get { return _DialogResult; } set { _DialogResult = value; RaisePropertyChanged(); } }

        private Position _CurrentPosition;
        public Position CurrentPosition { get { return _CurrentPosition; } set { _CurrentPosition = value; RaisePropertyChanged(); } }

        private Position _NewPosition;
        public Position NewPosition { get { return _NewPosition; } set { _NewPosition = value; RaisePropertyChanged(); } }

        public ObservableCollection<Position> Positions { get; set; }

        public GoniometerViewModel()
        {
            CurrentPosition = new Position { Name = "current", Translation = 1, HorizontalTilt = 1, VerticalTilt = 1, Rotation = 1 };
            NewPosition = new Position { Name = "new", Translation = 2, HorizontalTilt = 2, VerticalTilt = 2, Rotation = 2};
            Positions = new ObservableCollection<Position>() { CurrentPosition, NewPosition };
        }
    }
}
