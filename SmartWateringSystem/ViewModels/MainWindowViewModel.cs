using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using SmartWateringSystem.DataService;
using SmartWateringSystem.Infrastructure;
using SmartWateringSystem.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SmartWateringSystem.ViewModels
{
    class MainWindowViewModel : BindableBase
    {
        private readonly IRegionManager _regionManager;

        public MainWindowViewModel(IRegionManager regionManager,
                                   IEnumerable<IDataService> dataServices)
        {
            _regionManager = regionManager;

            _regionManager.RegisterViewWithRegion(Regions.ContentRegion, typeof(PlantPage));
            _regionManager.RegisterViewWithRegion(Regions.LeftMenuRegion, typeof(LeftMenu));

            _regionManager.Regions.CollectionChanged += (s, e) =>
            {
                if (_regionManager.Regions.ContainsRegionWithName(Regions.ContentRegion))
                {
                    var parameters = new NavigationParameters();
                    parameters.Add("StationId", 0);
                    _regionManager.RequestNavigate(Regions.ContentRegion, "PlantPage", parameters);
                }
            };
        }
    }
}
