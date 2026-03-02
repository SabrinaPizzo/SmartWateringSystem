using System;
using System.Threading;
using System.Threading.Tasks;

namespace SmartWateringSystem.DataService
{
    public interface IDataService
    {
        int StationId { get; }
        string PlantName { get; set; }
        float CurrentHumidity { get; }
        float MinHumidityThreshold { get; set; }
        bool PumpState { get; }
        bool AutomaticSystemEnabled { get; set; }
        EConnectionStatus ConnectionStatus { get; }

        TimeSpan PumpActiveTimer { get; }

        event EventHandler ValuesRefreshed;
        event EventHandler ConnectionStatusChanged;

        Task<bool> StartPumpAsync(CancellationToken ct = default);
        Task<bool> StopPumpAsync(CancellationToken ct = default);
    }
}
