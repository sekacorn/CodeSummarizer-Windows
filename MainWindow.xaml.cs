using System.Windows;
using CodeSummarizer.Windows.ViewModels;

namespace CodeSummarizer.Windows;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        Loaded += async (_, _) => await _viewModel.RefreshModelsAsync();
        Closed += (_, _) => _viewModel.Dispose();
    }
}
