using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace SchedulerWpfApp.Controls
{
    /// <summary>
    /// Interaction logic for ComboBoxTemplate.xaml
    /// </summary>
    public partial class ComboBoxTemplate : UserControl, INotifyPropertyChanged
    {
        public ComboBoxTemplate()
        {
            InitializeComponent();
            FilteredItems = new ObservableCollection<string>();
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable<string>), typeof(ComboBoxTemplate),
                new PropertyMetadata(null, OnItemsSourceChanged));

        public IEnumerable<string> ItemsSource
        {
            get => (IEnumerable<string>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(string), typeof(ComboBoxTemplate),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string SelectedItem
        {
            get => (string)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public ObservableCollection<string> FilteredItems { get; set; }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterItems();
                    if (ComboBox != null)
                    {
                        ComboBox.IsDropDownOpen = !string.IsNullOrWhiteSpace(value);
                    }
                }
            }
        }

        private void FilterItems()
        {
            if (ItemsSource == null)
            {
                FilteredItems.Clear();
                return;
            }

            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? ItemsSource.ToList()
                : ItemsSource.Where(item =>
                    item != null &&
                    item.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            // Clear trước khi add để tránh duplicate
            FilteredItems.Clear();

            // Add items theo batch để tránh multiple notifications
            foreach (var item in filtered)
            {
                if (!FilteredItems.Contains(item)) // Double check để tránh duplicate
                {
                    FilteredItems.Add(item);
                }
            }
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ComboBoxTemplate control)
            {
                // Reset search text khi ItemsSource thay đổi
                control._searchText = string.Empty;
                control.OnPropertyChanged(nameof(SearchText));

                // Clear và filter lại
                control.FilteredItems.Clear();
                control.FilterItems();
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBox.SelectedItem is string selected && !string.IsNullOrEmpty(selected))
            {
                SelectedItem = selected; // Truyền ra ngoài

                // Reset để tránh bị trigger lại - sử dụng backing field để tránh trigger FilterItems()
                _searchText = string.Empty;
                OnPropertyChanged(nameof(SearchText));

                if (ComboBox != null)
                {
                    ComboBox.Text = string.Empty;
                    ComboBox.SelectedItem = null; // Reset selection
                    ComboBox.IsDropDownOpen = false;
                }
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion
    }
}