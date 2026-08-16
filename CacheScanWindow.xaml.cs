using MSFSCacheManager.Models;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;

namespace MSFSCacheManager
{
    public partial class CacheScanWindow : Window
    {
        private readonly List<CacheScanItem> _cacheItems;

        public CacheScanWindow(List<CacheScanItem> cacheItems)
        {
            InitializeComponent();

            _cacheItems = cacheItems;
            CacheGrid.DataContext = _cacheItems;

            ResultCountText.Text =
                $"{cacheItems.Count} cache locations detected, sorted by size";

            UpdateSelectedSummary();
        }

        private void SelectionCheckBox_Click(
            object sender,
            RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(
                UpdateSelectedSummary,
                DispatcherPriority.DataBind);
        }

        private void SelectAllButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            SetAllSelections(true);
        }

        private void SelectNoneButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            SetAllSelections(false);
        }

        private void SetAllSelections(bool isSelected)
        {
            foreach (CacheScanItem item in _cacheItems)
            {
                item.IsSelected = isSelected;
            }

            CacheGrid.Items.Refresh();
            UpdateSelectedSummary();
        }

        private void UpdateSelectedSummary()
        {
            int selectedCount = 0;
            long selectedBytes = 0;

            foreach (CacheScanItem item in _cacheItems)
            {
                if (!item.IsSelected)
                {
                    continue;
                }

                selectedCount++;
                selectedBytes += item.SizeBytes;
            }

            SelectedSummaryText.Text =
                $"{selectedCount} selected  •  " +
                $"Estimated space: {CacheScanItem.FormatSize(selectedBytes)}";
        }

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }
    }
}
