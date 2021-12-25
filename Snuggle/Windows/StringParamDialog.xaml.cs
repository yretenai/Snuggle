using System.ComponentModel;
using System.Windows;

namespace Snuggle.Windows; 

public partial class StringParamDialog {
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register("Header", typeof(string), typeof(StringParamDialog), new PropertyMetadata(""));
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(StringParamDialog), new PropertyMetadata(""));

    public StringParamDialog() {
        InitializeComponent();
    }

    public StringParamDialog(string text, string header) : this() {
        Header = header;
        Text = text;
    }

    public string Header {
        get => (string) GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }
    
    public string Text {
        get => (string) GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private void OnClosing(object? sender, CancelEventArgs e) {
        DialogResult ??= false;
    }

    private void Accept(object sender, RoutedEventArgs e) {
        DialogResult = true;
        Close();
    }

    private void Reject(object sender, RoutedEventArgs e) {
        DialogResult = false;
        Close();
    }
}

