using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DSRemote.Models;

namespace DSRemote.Views;

public partial class KidsModeWindow : Window
{
    private readonly KidsModeManager _manager = new();
    public bool IsKidModeActive { get; private set; }

    public KidsModeWindow()
    {
        InitializeComponent();
        Loaded += KidsModeWindow_Loaded;
    }

    private void KidsModeWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Apply background if saved
        var bgPath = _manager.BackgroundImagePath;
        if (bgPath != null)
        {
            try
            {
                BgImage.Source = new BitmapImage(new Uri(bgPath));
            }
            catch { }
        }

        AgeSlider.Value = _manager.SelectedAge;
        UpdateAgeDisplay();

        if (_manager.HasPassword)
        {
            LockPanel.Visibility = Visibility.Visible;
            SetupPanel.Visibility = Visibility.Collapsed;
            ConnectedPanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            LockPanel.Visibility = Visibility.Collapsed;
            SetupPanel.Visibility = Visibility.Visible;
            ConnectedPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void AgeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _manager.SelectedAge = (int)e.NewValue;
        UpdateAgeDisplay();
    }

    private void UpdateAgeDisplay()
    {
        AgeDisplay.Text = $"Age: {_manager.SelectedAge}";
    }

    private void NewPassword_TextChanged(object sender, TextChangedEventArgs e)
    {
        var text = NewPasswordBox.Text;
        // Allow only digits
        var filtered = new string(text.Where(char.IsDigit).Take(4).ToArray());
        if (filtered != text)
        {
            NewPasswordBox.Text = filtered;
            NewPasswordBox.CaretIndex = filtered.Length;
        }

        if (filtered.Length == 4)
        {
            var encoded = KidsModeManager.EncodePassword(filtered);
            SymbolPreview.Text = $"Stored as: {encoded}";
            SetPasswordBtn.IsEnabled = true;
        }
        else
        {
            SymbolPreview.Text = "";
            SetPasswordBtn.IsEnabled = false;
        }
    }

    private void SetPassword_Click(object sender, RoutedEventArgs e)
    {
        var digits = NewPasswordBox.Text;
        if (digits.Length != 4 || !digits.All(char.IsDigit))
        {
            MessageBox.Show("Please enter exactly 4 digits.", "Invalid PIN",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _manager.SavePassword(digits);
        MessageBox.Show($"PIN set successfully!\nStored as: {KidsModeManager.EncodePassword(digits)}",
            "PIN Saved", MessageBoxButton.OK, MessageBoxImage.Information);

        ShowConnectedScreen();
    }

    private void LockPassword_TextChanged(object sender, TextChangedEventArgs e)
    {
        var text = LockPasswordBox.Text;
        // Allow only digits
        var filtered = new string(text.Where(char.IsDigit).Take(4).ToArray());
        if (filtered != text)
        {
            LockPasswordBox.Text = filtered;
            LockPasswordBox.CaretIndex = filtered.Length;
        }

        LockError.Text = "";
        UnlockBtn.IsEnabled = filtered.Length == 4;
    }

    private void Unlock_Click(object sender, RoutedEventArgs e)
    {
        var input = LockPasswordBox.Text;
        if (_manager.VerifyPassword(input))
        {
            LockError.Text = "";
            ShowConnectedScreen();
        }
        else
        {
            LockError.Text = "Incorrect PIN. Try again.";
            LockPasswordBox.Text = "";
        }
    }

    private void ShowConnectedScreen()
    {
        IsKidModeActive = true;
        LockPanel.Visibility = Visibility.Collapsed;
        SetupPanel.Visibility = Visibility.Collapsed;
        ConnectedPanel.Visibility = Visibility.Visible;
    }

    private void CloseKids_Click(object sender, RoutedEventArgs e)
    {
        IsKidModeActive = false;
        Close();
    }
}
