using Networker.Core;
using Networker.Models;
using Networker.Services.PortScan.Remote;
using System.Collections.ObjectModel;

namespace Networker.ViewModels;
public sealed class PortScanViewModel : ViewModelBase
{
    private readonly IPortScanService _scanService;
    private CancellationTokenSource? _cts;

    private string _host = "127.0.0.1";
    private string _portRangeText = "1-1024";
    private int _timeoutMs = 1000;
    private int _maxConcurrency = 200;
    private bool _isRunning;
    private int _scanned;
    private int _total;
    private int _openCount;
    private string? _errorMessage;

    public ObservableCollection<PortScanResult> OpenPorts { get; } = new();
    public string Host { get => _host; set => SetProperty(ref _host, value); }
    public string PortRangeText { get => _portRangeText; set=>SetProperty(ref _portRangeText, value); }
    public int TimeoutMs { get => _timeoutMs; set => SetProperty(ref _timeoutMs, value); }
    public int MaxConcurrency { get => _maxConcurrency; set=> SetProperty(ref _maxConcurrency, value); }
    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
                OnPropertyChanged(nameof(StatusSummary));
        }
    }
    public int Scanned { get => _scanned; private set => SetProperty(ref _scanned, value); }
    public int Total { get => _total; private set => SetProperty(ref _total, value); }
    public int OpenCount { get => _openCount; private set => SetProperty(ref _openCount, value); }
    public string? ErrorMessage { get => _errorMessage; private set => SetProperty(ref _errorMessage, value); }
    public string StatusSummary =>
        IsRunning ? $"Scanning {Host}\u2026 {Scanned}/{Total}  (open: {OpenCount})"
                  : Total > 0 ? $"Done. {Scanned}/{Total} scanned, {OpenCount} open"
                              : "Idle";

    public AsyncRelayCommand StartCommand { get; }
    public RelayCommand StopCommand { get; }
    public RelayCommand ClearCommand { get; }



    public PortScanViewModel(IPortScanService scanService)
    {
        _scanService = scanService;

        StartCommand = new AsyncRelayCommand(
            StartAsync,
            () => !IsRunning && !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(PortRangeText));
        StopCommand = new RelayCommand(Stop, () => IsRunning);
        ClearCommand = new RelayCommand(Clear, () => OpenPorts.Count > 0 && !IsRunning);
    }


    private async Task StartAsync()
    {
        IReadOnlyCollection<int> ports;
        try
        {
            ports = PortRange.Parse(PortRangeText);
        }
        catch(FormatException ex)
        {
            ErrorMessage = ex.Message;
            return;
        }

        ResetState();
        Total = ports.Count;
        IsRunning = true;
        _cts = new CancellationTokenSource();

        var progress = new Progress<PortScanResult>(OnResult);

        try
        {
            await ParallelExecutor.RunAsync(
                ports,
                Math.Max(1, MaxConcurrency),
                (port, token) => _scanService.ScanPortAsync(Host, port, TimeSpan.FromMilliseconds(TimeoutMs), token),
                progress,
                _cts.Token);
        }
        catch (OperationCanceledException)
        {
            // stop pressed -> keep whatever was found so far on screen.
        }
        finally
        {
            IsRunning = false;
            _cts.Dispose();
            _cts = null;
            OnPropertyChanged(nameof(StatusSummary));
        }
    }


    private void OnResult(PortScanResult result)
    {
        Scanned++;
        if (result.IsOpen)
        {
            OpenPorts.Add(result);
            OpenCount++;
        }
        OnPropertyChanged(nameof(StatusSummary));
    }

    private void Stop() => _cts?.Cancel();


    private void Clear()
    {
        OpenPorts.Clear();
        ResetState();
    }


    private void ResetState()
    {
        ErrorMessage = null;
        Scanned = 0;
        Total = 0;
        OpenCount = 0;
        OnPropertyChanged(nameof(StatusSummary));
    }
}
