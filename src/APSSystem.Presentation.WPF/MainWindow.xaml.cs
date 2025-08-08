using APSSystem.Application.Interfaces;
using APSSystem.Presentation.WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace APSSystem.Presentation.WPF;

public partial class MainWindow : Window
{
    private readonly IServiceProvider _serviceProvider;

    public MainWindow(DashboardViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;

        viewModel.OptimizationCompleted += OnOptimizationCompleted;

        DataContext = viewModel;
    }

    private void OnOptimizationCompleted(GamsExecutionResult result)
    {
        var resultadosView = _serviceProvider.GetRequiredService<ResultadosOtimizacaoWindow>();
        resultadosView.CarregarDadosDoJob(result.CaminhoPastaJob);
        resultadosView.Owner = this;
        resultadosView.Show();
    }
}