using MediatR;

namespace NtisPlatform.Application.Events;

/// <summary>
/// Raised when a property's certificate (Occupation / Completion) or the electricity-bill date
/// changes, requiring the Rateable Value and Occupation Tax to be recomputed.
/// </summary>
/// <param name="PropertyId">Property whose certificate changed.</param>
/// <param name="UserId">User who triggered the change; attributed to downstream writes.</param>
public sealed record PropertyCertificateChangedEvent(int PropertyId, int UserId) : INotification;
