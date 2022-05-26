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
using System.Collections.ObjectModel;
using Windows.Storage;
using WinAuth.Utils;
using AuthInfoStorageLibrary;
using WinAuth.Views.ContentDialogs;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WinAuth.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class OTPPage : Page
    {
        private DataPackageView data;
        private ObservableCollection<OTPInfo> otpList = new ObservableCollection<OTPInfo>();

        public OTPPage()
        {
            this.InitializeComponent();

            this.Loaded += OTPPage_Loaded; ;
        }

        private void OTPPage_Loaded(object sender, RoutedEventArgs e)
        {
            AuthInfoStorage authInfoStorage = AuthInfoStorage.Instance;
            otpList = new ObservableCollection<OTPInfo>(authInfoStorage.GetOTPInfoFromStorage());
            otpListView.ItemsSource = otpList;
        }

        private async void AddFromQR_Click(object sender, RoutedEventArgs e)
        {
            DataPackageView currentClipboard = Clipboard.GetContent();
            if (currentClipboard != null)
            {
                data = currentClipboard;
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
                    AddOTPInfo(Clipboard.GetContent());
                }
                Window.Current.Activated -= SnippingToolOpened;
            }
        }

        private async void AddOTPInfo(DataPackageView clipboard)
        {
            AuthInfoStorage authInfoStorage = AuthInfoStorage.Instance;
            try
            {
                Uri otpUri = await GetOTPUriFromScreenshot(clipboard);
                OTPInfo info = GetOTPInfoFromUri(otpUri);
                try
                {
                    authInfoStorage.AddOTPInfoToStorage(info);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
            catch (Exception e)
            {
                if (e.Message == "Not a valid otpauth URI")
                {
                    var dialog = new ContentDialog()
                    {
                        Title = "Invalid QR code",
                        Content = "Found QR code but, it's not OTP QR code. Check your image if it is correct QR code.",
                        CloseButtonText = "Ok"
                    };
                    await dialog.ShowAsync();
                }
                else if (e.Message == "Failed to detect QR code")
                {
                    var dialog = new ContentDialog()
                    {
                        Title = "Failed to Detect QR code",
                        Content = "Cannot found QR Code in captured image. Check your image and try again. Zooming in the QR code could help to detect well",
                        CloseButtonText = "Ok"
                    };
                    await dialog.ShowAsync();
                }
                else
                {
                    var dialog = new ContentDialog()
                    {
                        Title = "Error",
                        Content = "Error while reading QR code. Please try again.",
                        CloseButtonText = "Ok"
                    };
                    await dialog.ShowAsync();
                }

            }
            finally
            {
                // restore clipboard
                if (data != null)
                {
                    try
                    {
                        Clipboard.SetContent(await DataPackageViewToDataPackage(data));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }
            }
        }
        
        private OTPInfo GetOTPInfoFromUri(Uri otpUri)
        {
            OTPInfo otpInfo = new OTPInfo();
            otpInfo.Type = Uri.UnescapeDataString(otpUri.Host);
            otpInfo.AccountName = Uri.UnescapeDataString(otpUri.Segments[1].Split(':').Length > 1 ? otpUri.Segments[1].Split(':')[1] : otpUri.Segments[1]);
            otpInfo.Issuer = Uri.UnescapeDataString(otpUri.Segments[1].Split(':').Length > 1 ? otpUri.Segments[1].Split(':')[0] : otpUri.Segments[1]);
            char[] delimiterChars = { '?', '&' };
            string[] querys = Uri.UnescapeDataString(otpUri.Query).Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            foreach (var query in querys)
            {
                string[] value = query.Split('=');
                switch (value[0])
                {
                    case "secret":
                        otpInfo.Secret = value[1];
                        break;
                    case "issuer":
                        otpInfo.Issuer = value[1];
                        break;
                    case "algorithm":
                        otpInfo.Algorithm = value[1];
                        break;
                    case "digits":
                        otpInfo.Digits = value[1];
                        break;
                    case "period":
                        otpInfo.Period = value[1];
                        break;
                    case "counter":
                        otpInfo.Counter = value[1];
                        break;
                }
            }
            return otpInfo;
        }
        
        private async Task<Uri> GetOTPUriFromScreenshot(DataPackageView clipboard)
        {
            if (clipboard.Contains(StandardDataFormats.Bitmap))
            {
                RandomAccessStreamReference bitmap = await clipboard.GetBitmapAsync();
                string result = await QRDecodeHelper.GetQRInfoFromBitmap(bitmap);
                if (!string.IsNullOrEmpty(result))
                {
                    try
                    {
                        Uri otpUri = new Uri(result);
                        string scheme = otpUri.Scheme;
                        if (scheme == "otpauth")
                        {
                            return otpUri;
                        }
                        else
                        {
                            throw new Exception("Not a valid otpauth URI");
                        }
                    }
                    catch
                    {
                        throw new Exception("Not a valid otpauth URI");
                    }
                }
                else
                {
                    throw new Exception("Failed to detect QR code");
                }
            }
            else
            {
                throw new Exception("No screenshot found in clipboard");
            }
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

        private async void AddManually_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog contentDialog = new AddQRManually();
            await contentDialog.ShowAsync();
        }
    }
}