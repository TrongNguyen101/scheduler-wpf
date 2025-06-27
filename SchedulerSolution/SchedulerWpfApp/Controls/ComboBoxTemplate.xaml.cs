using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace SchedulerWpfApp.Controls
{
    /// <summary>
    /// Interaction logic for ComboBoxTemplate.xaml
    /// </summary>
    public partial class ComboBoxTemplate : UserControl
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

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                FilterItems();
                if (ComboBox != null)
                {
                    ComboBox.IsDropDownOpen = true;
                }
            }
        }

        private void FilterItems()
        {
            if (ItemsSource == null) return;

            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? ItemsSource
                : ItemsSource.Where(item =>
                    item != null &&
                    item.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0);

            FilteredItems.Clear();
            foreach (var item in filtered)
            {
                FilteredItems.Add(item);
            }
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ComboBoxTemplate control)
            {
                control.FilterItems();
            }
        }
    }
}
