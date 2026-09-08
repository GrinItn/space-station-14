// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameObjects;

namespace Content.Shared.SS220.Medical;

[RegisterComponent]
public sealed partial class BlueShieldMonitorComponent : Component
{
    /// <summary>
    ///     Job prototype IDs considered high clearance. Always shown on the Blue Shield monitor.
    /// </summary>
    [DataField]
    public HashSet<string> HighClearanceProtos = new()
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

    /// <summary>
    ///     Job icon prototype IDs for high clearance roles. Used to detect agents using chameleon to disguise as heads.
    /// </summary>
    [DataField]
    public HashSet<string> HighClearanceIcons = new()
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
    ///     Job icon prototype IDs treated as "unidentified". Always shown on the Blue Shield monitor.
    ///     "JobIconNoId" is the fallback icon when a person wears no ID card at all,
    ///     "JobIconUnknown" is the default icon of a blank ID card.
    /// </summary>
    [DataField]
    public HashSet<string> UnknownJobIcons = new()
    {
        "JobIconNoId",
        "JobIconUnknown",
    };
}
