using Networker.Core;
using Networker.Models;
using Networker.Services;
using System.Collections.ObjectModel;

namespace Networker.ViewModels;
public sealed class PingViewModel : ViewModelBase
{
    private readonly IPingService _pingService;
    private CancellationTokenSource? _cts;

    private string _host = "8.8.8.8";
    private int _intervalMs = 1000;
    private int _timeoutMs = 2000;
    private bool _isRunning;

    // running totals for the status bar
    private int _sent;
    private int _received;
    private long _minRtt = long.MaxValue;
    private long _maxRtt;
    private long _totalRtt;


    public ObservableCollection<PingResult> Results { get; } = new();
    public string Host { get => _host; set => SetProperty(ref _host, value); }
    public int IntervalMs { get => _intervalMs; set => SetProperty(ref _intervalMs, value); }
    public int TimeoutMs { get => _timeoutMs; set => SetProperty(ref _timeoutMs, value); }
    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
                OnPropertyChanged(nameof(StatusSummary));
        }
    }
    public int Sent { get => _sent; private set => SetProperty(ref _sent, value); }
    public int Received { get => _received; private set=> SetProperty(ref _received, value); }
    public int Lost => Sent - Received;
    public double LossPercent => Sent == 0 ? 0 : (double)Lost / Sent * 100;
    public long MinRtt => Received == 0 ? 0 : _minRtt;
    public long MaxRtt => _maxRtt;
    public double AvgRtt => Received == 0 ? 0 : (double)_totalRtt / Received;
    public string StatusSummary => IsRunning ? $"Pinging {Host}\u2026" : "Idle";
    public AsyncRelayCommand StartCommand { get; }
    public RelayCommand StopCommand { get; }
    public RelayCommand ClearCommand { get; }



    public PingViewModel(IPingService pingService)
    {
        _pingService = pingService;

        StartCommand = new AsyncRelayCommand(StartAsync, () => !IsRunning && !string.IsNullOrEmpty(Host));
        StopCommand = new RelayCommand(Stop, () => IsRunning);
        ClearCommand = new RelayCommand(Clear, () => Results.Count > 0 && !IsRunning);
    }


    private async Task StartAsync()
    {
        ResetStatus();
        IsRunning = true;
        _cts = new CancellationTokenSource();

        var progress = new Progress<PingResult>(OnResult);

        try
        {
            await _pingService.PingContinuousAsync(
                Host,
                TimeSpan.FromMilliseconds(IntervalMs),
                TimeSpan.FromMilliseconds(TimeoutMs),
                bufferSize: 32,
                progress,
                _cts.Token);
        }
        finally
        {
            IsRunning = false;
            _cts.Dispose();
            _cts = null;
        }
    }


    private void OnResult(PingResult result)
    {
        Results.Add(result);
        Sent++;

        if (result.IsSuccess)
        {
            Received++;
            _totalRtt += result.RoundtripMs;
            if (result.RoundtripMs < _minRtt) _minRtt = result.RoundtripMs;
            if (result.RoundtripMs > _maxRtt) _maxRtt = result.RoundtripMs;
        }

        RaiseStats();
    }


    private void Stop() => _cts?.Cancel();


    private void Clear()
    {
        Results.Clear();
        ResetStatus();
    }


    private void ResetStatus()
    {
        Sent = 0;
        Received = 0;
        _minRtt = long.MaxValue;
        _maxRtt = 0;
        _totalRtt = 0;
        RaiseStats();
    }


    private void RaiseStats()
    {
        OnPropertyChanged(nameof(Lost));
        OnPropertyChanged(nameof(LossPercent));
        OnPropertyChanged(nameof(MinRtt));
        OnPropertyChanged(nameof(MaxRtt));
        OnPropertyChanged(nameof(AvgRtt));
    }


}