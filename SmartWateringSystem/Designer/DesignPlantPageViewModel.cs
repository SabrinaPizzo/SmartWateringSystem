using SmartWateringSystem.DataService;
using SmartWateringSystem.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartWateringSystem.Designer
{
    class DesignPlantPageViewModel : PlantPageViewModel
    {
        public DesignPlantPageViewModel() : base(new[] { new DummyDataService(0) })
        {
        }
    }
}
