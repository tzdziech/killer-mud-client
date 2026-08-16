using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MudClient.App.Models;
using MudClient.App.ViewModels;

namespace MudClient.App.Views;

public partial class KilleropediaSkillsView : UserControl
{
    private KilleropediaViewModel? _viewModel;

    public KilleropediaSkillsView()
    {
        InitializeComponent();
        TreeCanvas.AbilitySelected += OnCanvasAbilitySelected;
    }

    private void OnResetViewClick(object? sender, RoutedEventArgs e) => TreeCanvas.ResetView();

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (_viewModel is not null)
        {
            _viewModel.FilteredAbilities.CollectionChanged -= OnFilteredAbilitiesChanged;
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = DataContext as KilleropediaViewModel;
        if (_viewModel is not null)
        {
            _viewModel.FilteredAbilities.CollectionChanged += OnFilteredAbilitiesChanged;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        SyncAbilities();
        SyncSelection();
    }

    private void OnFilteredAbilitiesChanged(object? sender, NotifyCollectionChangedEventArgs e) => SyncAbilities();

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(KilleropediaViewModel.SelectedAbility))
        {
            SyncSelection();
        }
    }

    private void SyncAbilities() =>
        TreeCanvas.SetAbilities(_viewModel?.FilteredAbilities.ToList() ?? []);

    private void SyncSelection() =>
        TreeCanvas.SelectedAbility = _viewModel?.SelectedAbility;

    private void OnCanvasAbilitySelected(AbilitySkillTreeEntry? ability)
    {
        if (_viewModel is not null)
        {
            _viewModel.SelectedAbility = ability;
        }
    }
}
