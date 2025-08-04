using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace APSSystem.Presentation.WPF.ViewModels;

/// <summary>
/// Uma classe base para todos os ViewModels que implementa a notificação de mudança de propriedade.
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Notifica os assinantes (a View) que o valor de uma propriedade foi alterado.
    /// </summary>
    /// <param name="propertyName">O nome da propriedade que mudou. 
    /// Preenchido automaticamente pelo compilador.</param>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}