namespace NtisPlatform.Application.Services;

/// <summary>
/// Defines when an unpaid statutory demand must be settled before an RTS workflow action.
/// </summary>
internal static class RTSPaymentWorkflowGuard
{
    /// <summary>
    /// Document verification is a payment checkpoint only at stages configured to collect payment.
    /// </summary>
    public static bool RequiresSettlementBeforeVerification(
        bool isPaymentAfterApproval,
        bool currentStageCanPay) =>
        !isPaymentAfterApproval && currentStageCanPay;

    /// <summary>
    /// A configured payment stage cannot advance with an unpaid demand. Final approval is
    /// always a safeguard so a normal service cannot be approved unpaid when its workflow
    /// configuration has no reachable payment stage.
    /// </summary>
    public static bool RequiresSettlementBeforeApproval(
        bool isPaymentAfterApproval,
        bool currentStageCanPay,
        bool isFinalStage) =>
        !isPaymentAfterApproval && (currentStageCanPay || isFinalStage);
}
