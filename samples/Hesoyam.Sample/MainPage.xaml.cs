using Hesoyam.Sample.ViewModels;

namespace Hesoyam.Sample;

/// <summary>
/// The main page of the Hesoyam sample application.
/// </summary>
public partial class MainPage : ContentPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainPage"/> class.
    /// </summary>
    /// <param name="viewModel">The view model for this page.</param>
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <inheritdoc/>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Refresh scheduled tasks when page appears
        if (BindingContext is MainViewModel vm)
        {
            await vm.RefreshScheduledTasksCommand.ExecuteAsync(null);
        }
    }
}