using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Core;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using Windows.Storage;
using WinAuth.Utils;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WinAuth.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class OTPPage : Page
    {
        private DataPackage data;
        
        public OTPPage()
        {
            this.InitializeComponent();
        }

        private async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            DataPackageView currentClipboard = Clipboard.GetContent();
            if (currentClipboard != null)
            {
                data = await DataPackageViewToDataPackage(currentClipboard);
            }
            Clipboard.Clear();
            bool result = await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-screenclip:snip?source=WinAuth"));
            if (result)
            {
                Window.Current.Activated += SnippingToolOpened;
            }
            else
            {
                // Failed to launch screen clip (different from didn't take a screenshot)
            }
        }

        private void SnippingToolOpened(object sender, WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState != CoreWindowActivationState.Deactivated)
            {
                DataPackageView dataPackageView = Clipboard.GetContent();
                if (dataPackageView.Contains(StandardDataFormats.Bitmap))
                {
                    Debug.WriteLine("Screenshot copied to clipboard");
                    VerifyQR(Clipboard.GetContent());
                }
                Window.Current.Activated -= SnippingToolOpened;
            }
        }
        
        private async void VerifyQR(DataPackageView clipboard)
        {
            if (clipboard.Contains(StandardDataFormats.Bitmap))
            {
                RandomAccessStreamReference bitmap = await clipboard.GetBitmapAsync();
                string result = await QRDecodeHelper.GetQRInfoFromBitmap(bitmap);
                if (result != null && result.Length > 0)
                {
                    try
                    {
                        Uri otpUri = new Uri(result);
                        string scheme = otpUri.Scheme;
                        if (scheme == "otpauth")
                        {
                            Debug.WriteLine(otpUri.Host);
                            Debug.WriteLine(otpUri.Segments[1]);
                            char[] delimiterChars = { '?', '&' };
                            string[] querys = otpUri.Query.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var query in querys)
                            {
                                string[] value = Uri.UnescapeDataString(query).Split('=');
                                Debug.WriteLine(value[0] + ": " + value[1]);
                            }
                        }
                        else
                        {
                            var dialog = new ContentDialog()
                            {
                                Title = "Invalid QR code",
                                Content = "Found QR code but, it's not OTP QR code. Check your image if it is correct QR code.",
                                CloseButtonText = "Ok"
                            };
                            await dialog.ShowAsync();
                        }
                    }
                    catch
                    {
                        var dialog = new ContentDialog()
                        {
                            Title = "Invalid QR code",
                            Content = "Found QR code but, it's not OTP QR code. Check your image if it is correct QR code.",
                            CloseButtonText = "Ok"
                        };
                        await dialog.ShowAsync();
                    }
                }
                else
                {
                    var dialog = new ContentDialog()
                    {
                        Title = "Failed to Detect QR code",
                        Content = "Cannot found QR Code in captured image. Check your image and try again. Zooming in the QR code could help to detect well",
                        CloseButtonText = "Ok"
                    };
                    await dialog.ShowAsync();
                }
            }
            else
            {
                throw new Exception("No screenshot found in clipboard");
            }
            Clipboard.SetContent(data);
        }

        private async Task<DataPackage> DataPackageViewToDataPackage(DataPackageView dataPackageView)
        {
            DataPackage dataPackage = new DataPackage();
            if (dataPackageView.Contains(StandardDataFormats.Bitmap))
            {
                RandomAccessStreamReference bitmap = await dataPackageView.GetBitmapAsync();
                dataPackage.SetBitmap(bitmap);
            }
            else if (dataPackageView.Contains(StandardDataFormats.Text))
            {
                string text = await dataPackageView.GetTextAsync();
                dataPackage.SetText(text);
            }
            else if (dataPackageView.Contains(StandardDataFormats.Html))
            {
                string html = await dataPackageView.GetHtmlFormatAsync();
                dataPackage.SetHtmlFormat(html);
            }
            else if (dataPackageView.Contains(StandardDataFormats.Rtf))
            {
                string rtf = await dataPackageView.GetRtfAsync();
                dataPackage.SetRtf(rtf);
            }
            else if (dataPackageView.Contains(StandardDataFormats.StorageItems))
            {
                IReadOnlyList<IStorageItem> storageItems = await dataPackageView.GetStorageItemsAsync();
                dataPackage.SetStorageItems(storageItems);
            }
            else if (dataPackageView.Contains(StandardDataFormats.WebLink))
            {
                Uri webLink = await dataPackageView.GetWebLinkAsync();
                dataPackage.SetWebLink(webLink);
            }
            return dataPackage;
        }
    }
}