using NtisPlatform.Core.Exceptions;

namespace NtisPlatform.Application.Exceptions;

/// <summary>
/// Thrown when a tax's CalculationMode change is refused because deleting the abandoned mode's
/// configuration was not explicitly opted into, or because the caller's view of the tax's current
/// mode is stale. Surfaces as 409 Conflict — nothing is written and nothing is deleted.
/// </summary>
public class TaxModeChangeConflictException : NtisPlatformException
{
    public string CurrentMode { get; }
    public string RequestedMode { get; }

    private TaxModeChangeConflictException(string message, string errorCode, string currentMode, string requestedMode)
        : base(message, errorCode)
    {
        CurrentMode = currentMode;
        RequestedMode = requestedMode;
        Data["CurrentMode"] = currentMode;
        Data["RequestedMode"] = requestedMode;
    }

    /// <summary>The caller changed the mode without providing the ExpectedCurrentMode precondition.</summary>
    public static TaxModeChangeConflictException ExpectedModeRequired(string currentMode, string requestedMode)
        => new(
            $"Changing CalculationMode from '{currentMode}' to '{requestedMode}' requires ExpectedCurrentMode to be specified. Re-send with ExpectedCurrentMode set to '{currentMode}'.",
            "DTR_MODE_CHANGE_EXPECTED_MODE_REQUIRED",
            currentMode,
            requestedMode);

    /// <summary>The caller changed the mode without setting ConfirmModeChangeCleanup.</summary>
    public static TaxModeChangeConflictException ConfirmationRequired(string currentMode, string requestedMode)
        => new(
            $"Changing CalculationMode from '{currentMode}' to '{requestedMode}' deletes the configuration saved under '{currentMode}'. Re-send with ConfirmModeChangeCleanup=true to proceed.",
            "DTR_MODE_CHANGE_CONFIRMATION_REQUIRED",
            currentMode,
            requestedMode);

    /// <summary>The caller's ExpectedCurrentMode did not match what is actually stored, so any
    /// confirmation it showed the user described the wrong configuration.</summary>
    public static TaxModeChangeConflictException StaleClient(string currentMode, string expectedMode)
        => new(
            $"This tax's CalculationMode is '{currentMode}', not '{expectedMode}' as expected. Reload the tax and try again.",
            "DTR_MODE_CHANGE_STALE_CLIENT",
            currentMode,
            expectedMode);
}
