using Prism.Mvvm;
using Prism.Regions;
using SmartWateringSystem.DataService;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartWateringSystem.ViewModels
{
    // TODO: Implement summary
    class SummaryPageViewModel :BindableBase, INavigationAware
    {
        public int InletPumpSpeed
        {
            get { return _inletPumpSpeed; }
            set { SetProperty(ref _inletPumpSpeed, value); }
        }
        private int _inletPumpSpeed;

        public int OutletSpeed
        {
            get { return _outletSpeed; }
            set { SetProperty(ref _outletSpeed, value); }
        }
        private int _outletSpeed;

        private readonly IDataService _dataService;

        public SummaryPageViewModel(IDataService DataService)
        {
            _dataService = DataService;
            this.PropertyChanged += OnPropertyChanged;
        }

        private async void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            
        }
    }
}
