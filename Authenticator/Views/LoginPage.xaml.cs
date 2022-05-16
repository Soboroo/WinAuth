using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Security.Credentials.UI;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WinAuth.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LoginPage : Page
    {
        public LoginPage()
        {
            this.InitializeComponent();
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;

            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            // Hide default title bar.
            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            UpdateTitleBarLayout(coreTitleBar);

            // Set XAML element as a draggable region.
            Window.Current.SetTitleBar(AppTitleBar);

            // Register a handler for when the size of the overlaid caption control changes.
            // For example, when the app moves to a screen with a different DPI.
            coreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;

            // Register a handler for when the title bar visibility changes.
            // For example, when the title bar is invoked in full screen mode.
            coreTitleBar.IsVisibleChanged += CoreTitleBar_IsVisibleChanged;

            //Register a handler for when the window changes focus
            Window.Current.Activated += Current_Activated;
        }
        private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            UpdateTitleBarLayout(sender);
        }

        private void UpdateTitleBarLayout(CoreApplicationViewTitleBar coreTitleBar)
        {
            // Update title bar control size as needed to account for system size changes.
            AppTitleBar.Height = coreTitleBar.Height;

            // Ensure the custom title bar does not overlap window caption controls
            Thickness currMargin = AppTitleBar.Margin;
            AppTitleBar.Margin = new Thickness(currMargin.Left, currMargin.Top, coreTitleBar.SystemOverlayRightInset, currMargin.Bottom);
        }

        private void CoreTitleBar_IsVisibleChanged(CoreApplicationViewTitleBar sender, object args)
        {
            if (sender.IsVisible)
            {
                AppTitleBar.Visibility = Visibility.Visible;
            }
            else
            {
                AppTitleBar.Visibility = Visibility.Collapsed;
            }
        }

        // Update the TitleBar based on the inactive/active state of the app
        private void Current_Activated(object sender, Windows.UI.Core.WindowActivatedEventArgs e)
        {
            SolidColorBrush defaultForegroundBrush = (SolidColorBrush)Application.Current.Resources["TextFillColorPrimaryBrush"];
            SolidColorBrush inactiveForegroundBrush = (SolidColorBrush)Application.Current.Resources["TextFillColorDisabledBrush"];

            if (e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.Deactivated)
            {
                AppTitle.Foreground = inactiveForegroundBrush;
            }
            else
            {
                AppTitle.Foreground = defaultForegroundBrush;
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var ucvAvailability = await UserConsentVerifier.CheckAvailabilityAsync();
            if (ucvAvailability == UserConsentVerifierAvailability.Available)
            {
                UserConsentVerificationResult consentReult = await UserConsentVerifier.RequestVerificationAsync("You need to login to access this application");
                if (consentReult.Equals(UserConsentVerificationResult.Verified))
                {
                    Frame.Navigate(typeof(MainPage));
                }
                else
                {
                    var dialog = new ContentDialog
                    {
                        Title = "Login Failed",
                        Content = "You need to login to access this application",
                        PrimaryButtonText = "OK"
                    };
                    await dialog.ShowAsync();
                }
            }
            else
            {
                string dialogMessage;
                switch (ucvAvailability)
                {
                    case UserConsentVerifierAvailability.DeviceBusy:
                        dialogMessage = "Biometric device is busy.";
                        break;
                    case UserConsentVerifierAvailability.DeviceNotPresent:
                        dialogMessage = "No biometric device found.";
                        break;
                    case UserConsentVerifierAvailability.DisabledByPolicy:
                        dialogMessage = "Biometric verification is disabled by policy.";
                        break;
                    case UserConsentVerifierAvailability.NotConfiguredForUser:
                        dialogMessage = "The user has no fingerprints registered. Please add a fingerprint to the " +
                                        "fingerprint database and try again.";
                        break;
                    default:
                        dialogMessage = "Fingerprints verification is currently unavailable.";
                        break;
                }
                var dialog = new ContentDialog
                {
                    Title = "Login Failed",
                    Content = dialogMessage,
                    CloseButtonText = "OK"
                };
                await dialog.ShowAsync();
            }
        }
    }
}
