// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Medical.SuitSensor;
using Content.Shared.SS220.Medical;
using Robust.Shared.GameObjects;

namespace Content.Server.SS220.Medical;

public sealed class BlueShieldMonitorSystem : EntitySystem
{
    // Job prototype IDs considered high clearance. Always shown on the Blue Shield monitor.
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

    // Job icon prototype IDs for high clearance roles. Used to detect agents using chameleon to disguise as heads.
    private static readonly HashSet<string> HighClearanceIcons = new()
    {
        "JobIconCaptain",
        "JobIconHeadOfPersonnel",
        "JobIconChiefMedicalOfficer",
        "JobIconHeadOfSecurity",
        "JobIconQuarterMaster",
        "JobIconResearchDirector",
        "JobIconBlueShield",
        "JobIconNanotrasen",
        "JobIconChiefEngineer",
    };

    /// <summary>
    ///     Prepares suit sensor statuses for a crew monitoring console.
    ///     Blue Shield monitors receive a filtered list: high clearance personnel,
    ///     agents disguised as heads (chameleon) and unidentified crew without ID cards.
    ///     Regular consoles receive the full list with the SS220-only fields stripped,
    ///     so modified clients cannot use them to reveal disguised agents.
    /// </summary>
    public Dictionary<string, SuitSensorStatus> ProcessSensorStatus(EntityUid uid, Dictionary<string, SuitSensorStatus> sensors)
    {
        if (HasComp<BlueShieldMonitorComponent>(uid))
            return FilterBlueShieldSensors(sensors);

        return StripSensitiveSensorData(sensors);
    }

    private Dictionary<string, SuitSensorStatus> FilterBlueShieldSensors(Dictionary<string, SuitSensorStatus> sensors)
    {
        var filtered = new Dictionary<string, SuitSensorStatus>();

        foreach (var (address, sensor) in sensors)
        {
            var protoLower = sensor.JobPrototypeId.ToLower().Trim();

            // Show only in three cases:
            // 1. Genuine high-ranking personnel (real ID card with JobPrototypeId in HighClearanceProtos).
            // 2. Agent disguised as a head (IsAgentIdCard = true AND JobIcon matches a high clearance icon).
            // 3. Unknown person — JobIcon is "JobIconNoId" (no ID card at all).
            if (HighClearanceProtos.Contains(protoLower))
            {
                // Genuine high-ranking personnel with a real ID card.
                filtered.Add(address, sensor);
            }
            else if (sensor.IsAgentIdCard && HighClearanceIcons.Contains(sensor.JobIcon))
            {
                // Agent ID card disguised as a head via chameleon — show them.
                filtered.Add(address, sensor);
            }
            else if (sensor.JobIcon == "JobIconNoId")
            {
                // Unknown person with no ID card.
                filtered.Add(address, sensor);
            }
            // Everyone else is hidden:
            // - Agents disguised as regular crew (IsAgentIdCard = true but JobIcon is not high clearance).
            // - Regular crew members (no JobPrototypeId in HighClearanceProtos, not an Agent ID card).
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
