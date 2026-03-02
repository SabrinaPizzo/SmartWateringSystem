using Prism.Commands;
using Prism.Regions;
using SmartWateringSystem.DataService;
using SmartWateringSystem.Infrastructure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SmartWateringSystem.ViewModels
{
    public class PlantMenuItemViewModel
    {
        public string PlantName { get; set; }
        public int StationId { get; set; }
    }

    class LeftMenuViewModel
    {
        public ObservableCollection<PlantMenuItemViewModel> Plants { get; }
        public ICommand NavigateToPlantPageCommand { get; private set; }
        public ICommand NavigateToSummaryPageCommand { get; private set; }

        private readonly IRegionManager _regionManager;

        public LeftMenuViewModel(IRegionManager regionManager,
                                 IEnumerable<IDataService> dataServices)
        {
            _regionManager = regionManager;

            Plants = new ObservableCollection<PlantMenuItemViewModel>(
                dataServices.Select(ds => new PlantMenuItemViewModel
                {
                    PlantName = ds.PlantName,
                    StationId = ds.StationId
                })
            );

            NavigateToPlantPageCommand = new DelegateCommand<PlantMenuItemViewModel>(item => NavigateTo("PlantPage", item.StationId));
            NavigateToSummaryPageCommand = new DelegateCommand(() => NavigateTo("SummaryPage", null));
        }
        private void NavigateTo(string view, int? stationId)
        {
            var parameters = new NavigationParameters();
            if (stationId.HasValue)
                parameters.Add("StationId", stationId.Value);

            _regionManager.RequestNavigate(Regions.ContentRegion, view, parameters);
        }
    }
}
