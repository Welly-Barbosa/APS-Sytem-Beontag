using APSSystem.Presentation.WPF.ViewModels;
using System.Windows;

namespace APSSystem.Presentation.WPF;

public partial class MainWindow : Window
{
    public MainWindow(DashboardViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel; // Conecta a View ao ViewModel
    }
}