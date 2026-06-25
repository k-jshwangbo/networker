using Networker.Core;
using Networker.Models;
using Networker.Services.NetworkInfo;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace Networker.ViewModels;

public sealed class NetworkInfoViewModel : ViewModelBase
{
    private readonly INetworkInfoService _service;
    private CancellationTokenSource? _cts;

    private bool _isLoading;
    private bool _upOnly = true;
    private NetworkAdapterInfo? _selectedAdapter;
    private string? _errorMessage;

    public ObservableCollection<NetworkAdapterInfo> Adapters { get; } = new();
    public ICollectionView AdaptersView { get; }

    public NetworkAdapterInfo? SelectedAdapter
    {
        get => _selectedAdapter;
        set
        {
            if (SetProperty(ref _selectedAdapter, value))
                OnPropertyChanged(nameof(HasSelection));
        }
    }

    public bool HasSelection => SelectedAdapter is not null;

    public bool UpOnly
    {
        get => _upOnly;
        set
        {
            if (SetProperty(ref _upOnly, value))
            {
                AdaptersView.Refresh();
                EnsureSelection();
                OnPropertyChanged(nameof(StatusSummary));
            }
        }
    }

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
        IsLoading ? "Reading adapters\u2026"
                  : Adapters.Count > 0
                      ? $"{AdaptersView.Cast<object>().Count()} shown / {Adapters.Count} adapters"
                      : "No adapters";

    public AsyncRelayCommand RefreshCommand { get; }

    public NetworkInfoViewModel(INetworkInfoService service)
    {
        _service = service;

        AdaptersView = CollectionViewSource.GetDefaultView(Adapters);
        AdaptersView.Filter = o => o is NetworkAdapterInfo a && (!UpOnly || a.IsUp);

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
            var adapters = await _service.GetAdaptersAsync(_cts.Token);

            Adapters.Clear();
            foreach (var a in adapters)
                Adapters.Add(a);

            EnsureSelection();
            OnPropertyChanged(nameof(StatusSummary));
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

    /// <summary>Keep a valid selection: if the current one is filtered out, pick the first visible.</summary>
    private void EnsureSelection()
    {
        var visible = AdaptersView.Cast<NetworkAdapterInfo>().ToList();

        if (SelectedAdapter is null || !visible.Contains(SelectedAdapter))
            SelectedAdapter = visible.FirstOrDefault();
    }
}
