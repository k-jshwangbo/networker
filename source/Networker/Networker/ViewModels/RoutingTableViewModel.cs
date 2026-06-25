using Networker.Core;
using Networker.Models;
using Networker.Services;
using Networker.Services.RoutingTable;
using System.Collections.ObjectModel;

namespace Networker.ViewModels;

public sealed class RoutingTableViewModel : ViewModelBase
{
    private readonly IRoutingTableService _service;
    private CancellationTokenSource? _cts;

    private bool _isLoading;
    private string? _errorMessage;

    public ObservableCollection<RouteEntry> Routes { get; } = new();

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
                OnPropertyChanged(nameof(StatusSummary));
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public string StatusSummary =>
        IsLoading ? "Reading routing table\u2026"
                  : Routes.Count > 0 ? $"{Routes.Count} routes"
                                     : "No routes";

    public AsyncRelayCommand RefreshCommand { get; }

    public RoutingTableViewModel(IRoutingTableService service)
    {
        _service = service;
        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsLoading);
        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        _cts = new CancellationTokenSource();

        try
        {
            var routes = await _service.GetRoutesAsync(_cts.Token);

            Routes.Clear();
            foreach (var r in routes)
                Routes.Add(r);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            _cts.Dispose();
            _cts = null;
            OnPropertyChanged(nameof(StatusSummary));
        }
    }
}
