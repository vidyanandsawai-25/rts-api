using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Partial class for GetPropertiesAsync implementation
/// </summary>
public partial class PropertyService
{
    public async Task<List<GetPropertiesItemDto>> GetPropertiesAsync(
        GetPropertiesQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        if (queryParameters == null)
            throw new ArgumentNullException(nameof(queryParameters));

        if (string.IsNullOrWhiteSpace(queryParameters.WardNo))
            throw new ArgumentException("WardNo is required.", nameof(queryParameters.WardNo));

        if (string.IsNullOrWhiteSpace(queryParameters.FromPropertyNo))
            throw new ArgumentException("FromPropertyNo is required.", nameof(queryParameters.FromPropertyNo));

        if (string.IsNullOrWhiteSpace(queryParameters.ToPropertyNo))
            throw new ArgumentException("ToPropertyNo is required.", nameof(queryParameters.ToPropertyNo));

        if (queryParameters.UserId <= 0)
            throw new ArgumentException("UserId is required and must be greater than 0.", nameof(queryParameters.UserId));

        if (string.IsNullOrWhiteSpace(queryParameters.Flag))
            throw new ArgumentException("Flag is required.", nameof(queryParameters.Flag));

        string flag = queryParameters.Flag.Trim().ToUpperInvariant();
        if (flag != "NEW" && flag != "OLD" && flag != "ALL")
            throw new ArgumentException("Flag must be one of: New, Old, All.", nameof(queryParameters.Flag));

        string wardNoInput = queryParameters.WardNo.Trim();
        bool isNumericWardId = int.TryParse(wardNoInput, out var wardIdInput);
        string fromStr = queryParameters.FromPropertyNo.Trim();
        string toStr = queryParameters.ToPropertyNo.Trim();
        string? partitionNo = string.IsNullOrWhiteSpace(queryParameters.PartitionNo) ? null : queryParameters.PartitionNo.Trim();
        int userId = queryParameters.UserId;

        // Step 1: Look up Ward from PTIS.WardMaster by WardNo or Id
        var wardEntity = await _wardRepository.GetQueryable()
            .AsNoTracking()
            .Where(w => w.IsActive && (w.WardNo == wardNoInput || (isNumericWardId && w.Id == wardIdInput)))
            .FirstOrDefaultAsync(cancellationToken);

        int targetWardId = wardEntity?.Id ?? (isNumericWardId ? wardIdInput : 0);
        string targetWardNo = wardEntity?.WardNo ?? wardNoInput;

        // Step 2: Base query from PTIS.PropertyMast filtered by WardId
        var propertiesQuery = _repository.GetQueryable()
            .AsNoTracking()
            .Where(pm => pm.IsActive && !pm.MarkedForDeletion && pm.WardId == targetWardId);

        if (queryParameters.Apartment)
        {
            var societyQuery = _societyRepository.GetQueryable().AsNoTracking().Where(sd => sd.IsActive && !sd.MarkedForDeletion);

            // Main Society: no PartitionNo in PropertyMast and has PropertyId in SocietyDetailsMast
            propertiesQuery = propertiesQuery.Where(pm =>
                (pm.PartitionNo == null || pm.PartitionNo == "") &&
                societyQuery.Any(sd => sd.PropertyId == pm.Id));
        }
        else
        {
            // Excluding Apartment properties where CategoryId = 1
            propertiesQuery = propertiesQuery.Where(pm => pm.CategoryId != 1);
        }

        // Step 3: PropertyNo range filtering (supports numeric and alphanumeric ranges)
        if (fromStr.Equals(toStr, StringComparison.OrdinalIgnoreCase))
        {
            propertiesQuery = propertiesQuery.Where(pm => pm.PropertyNo == fromStr);
        }
        else if (long.TryParse(fromStr, out long fromNum) && long.TryParse(toStr, out long toNum))
        {
            // When both FromPropertyNo and ToPropertyNo are numeric, perform EF Core-translatable length & string comparison
            // so string values like "15" or "16" are not incorrectly included between "1" and "5".
            int minLen = fromStr.Length;
            int maxLen = toStr.Length;

            if (minLen == maxLen)
            {
                propertiesQuery = propertiesQuery.Where(pm => pm.PropertyNo != null &&
                    pm.PropertyNo.Length == minLen &&
                    string.Compare(pm.PropertyNo, fromStr) >= 0 &&
                    string.Compare(pm.PropertyNo, toStr) <= 0);
            }
            else
            {
                propertiesQuery = propertiesQuery.Where(pm => pm.PropertyNo != null &&
                    (
                        (pm.PropertyNo.Length == minLen && string.Compare(pm.PropertyNo, fromStr) >= 0) ||
                        (pm.PropertyNo.Length > minLen && pm.PropertyNo.Length < maxLen) ||
                        (pm.PropertyNo.Length == maxLen && string.Compare(pm.PropertyNo, toStr) <= 0)
                    ));
            }
        }
        else
        {
            propertiesQuery = propertiesQuery.Where(pm => pm.PropertyNo != null &&
                string.Compare(pm.PropertyNo, fromStr) >= 0 &&
                string.Compare(pm.PropertyNo, toStr) <= 0);
        }

        // Step 4: Optional PartitionNo filtering
        if (!string.IsNullOrWhiteSpace(partitionNo) && partitionNo != "0")
        {
            propertiesQuery = propertiesQuery.Where(pm => pm.PartitionNo == partitionNo);
        }

        var mapDetailsQuery = _propertyMapDetailRepository.GetQueryable().AsNoTracking();

        using var scope = _serviceProvider?.CreateScope();
        var sp = scope?.ServiceProvider;

        var workflowRepo = _workflowDetailsRepository ?? sp?.GetService<IRepository<PropertyWorkflowDetailsEntity, int>>();
        var surveyVisitRepo = _propertySurveyVisitRepository ?? sp?.GetService<IRepository<PropertySurveyVisitEntity, int>>();

        var workflowQuery = workflowRepo != null ? workflowRepo.GetQueryable().AsNoTracking() : Enumerable.Empty<PropertyWorkflowDetailsEntity>().AsQueryable();
        var surveyVisitQuery = surveyVisitRepo != null ? surveyVisitRepo.GetQueryable().AsNoTracking() : Enumerable.Empty<PropertySurveyVisitEntity>().AsQueryable();
        var usersQuery = _userRepository.GetQueryable().AsNoTracking();

        // Step 5: Flag filtering (New / Old / All)
        if (flag == "NEW")
        {
            propertiesQuery = propertiesQuery.Where(pm => !mapDetailsQuery.Any(pmd =>
                pmd.PropertyIdNew == pm.Id && pmd.Status == "Active" && pmd.IsActive));
        }
        else if (flag == "OLD")
        {
            propertiesQuery = propertiesQuery.Where(pm => mapDetailsQuery.Any(pmd =>
                pmd.PropertyIdNew == pm.Id && pmd.Status == "Active" && pmd.IsActive));
        }

        var propertyTypesQuery = _propertyTypeRepository.GetQueryable().AsNoTracking();

        // Step 6: Projection with UserMaster join, PropertyTypeMaster join, SurveyVisit (Latitude/Longitude only), and MapCount/IsMerged
        var query = from pm in propertiesQuery
                    join u in usersQuery on pm.CreatedBy equals u.Id into userJoin
                    from u in userJoin.DefaultIfEmpty()
                    join pt in propertyTypesQuery on pm.PropertyTypeId equals pt.Id into ptJoin
                    from pt in ptJoin.DefaultIfEmpty()
                    let survey = (
                        from pwd in workflowQuery
                        join psv in surveyVisitQuery on pwd.Id equals psv.PropertyWorkflowDetailsId
                        where pwd.PropertyId == pm.Id && pwd.IsActive && psv.IsActive && psv.CreatedBy == userId
                        orderby psv.CreatedDate descending, psv.Id descending
                        select new { psv.Latitude, psv.Longitude }
                    ).FirstOrDefault()
                    let mapCount = mapDetailsQuery.Count(pmd =>
                        pmd.PropertyIdNew == pm.Id && pmd.Status == "Active" && pmd.IsActive)
                    orderby pm.Id
                    select new GetPropertiesItemDto
                    {
                        PropertyId = pm.Id,
                        WardId = pm.WardId,
                        WardNo = targetWardNo,
                        PropertyNo = pm.PropertyNo,
                        PartitionNo = pm.PartitionNo,
                        PropertyTypeId = pm.PropertyTypeId,
                        UPICId = pm.UPICId,
                        CSN = pm.CSN,
                        SubZoneNo = pm.SubZoneNo,
                        PlotNo = pm.PlotNo,
                        CategoryId = pm.CategoryId,
                        Type = pm.Type ?? (pt != null ? pt.Type : null),
                        PartType = pt != null ? pt.PartType : null,
                        OwnerTitle = pm.OwnerTitle,
                        OwnerName = pm.OwnerName,
                        OwnerTitleEnglish = pm.OwnerTitleEnglish,
                        OwnerNameEnglish = pm.OwnerNameEnglish,
                        OccupierTitle = pm.OccupierTitle,
                        OccupierName = pm.OccupierName,
                        OccupierTitleEnglish = pm.OccupierTitleEnglish,
                        OccupierNameEnglish = pm.OccupierNameEnglish,
                        Address = pm.Address,
                        Location = pm.Location,
                        AddressEnglish = pm.AddressEnglish,
                        LocationEnglish = pm.LocationEnglish,
                        Latitude = survey != null ? survey.Latitude : null,
                        Longitude = survey != null ? survey.Longitude : null,
                        IsMerged = mapCount > 0,
                        MapCount = mapCount,
                        CreatedBy = u != null ? u.UserName : (pm.CreatedBy != null ? pm.CreatedBy.ToString() : null),
                        CreatedDate = pm.CreatedDate
                    };

        return await query.ToListAsync(cancellationToken);
    }
}
