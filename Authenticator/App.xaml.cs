using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WinAuth.Views;
using AuthInfoStorageLibrary;
using Windows.Security.Credentials.UI;
using Windows.Security.Credentials;
using System.Diagnostics;

namespace WinAuth
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;

            PasswordVault vault = new PasswordVault();
            try
            {
                var credential = vault.Retrieve("WinAuth", "Access Token");
                credential.RetrievePassword();
                Debug.WriteLine("Access Token: " + credential.Password);
            }
            catch (Exception)
            {
                Debug.WriteLine("No access token found");
                var characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
                var random = new Random();
                var result = new string(Enumerable.Repeat(characters, 128)
                  .Select(s => s[random.Next(s.Length)]).ToArray());
                vault.Add(new PasswordCredential("WinAuth", "Access Token", result));
            }

            AuthInfoStorage.InitializeDatabase();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected async override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(LoginPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }

            // Login UI
            // If window has been deactivated after prelaunch screen, Windows Hello UI is never shown
            var ucvAvailability = await UserConsentVerifier.CheckAvailabilityAsync();
            if (ucvAvailability == UserConsentVerifierAvailability.Available)
            {
                UserConsentVerificationResult consentResult = await UserConsentVerifier.RequestVerificationAsync("You need to login to access this application");
                if (consentResult.Equals(UserConsentVerificationResult.Verified))
                {
                    rootFrame.Navigate(typeof(MainPage));
                }
                else if (!consentResult.Equals(UserConsentVerificationResult.Canceled))
                {
                    string dialogMessage;
                    switch (consentResult)
                    {
                        case UserConsentVerificationResult.DeviceBusy:
                            dialogMessage = "Biometric device is busy.";
                            break;
                        case UserConsentVerificationResult.DeviceNotPresent:
                            dialogMessage = "No biometric device found.";
                            break;
                        case UserConsentVerificationResult.DisabledByPolicy:
                            dialogMessage = "Biometric verification is disabled by policy.";
                            break;
                        case UserConsentVerificationResult.NotConfiguredForUser:
                            dialogMessage = "A biometric verifier device is not configured.";
                            break;
                        case UserConsentVerificationResult.RetriesExhausted:
                            dialogMessage = "There have been too many failed attempts. Biometric authentication canceled.";
                            break;
                        default:
                            dialogMessage = "Biometric authentication is currently unavailable.";
                            break;
                    }
                    var dialog = new ContentDialog
                    {
                        Title = "Login Failed",
                        Content = dialogMessage,
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
                        dialogMessage = "A biometric verifier device is not configured";
                        break;
                    default:
                        dialogMessage = "Biometric verification is currently unavailable.";
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

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}
