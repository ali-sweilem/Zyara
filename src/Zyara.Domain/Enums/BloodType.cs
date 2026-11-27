namespace Zyara.Domain.Enums;

public enum BloodType
{
    Unknown = 0,
    APositive = 1,
    ANegative = 2,
    BPositive = 3,
    BNegative = 4,
    ABPositive = 5,
    ABNegative = 6,
    OPositive = 7,
    ONegative = 8
}

public static class BloodTypeExtensions
{
    public static string ToDisplayString(this BloodType bloodType) => bloodType switch
    {
        BloodType.APositive => "A+",
        BloodType.ANegative => "A-",
        BloodType.BPositive => "B+",
        BloodType.BNegative => "B-",
        BloodType.ABPositive => "AB+",
        BloodType.ABNegative => "AB-",
        BloodType.OPositive => "O+",
        BloodType.ONegative => "O-",
        _ => "Unknown"
    };

    public static BloodType FromDisplayString(string? value) => value?.Trim().ToUpperInvariant() switch
    {
        "A+" or "APOSITIVE" => BloodType.APositive,
        "A-" or "ANEGATIVE" => BloodType.ANegative,
        "B+" or "BPOSITIVE" => BloodType.BPositive,
        "B-" or "BNEGATIVE" => BloodType.BNegative,
        "AB+" or "ABPOSITIVE" => BloodType.ABPositive,
        "AB-" or "ABNEGATIVE" => BloodType.ABNegative,
        "O+" or "OPOSITIVE" => BloodType.OPositive,
        "O-" or "ONEGATIVE" => BloodType.ONegative,
        _ => BloodType.Unknown
    };
}

