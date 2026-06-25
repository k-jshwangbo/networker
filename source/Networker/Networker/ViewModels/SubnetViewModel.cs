using Networker.Core;
using System.Collections.ObjectModel;

namespace Networker.ViewModels;

/// <summary>A single label/value row shown in the subnet results table.</summary>
public sealed class SubnetField
{
    public string Label { get; init; } = "";
    public string Value { get; init; } = "";
}

public sealed class SubnetViewModel : ViewModelBase
{
    private string _input = "192.168.1.10/24";
    private string? _errorMessage;

    public ObservableCollection<SubnetField> Fields { get; } = new();

    public string Input
    {
        get => _input;
        set
        {
            if (SetProperty(ref _input, value))
                Recalculate();
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool HasResult => Fields.Count > 0;

    public SubnetViewModel()
    {
        Recalculate();
    }

    private void Recalculate()
    {
        Fields.Clear();

        try
        {
            var info = SubnetCalculator.Calculate(Input);

            Add("Address", info.Address.ToString());
            Add("Prefix length", $"/{info.PrefixLength}");
            Add("Subnet mask", info.SubnetMask.ToString());
            Add("Wildcard mask", info.WildcardMask.ToString());
            Add("Network address", info.NetworkAddress.ToString());
            Add("Broadcast address", info.BroadcastAddress.ToString());
            Add("Usable host range",
                info.UsableHosts > 0 ? $"{info.FirstHost} - {info.LastHost}" : "-");
            Add("Usable hosts", info.UsableHosts.ToString("N0"));
            Add("Total addresses", info.TotalAddresses.ToString("N0"));
            Add("Address class", info.AddressClass);
            Add("Scope", info.Scope);
            Add("Mask (binary)", info.MaskBinary);

            ErrorMessage = null;
        }
        catch (FormatException ex)
        {
            ErrorMessage = ex.Message;
        }

        OnPropertyChanged(nameof(HasResult));
    }

    private void Add(string label, string value)
        => Fields.Add(new SubnetField { Label = label, Value = value });
}
