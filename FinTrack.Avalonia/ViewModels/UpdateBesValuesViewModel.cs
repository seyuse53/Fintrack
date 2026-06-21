using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinTrack.Core.Models;
using FinTrack.Data;
using System;
using System.Threading.Tasks;

namespace FinTrack.Avalonia.ViewModels;

public partial class UpdateBesValuesViewModel : ObservableObject
{
    private readonly AppDbContext _context;
    private readonly InvestmentAsset _asset;

    [ObservableProperty]
    private string _assetName;

    [ObservableProperty]
    private decimal? _customCurrentValue;

    [ObservableProperty]
    private decimal? _customStateContribution;

    [ObservableProperty]
    private DateTimeOffset? _besStartDate;

    [ObservableProperty]
    private DateTimeOffset? _besRetirementDate;

    [ObservableProperty]
    private string? _besContractNo;

    private int _retirementAge = 56;
    public int RetirementAge
    {
        get => _retirementAge;
        set
        {
            if (SetProperty(ref _retirementAge, value))
            {
                UpdateRetirementDate();
            }
        }
    }

    private DateTimeOffset? _participantBirthDate = new DateTimeOffset(new DateTime(1982, 1, 9));
    public DateTimeOffset? ParticipantBirthDate
    {
        get => _participantBirthDate;
        set
        {
            if (SetProperty(ref _participantBirthDate, value))
            {
                UpdateRetirementDate();
            }
        }
    }

    private void UpdateRetirementDate()
    {
        if (ParticipantBirthDate.HasValue)
        {
            BesRetirementDate = ParticipantBirthDate.Value.AddYears(RetirementAge);
        }
    }

    public event EventHandler? CloseRequested;

    public UpdateBesValuesViewModel(AppDbContext context, InvestmentAsset asset)
    {
        _context = context;
        _asset = asset;
        AssetName = asset.Symbol + " - " + asset.Name;
        CustomCurrentValue = asset.CustomCurrentValue;
        CustomStateContribution = asset.CustomStateContribution;
        
        if (asset.ParticipantBirthDate.HasValue) 
        {
            ParticipantBirthDate = new DateTimeOffset(asset.ParticipantBirthDate.Value);
        }
        else 
        {
            ParticipantBirthDate = new DateTimeOffset(new DateTime(1982, 1, 9));
        }

        if (asset.BesRetirementAge.HasValue)
        {
            RetirementAge = asset.BesRetirementAge.Value;
        }

        if (asset.BesStartDate.HasValue) BesStartDate = new DateTimeOffset(asset.BesStartDate.Value);
        
        if (asset.BesRetirementDate.HasValue) 
        {
            BesRetirementDate = new DateTimeOffset(asset.BesRetirementDate.Value);
        }
        else
        {
            // Set default based on ParticipantBirthDate and RetirementAge
            if (ParticipantBirthDate.HasValue)
            {
                BesRetirementDate = ParticipantBirthDate.Value.AddYears(RetirementAge);
            }
        }
        
        BesContractNo = asset.BesContractNo;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        _asset.CustomCurrentValue = CustomCurrentValue;
        _asset.CustomStateContribution = CustomStateContribution;
        _asset.BesStartDate = BesStartDate?.Date;
        _asset.BesRetirementDate = BesRetirementDate?.Date;
        _asset.BesContractNo = BesContractNo;
        _asset.ParticipantBirthDate = ParticipantBirthDate?.Date;
        _asset.BesRetirementAge = RetirementAge;
        
        _context.InvestmentAssets.Update(_asset);
        await _context.SaveChangesAsync();
        
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
