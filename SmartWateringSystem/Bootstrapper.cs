using Autofac;
using Prism.Autofac;
using SmartWateringSystem.DataService;
using SmartWateringSystem.Infrastructure;
using SmartWateringSystem.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SmartWateringSystem
{
    class Bootstrapper : AutofacBootstrapper
    {
        public static readonly List<IDataService> DataServices = new List<IDataService>
        {
            new ModbusDataService(0, "COM3", slaveId: 1)
            // TODO: new slaves to add, final will support 4 slaves

            /// TESTING PURPOSES ONLY - REGISTERING DUMMY SERVICES 
            /*
            new DummyDataService(0);
            new DummyDataService(1);
            new DummyDataService(2);
            new DummyDataService(3);
            */
        };

        protected override DependencyObject CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void InitializeShell()
        {
            Application.Current.MainWindow.Show();
        }

        protected override void ConfigureContainerBuilder(ContainerBuilder builder)
        {
            base.ConfigureContainerBuilder(builder);

            foreach (var service in DataServices)
                builder.RegisterInstance(service).As<IDataService>();

            builder.RegisterTypeForNavigation<SummaryPage>("SummaryPage");
            builder.RegisterTypeForNavigation<PlantPage>("PlantPage");
        }
    }
}
