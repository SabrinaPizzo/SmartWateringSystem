using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using SmartWateringSystem.DataService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SmartWateringSystem.ViewModels
{
    class PlantPageViewModel : BindableBase, INavigationAware
    {
        public enum EPumpCommandState
        {
            Idle,       // Pump is confirmed off
            Pending,    // Command sent, waiting for Arduino to confirm
            Running,    // Pump is confirmed on
            Fault       // We commanded start, but Arduino still reports off after timeout
        }

        public int StationId
        {
            get { return _stationId; }
            set { SetProperty(ref _stationId, value); }
        }
        private int _stationId;

        public float CurrentHumidity
        {
            get { return _currentHumidity; }
            set { SetProperty(ref _currentHumidity, value); }
        }
        private float _currentHumidity;

        public string PlantName
        {
            get { return _plantName; }
            set { SetProperty(ref _plantName, value); }
        }
        private string _plantName;

        public float MinHumidityThreshold
        {
            get { return _minHumidityThreshold; }
            set
            {
                if (SetProperty(ref _minHumidityThreshold, value))
                {
                    if (_dataService != null)
                        _dataService.MinHumidityThreshold = value;
                }
            }
        }
        private float _minHumidityThreshold;

        public bool PumpState
        {
            get { return _pumpState; }
            set { SetProperty(ref _pumpState, value); }
        }
        private bool _pumpState;

        public bool AutomaticSystemEnabled
        {
            get { return _automaticSystemEnabled; }
            set 
            { 
                if (SetProperty(ref _automaticSystemEnabled, value))
                {
                    if (_dataService != null)
                        _dataService.AutomaticSystemEnabled = value;
                }
            }
        }
        private bool _automaticSystemEnabled;

        public TimeSpan PumpActiveTimer
        {
            get { return _pumpActiveTimer; }
            set { SetProperty(ref _pumpActiveTimer, value); }
        }
        private TimeSpan _pumpActiveTimer;

        public EPumpCommandState PumpCommandState
        {
            get => _pumpCommandState;
            set => SetProperty(ref _pumpCommandState, value);
        }

        private bool _awaitingConfirmation;
        private EPumpCommandState _pumpCommandState = EPumpCommandState.Idle;

        public bool CanStartPump => PumpCommandState == EPumpCommandState.Idle;

        public EConnectionStatus ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }
        private EConnectionStatus _connectionStatus = EConnectionStatus.Disconnected;
        public bool IsConnected => ConnectionStatus == EConnectionStatus.Connected;

        public DelegateCommand StartPumpCommand { get; private set; }

        public DelegateCommand StopPumpCommand { get; private set; }

        IDataService _dataService;

        private CancellationTokenSource _confirmationTimeout;

        private readonly IEnumerable<IDataService> _allDataServices;

        public PlantPageViewModel(IEnumerable<IDataService> dataServices)
        {
            _allDataServices = dataServices;

            StartPumpCommand = new DelegateCommand(async () => await StartPumpExecute(), () => _dataService != null && CanStartPump && IsConnected);
            StopPumpCommand = new DelegateCommand(async () => await StopPumpExecute(), () => _dataService != null && IsConnected);
        }

        private void OnPlantServiceValuesRefreshed(object sender, EventArgs e)
        {
            StationId = _dataService.StationId;
            PlantName = _dataService.PlantName;
            CurrentHumidity = _dataService.CurrentHumidity;
            PumpActiveTimer = _dataService.PumpActiveTimer;
            PumpState = _dataService.PumpState;

            if (_awaitingConfirmation && PumpState == true)
            {
                PumpCommandState = EPumpCommandState.Running;
                _awaitingConfirmation = false;
            }
            else if (_awaitingConfirmation && PumpState == false)
            {
                PumpCommandState = EPumpCommandState.Idle;
                _awaitingConfirmation = false;
            }
            else if (_awaitingConfirmation && _confirmationTimeout.IsCancellationRequested)
            {
                PumpCommandState = EPumpCommandState.Fault;
                _awaitingConfirmation = false;
            }

            StartPumpCommand.RaiseCanExecuteChanged();
            StopPumpCommand.RaiseCanExecuteChanged();
        }       

        private async Task StartPumpExecute()
        {
            if (AutomaticSystemEnabled)
            {
                return;
            }

            PumpCommandState = EPumpCommandState.Pending;
            StartPumpCommand.RaiseCanExecuteChanged();

            bool writeSucceeded = await _dataService.StartPumpAsync();

            if (!writeSucceeded)
            {
                PumpCommandState = EPumpCommandState.Fault;
                return;
            }

            _confirmationTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            _awaitingConfirmation = true;
        }

        private async Task StopPumpExecute()
        {
            PumpCommandState = EPumpCommandState.Pending;
            StopPumpCommand.RaiseCanExecuteChanged();

            bool writeSucceeded = await _dataService.StopPumpAsync();

            if (!writeSucceeded)
            {
                PumpCommandState = EPumpCommandState.Fault;
                return;
            }

            _confirmationTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            _awaitingConfirmation = true;
        }

        private void OnConnectionStatusChanged(object sender, EventArgs e)
        {
            ConnectionStatus = _dataService.ConnectionStatus;
            StartPumpCommand.RaiseCanExecuteChanged();
            StopPumpCommand.RaiseCanExecuteChanged();
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            int stationId = (int)navigationContext.Parameters["StationId"];

            if (_dataService != null)
            {
                _dataService.ValuesRefreshed -= OnPlantServiceValuesRefreshed;
                _dataService.ConnectionStatusChanged -= OnConnectionStatusChanged;
            }

            _dataService = _allDataServices.First(ds => ds.StationId == stationId);
            _dataService.ValuesRefreshed += OnPlantServiceValuesRefreshed;
            _dataService.ConnectionStatusChanged += OnConnectionStatusChanged;

            OnPlantServiceValuesRefreshed(null, null);

            _minHumidityThreshold = _dataService.MinHumidityThreshold;
            OnPropertyChanged(nameof(MinHumidityThreshold));

            _automaticSystemEnabled = _dataService.AutomaticSystemEnabled;
            OnPropertyChanged(nameof(AutomaticSystemEnabled));

            ConnectionStatus = _dataService.ConnectionStatus;
            StartPumpCommand?.RaiseCanExecuteChanged();
            StopPumpCommand?.RaiseCanExecuteChanged();
        }
        public bool IsNavigationTarget(NavigationContext navigationContext) => true;
        public void OnNavigatedFrom(NavigationContext navigationContext) { }
    }
}
