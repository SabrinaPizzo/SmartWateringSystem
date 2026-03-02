using System;
using System.Threading;
using System.Threading.Tasks;

namespace SmartWateringSystem.DataService
{
    /// <summary>
    /// Simulates a real Arduino station for UI development and testing.
    /// Behavior:
    ///   - Humidity slowly decreases over time (plant drying out)
    ///   - Running pump increases humidity
    ///   - Auto mode triggers pump when humidity drops below threshold
    ///   - Small random fluctuations simulate a real sensor
    ///   - Occasional command failures let you test fault states in the UI
    /// </summary>
    public class DummyDataService : IDataService
    {
        // -------------------------------------------------------------------------
        // IDataService properties
        // -------------------------------------------------------------------------

        public int StationId { get; }

        public string PlantName
        {
            get => _plantName;
            set
            {
                _plantName = value;
                // In the real service this would write to a local config file,
                // since Arduino has no concept of a human-readable name.
            }
        }
        private string _plantName;

        public float CurrentHumidity { get; private set; }

        public float MinHumidityThreshold
        {
            get => _minHumidityThreshold;
            set
            {
                _minHumidityThreshold = value < 0f ? 0f : value > 100f ? 100f : value;
                // In the real service this would write to a Modbus Holding Register.
            }
        }
        private float _minHumidityThreshold;

        public bool PumpState { get; private set; }

        public bool AutomaticSystemEnabled { get; set; } = true;

        public TimeSpan PumpActiveTimer { get; private set; }

        public EConnectionStatus ConnectionStatus { get; private set; }
            = EConnectionStatus.Disconnected;

        public event EventHandler ValuesRefreshed;
        public event EventHandler ConnectionStatusChanged;

        // -------------------------------------------------------------------------
        // Simulation internals
        // -------------------------------------------------------------------------

        // How fast humidity drops per second when idle (no pump)
        private const float HumidityDecayPerSecond = 0.3f;

        // How fast humidity rises per second when pump is running
        private const float HumidityIncreasePerSecond = 2.0f;

        // Max random noise applied each tick (simulates sensor jitter)
        private const float SensorNoise = 0.4f;

        // Chance (0.0 - 1.0) that any given command write "fails" (simulates comms glitch)
        // Set to 0f to disable faults entirely during early development
        private const float CommandFailureChance = 0.15f;

        private static readonly Random _random = new Random();
        private Timer _simulationTimer;
        private DateTime _pumpStartedAt;

        // -------------------------------------------------------------------------
        // Default station names — mirrors what you'd store in a local config file
        // -------------------------------------------------------------------------

        private static readonly string[] DefaultNames =
        {
            "Monstera",
            "Avocado",
            "Tronchetto",
            "Fiore"
        };

        // -------------------------------------------------------------------------
        // Constructor
        // -------------------------------------------------------------------------

        public DummyDataService(int stationId)
        {
            StationId = stationId;

            // Pick the default name, guard against out-of-range IDs
            _plantName = stationId >= 0 && stationId < DefaultNames.Length
                ? DefaultNames[stationId]
                : $"Plant {stationId + 1}";

            // Start each station at a different humidity so the summary page
            // looks varied and interesting during development
            CurrentHumidity = 55f + stationId * 8f;
            _minHumidityThreshold = 30f;

            // Simulate the connection handshake before starting the poll loop
            Task.Run(SimulateConnectionAsync);
        }

        // -------------------------------------------------------------------------
        // Connection simulation
        // -------------------------------------------------------------------------

        private async Task SimulateConnectionAsync()
        {
            await Task.Delay(500 + StationId * 400); // stagger stations slightly

            SetConnectionStatus(EConnectionStatus.Connecting);

            await Task.Delay(1500); // pretend Modbus handshake takes time

            SetConnectionStatus(EConnectionStatus.Connected);

            // Start the main simulation loop only once connected
            // Tick every 500 ms — matches typical HMI poll rate
            _simulationTimer = new Timer(SimulationTick, null,
                TimeSpan.Zero, TimeSpan.FromMilliseconds(500));
        }

        private void SetConnectionStatus(EConnectionStatus status)
        {
            ConnectionStatus = status;
            ConnectionStatusChanged?.Invoke(this, EventArgs.Empty);
        }

        // -------------------------------------------------------------------------
        // Simulation tick — runs every 500 ms, equivalent to one Modbus poll cycle
        // -------------------------------------------------------------------------

        private void SimulationTick(object state)
        {
            const float deltaTime = 0.5f; // seconds per tick

            // --- Automatic mode logic ---
            // Arduino's sketch would do this internally; here we mirror that logic.
            if (AutomaticSystemEnabled && !PumpState
                && CurrentHumidity < MinHumidityThreshold)
            {
                PumpState = true;
                _pumpStartedAt = DateTime.UtcNow;
            }

            // Auto-stop: turn pump off once humidity is well above threshold
            if (AutomaticSystemEnabled && PumpState
                && CurrentHumidity > MinHumidityThreshold + 20f)
            {
                PumpState = false;
                PumpActiveTimer = TimeSpan.Zero;
            }

            // --- Update humidity ---
            float noise = ((float)_random.NextDouble() - 0.5f) * SensorNoise;

            if (PumpState)
            {
                CurrentHumidity += HumidityIncreasePerSecond * deltaTime + noise;
                PumpActiveTimer = DateTime.UtcNow - _pumpStartedAt;
            }
            else
            {
                CurrentHumidity -= HumidityDecayPerSecond * deltaTime + noise;
            }

            CurrentHumidity = CurrentHumidity < 0f ? 0f : CurrentHumidity > 100f ? 100f : CurrentHumidity;

            // Notify the ViewModel that fresh data is available
            ValuesRefreshed?.Invoke(this, EventArgs.Empty);
        }

        // -------------------------------------------------------------------------
        // Commands — IDataService
        // -------------------------------------------------------------------------

        public async Task<bool> StartPumpAsync(CancellationToken ct = default)
        {
            // Simulate the time a Modbus write takes over serial
            await Task.Delay(200, ct);

            if (ShouldSimulateFault())
                return false; // tell ViewModel the write failed

            PumpState = true;
            _pumpStartedAt = DateTime.UtcNow;
            return true;
        }

        public async Task<bool> StopPumpAsync(CancellationToken ct = default)
        {
            await Task.Delay(200, ct);

            if (ShouldSimulateFault())
                return false;

            PumpState = false;
            PumpActiveTimer = TimeSpan.Zero;
            return true;
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns true occasionally to simulate a Modbus communication glitch.
        /// This lets you verify that the ViewModel's Fault state and UI error
        /// indicators work correctly without needing real hardware.
        /// Set CommandFailureChance = 0f to disable.
        /// </summary>
        private static bool ShouldSimulateFault()
        {
            return _random.NextDouble() < CommandFailureChance;
        }
    }
}