namespace Frisson.Core;

/// <summary>
/// Single source of truth for all SettingsService default values.
/// Both initialization code and Reset functionality read from here.
/// </summary>
public static class SettingsDefaults
{
    public const int MaxA = 100;
    public const int MaxB = 100;
    public const bool UseActuatorLimits = true;
}
