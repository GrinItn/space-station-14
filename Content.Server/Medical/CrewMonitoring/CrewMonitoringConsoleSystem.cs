using System.Linq;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Content.Shared.PowerCell;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Medical.CrewMonitoring;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Pinpointer;
using Robust.Server.GameObjects;

namespace Content.Server.Medical.CrewMonitoring;

public sealed class CrewMonitoringConsoleSystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, BoundUIOpenedEvent>(OnUIOpened);
    }

    private void OnRemove(EntityUid uid, CrewMonitoringConsoleComponent component, ComponentRemove args)
    {
        component.ConnectedSensors.Clear();
    }

    private void OnPacketReceived(EntityUid uid, CrewMonitoringConsoleComponent component, DeviceNetworkPacketEvent args)
    {
        var payload = args.Data;

        // Check command
        if (!payload.TryGetValue(DeviceNetworkConstants.Command, out string? command))
            return;

        if (command != DeviceNetworkConstants.CmdUpdatedState)
            return;

        if (!payload.TryGetValue(SuitSensorConstants.NET_STATUS_COLLECTION, out Dictionary<string, SuitSensorStatus>? sensorStatus))
            return;

        //SS220-new-feature begin
        if (HasComp<Content.Shared.SS220.Medical.BlueShieldMonitorComponent>(uid))
        {
            sensorStatus = FilterBlueShieldSensors(sensorStatus);
        }
        //SS220-new-feature end

        component.ConnectedSensors = sensorStatus;
        UpdateUserInterface(uid, component);
    }

    private void OnUIOpened(EntityUid uid, CrewMonitoringConsoleComponent component, BoundUIOpenedEvent args)
    {
        if (!_cell.TryUseActivatableCharge(uid))
            return;

        UpdateUserInterface(uid, component);
    }

    private void UpdateUserInterface(EntityUid uid, CrewMonitoringConsoleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!_uiSystem.IsUiOpen(uid, CrewMonitoringUIKey.Key))
            return;

        // The grid must have a NavMapComponent to visualize the map in the UI
        var xform = Transform(uid);

        if (xform.GridUid != null)
            EnsureComp<NavMapComponent>(xform.GridUid.Value);

        // Update all sensors info
        var allSensors = component.ConnectedSensors.Values.ToList();
        _uiSystem.SetUiState(uid, CrewMonitoringUIKey.Key, new CrewMonitoringState(allSensors));
    }

    //SS220-new-feature begin
    private static readonly HashSet<string> HighClearanceProtos = new()
    {
        "captain",
        "headofpersonnel",
        "chiefengineer",
        "chiefmedicalofficer",
        "headofsecurity",
        "quartermaster",
        "researchdirector",
        "blueshield",
        "nanotrasenrepresentative",
    };

    private Dictionary<string, SuitSensorStatus> FilterBlueShieldSensors(Dictionary<string, SuitSensorStatus> sensors)
    {
        var filtered = new Dictionary<string, SuitSensorStatus>();

        foreach (var (address, sensor) in sensors)
        {
            var protoLower = sensor.JobPrototypeId?.ToLower().Trim() ?? string.Empty;

            // Unknown persons (no ID card or undefined job) — always visible to Blue Shield.
            if (string.IsNullOrWhiteSpace(protoLower) || HighClearanceProtos.Contains(protoLower))
            {
                filtered.Add(address, sensor);
            }
        }

        return filtered;
    }
    //SS220-new-feature end
}
