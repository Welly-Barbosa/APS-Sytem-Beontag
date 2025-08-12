using System.Windows;

namespace APSSystem.Presentation.WPF;

/// <summary>
/// Janela de diálogo para exibir mensagens de erro detalhadas e copiáveis.
/// Herda de System.Windows.Window, o que lhe dá o método .ShowDialog().
/// </summary>
public partial class ErrorWindow : Window
{
    public ErrorWindow(string errorMessage)
    {
        InitializeComponent();
        DataContext = errorMessage; // Define a mensagem de erro como o contexto de dados para o TextBox
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close(); // Fecha a janela ao clicar em OK
    }
}