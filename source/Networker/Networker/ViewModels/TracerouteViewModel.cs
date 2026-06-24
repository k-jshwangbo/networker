using Networker.Core;
using Networker.Models;
using Networker.Services;
using System.Collections.ObjectModel;

namespace Networker.ViewModels;
public sealed class TracerouteViewModel : ViewModelBase
{
    private readonly ITracerouteService _traceService;
    private CancellationTokenSource? _cts;

    private string _host = "8.8.8.8";
    private int _maxHops = 30;
    private int _probePerHop = 3;
    private int _timeoutMs = 2000;
    private bool _resolveHostNames = true;
    private bool _isRunning;
    private int _currentHop;
    private string? _errorMessage;

    public ObservableCollection<TracerouteHop> Hops { get; } = new();
    public string Host { get => _host; set => SetProperty(ref _host, value); }
    public int MaxHops { get => _maxHops; set => SetProperty(ref _maxHops, value); }
    public int ProbePerHop { get => _probePerHop; set => SetProperty(ref _probePerHop, value); }
    public int TimeoutMs { get => _timeoutMs; set => SetProperty(ref _timeoutMs, value); }
    public bool ResolveHostNames { get => _resolveHostNames; set => SetProperty(ref _resolveHostNames, value); }
    public bool IsRunning
    {
        get => _isRunning;
        private set { if (SetProperty(ref _isRunning, value)) OnPropertyChanged(StatusSummary); }
    }

    public int CurrentHop { get => _currentHop; private set => SetProperty(ref _currentHop, value); }
    public string? ErrorMessage { get => _errorMessage; private set => SetProperty(ref _errorMessage, _errorMessage); }
    public string StatusSummary =>
        IsRunning ? $"Tracing to {Host}\u2026 hop {CurrentHop}"
                  : Hops.Count > 0 ? $"Done. {Hops.Count} hops"
                                   : "Idle";

    public AsyncRelayCommand StartCommand { get; }
    public RelayCommand StopCommand { get; }
    public RelayCommand ClearCommand { get; }

    public TracerouteViewModel(ITracerouteService tracerService)
    {
        _traceService = tracerService;

        StartCommand = new AsyncRelayCommand(
            StartAsync,
            () => !IsRunning && !string.IsNullOrWhiteSpace(Host));
        StopCommand = new RelayCommand(Stop, () => IsRunning);
        ClearCommand = new RelayCommand(Clear, () => Hops.Count > 0 && !IsRunning);
    }

    private async Task StartAsync()
    {
        ResetState();
        IsRunning = true;
        _cts= new CancellationTokenSource();

        var progress = new Progress<TracerouteHop>(OnHop);

        try
        {
            await _traceService.TraceAsync(
                Host, MaxHops, ProbePerHop, TimeSpan.FromMilliseconds(TimeoutMs),
                ResolveHostNames, progress, _cts.Token);
        }
        catch (FormatException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (OperationCanceledException)
        {
            // stop pressed -> keep hops found so far.
        }
        finally
        {
            IsRunning = false;
            _cts.Dispose();
            _cts = null;
            OnPropertyChanged(StatusSummary);
        }
    }


    private void OnHop(TracerouteHop hop)
    {
        Hops.Add(hop);
        CurrentHop = hop.Hop;
        OnPropertyChanged(nameof(StatusSummary));
    }


    private void Stop() => _cts?.Cancel();


    private void Clear()
    {
        Hops.Clear();
        ResetState();
    }


    private void ResetState()
    {
        ErrorMessage = null;
        CurrentHop = 0;
        OnPropertyChanged(nameof(StatusSummary));
    }
}
