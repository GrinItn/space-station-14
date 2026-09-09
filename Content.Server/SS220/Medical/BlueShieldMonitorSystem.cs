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
            return FilterBlueShieldSensors(uid, sensors);
        }

        return StripSensitiveSensorData(sensors);
    }

    private Dictionary<string, SuitSensorStatus> FilterBlueShieldSensors(
        EntityUid monitorUid,
        Dictionary<string, SuitSensorStatus> sensors)
    {
        if (!TryComp<BlueShieldMonitorComponent>(monitorUid, out var comp))
            return new Dictionary<string, SuitSensorStatus>();

        var filtered = new Dictionary<string, SuitSensorStatus>();

        foreach (var (address, sensor) in sensors)
        {
            var protoLower = sensor.JobPrototypeId.ToLowerInvariant().Trim();

            if (ShouldShowOnBlueShield(comp, sensor, protoLower))
            {
                filtered.Add(address, sensor);
            }
        }

        return filtered;

        // Local function keeps the filtering logic encapsulated and readable
        bool ShouldShowOnBlueShield(BlueShieldMonitorComponent c, SuitSensorStatus s, string jobProto)
        {
            // 1. Genuine high-ranking personnel
            if (c.HighClearanceProtos.Contains(jobProto))
                return true;

            // 2. Agent disguised as a head (chameleon)
            if (s.IsAgentIdCard && c.HighClearanceIcons.Contains(s.JobIcon))
                return true;

            // 3. Unidentified person (no ID or blank ID card)
            return c.UnknownJobIcons.Contains(s.JobIcon);
        }
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
