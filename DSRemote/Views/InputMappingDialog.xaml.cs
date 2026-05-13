using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DSRemote.Models;

namespace DSRemote.Views;

public partial class InputMappingDialog : Window
{
    private readonly Dictionary<int, string> _buttonNames = new()
    {
        [0] = "A", [1] = "B", [2] = "X", [3] = "Y",
        [4] = "L", [5] = "R", [6] = "Start", [7] = "Select",
    };

    private readonly Dictionary<int, string> _dpadNames = new()
    {
        [0] = "D-Pad Up", [1] = "D-Pad Down", [2] = "D-Pad Left", [3] = "D-Pad Right",
    };

    private readonly Dictionary<int, int> _buttonMappings;
    private readonly Dictionary<int, int> _dpadMappings;
    private int? _selectedButtonId;
    private bool _isDpad;

    public AppConfig? ResultConfig { get; private set; }

    public InputMappingDialog(Dictionary<int, int> currentButtons, Dictionary<int, int> currentDpad)
    {
        InitializeComponent();
        _buttonMappings = new Dictionary<int, int>(currentButtons);
        _dpadMappings = new Dictionary<int, int>(currentDpad);
        BuildBindingList();
    }

    private static string KeyName(int vk)
    {
        if (vk >= 0x30 && vk <= 0x5A) return ((char)vk).ToString();
        return vk switch
        {
            0x08 => "Backspace",
            0x09 => "Tab",
            0x0D => "Enter",
            0x10 => "Shift",
            0x11 => "Ctrl",
            0x12 => "Alt",
            0x1B => "Escape",
            0x20 => "Space",
            0x25 => "Left Arrow",
            0x26 => "Up Arrow",
            0x27 => "Right Arrow",
            0x28 => "Down Arrow",
            _ => $"0x{vk:X}"
        };
    }

    private void BuildBindingList()
    {
        BindingsPanel.Children.Clear();

        AddHeader("Face & Shoulder Buttons");
        foreach (var (id, name) in _buttonNames)
        {
            var vk = _buttonMappings.GetValueOrDefault(id);
            AddBindingRow($"  {name}", KeyName(vk), isDpad: false, id);
        }

        AddHeader("D-Pad");
        foreach (var (id, name) in _dpadNames)
        {
            var vk = _dpadMappings.GetValueOrDefault(id);
            AddBindingRow(name, KeyName(vk), isDpad: true, id);
        }
    }

    private void AddHeader(string text)
    {
        BindingsPanel.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.Gray,
            Margin = new Thickness(0, 10, 0, 5)
        });
    }

    private void AddBindingRow(string label, string key, bool isDpad, int id)
    {
        var border = new Border
        {
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(2, 2, 2, 2),
            CornerRadius = new CornerRadius(6),
            Cursor = Cursors.Hand,
            Tag = (isDpad, id),
            Background = Brushes.Transparent,
        };

        border.MouseLeftButtonUp += (_, _) =>
        {
            _selectedButtonId = id;
            _isDpad = isDpad;
            StatusText.Text = $"Press a key to bind \"{label}\"...";
            foreach (var child in BindingsPanel.Children)
            {
                if (child is Border b)
                    b.Background = b == border
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0f3460"))
                        : Brushes.Transparent;
            }
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        grid.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 14,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeights.Medium
        });

        var keyBorder = new Border
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16213e")),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(12, 4, 12, 4),
            MinWidth = 80,
        };
        Grid.SetColumn(keyBorder, 1);

        var keyText = new TextBlock
        {
            Text = key,
            FontSize = 13,
            Foreground = Brushes.LightGreen,
            FontFamily = new FontFamily("Consolas"),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        keyBorder.Child = keyText;
        grid.Children.Add(keyBorder);

        border.Child = grid;
        BindingsPanel.Children.Add(border);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (_selectedButtonId == null)
        {
            StatusText.Text = "Select a button binding first by clicking on it.";
            return;
        }

        var vk = KeyInterop.VirtualKeyFromKey(e.Key);
        if (vk == 0) return;

        if (_isDpad)
            _dpadMappings[_selectedButtonId.Value] = vk;
        else
            _buttonMappings[_selectedButtonId.Value] = vk;

        _selectedButtonId = null;
        StatusText.Text = $"Bound to {KeyName(vk)}!";
        BuildBindingList();
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        var defaults = new AppConfig();
        foreach (var kv in defaults.ButtonMappings)
            _buttonMappings[kv.Key] = kv.Value;
        foreach (var kv in defaults.DPadMappings)
            _dpadMappings[kv.Key] = kv.Value;
        _selectedButtonId = null;
        StatusText.Text = "Reset to defaults.";
        BuildBindingList();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ResultConfig = new AppConfig
        {
            ButtonMappings = new Dictionary<int, int>(_buttonMappings),
            DPadMappings = new Dictionary<int, int>(_dpadMappings),
        };
        DialogResult = true;
        Close();
    }
}
