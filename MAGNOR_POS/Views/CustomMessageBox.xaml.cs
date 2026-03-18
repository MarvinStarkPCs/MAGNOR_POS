using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MAGNOR_POS.Views;

public partial class CustomMessageBox : Window
{
    public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

    private static readonly Color PrimaryGreen = (Color)ColorConverter.ConvertFromString("#146e39");
    private static readonly Color DarkGreen = (Color)ColorConverter.ConvertFromString("#0d5028");
    private static readonly Color ErrorRed = (Color)ColorConverter.ConvertFromString("#cc2128");
    private static readonly Color DarkRed = (Color)ColorConverter.ConvertFromString("#a01b20");
    private static readonly Color WarningOrange = (Color)ColorConverter.ConvertFromString("#E65100");

    private CustomMessageBox(string message, string title, MessageBoxButton buttons, MessageBoxImage icon)
    {
        InitializeComponent();

        TxtMessage.Text = message;
        TxtTitle.Text = title;

        // Set icon and colors based on type
        switch (icon)
        {
            case MessageBoxImage.Error:
                TxtIcon.Text = "\u274C"; // X mark
                TxtTitle.Foreground = new SolidColorBrush(ErrorRed);
                FindBorder().BorderBrush = new SolidColorBrush(ErrorRed);
                break;
            case MessageBoxImage.Warning:
                TxtIcon.Text = "\u26A0\uFE0F"; // Warning
                TxtTitle.Foreground = new SolidColorBrush(WarningOrange);
                FindBorder().BorderBrush = new SolidColorBrush(WarningOrange);
                break;
            case MessageBoxImage.Question:
                TxtIcon.Text = "\u2753"; // Question
                TxtTitle.Foreground = new SolidColorBrush(PrimaryGreen);
                FindBorder().BorderBrush = new SolidColorBrush(PrimaryGreen);
                break;
            default: // Information
                TxtIcon.Text = "\u2705"; // Check
                TxtTitle.Foreground = new SolidColorBrush(PrimaryGreen);
                FindBorder().BorderBrush = new SolidColorBrush(PrimaryGreen);
                break;
        }

        // Create buttons based on type
        switch (buttons)
        {
            case MessageBoxButton.OK:
                AddButton("Aceptar", MessageBoxResult.OK, true, icon == MessageBoxImage.Error);
                break;
            case MessageBoxButton.OKCancel:
                AddButton("Cancelar", MessageBoxResult.Cancel, false, false);
                AddButton("Aceptar", MessageBoxResult.OK, true, icon == MessageBoxImage.Error);
                break;
            case MessageBoxButton.YesNo:
                AddButton("No", MessageBoxResult.No, false, false);
                AddButton("Si", MessageBoxResult.Yes, true, icon == MessageBoxImage.Error);
                break;
            case MessageBoxButton.YesNoCancel:
                AddButton("Cancelar", MessageBoxResult.Cancel, false, false);
                AddButton("No", MessageBoxResult.No, false, false);
                AddButton("Si", MessageBoxResult.Yes, true, icon == MessageBoxImage.Error);
                break;
        }
    }

    private Border FindBorder()
    {
        return (Border)Content;
    }

    private void AddButton(string text, MessageBoxResult result, bool isPrimary, bool isError)
    {
        var btn = new Button
        {
            Content = text,
            MinWidth = 90,
            Height = 40,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(8, 0, 0, 0),
            Cursor = Cursors.Hand,
            BorderThickness = new Thickness(0)
        };

        if (isPrimary)
        {
            var bgColor = isError ? ErrorRed : PrimaryGreen;
            var hoverColor = isError ? DarkRed : DarkGreen;

            btn.Background = new SolidColorBrush(bgColor);
            btn.Foreground = Brushes.White;
            btn.Template = CreateButtonTemplate(bgColor, hoverColor);
        }
        else
        {
            btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8E8E8"));
            btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
            btn.Template = CreateButtonTemplate(
                (Color)ColorConverter.ConvertFromString("#E8E8E8"),
                (Color)ColorConverter.ConvertFromString("#D0D0D0"));
        }

        btn.Click += (s, e) =>
        {
            Result = result;
            DialogResult = true;
            Close();
        };

        ButtonPanel.Children.Add(btn);
    }

    private static ControlTemplate CreateButtonTemplate(Color bgColor, Color hoverColor)
    {
        var template = new ControlTemplate(typeof(Button));

        var borderFactory = new FrameworkElementFactory(typeof(Border));
        borderFactory.Name = "border";
        borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush(bgColor));
        borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
        borderFactory.SetValue(Border.PaddingProperty, new Thickness(16, 0, 16, 0));

        var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
        contentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        contentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        borderFactory.AppendChild(contentFactory);

        template.VisualTree = borderFactory;

        // Hover trigger
        var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(hoverColor), "border"));
        template.Triggers.Add(hoverTrigger);

        return template;
    }

    private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    // ============== Static Show methods ==============

    /// <summary>
    /// Show a custom message box with OK button
    /// </summary>
    public static MessageBoxResult Show(string message, string title = "MAGNOR POS",
        MessageBoxButton buttons = MessageBoxButton.OK,
        MessageBoxImage icon = MessageBoxImage.Information,
        MessageBoxResult defaultResult = MessageBoxResult.None)
    {
        var msgBox = new CustomMessageBox(message, title, buttons, icon);

        try
        {
            var mainWindow = Application.Current?.MainWindow;
            if (mainWindow != null && mainWindow.IsVisible && mainWindow.IsLoaded)
            {
                msgBox.Owner = mainWindow;
            }
        }
        catch { }

        msgBox.ShowDialog();

        if (msgBox.Result == MessageBoxResult.None && defaultResult != MessageBoxResult.None)
            return defaultResult;

        return msgBox.Result;
    }

    /// <summary>
    /// Show info message
    /// </summary>
    public static void Info(string message, string title = "Informacion")
    {
        Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>
    /// Show error message
    /// </summary>
    public static void Error(string message, string title = "Error")
    {
        Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    /// <summary>
    /// Show warning message
    /// </summary>
    public static void Warning(string message, string title = "Advertencia")
    {
        Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    /// <summary>
    /// Show Yes/No question (No is default)
    /// </summary>
    public static bool Confirm(string message, string title = "Confirmar")
    {
        var result = Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        return result == MessageBoxResult.Yes;
    }
}
