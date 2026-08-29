using System.ComponentModel;
using Microsoft.AspNetCore.Components;

namespace DhahabiDelivery.Modules.Shared;

public class ViewModelComponent<TViewModelType> : ComponentBase, IDisposable
    where TViewModelType : INotifyPropertyChanged
{
    [Inject] public required TViewModelType ViewModel { get; set; }

    // Método para liberar recursos al destruir el componente
    public void Dispose()
    {
        ViewModel.PropertyChanged -= OnPropertyChangedHandler;
        GC.SuppressFinalize(this);
    }

    protected override async Task OnInitializedAsync()
    {
        // Suscribirse al evento PropertyChanged del ViewModel para actualizar la vista cuando cambien las propiedades
        ViewModel.PropertyChanged += OnPropertyChangedHandler;
        await base.OnInitializedAsync();
    }

    // Método para manejar el cambio en las propiedades del ViewModel
    private async void OnPropertyChangedHandler(object? sender, PropertyChangedEventArgs e)
    {
        await InvokeAsync(StateHasChanged);
    }
}