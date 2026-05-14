using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DSRemote.Models;

namespace DSRemote.Views;

public partial class SetupWizard : Window
{
    private int _currentStep;
    private readonly AppConfig _config = new();
    private static readonly string[] StepTitles =
    {
        "Select Game Folders",
        "Choose Accent Color",
        "Finish Setup"
    };

    public AppConfig? Result { get; private set; }

    public SetupWizard()
    {
        InitializeComponent();
        UpdateStep();
    }

    private void UpdateStep()
    {
        StepTitle.Text = StepTitles[_currentStep];
        StepIndicator.Text = $"Step {_currentStep + 1} of {StepTitles.Length}";
        BackBtn.IsEnabled = _currentStep > 0;
        NextBtn.Content = _currentStep == StepTitles.Length - 1 ? "Finish" : "Next";

        ContentFrame.Navigate(GetStepPage(_currentStep));
    }

    internal AppConfig CurrentConfig => _config;

    private Page GetStepPage(int step) => step switch
    {
        0 => new GameFolderPage(this),
        1 => new ColorPickerPage(),
        2 => new SummaryPage(),
        _ => new GameFolderPage(this)
    };

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (_currentStep > 0)
        {
            _currentStep--;
            UpdateStep();
        }
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        var page = ContentFrame.Content as ISetupPage;
        if (page != null && !page.Validate(_config)) return;

        if (_currentStep == StepTitles.Length - 1)
        {
            _config.SetupComplete = true;
            Result = _config;
            DialogResult = true;
            Close();
            return;
        }

        _currentStep++;
        UpdateStep();
    }
}

public interface ISetupPage
{
    bool Validate(AppConfig config);
}

public class GameFolderPage : Page, ISetupPage
{
    private readonly SetupWizard _wizard = null!;
    private readonly ListBox _folderList = new()
    {
        Height = 150,
        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16213e")),
        Foreground = Brushes.White, BorderThickness = new Thickness(1),
        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0f3460"))
    };

    private readonly TextBlock _gameCount = new()
    {
        FontSize = 13, Foreground = Brushes.Gray, Margin = new Thickness(0, 10, 0, 0)
    };

    public GameFolderPage(SetupWizard wizard)
    {
        _wizard = wizard;
        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1a1a2e"));

        var addBtn = new Button
        {
            Content = "+ Add Folder", Padding = new Thickness(15, 8, 15, 8), Margin = new Thickness(0, 0, 10, 0),
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0f3460")),
            Foreground = Brushes.White, BorderThickness = new Thickness(0)
        };
        addBtn.Click += AddFolder_Click;

        var removeBtn = new Button
        {
            Content = "Remove", Padding = new Thickness(15, 8, 15, 8),
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e94560")),
            Foreground = Brushes.White, BorderThickness = new Thickness(0)
        };
        removeBtn.Click += RemoveFolder_Click;

        var btnGrid = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
        btnGrid.Children.Add(addBtn);
        btnGrid.Children.Add(removeBtn);

        var stack = new StackPanel { Margin = new Thickness(30) };
        stack.Children.Add(new TextBlock
        {
            Text = "Select folders containing your 3DS ROMs:", FontSize = 16,
            Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 10)
        });
        stack.Children.Add(new TextBlock
        {
            Text = "Supported: .3ds, .cci, .cxi, .cia, .3dsx",
            FontSize = 12, Foreground = Brushes.Gray, Margin = new Thickness(0, 0, 0, 15)
        });
        stack.Children.Add(btnGrid);
        stack.Children.Add(_folderList);
        stack.Children.Add(_gameCount);
        Content = stack;
    }

    private void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog { Title = "Select ROM folder" };
        if (dlg.ShowDialog() == true)
        {
            var config = _wizard.CurrentConfig;
            if (!config.GameFolders.Contains(dlg.FolderName))
            {
                config.GameFolders.Add(dlg.FolderName);
                _folderList.Items.Add(dlg.FolderName);
            }
        }
    }

    private void RemoveFolder_Click(object sender, RoutedEventArgs e)
    {
        if (_folderList.SelectedItem is string folder)
        {
            var config = _wizard.CurrentConfig;
            config.GameFolders.Remove(folder);
            _folderList.Items.Remove(folder);
        }
    }

    public bool Validate(AppConfig config)
    {
        foreach (var item in _folderList.Items)
        {
            if (item is string folder && !config.GameFolders.Contains(folder))
                config.GameFolders.Add(folder);
        }

        if (config.GameFolders.Count == 0)
        {
            MessageBox.Show("Please add at least one game folder.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        return true;
    }
}

public class ColorPickerPage : Page, ISetupPage
{
    private readonly Border _preview;

    private static readonly (string Name, string Hex)[] Colors =
    {
        ("Lime Green", "#32CD32"), ("Blue", "#2196F3"), ("Red", "#F44336"),
        ("Purple", "#9C27B0"), ("Orange", "#FF9800"), ("Pink", "#E91E63"),
        ("Teal", "#009688"), ("Cyan", "#00BCD4"), ("Amber", "#FFC107"),
        ("Deep Purple", "#673AB7"), ("Indigo", "#3F51B5"), ("White", "#FFFFFF")
    };

    public ColorPickerPage()
    {
        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1a1a2e"));

        _preview = new Border
        {
            Width = 60, Height = 60, CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#32CD32")),
            Margin = new Thickness(0, 0, 0, 15),
            HorizontalAlignment = HorizontalAlignment.Left,
            BorderThickness = new Thickness(2), BorderBrush = Brushes.White
        };

        var colorStack = new StackPanel();
        var selectedHex = "#32CD32";

        foreach (var (name, hex) in Colors)
        {
            var localHex = hex;
            var border = new Border
            {
                Width = 20, Height = 20, CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)),
                Margin = new Thickness(0, 0, 10, 0)
            };
            var text = new TextBlock
            {
                Text = name, FontSize = 14, Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };

            var item = new Border
            {
                Padding = new Thickness(8, 6, 8, 6), Margin = new Thickness(2),
                Background = Brushes.Transparent, CornerRadius = new CornerRadius(6),
                Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { border, text } }
            };

            item.MouseLeftButtonUp += (_, _) =>
            {
                selectedHex = localHex;
                _preview.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(localHex));
            };

            colorStack.Children.Add(item);
        }

        var scrollViewer = new ScrollViewer
        {
            Content = colorStack, Height = 250,
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16213e")),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0f3460"))
        };

        var stack = new StackPanel { Margin = new Thickness(30) };
        stack.Children.Add(new TextBlock
        {
            Text = "Choose your accent color:", FontSize = 16,
            Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 15)
        });
        stack.Children.Add(_preview);
        stack.Children.Add(scrollViewer);

        Tag = selectedHex;
        Content = stack;
    }

    public bool Validate(AppConfig config)
    {
        if (_preview.Background is SolidColorBrush brush)
            config.AccentColor = brush.Color.ToString();
        return true;
    }
}

public class SummaryPage : Page, ISetupPage
{
    public SummaryPage()
    {
        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1a1a2e"));
        Content = new StackPanel { Margin = new Thickness(30) };
    }

    public bool Validate(AppConfig config)
    {
        var stack = (StackPanel)Content;
        stack.Children.Clear();

        stack.Children.Add(new TextBlock
        {
            Text = "Setup Complete!", FontSize = 22,
            Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 20)
        });

        AddDetail(stack, "Platform:", "Nintendo 3DS (Azahar)");
        AddDetail(stack, "Game Folders:",
            config.GameFolders.Count > 0
                ? string.Join(", ", config.GameFolders.Select(Path.GetFileName))
                : "None");
        AddDetail(stack, "Accent Color:", config.AccentColor);

        stack.Children.Add(new TextBlock
        {
            Text = "Press Finish to save and start using DSRemote!",
            FontSize = 15, Foreground = Brushes.LightGreen,
            Margin = new Thickness(0, 30, 0, 0), FontStyle = FontStyles.Italic
        });

        return true;
    }

    private static void AddDetail(StackPanel parent, string label, string value)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.Children.Add(new TextBlock
        {
            Text = label, FontSize = 14, Foreground = Brushes.Gray,
            FontWeight = FontWeights.SemiBold, MinWidth = 120
        });
        grid.Children.Add(new TextBlock
        {
            Text = value, FontSize = 14, Foreground = Brushes.White,
            Margin = new Thickness(10, 0, 0, 0)
        });
        Grid.SetColumn(grid.Children[1], 1);
        parent.Children.Add(grid);
    }
}
