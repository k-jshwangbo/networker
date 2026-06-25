using Networker.Core;
using Networker.Models;
using Networker.Services.BannerGrab;
using System.Collections.ObjectModel;

namespace Networker.ViewModels;
public sealed class BannerGrabViewModel : ViewModelBase
{
    private readonly IBannerGrabService _service;
    private CancellationTokenSource? _cts;

    private string _host = "127.0.0.1";
    private string _portRangeText = "21,22,23,25,80,110,143,443,587,993,995,3306,5432,6379,8080,8443";
    private int _timeoutMs = 2000;
    private int _maxConcurreny = 100;
    private bool _useTls = true;
    private bool _sendHttpProbe = true;
    private string _customProbe = "";
    private bool _isRunning;
    private int _scanned;
    private int _total;
    private int _grabbedCount;
    private string? _errorMessage;

    public ObservableCollection<BannerResult> Banners { get; } = new();

    public string Host { get => _host; set => SetProperty(ref _host, value); }
    public string PortRangeText { get => _portRangeText; set => SetProperty(ref _portRangeText, value); }
    public int TimeoutMs { get => _timeoutMs; set => SetProperty(ref _timeoutMs, value); }
    public int MaxConcurrency { get => _maxConcurreny; set => SetProperty(ref _maxConcurreny, value); }
    public bool UseTls { get => _useTls; set => SetProperty(ref _useTls, value); }
    public bool SendHttpProbe { get => _sendHttpProbe; set => SetProperty(ref _sendHttpProbe, value); }
    public string CustomProbe { get => _customProbe; set => SetProperty(ref _customProbe, value); }

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
    public int GrabbedCount { get => _grabbedCount; private set => SetProperty(ref _grabbedCount, value); }
    public string? ErrorMessage { get => _errorMessage; private set => SetProperty(ref _errorMessage, value); }

    public string StatusSummary =>
        IsRunning ? $"Grabbing {Host}\u2026 {Scanned}/{Total}  (banners: {GrabbedCount})"
                  : Total > 0 ? $"Done. {Scanned}/{Total} probed, {GrabbedCount} banners"
                              : "Idle";

    public AsyncRelayCommand StartCommand { get; }
    public RelayCommand StopCommand { get; }
    public RelayCommand ClearCommand { get; }


    public BannerGrabViewModel(IBannerGrabService service)
    {
        _service = service;

        StartCommand = new AsyncRelayCommand(
            StartAsync,
            () => !IsRunning && !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(PortRangeText));
        StopCommand = new RelayCommand(Stop, () => IsRunning);
        ClearCommand = new RelayCommand(Clear, () => Banners.Count > 0 && !IsRunning);
    }

    private async Task StartAsync()
    {
        IReadOnlyCollection<int> ports;
        try
        {
            ports = PortRange.Parse(PortRangeText);
        }
        catch (FormatException ex)
        {
            ErrorMessage = ex.Message;
            return;
        }

        ResetState();
        Total = ports.Count;
        IsRunning = true;
        _cts = new CancellationTokenSource();

        var options = new BannerGrabOptions
        {
            UseTls = UseTls,
            SendHttpProbe = SendHttpProbe,
            CustomProbe = string.IsNullOrWhiteSpace(CustomProbe) ? null : CustomProbe
        };

        var progress = new Progress<BannerResult>(OnResult);

        try
        {
            await ParallelExecutor.RunAsync(
                ports,
                Math.Max(1, MaxConcurrency),
                (port, token) => _service.GrabAsync(
                    Host, port, TimeSpan.FromMilliseconds(TimeoutMs), options, token),
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

    private void OnResult(BannerResult result)
    {
        Scanned++;

        // Show every port we actually connected to: with a banner, empty, or a
        // post-connect error. Closed/filtered ports are just counted.
        if (result.Status is BannerStatus.Banner or BannerStatus.Empty or BannerStatus.Error)
            Banners.Add(result);

        if (result.Status == BannerStatus.Banner)
            GrabbedCount++;

        OnPropertyChanged(nameof(StatusSummary));
    }

    private void Stop() => _cts?.Cancel();

    private void Clear()
    {
        Banners.Clear();
        ResetState();
    }

    private void ResetState()
    {
        ErrorMessage = null;
        Scanned = 0;
        Total = 0;
        GrabbedCount = 0;
        OnPropertyChanged(nameof(StatusSummary));
    }
}
