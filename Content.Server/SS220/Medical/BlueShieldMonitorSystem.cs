// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Medical.SuitSensor;
using Content.Shared.SS220.Medical;
using Robust.Shared.GameObjects;

namespace Content.Server.SS220.Medical;

public sealed class BlueShieldMonitorSystem : EntitySystem
{
    /// <summary>
    ///     Prepares suit sensor statuses for a crew monitoring console.
    ///     Blue Shield monitors receive a filtered list: high clearance personnel,
    ///     agents disguised as heads (chameleon) and unidentified crew without ID cards.
    ///     Regular consoles receive the full list with the SS220-only fields stripped,
    ///     so modified clients cannot use them to reveal disguised agents.
    /// </summary>
    public Dictionary<string, SuitSensorStatus> ProcessSensorStatus(EntityUid uid, Dictionary<string, SuitSensorStatus> sensors)
    {
        if (TryComp<BlueShieldMonitorComponent>(uid, out var blueShieldMonitor))
        {
            return FilterBlueShieldSensors(
                sensors,
                blueShieldMonitor.HighClearanceProtos,
                blueShieldMonitor.HighClearanceIcons,
                blueShieldMonitor.UnknownJobIcons);
        }

        return StripSensitiveSensorData(sensors);
    }

    private Dictionary<string, SuitSensorStatus> FilterBlueShieldSensors(
        Dictionary<string, SuitSensorStatus> sensors,
        HashSet<string> highClearanceProtos,
        HashSet<string> highClearanceIcons,
        HashSet<string> unknownJobIcons)
    {
        var filtered = new Dictionary<string, SuitSensorStatus>();

        foreach (var (address, sensor) in sensors)
        {
            // ToLowerInvariant: job prototype IDs are ASCII and must not depend on the server locale.
            var protoLower = sensor.JobPrototypeId.ToLowerInvariant().Trim();

            // Show sensor if any of the following conditions are met:
            // 1. Genuine high-ranking personnel (real ID card with JobPrototypeId in highClearanceProtos).
            // 2. Agent disguised as a head (IsAgentIdCard = true AND JobIcon matches a high clearance icon).
            // 3. Unidentified person — no ID card at all ("JobIconNoId") or a blank ID card ("JobIconUnknown").
            bool isHighClearance = highClearanceProtos.Contains(protoLower);
            bool isDisguisedAgent = sensor.IsAgentIdCard && highClearanceIcons.Contains(sensor.JobIcon);
            bool isUnknown = unknownJobIcons.Contains(sensor.JobIcon);

            if (isHighClearance || isDisguisedAgent || isUnknown)
            {
                filtered.Add(address, sensor);
            }
            // Everyone else is hidden:
            // - Agents disguised as regular crew (IsAgentIdCard = true but JobIcon is not high clearance).
            // - Regular crew members (no JobPrototypeId in highClearanceProtos, not an Agent ID card).
        }

        return filtered;
    }

    private static Dictionary<string, SuitSensorStatus> StripSensitiveSensorData(Dictionary<string, SuitSensorStatus> sensors)
    {
        var stripped = new Dictionary<string, SuitSensorStatus>(sensors.Count);

        foreach (var (address, sensor) in sensors)
        {
            stripped[address] = new SuitSensorStatus(
                sensor.OwnerUid,
                sensor.SuitSensorUid,
                sensor.Name,
                sensor.Job,
                sensor.JobIcon,
                sensor.JobDepartments,
                string.Empty,
                isAgentIdCard: false)
            {
                Timestamp = sensor.Timestamp,
                IsAlive = sensor.IsAlive,
                TotalDamage = sensor.TotalDamage,
                TotalDamageThreshold = sensor.TotalDamageThreshold,
                Coordinates = sensor.Coordinates,
            };
        }

        return stripped;
    }
}
