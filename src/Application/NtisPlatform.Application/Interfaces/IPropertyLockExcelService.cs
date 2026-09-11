using Microsoft.AspNetCore.Http;
using NtisPlatform.Application.DTOs.LockUnlock;
using NtisPlatform.Application.Models;
using System.IO;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Application service interface for searching property lock status via Excel file uploads or row payloads.
/// </summary>
public interface IPropertyLockExcelService
{
    Task<PropertyLockExcelPagedResultDto> GetPropertyLocksByExcelFileAsync(
        Stream fileStream, int pageNumber, int pageSize, string? searchTerm = null, CancellationToken ct = default);

    Task<PropertyLockExcelPagedResultDto> GetPropertyLocksByExcelRowsAsync(
        SearchByExcelRequestDto request, CancellationToken ct = default);
}
