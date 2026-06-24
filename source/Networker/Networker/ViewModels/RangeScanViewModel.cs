using Networker.Core;
using Networker.Models;
using Networker.Services.RangeScan;
using System.Collections.ObjectModel;
using System.Net;

namespace Networker.ViewModels;
public sealed class RangeScanViewModel : ViewModelBase
{
    private readonly IHostDiscoveryService _discovery;
    private CancellationTokenSource? _cts;

    private string _targetText = LocalNetwork.GetDefaultRange();
    private int _pingTimeoutMs = 1000;
    private int _maxConcurrency = 100;
    private bool _resolveHostNames = true;
    private bool _isRunning;
    private int _scanned;
    private int _total;
    private int _aliveCount;
    private string? _errorMessage;


    public ObservableCollection<HostScanResult> AliveHosts { get; } = new();
    public string TargetText { get => _targetText; set => SetProperty(ref _targetText, value); }
    public int PingTimeoutMs { get => _pingTimeoutMs; set => SetProperty(ref _pingTimeoutMs, value); }
    public int MaxConcurrency { get => _maxConcurrency; set => SetProperty(ref _maxConcurrency, value); }
    public bool ResolveHostNames { get => _resolveHostNames; set=> SetProperty(ref _resolveHostNames, value); }
    public bool IsRunning
    {
        get => _isRunning;
        private set { if (SetProperty(ref _isRunning, value)) OnPropertyChanged(nameof(StatusSummary)); }
    }

    public int Scanned { get => _scanned; private set => SetProperty(ref _scanned, value); }
    public int Total { get => _total; private set => SetProperty(ref _total, value); }
    public int AliveCount { get => _aliveCount; private set => SetProperty(ref _aliveCount, value); }
    public string? ErrorMessage { get => _errorMessage; private set => SetProperty(ref _errorMessage, value); }
    public string StatusSummary =>
        IsRunning ? $"Scanning\u2026 {Scanned}/{Total}  (alive:{AliveCount})"
                  : Total > 0 ? $"Done.  {Scanned}/{Total} scanned, {AliveCount} alive"
                              : "Idle";

    public AsyncRelayCommand StartCommand { get; }
    public RelayCommand StopCommand { get; }
    public RelayCommand ClearCommand { get; }


    public RangeScanViewModel(IHostDiscoveryService discovery)
    {
        _discovery = discovery;

        StartCommand = new AsyncRelayCommand(
            StartAsync,
            () => !IsRunning && !string.IsNullOrWhiteSpace(TargetText));
        StopCommand = new RelayCommand(Stop, () => IsRunning);
        ClearCommand = new RelayCommand(Clear, () => AliveHosts.Count > 0 && !IsRunning);
    }


    private async Task StartAsync()
    {
        IReadOnlyList<IPAddress> hosts;

        try
        {
            hosts = IpRange.Parse(TargetText);
        }
        catch (FormatException ex)
        {
            ErrorMessage = ex.Message;
            return;
        }

        ResetState();
        Total = hosts.Count;
        IsRunning = true;
        _cts = new CancellationTokenSource();

        var progress = new Progress<HostScanResult>(OnResult);

        try
        {
            await ParallelExecutor.RunAsync(
                hosts,
                Math.Max(1, MaxConcurrency),
                (ip, token) => _discovery.ProbeAsync(ip, TimeSpan.FromMilliseconds(PingTimeoutMs), ResolveHostNames, token),
                progress,
                _cts.Token);
        }
        catch (OperationCanceledException)
        {
            // stop pressed -> keep hosts found so far.
        }
        finally
        {
            IsRunning = false;
            _cts.Dispose();
            _cts = null;
            OnPropertyChanged(nameof(StatusSummary));
        }
    }


    private void OnResult(HostScanResult result)
    {
        Scanned++;
        if (result.IsAlive)
        {
            AliveHosts.Add(result);
            AliveCount++;
        }

        OnPropertyChanged(nameof(StatusSummary));
    }


    private void Stop() => _cts?.Cancel();


    private void Clear()
    {
        AliveHosts.Clear();
        ResetState();
    }


    private void ResetState()
    {
        ErrorMessage = null;
        Scanned = 0;
        Total = 0;
        AliveCount = 0;
        OnPropertyChanged(nameof(StatusSummary));
    }
}
