using System.Collections.Generic;
using System.Windows;

namespace MSFSCacheManager
{
    public partial class CacheScanWindow : Window
    {
        public CacheScanWindow(List<string> cacheLocations)
        {
            InitializeComponent();

            CacheList.ItemsSource = cacheLocations;

            ResultCountText.Text =
                $"{cacheLocations.Count} cache locations detected";
        }

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }
    }
}