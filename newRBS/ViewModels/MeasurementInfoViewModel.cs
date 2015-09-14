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
using newRBS.Database;
using System.Data.Linq;

namespace newRBS.ViewModels
{
    public class MeasurementInfoViewModel : ViewModelBase
    {
        public ICommand SaveCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        private bool? _DialogResult;
        public bool? DialogResult
        { get { return _DialogResult; } set { _DialogResult = value; RaisePropertyChanged(); } }

        private DatabaseDataContext Database;

        public MeasurementInfoClass MeasurementInfo { get; set; }

        public MeasurementInfoViewModel(int MeasurementID)
        {
            SaveCommand = new RelayCommand(() => _SaveCommand(), () => true);
            CancelCommand = new RelayCommand(() => _CancelCommand(), () => true);

            Database = MyGlobals.Database;
            MeasurementInfo = new MeasurementInfoClass(Database);
            MeasurementInfo.Measurement = Database.Measurements.FirstOrDefault(x => x.MeasurementID == MeasurementID);
        }

        public void _SaveCommand()
        {
            try
            {
                Database.SubmitChanges(ConflictMode.ContinueOnConflict);
            }
            catch (ChangeConflictException e)
            {
                foreach (ObjectChangeConflict changeConflict in Database.ChangeConflicts)
                {
                    System.Data.Linq.Mapping.MetaTable metatable = Database.Mapping.GetTable(changeConflict.Object.GetType());
                    Console.WriteLine("fasdjklasfdjklsfda");
                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat("Table name: {0}", metatable.TableName);
                    sb.AppendLine();

                    foreach (MemberChangeConflict col in changeConflict.MemberConflicts)
                    {
                        sb.AppendFormat("Column name : {0}", col.Member.Name);
                        sb.AppendLine();
                        sb.AppendFormat("Original value : {0}", col.OriginalValue.ToString());
                        sb.AppendLine();
                        sb.AppendFormat("Current value : {0}", col.CurrentValue.ToString());
                        sb.AppendLine();
                        sb.AppendFormat("Database value : {0}", col.DatabaseValue.ToString());
                        sb.AppendLine();
                        sb.AppendLine();
                    }
                    Console.WriteLine(sb);
                }
            }
            DialogResult = true;
        }

        public void _CancelCommand()
        {
            DialogResult = true;
        }
    }
}
