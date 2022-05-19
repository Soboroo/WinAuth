using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Toolkit.Uwp.UI;
using AuthInfoStorageLibrary;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WinAuth.Views.ContentDialogs
{
    public sealed partial class AddQRManually : ContentDialog
    {
        public AddQRManually()
        {
            this.InitializeComponent();
            settingComboBox.SelectionChanged += SettingComboBox_SelectionChanged;
            advancedType.SelectionChanged += AdvancedType_SelectionChanged;

            basicLabel.TextChanged += Validation;
            basicSecret.TextChanged += Validation;

            advancedName.TextChanged += Validation;
            advancedSecret.TextChanged += Validation;
            advancedDigits.TextChanged += Validation;
            advancedPeriod.TextChanged += Validation;
            advancedCounter.TextChanged += Validation;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            OTPInfo info = new OTPInfo();

            if (settingComboBox.SelectedIndex == 0)
            {
                info.Type = "totp";
                info.Label = basicLabel.Text;
                info.Secret = basicSecret.Text;
            }
            else
            {
                if (advancedType.SelectedIndex == 0)
                {
                    info.Type = "hotp";
                }
                else
                {
                    info.Type = "hotp";
                }
                if (advancedIssuer.Text.Length == 0)
                {
                    info.Label = advancedName.Text + ":" + advancedSecret.Text;
                    info.Issuer = advancedName.Text;
                }
                else
                {
                    info.Label = advancedIssuer.Text + ":" + advancedName.Text;
                    info.Issuer = advancedIssuer.Text;
                }
                info.Secret = advancedSecret.Text;
                info.Digits = advancedDigits.Text;
                info.Period = advancedPeriod.Text;
                info.Counter = advancedCounter.Text;
                info.Algorithm = advancedAlgorithm.SelectedItem.ToString();
            }
            AuthInfoStorage.AddOTPInfoToStorage(info);
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void SettingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (settingComboBox.SelectedIndex)
            {
                case 0:
                    basic.Visibility = Visibility.Visible;
                    advanced.Visibility = Visibility.Collapsed;
                    BasicSetting_Reset();
                    break;
                case 1:
                    basic.Visibility = Visibility.Collapsed;
                    advanced.Visibility = Visibility.Visible;
                    AdvancedSetting_Reset();
                    break;
            }
        }

        private void AdvancedType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (advancedType.SelectedIndex)
            {
                case 0:
                    advancedTotp.Visibility = Visibility.Visible;
                    advancedHotp.Visibility = Visibility.Collapsed;
                    TotpSetting_Reset();
                    break;
                case 1:
                    advancedTotp.Visibility = Visibility.Collapsed;
                    advancedHotp.Visibility = Visibility.Visible;
                    HotpSetting_Reset();
                    break;
            }
        }

        private void Validation(object sender, TextChangedEventArgs e)
        {
            switch (settingComboBox.SelectedIndex)
            {
                case 0:
                    BasicSetting_Validation();
                    break;
                case 1:
                    AdvancedSetting_Validation();
                    break;
            }
        }

        private void BasicSetting_Validation()
        {
            if (TextBoxExtensions.GetIsValid(basicLabel) && TextBoxExtensions.GetIsValid(basicSecret))
            {
                this.IsPrimaryButtonEnabled = true;
            }
            else
            {
                this.IsPrimaryButtonEnabled = false;
            }
        }

        private void AdvancedSetting_Validation()
        {
            if (TextBoxExtensions.GetIsValid(advancedName) && 
                TextBoxExtensions.GetIsValid(advancedSecret) &&
                (TextBoxExtensions.GetIsValid(advancedDigits) || advancedDigits.Text.Length == 0))
            {
                if (advancedType.SelectedIndex == 0)
                {
                    if (TextBoxExtensions.GetIsValid(advancedPeriod) || advancedPeriod.Text.Length == 0)
                    {
                        this.IsPrimaryButtonEnabled = true;
                    }
                    else
                    {
                        this.IsPrimaryButtonEnabled = false;
                    }
                }
                else
                {
                    if (TextBoxExtensions.GetIsValid(advancedCounter) || advancedCounter.Text.Length == 0)
                    {
                        this.IsPrimaryButtonEnabled = true;
                    }
                    else
                    {
                        this.IsPrimaryButtonEnabled = false;
                    }
                }
            }
            else
            {
                this.IsPrimaryButtonEnabled = false;
            }
        }

        private void BasicSetting_Reset()
        {
            basicLabel.Text = "";
            basicSecret.Text = "";
            this.IsPrimaryButtonEnabled = false;
        }

        private void AdvancedSetting_Reset()
        {
            advancedType.SelectedIndex = 0;
            advancedName.Text = "";
            advancedIssuer.Text = "";
            advancedSecret.Text = "";
            advancedCounter.Text = "";
            advancedAlgorithm.SelectedIndex = 0;
            advancedDigits.Text = "";
            this.IsPrimaryButtonEnabled = false;
        }

        private void TotpSetting_Reset()
        {
            advancedPeriod.Text = "";
            this.IsPrimaryButtonEnabled = false;
        }

        private void HotpSetting_Reset()
        {
            advancedCounter.Text = "";
            this.IsPrimaryButtonEnabled = false;
        }
    }
}
