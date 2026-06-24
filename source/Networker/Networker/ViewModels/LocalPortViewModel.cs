using Networker.Core;
using Networker.Models;
using Networker.Services.PortScan.Local;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace Networker.ViewModels;

public sealed class LocalPortViewModel : ViewModelBase
{
    private readonly ILocalPortService _service;
    private CancellationTokenSource? _cts;

    private bool _isRunning;
    private bool _listenOnly = true;
    private string _filterText = "";
    private string? _errorMessage;

    public ObservableCollection<LocalConnectionResult> Connections { get; } = new();

    /// <summary>Filtered/sorted view bound by the DataGrid.</summary>
    public ICollectionView ConnectionsView { get; }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
                OnPropertyChanged(nameof(StatusSummary));
        }
    }

    public bool ListenOnly
    {
        get => _listenOnly;
        set
        {
            if (SetProperty(ref _listenOnly, value))
            {
                ConnectionsView.Refresh();
                OnPropertyChanged(nameof(StatusSummary));
            }
        }
    }

    public string FilterText
    {
        get => _filterText;
        set
        {
            if (SetProperty(ref _filterText, value))
            {
                ConnectionsView.Refresh();
                OnPropertyChanged(nameof(StatusSummary));
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public int TotalCount => Connections.Count;

    public int ShownCount => ConnectionsView.Cast<object>().Count();

    public string StatusSummary =>
        IsRunning ? "Reading local connections\u2026"
                  : TotalCount > 0
                      ? $"{ShownCount} shown / {TotalCount} total"
                      : "Idle";

    public AsyncRelayCommand RefreshCommand { get; }
    public RelayCommand ClearCommand { get; }

    public LocalPortViewModel(ILocalPortService service)
    {
        _service = service;

        ConnectionsView = CollectionViewSource.GetDefaultView(Connections);
        ConnectionsView.Filter = MatchesFilter;
        ((ListCollectionView)ConnectionsView).CustomSort = new ConnectionComparer();

        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsRunning);
        ClearCommand = new RelayCommand(Clear, () => Connections.Count > 0 && !IsRunning);
    }

    private bool MatchesFilter(object item)
    {
        if (item is not LocalConnectionResult r)
            return false;

        if (ListenOnly && r.State != TcpConnectionState.Listen)
            return false;

        var f = FilterText.Trim();
        if (f.Length == 0)
            return true;

        return r.ProcessName.Contains(f, StringComparison.OrdinalIgnoreCase)
            || r.LocalPort.ToString().Contains(f, StringComparison.Ordinal)
            || r.Pid.ToString().Contains(f, StringComparison.Ordinal)
            || r.Service.Contains(f, StringComparison.OrdinalIgnoreCase);
    }

    private async Task RefreshAsync()
    {
        IsRunning = true;
        ErrorMessage = null;
        _cts = new CancellationTokenSource();

        try
        {
            var rows = await _service.GetTcpConnectionsAsync(_cts.Token);

            Connections.Clear();
            foreach (var row in rows)
                Connections.Add(row);

            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(ShownCount));
        }
        catch (OperationCanceledException)
        {
            // refresh cancelled -> keep whatever is on screen.
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsRunning = false;
            _cts.Dispose();
            _cts = null;
            OnPropertyChanged(nameof(StatusSummary));
        }
    }

    private void Clear()
    {
        Connections.Clear();
        ErrorMessage = null;
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(ShownCount));
        OnPropertyChanged(nameof(StatusSummary));
    }

    /// <summary>Listeners first, then by local port, then by PID.</summary>
    private sealed class ConnectionComparer : IComparer
    {
        public int Compare(object? x, object? y)
        {
            if (x is not LocalConnectionResult a || y is not LocalConnectionResult b)
                return 0;

            int byListen = (b.IsListening ? 1 : 0) - (a.IsListening ? 1 : 0);
            if (byListen != 0)
                return byListen;

            int byPort = a.LocalPort.CompareTo(b.LocalPort);
            if (byPort != 0)
                return byPort;

            return a.Pid.CompareTo(b.Pid);
        }
    }
}
