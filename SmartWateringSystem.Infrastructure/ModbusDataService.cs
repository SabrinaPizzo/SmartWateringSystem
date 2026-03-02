using Modbus.Device;
using SmartWateringSystem.DataService;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SmartWateringSystem.Infrastructure
{
    public class ModbusDataService : IDataService
    {
        #region DataService Implementation
        public int StationId { get; }

        public string PlantName
        {
            get => _plantName;
            set => _plantName = value;
        }
        private string _plantName;

        public float CurrentHumidity { get; private set; }

        public float MinHumidityThreshold
        {
            get => _minHumidityThreshold;
            set
            {
                _minHumidityThreshold = value < 0f ? 0f : value;
            }
        }
        private float _minHumidityThreshold = 475f;

        public bool PumpState { get; private set; }
        public bool AutomaticSystemEnabled { get; set; } = true;
        public TimeSpan PumpActiveTimer { get; private set; }

        public EConnectionStatus ConnectionStatus { get; private set; }
            = EConnectionStatus.Disconnected;

        public event EventHandler ValuesRefreshed;
        public event EventHandler ConnectionStatusChanged;
        #endregion

        #region Modbus Internals
        private readonly string _portName;   // Arduino COM3
        private readonly byte _slaveId;      // Arduino slave Id

        private SerialPort _serialPort;
        private IModbusMaster _modbusMaster;
        private Timer _pollingTimer;
        private DateTime _pumpStartedAt;
        private int _consecutiveFailures = 0;
        private const int MaxFailuresBeforeLost = 3;

        private const int CoilPumpCommand = 0;
        private const int InputRegHumidity = 0;
        #endregion

        #region Constructor
        public ModbusDataService(int stationId, string portName, byte slaveId = 1)
        {
            StationId = stationId;
            _portName = portName;
            _slaveId = slaveId;

            _plantName = $"Plant {stationId + 1}"; //TODO: Let the user edit the name

            Task.Run(ConnectAsync);
        }
        #endregion

        #region Async Connection
        private async Task ConnectAsync()
        {
            SetConnectionStatus(EConnectionStatus.Connecting);

            try
            {
                // Open the serial port
                _serialPort = new SerialPort(_portName)
                {
                    BaudRate = 9600,
                    DataBits = 8,
                    Parity = Parity.None,
                    StopBits = StopBits.One
                };
                _serialPort.Open();

                // Create the Modbus master on top of the serial port
                _modbusMaster = ModbusSerialMaster.CreateRtu(_serialPort);
                _modbusMaster.Transport.WriteTimeout = 1000;
                _modbusMaster.Transport.ReadTimeout = 1000;

                SetConnectionStatus(EConnectionStatus.Connected);
                _consecutiveFailures = 0;

                // Start polling every 500ms
                _pollingTimer = new Timer(PollArduino, null, 0, 500);
            }
            catch (Exception ex)
            {
                SetConnectionStatus(EConnectionStatus.Disconnected);
            }
        }
        #endregion

        #region Polling Arduino
        private void PollArduino(object state)
        {
            try
            {
                // Read 1 input register starting at address 0 (humidity raw value)
                ushort[] inputRegs = _modbusMaster.ReadInputRegisters(_slaveId, InputRegHumidity, 1);
                float rawHumidity = inputRegs[0];

                // TODO: instead of raw values implement % or calibrated values
                CurrentHumidity = rawHumidity;

                // Read coil 0 for actual pump state
                bool[] coils = _modbusMaster.ReadCoils(_slaveId, CoilPumpCommand, 1);
                bool newPumpState = coils[0];

                if (newPumpState && !PumpState)
                    _pumpStartedAt = DateTime.UtcNow;

                PumpState = newPumpState;

                if (PumpState)
                    PumpActiveTimer = DateTime.UtcNow - _pumpStartedAt;
                else
                    PumpActiveTimer = TimeSpan.Zero;
                
                // Start/stop pump automatically
                if (AutomaticSystemEnabled)
                {
                    if (PumpState && CurrentHumidity < MinHumidityThreshold)
                    {
                        _modbusMaster.WriteSingleCoil(_slaveId, 0, false);
                    }
                    else if (!PumpState && CurrentHumidity > MinHumidityThreshold)
                    {
                        _modbusMaster.WriteSingleCoil(_slaveId, 0, true);
                    }
                }

                _consecutiveFailures = 0;
                if (ConnectionStatus != EConnectionStatus.Connected)
                    SetConnectionStatus(EConnectionStatus.Connected);

                ValuesRefreshed?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception)
            {
                _consecutiveFailures++;
                if (_consecutiveFailures >= MaxFailuresBeforeLost)
                    SetConnectionStatus(EConnectionStatus.Lost);
            }
        }
        #endregion

        #region Pump Commands
        public async Task<bool> StartPumpAsync(CancellationToken ct = default)
        {
            return await WriteCoilAsync(CoilPumpCommand, true, ct);
        }

        public async Task<bool> StopPumpAsync(CancellationToken ct = default)
        {
            if (ConnectionStatus != EConnectionStatus.Connected)
            {
                return false;
            }

            return await WriteCoilAsync(CoilPumpCommand, false, ct);
        }

        private async Task<bool> WriteCoilAsync(int address, bool value, CancellationToken ct)
        {
            try
            {
                await Task.Run(() =>
                    _modbusMaster.WriteSingleCoil(_slaveId, (ushort)address, value), ct);
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        private void SetConnectionStatus(EConnectionStatus status)
        {
            ConnectionStatus = status;
            ConnectionStatusChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

