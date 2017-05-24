using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;
using Windows.UI.Xaml.Media.Imaging;
using System.Net.NetworkInformation;
using System.IO;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SlideShow
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 
    public sealed partial class MainPage : Page
    {
        private ConcurrentBag<BitmapImage> blobUris = new ConcurrentBag<BitmapImage>();
        private ConcurrentBag<string> collection = new ConcurrentBag<string>();
        private ConcurrentBag<string> slideShowBlogUris = new ConcurrentBag<string>();
        
        CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=iotslideshowvisuals;AccountKey=yY8q5u5aCbD4RTDS5D4JAihP+0K2c3PLcQIxDwXMlK1SFrqW2tgOcxQk4TCFArKsVsi5Tp6AGH961Nyiu88zWQ==;EndpointSuffix=core.windows.net");
        BlobContinuationToken continuationToken = null;
        CloudBlobClient blobClient;

        DispatcherTimer timer = new DispatcherTimer();
        DispatcherTimer globalTimer = new DispatcherTimer();
        int change = 1;
        
        string[] fileExtensions = { ".jpg", ".png", ".jpeg", ".gif", ".mp4", ".bmp", ".tiff" };

        public MainPage()
        {
            this.InitializeComponent();
        }

        async private void Page_Load(object sender, RoutedEventArgs e)
        {
            hideCollectionShowSlideShow();

            await initCollection();

            globalTimer.Interval = TimeSpan.FromSeconds(120);
            globalTimer.Tick += async (o, a) =>
            {
                if(NetworkInterface.GetIsNetworkAvailable())
                {
                    await initCollection();
                }
            };
            scrollThroughSlideShow();
            globalTimer.Start();
        }

        public void scrollThroughSlideShow()
        {
            timer.Interval = TimeSpan.FromSeconds(4);
            timer.Tick += (o, a) =>
            {
                // If we'd go out of bounds then start from the beginning
                int newIndex = flipView.SelectedIndex + change;

                if (newIndex >= flipView.Items.Count || newIndex < 0)
                {
                    change *= -1;
                }
                var selectedItem = flipView.SelectedItem;

                flipView.SelectedIndex += change;
            };
            timer.Start();         
        } 

        private void hideCollectionShowSlideShow()
        {
            listView.Visibility = Visibility.Collapsed;
            textBlock.Visibility = Visibility.Collapsed;

            flipView.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Initializes a list of containers within the a blob. 
        /// </summary>
        /// <returns></returns>
        async private Task initBlogClient()
        {
            blobClient = storageAccount.CreateCloudBlobClient();

            ContainerResultSegment resultSegment = null;

            resultSegment = await blobClient.ListContainersSegmentedAsync(
            "", ContainerListingDetails.Metadata, 5, continuationToken, null, null);
            foreach (CloudBlobContainer container in resultSegment.Results)
            {
                collection.Add(container.Name);
            }
            listView.ItemsSource = collection;
        }

        /// <summary>
        /// Initializes a specific collection in the blob storage and will automatically start slide the slide show
        /// for all images in that blob container.
        /// </summary>
        /// <returns></returns>
        async private Task initCollection()
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                blobClient = storageAccount.CreateCloudBlobClient();
                BlobResultSegment resultSegment = null;
                CloudBlobContainer container = blobClient.GetContainerReference("tcs");

                //refresh blobURI's
                blobUris = new ConcurrentBag<BitmapImage>();
                do
                {
                    resultSegment = await container.ListBlobsSegmentedAsync
                        ("", true, BlobListingDetails.All, 10, continuationToken, null, null);

                    foreach (var blobItem in resultSegment.Results)
                    {
                        //If the file is a picture then add it to the blobUri list
                        if (fileExtensions.Any(blobItem.StorageUri.PrimaryUri.ToString().Contains))
                        {
                            BitmapImage tempBitMap = new BitmapImage(new Uri(blobItem.StorageUri.PrimaryUri.ToString()));
                            blobUris.Add(tempBitMap);
                        }
                    }

                    continuationToken = resultSegment.ContinuationToken;
                }
                while (continuationToken != null);

                flipView.ItemsSource = blobUris;
            }
            else
            {
                ConcurrentBag<string> offlineStorage = new ConcurrentBag<string>();

                offlineStorage.Add("/StockImages/1d242f57-ce49-432b-9dbf-f804816a2fd9.jpg");
                offlineStorage.Add("/StockImages/3b8778fa-de98-430f-b141-39b4e028fabf.jpg");
                offlineStorage.Add("/StockImages/57239e16-d715-4f4f-8494-dca8dcef188d.jpg");
                offlineStorage.Add("/StockImages/6afb2101-0f52-4de6-a562-baea8203f36c.jpg");
                offlineStorage.Add("/StockImages/93359c7b-de59-43d0-8c29-6ad141638d74.jpg");
                offlineStorage.Add("/StockImages/Post_May17_Google+_01.jpg");
                
                flipView.ItemsSource = offlineStorage;
            }
        }

        #region pop-up
        private void showCollectionHideSlideshow()
        {
            listView.Visibility = Visibility.Visible;
            textBlock.Visibility = Visibility.Visible;

            flipView.Visibility = Visibility.Collapsed;
        }

        async private Task loadImages(string collectionClicked)
        {
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(collectionClicked);
            BlobResultSegment resultSegment = null;

            //refresh bloburis
            blobUris = new ConcurrentBag<BitmapImage>();
            do
            {
                resultSegment = await container.ListBlobsSegmentedAsync("", true, BlobListingDetails.All, 10, continuationToken, null, null);
                foreach (var blobItem in resultSegment.Results)
                {
                    if (fileExtensions.Any(blobItem.StorageUri.PrimaryUri.ToString().Contains))
                    {
                        BitmapImage tempBitMap = new BitmapImage(new Uri(blobItem.StorageUri.PrimaryUri.ToString()));
                        blobUris.Add(tempBitMap);
                    }
                }
                continuationToken = resultSegment.ContinuationToken;
            }
            while (continuationToken != null);
            popupGridView.ItemsSource = blobUris;
        }

        private void startSlideShow()
        {
            if(slideShowBlogUris.IsEmpty)
            {
                flipView.ItemsSource = blobUris;
            }
            else
            {
                flipView.ItemsSource = slideShowBlogUris;
            }

            scrollThroughSlideShow();
        }

        async private void listView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            listView.Visibility = Visibility.Collapsed;
            textBlock.Visibility = Visibility.Collapsed;

            ImagePopup.IsOpen = true;

            if (e.OriginalSource.GetType() == typeof(TextBlock))
            {
                TextBlock item = (TextBlock)e.OriginalSource;
                await loadImages(item.DataContext.ToString());
            }
            else
            {
                ListViewItemPresenter item = (ListViewItemPresenter)e.OriginalSource;
                await loadImages(item.Content.ToString());
            }
        }

        private void play_Click(object sender, RoutedEventArgs e)
        {
            timer.Start();
        }

        private void pause_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
        }

        private void StartShowClicked(object sender, RoutedEventArgs e)
        {
            ImagePopup.IsOpen = false;
            hideCollectionShowSlideShow();
            startSlideShow();
        }

        private void ImagePopup_LostFocus(object sender, RoutedEventArgs e)
        {
            //showCollectionHideSlideshow();

            //When popup has gone out of focus then clear image source
            //blobUris = new ConcurrentBag<string>();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if(e.OriginalSource.GetType() == typeof(CheckBox))
            {
                CheckBox cb = sender as CheckBox;
                string uri = cb.DataContext.ToString();
                slideShowBlogUris.Add(uri);
            }
        }

        async private void button_Click(object sender, RoutedEventArgs e)
        {
            //Go back to main menu
            showCollectionHideSlideshow();
            slideShowBlogUris = new ConcurrentBag<string>();
            await initBlogClient();
        }
        #endregion
        private void Grid_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            button.Visibility = Visibility.Visible;
        }

        private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            button.Visibility = Visibility.Collapsed;
        }
    }
}
