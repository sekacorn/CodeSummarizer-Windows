using System.Windows;
using System.ComponentModel;
using CodeSummarizer.Windows.Services;
using CodeSummarizer.Windows.ViewModels;

namespace CodeSummarizer.Windows;

public partial class MainWindow : Window, IDisposable
{
    private readonly MainViewModel _viewModel = new();
    private bool _disposed;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        SourceInitialized += (_, _) => ApplyWindowSecurity();
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        Loaded += async (_, _) => await _viewModel.RefreshModelsAsync();
        Closed += (_, _) => Dispose();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SensitiveMode))
            ApplyWindowSecurity();
    }

    private void ApplyWindowSecurity()
    {
        var enabled = NativeWindowSecurity.SetCaptureProtection(this, _viewModel.SensitiveMode);
        _viewModel.ReportCaptureProtection(enabled);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
