using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CloudX.Models;
using MahApps.Metro;

namespace CloudX
{
    public class MainWindowViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private bool _animateOnPositionChange = true;
        private DateTime? _datePickerDate;
        private int? _integerGreater10Property;
        private bool _magicToggleButtonIsChecked = true;
        private ICommand textBoxButtonCmd;
        private ICommand textBoxButtonCmdWithParameter;

        public MainWindowViewModel()
        {
            SampleData.Seed();

            // create accent color menu items for the demo
            AccentColors = ThemeManager.DefaultAccents
                .Select(
                    a => new AccentColorMenuData {Name = a.Name, ColorBrush = a.Resources["AccentColorBrush"] as Brush})
                .ToList();

            Albums = SampleData.Albums;
            Artists = SampleData.Artists;
            FileLists = SampleData.FileList;

            BrushResources = FindBrushResources();
        }

        public string Title { get; set; }
        public int SelectedIndex { get; set; }
        public List<Music> Albums { get; set; }
        public List<Movie> Artists { get; set; }
        public List<File> FileLists { get; set; }
        public List<AccentColorMenuData> AccentColors { get; set; }

        public int? IntegerGreater10Property
        {
            get { return _integerGreater10Property; }
            set
            {
                if (Equals(value, _integerGreater10Property))
                {
                    return;
                }

                _integerGreater10Property = value;
                RaisePropertyChanged("IntegerGreater10Property");
            }
        }

        public DateTime? DatePickerDate
        {
            get { return _datePickerDate; }
            set
            {
                if (Equals(value, _datePickerDate))
                {
                    return;
                }

                _datePickerDate = value;
                RaisePropertyChanged("DatePickerDate");
            }
        }

        public bool MagicToggleButtonIsChecked
        {
            get { return _magicToggleButtonIsChecked; }
            set
            {
                if (Equals(value, _magicToggleButtonIsChecked))
                {
                    return;
                }

                _magicToggleButtonIsChecked = value;
                RaisePropertyChanged("MagicToggleButtonIsChecked");
            }
        }

        public ICommand TextBoxButtonCmd
        {
            get { return textBoxButtonCmd ?? (textBoxButtonCmd = new TextBoxButtonCommand()); }
        }

        public ICommand TextBoxButtonCmdWithParameter
        {
            get
            {
                return textBoxButtonCmdWithParameter ??
                       (textBoxButtonCmdWithParameter = new TextBoxButtonCommandWithIntParameter());
            }
        }

        public ICommand SingleCloseTabCommand
        {
            get { return new ExampleSingleTabCloseCommand(); }
        }

        public ICommand NeverCloseTabCommand
        {
            get { return new AlwaysInvalidCloseCommand(); }
        }

        public IEnumerable<string> BrushResources { get; private set; }

        public bool AnimateOnPositionChange
        {
            get { return _animateOnPositionChange; }
            set
            {
                if (Equals(_animateOnPositionChange, value)) return;
                _animateOnPositionChange = value;
                RaisePropertyChanged("AnimateOnPositionChange");
            }
        }

        public string this[string columnName]
        {
            get
            {
                if (columnName == "IntegerGreater10Property" && IntegerGreater10Property < 10)
                {
                    return "Number is not greater than 10!";
                }

                if (columnName == "DatePickerDate" && DatePickerDate == null)
                {
                    return "No date given!";
                }

                return null;
            }
        }

        public string Error
        {
            get { return string.Empty; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        ///     Raises the PropertyChanged event if needed.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected virtual void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private IEnumerable<string> FindBrushResources()
        {
            var rd = new ResourceDictionary
            {
                Source = new Uri(@"/MahApps.Metro;component/Styles/Colors.xaml", UriKind.RelativeOrAbsolute)
            };

            List<string> resources = rd.Keys.Cast<object>()
                .Where(key => rd[key] is Brush)
                .Select(key => key.ToString())
                .OrderBy(s => s)
                .ToList();

            return resources;
        }

        public class AccentColorMenuData
        {
            private ICommand changeAccentCommand;
            public string Name { get; set; }
            public Brush ColorBrush { get; set; }

            public ICommand ChangeAccentCommand
            {
                get
                {
                    return changeAccentCommand ??
                           (changeAccentCommand =
                               new SimpleCommand
                               {
                                   CanExecuteDelegate = x => true,
                                   ExecuteDelegate = x => ChangeAccent(x)
                               });
                }
            }

            private void ChangeAccent(object sender)
            {
                Tuple<Theme, Accent> theme = ThemeManager.DetectTheme(Application.Current);
                Accent accent = ThemeManager.DefaultAccents.First(x => x.Name == Name);
                ThemeManager.ChangeTheme(Application.Current, accent, theme.Item1);
            }
        }

        public class AlwaysInvalidCloseCommand : ICommand
        {
            public bool CanExecute(object parameter)
            {
                return false;
            }

            public event EventHandler CanExecuteChanged;

            public void Execute(object parameter)
            {
            }
        }

        public class ExampleSingleTabCloseCommand : ICommand
        {
            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged;

            public void Execute(object parameter)
            {
                MessageBox.Show("You are now closing the '" + parameter + "' tab!");
            }
        }

        public class TextBoxButtonCommand : ICommand
        {
            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged;

            public void Execute(object parameter)
            {
                if (parameter is TextBox)
                {
                    MessageBox.Show("TextBox Button was clicked!" + Environment.NewLine + "Text: " +
                                    ((TextBox) parameter).Text);
                }
                else if (parameter is PasswordBox)
                {
                    MessageBox.Show("PasswordBox Button was clicked!" + Environment.NewLine + "Text: " +
                                    ((PasswordBox) parameter).Password);
                }
            }
        }

        public class TextBoxButtonCommandWithIntParameter : ICommand
        {
            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged;

            public void Execute(object parameter)
            {
                if (parameter is String)
                {
                    MessageBox.Show("TextBox Button was clicked with parameter!" + Environment.NewLine + "Text: " +
                                    parameter);
                }
            }
        }
    }
}