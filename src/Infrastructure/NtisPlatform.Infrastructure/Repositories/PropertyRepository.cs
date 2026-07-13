using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NtisPlatform.Application.Enums;
using NtisPlatform.Core.Constants;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Enums;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Models;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories;

/// <summary>
/// Specialized repository implementation for Property entity
/// Provides custom query methods for property-related operations
/// </summary>
public class PropertyRepository : Repository<PropertyEntity, int>, IPropertyRepository
{
    public PropertyRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<PropertyTaxDetailsDto?> GetTaxDetailsAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        var policies = await GetTaxDetailsPivotedAsync(
            propertyId,
            isCapitalValue: false,
            excludeEducationEmploymentTax: true,  // Hide education/employment tax in details-taxes API
            cancellationToken);

        if (policies == null)
            return null;

        return new PropertyTaxDetailsDto
        {
            PropertyId = propertyId,
            Policies = policies
        };
    }

    /// <summary>
    /// Private helper method to query and pivot tax details from a given tax details table.
    /// Joins with TaxMaster, filters by active/deleted flags, orders by DisplayOrder, and groups by PolicyCode.
    /// </summary>
    /// <param name="propertyId">The property identifier</param>
    /// <param name="isCapitalValue">Whether to query CapitalValue or RateableValue tax details</param>
    /// <param name="excludeEducationEmploymentTax">If true, excludes Education and Employment taxes from results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of pivoted PolicyTaxDetail objects, or null if property not found or no data exists</returns>
    private async Task<List<PolicyTaxDetail>?> GetTaxDetailsPivotedAsync(
        int propertyId,
        bool isCapitalValue,
        bool excludeEducationEmploymentTax = false,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Check if property exists
        var propertyExists = await _context.PropertyMast
            .AnyAsync(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion, cancellationToken);

        if (!propertyExists)
            return null;

        // Step 2: Query tax details with TaxMaster join, ordered by DisplayOrder
        List<(string PolicyCode, string TaxName, decimal? TaxAmount)> taxData;

        if (isCapitalValue)
        {
            taxData = await (from td in _context.PolicyTaxDetailsCV
                             join tm in _context.TaxMaster on td.TaxId equals tm.Id
                             join tc in _context.TaxCategoryMaster on tm.TaxCategoryId equals tc.Id
                             where td.PropertyId == propertyId && td.IsActive && !td.MarkedForDeletion
                                && tm.IsActive                              
                             orderby tm.DisplayOrder
                             select new ValueTuple<string, string, decimal?>(
                                 td.PolicyCode,
                                 tm.TaxName,
                                 td.TaxAmount
                             ))
                            .ToListAsync(cancellationToken);
        }
        else
        {
            taxData = await (from td in _context.PolicyTaxDetails
                             join tm in _context.TaxMaster on td.TaxId equals tm.Id
                             join tc in _context.TaxCategoryMaster on tm.TaxCategoryId equals tc.Id
                             where td.PropertyId == propertyId && td.IsActive && !td.MarkedForDeletion
                                && tm.IsActive                                
                             orderby tm.DisplayOrder
                             select new ValueTuple<string, string, decimal?>(
                                 td.PolicyCode,
                                 tm.TaxName,
                                 td.TaxAmount
                             ))
                            .ToListAsync(cancellationToken);
        }

        // Step 3: Return null if no tax details found
        if (taxData.Count == 0)
            return null;

        // Step 4: Group by PolicyCode and create pivoted structure
        var policies = taxData
            .GroupBy(x => x.Item1)
            .Select(g =>
            {
                var taxAmounts = g
                    .GroupBy(x => x.Item2)
                    .Select(tg => new TaxAmountDetail
                    {
                        TaxName = tg.Key,
                        TaxAmount = tg.Sum(x => x.Item3 ?? 0)
                    })
                    .ToList();

                var taxTotal = taxAmounts.Sum(t => t.TaxAmount);

                return new PolicyTaxDetail
                {
                    PolicyCode = g.Key,
                    TaxAmounts = taxAmounts,
                    TaxTotal = taxTotal
                };
            })
            .ToList();

        return policies;
    }

    public async Task<PropertyTaxDetailsCVDto?> GetTaxDetailsCVAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        var policies = await GetTaxDetailsPivotedAsync(
            propertyId,
            isCapitalValue: true,
            excludeEducationEmploymentTax: false,  // Show all taxes for CV
            cancellationToken);

        if (policies == null)
            return null;

        return new PropertyTaxDetailsCVDto
        {
            PropertyId = propertyId,
            Policies = policies
        };
    }

    public async Task<PropertyTaxApartmentDetailsDto?> GetAggregatedPropertyTaxDetailsAsync(PropertyApartmentTaxRequestDto dto, CancellationToken cancellationToken = default)
    {
        var normalizedPropertyNo = string.IsNullOrWhiteSpace(dto.PropertyNo) ? null : dto.PropertyNo.ToLower();
        var normalizedPartType = string.IsNullOrWhiteSpace(dto.PartType) ? null : dto.PartType.ToLower();
        var normalizedPartitionNo = string.IsNullOrWhiteSpace(dto.PartitionNo) ? null : dto.PartitionNo.ToLower();

        var totalwingList = await _context.Set<WingEntity>().AsNoTracking()
            .Where(d => d.IsActive && d.WingNo != null)
            .Select(d => d.WingNo.ToLower())
            .ToListAsync(cancellationToken);

        var isPartitionInWingList = normalizedPartitionNo != null && totalwingList.Contains(normalizedPartitionNo);

        var propertyIds = await (from pm in _context.PropertyMast.AsNoTracking()
                                 join pt in _context.PropertyTypeMasters on pm.PropertyTypeId equals pt.Id
                                 where (dto.WardId == null || pm.WardId == dto.WardId) &&
                                       (normalizedPropertyNo == null || (pm.PropertyNo != null && pm.PropertyNo.ToLower().Contains(normalizedPropertyNo))) &&
                                       (normalizedPartitionNo == null || (pm.PartitionNo != null && 
                                           (isPartitionInWingList 
                                               ? pm.PartitionNo.ToLower().Contains(normalizedPartitionNo) 
                                               : pm.PartitionNo.ToLower() == normalizedPartitionNo))) &&
                                       (normalizedPartType == null || (pt.PartType != null && pt.PartType.ToLower().Contains(normalizedPartType))) &&
                                       (dto.PropertyId == null || pm.Id == dto.PropertyId) &&
                                       pm.IsActive && !pm.MarkedForDeletion &&
                                       pt.IsActive
                                 select pm.Id)
                                .ToListAsync(cancellationToken);

        if (propertyIds == null || !propertyIds.Any())
            return null;

        var taxData = await (from tmrv in _context.TransMastRV
                             join tm in _context.TaxMaster on tmrv.TaxId equals tm.Id
                             join ym in _context.YearMaster on tmrv.FinanceYearId equals ym.Id
                             where propertyIds.Contains(tmrv.PropertyId)
                                && tmrv.IsActive && !tmrv.MarkedForDeletion
                                && tm.IsActive
                                && ym.IsActive
                             orderby tm.DisplayOrder
                             select new
                             {
                                 TaxName = tm.TaxName,
                                 TaxAmount = tmrv.TaxAmount,
                                 DisplayOrder = tm.DisplayOrder
                             })
                            .ToListAsync(cancellationToken);

        if (!taxData.Any())
            return null;

        var taxAmountList = taxData
            .GroupBy(x => new { x.TaxName, x.DisplayOrder })
            .Select(g => new TaxAmountDto
            {
                TaxName = g.Key.TaxName,
                TaxAmount = g.Sum(x => x.TaxAmount),
                DisplayOrder = g.Key.DisplayOrder
            })
            .OrderBy(x => x.DisplayOrder)
            .ToList();

        return new PropertyTaxApartmentDetailsDto
        {
            PropertyId = propertyIds.Count == 1 ? propertyIds[0] : 0,
            PropertyCount = propertyIds.Count,
            TaxAmounts = taxAmountList
        };
    }

    public async Task<PropertyTaxApartmentDetailsCVDto?> GetAggregatedPropertyTaxDetailsCVAsync(PropertyApartmentTaxRequestDto dto, CancellationToken cancellationToken = default)
    {
        var normalizedPropertyNo = string.IsNullOrWhiteSpace(dto.PropertyNo) ? null : dto.PropertyNo.ToLower();
        var normalizedPartType = string.IsNullOrWhiteSpace(dto.PartType) ? null : dto.PartType.ToLower();
        var normalizedPartitionNo = string.IsNullOrWhiteSpace(dto.PartitionNo) ? null : dto.PartitionNo.ToLower();

        var totalwingList = await _context.Set<WingEntity>().AsNoTracking()
            .Where(d => d.IsActive && d.WingNo != null)
            .Select(d => d.WingNo.ToLower())
            .ToListAsync(cancellationToken);

        var isPartitionInWingList = normalizedPartitionNo != null && totalwingList.Contains(normalizedPartitionNo);

        var propertyIds = await (from pm in _context.PropertyMast.AsNoTracking()
                                 join pt in _context.PropertyTypeMasters on pm.PropertyTypeId equals pt.Id
                                 where (dto.WardId == null || pm.WardId == dto.WardId) &&
                                       (normalizedPropertyNo == null || (pm.PropertyNo != null && pm.PropertyNo.ToLower().Contains(normalizedPropertyNo))) &&
                                       (normalizedPartitionNo == null || (pm.PartitionNo != null && 
                                           (isPartitionInWingList 
                                               ? pm.PartitionNo.ToLower().Contains(normalizedPartitionNo) 
                                               : pm.PartitionNo.ToLower() == normalizedPartitionNo))) &&
                                       (normalizedPartType == null || (pt.PartType != null && pt.PartType.ToLower().Contains(normalizedPartType))) &&
                                       (dto.PropertyId == null || pm.Id == dto.PropertyId) &&
                                       pm.IsActive && !pm.MarkedForDeletion &&
                                       pt.IsActive
                                 select pm.Id)
                                .ToListAsync(cancellationToken);

        if (propertyIds == null || !propertyIds.Any())
            return null;

        var taxData = await (from tmcv in _context.TransMastCV
                             join tm in _context.TaxMaster on tmcv.TaxId equals tm.Id
                             join ym in _context.YearMaster on tmcv.FinanceYearId equals ym.Id
                             where propertyIds.Contains(tmcv.PropertyId)
                                && tmcv.IsActive && !tmcv.MarkedForDeletion
                                && tm.IsActive
                                && ym.IsActive
                             orderby tm.DisplayOrder
                             select new
                             {
                                 TaxName = tm.TaxName,
                                 TaxAmount = tmcv.TaxAmount,
                                 DisplayOrder = tm.DisplayOrder
                             })
                            .ToListAsync(cancellationToken);

        if (!taxData.Any())
            return null;

        var taxAmountList = taxData
            .GroupBy(x => new { x.TaxName, x.DisplayOrder })
            .Select(g => new TaxAmountDto
            {
                TaxName = g.Key.TaxName,
                TaxAmount = g.Sum(x => x.TaxAmount),
                DisplayOrder = g.Key.DisplayOrder
            })
            .OrderBy(x => x.DisplayOrder)
            .ToList();

        return new PropertyTaxApartmentDetailsCVDto
        {
            PropertyId = propertyIds.Count == 1 ? propertyIds[0] : 0,
            PropertyCount = propertyIds.Count,
            TaxAmounts = taxAmountList
        };
    }


    public async Task<List<BuildingGenerateStructureDto>?> GetGenerateBuildingStructureAsync(BuildingGenerateDetailsDto dto, CancellationToken cancellationToken = default)
    {
        int iFromFloor = 1;
        int iToFloor = 1;
        int number;

        string? floorCode = "";
        if (dto.GenerationType.ToLower() == "HC".ToLower() & dto.FromFloor != dto.ToFloor)
        {
            throw new InvalidOperationException("From floor and to floor must be same");
        }
        else if (dto.GenerationType.ToLower() == "VC".ToLower() & dto.NoOfFlatOnOneFloor > 1)
        {
            throw new InvalidOperationException("Vertical Custom Generation no of flat in one floor must be 1");
        }


        if (int.TryParse(dto.FromFloor, out number) && number >= 1 && number <= 1000)
        {
            iFromFloor = Convert.ToInt32(dto.FromFloor);
            iToFloor = Convert.ToInt32(dto.ToFloor);

            if (iFromFloor > iToFloor)
            {
                throw new InvalidOperationException("From Floor cannot be greater than To Floor");
            }
        }

        else
        {

            floorCode = dto.FromFloor?.ToString();

            if (dto.GenerationType.ToLower() != "hc" && dto.GenerationType.ToLower() != "vc")
            {
                throw new InvalidOperationException("Select horizontal custom or vertical custom for generation");
            }


        }


        // Step 1: Validate input parameters


        if (dto.NoOfFlatOnOneFloor <= 0)
        {
            throw new InvalidOperationException("No Of Flat On One Floor must be greater than zero");
        }

        if (dto.Prifix != "" && dto.Prifix != null)
        {
            dto.Prifix = dto.Prifix + "-";
        }

        // Step 2: Validate WingId exists and get WingNo
        var wingNo = await _context.Set<WingEntity>()
            .Where(w => w.Id == dto.WingId && w.IsActive)
            .Select(w => w.WingNo)
            .FirstOrDefaultAsync(cancellationToken);

        if (wingNo == null)
        {
            throw new InvalidOperationException("Wing Not Found");
        }

        // Step 3: Get existing property count for partition number calculation
        // This is equivalent to @LastPropertyNo in the SQL query
        var lastPropertyNo = await (from p in _context.PropertyMast
                                    join s in _context.SocietyDetailsMast on p.SocietyDetailId equals s.Id
                                    where p.WardId == dto.WardId
                                          && p.PropertyNo == dto.PropertyNo
                                          && s.WingId == dto.WingId
                                          && p.IsActive
                                          && !p.MarkedForDeletion
                                          && s.IsActive
                                          && !s.MarkedForDeletion
                                    select p).CountAsync(cancellationToken);

        // Step 4: Generate floor and unit sequences (equivalent to CTEs in SQL)
        // Floors CTE: SELECT @FromFloor AS FloorNo UNION ALL SELECT FloorNo + 1 FROM Floors WHERE FloorNo < @ToFloor
        var floors = Enumerable.Range(iFromFloor, iToFloor - iFromFloor + 1).ToList();

        // Units CTE: SELECT 1 AS UnitNo UNION ALL SELECT UnitNo + 1 FROM Units WHERE UnitNo < @NoOfFlatOnOneFloor
        var units = Enumerable.Range(1, dto.NoOfFlatOnOneFloor).ToList();

        // Step 5: Vertical Generation - Cross join ordered by UnitNo, then FloorNo

        // Determine generation type flags
        var isVertical = dto.GenerationType.Equals("V", StringComparison.OrdinalIgnoreCase) ||
                         dto.GenerationType.Equals("VC", StringComparison.OrdinalIgnoreCase);
        var isHorizontal = dto.GenerationType.Equals("H", StringComparison.OrdinalIgnoreCase) ||
                           dto.GenerationType.Equals("HC", StringComparison.OrdinalIgnoreCase);
        var isHC = dto.GenerationType.Equals("HC", StringComparison.OrdinalIgnoreCase);

        if (!isVertical && !isHorizontal)
        {
            throw new InvalidOperationException("Invalid Generation Type");
        }

        // Normalize prefix
        var prefix = !string.IsNullOrEmpty(dto.Prifix) ? $"{dto.Prifix}" : string.Empty;
        var normalizedType = dto.GenerationType.ToUpperInvariant();

        // Create cross join of units and floors
        var crossJoin = from u in units
                        from f in floors
                        select (FloorNo: f, UnitNo: u);

        // Apply ordering based on generation type
        // Vertical (V, VC): order by UnitNo then FloorNo
        // Horizontal (H, HC): order by FloorNo then UnitNo
        var orderedItems = isVertical
            ? crossJoin.OrderBy(x => x.UnitNo).ThenBy(x => x.FloorNo)
            : crossJoin.OrderBy(x => x.FloorNo).ThenBy(x => x.UnitNo);

        var floorlst = await _context.FloorEntity.Where(f => f.IsActive)
                            .ToListAsync(cancellationToken);

        // Generate result with floor multiplier (HC uses 0, others use FloorNo - 1)
        return orderedItems
            .Select((item, index) => new BuildingGenerateStructureDto
            {
                WardId = dto.WardId,
                PropertyNo = dto.PropertyNo,
                WingId = dto.WingId,
                RowNo = index + 1,
                FloorNo = item.FloorNo,
                floorCode = string.IsNullOrEmpty(floorCode) ? item.FloorNo.ToString() : floorCode,
                PropertyFloorId = floorlst.Where(e => e.FloorCode == (string.IsNullOrEmpty(floorCode) ? item.FloorNo.ToString() : floorCode)).Select(e => e.Id).FirstOrDefault(),
                UnitNo = item.UnitNo,
                FlatNo = $"{prefix}{dto.FlatStart + (isHC ? 0 : (item.FloorNo - 1) * dto.IncrementedBy) + (item.UnitNo - 1)}",
                PartitionNo = $"{wingNo}{index + 1 + lastPropertyNo}",
                GenerationType = normalizedType
            })
            .ToList();

    }


    public async Task<List<SocietyAminityDetailsDto>?> GetSocietyAmenityDetailsAsync(int SocietyDetailId, bool isAmenity, CancellationToken cancellationToken = default)
    {
        var amenityProperties = await (
            from pm in _context.PropertyMast
            join ptm in _context.PropertyTypeMasters on pm.PropertyTypeId equals ptm.Id
            join wm in _context.WardMaster on pm.WardId equals wm.Id
            join sdm in _context.SocietyDetailsMast on pm.SocietyDetailId equals sdm.Id
            join we in _context.WingEntity on sdm.WingId equals we.Id
            where pm.SocietyDetailId == SocietyDetailId
    && !string.IsNullOrEmpty(pm.PartitionNo)
    && pm.PartitionNo != we.WingNo
    && pm.MarkedForDeletion != true
    && pm.IsActive == true
    && (
                        isAmenity
                            ? ptm.PartType == PartTypeConstants.Amenity
                            : ptm.PartType != PartTypeConstants.Amenity
                     )
            orderby pm.Id descending

            select new SocietyAminityDetailsDto
            {
                PropertyId = pm.Id,
                SocietyDetailId = pm.SocietyDetailId ?? 0,
                WardId = pm.WardId,
                WardNo = wm.WardNo,
                wingId = we.Id,
                WingNo = we.WingNo,
                WingName = sdm.WingName,
                PropertyNo = pm.PropertyNo,
                PartitionNo = pm.PartitionNo,
                PartType = ptm.PartType
            })
            .ToListAsync(cancellationToken);

        return amenityProperties;
    }

    public async Task<List<PropertySocietyDetailsDto>?> GetSocietyWingListAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        var property = await (
        from p in _context.PropertyMast
        join w in _context.WardMaster
            on p.WardId equals w.Id
        where p.Id == propertyId
              && p.IsActive
              && !p.MarkedForDeletion
        select new
        {
            p.Id,
            WardId = w.Id,
            WardNo = w.WardNo,
            PropertyNo = p.PropertyNo
        }
    ).FirstOrDefaultAsync(cancellationToken);


        if (property == null)
            return null;

        var amenityProperties = await (
      from sdm in _context.SocietyDetailsMast
      join we in _context.WingEntity on sdm.WingId equals we.Id into wingJoin
      from we in wingJoin.Where(x => x.IsActive).DefaultIfEmpty()
      where sdm.PropertyId == property.Id
            && sdm.IsActive
            && !sdm.MarkedForDeletion
      select new PropertySocietyDetailsDto
      {
          PropertyId = sdm.PropertyId,
          SocietyDetailId = sdm.Id,
          WingId = sdm.WingId,
          WingNo = we != null ? we.WingNo : null,
          WardNo = property.WardNo,
          PropertyNo = property.PropertyNo,
          WingName = sdm.WingName,
          SocietyName = sdm.SocietyName,
          SocietyAddress = sdm.SocietyAddress,
          SecretaryName = sdm.SecretaryName,
          ManagerName = sdm.ManagerName,
          LandOwnerName = sdm.LandOwnerName,
          BuilderName = sdm.BuilderName,
          SocietyNameEnglish = sdm.SocietyNameEnglish,
          SocietyAddressEnglish = sdm.SocietyAddressEnglish,
          SecretaryNameEnglish = sdm.SecretaryNameEnglish,
          ManagerNameEnglish = sdm.ManagerNameEnglish,
          LandOwnerNameEnglish = sdm.LandOwnerNameEnglish,
          BuilderNameEnglish = sdm.BuilderNameEnglish,
          ManagerMobileNo = sdm.ManagerMobileNo,
          SecretaryMobileNo = sdm.SecretaryMobileNo,
          SocietyEmailId = sdm.SocietyEmailId,
          SecretaryEmailId = sdm.SecretaryEmailId,
          ManagerEmailId = sdm.ManagerEmailId,
          PropertyCount = _context.PropertyMast
              .Where(pm => pm.SocietyDetailId == sdm.Id
                  && !string.IsNullOrEmpty(pm.PartitionNo)
                  && pm.IsActive
                  && !pm.MarkedForDeletion)
              .Join(_context.PropertyTypeMasters,
                  pm => pm.PropertyTypeId,
                  ptm => ptm.Id,
                  (pm, ptm) => ptm)
              .Count(ptm => ptm.PartType != PartTypeConstants.Amenity && ptm.IsActive),
          AminityCount = _context.PropertyMast
              .Where(pm => pm.SocietyDetailId == sdm.Id
                  && !string.IsNullOrEmpty(pm.PartitionNo)
                  && pm.IsActive
                  && !pm.MarkedForDeletion)
              .Join(_context.PropertyTypeMasters,
                  pm => pm.PropertyTypeId,
                  ptm => ptm.Id,
                  (pm, ptm) => ptm)
              .Count(ptm => ptm.PartType == PartTypeConstants.Amenity && ptm.IsActive),
      })
      .AsNoTracking()
      .ToListAsync(cancellationToken);
        return amenityProperties;
    }

    public async Task<List<BuildingListDto>?> GetBuildingListAsync(int wardId, CancellationToken cancellationToken = default)
    {
        var WardDetails = await _context.WardMaster
     .Where(p => p.Id == wardId && p.IsActive)
     .Select(p => new { p.Id })
     .FirstOrDefaultAsync(cancellationToken);

        if (WardDetails == null)
            return null;

        // Step 1: Query builing list properties as per ward
        var buildingProperties = await (from pm in _context.PropertyMast
                                        join pcm in _context.PropertyCategoryMaster on pm.CategoryId equals pcm.Id
                                        join wm in _context.WardMaster on pm.WardId equals wm.Id
                                        where pm.WardId == wardId
                                        && string.IsNullOrEmpty(pm.PartitionNo)
                                         && pm.IsActive
                                         && !pm.MarkedForDeletion
                                         && wm.IsActive
                                         && pcm.IsActive

                                        select new BuildingListDto
                                        {
                                            PropertyId = pm.Id,
                                            WardNo = wm.WardNo,
                                            CatPropertyCategoryName = pcm.PropertyCategoryName,
                                            PropertyNo = pm.PropertyNo,
                                            PartitionNo = pm.PartitionNo
                                        })
                                      .ToListAsync(cancellationToken);


        return buildingProperties;
    }



    public async Task<bool> IsPropertyExists(int wardId, string propertyNo, int? propertyId)
    {
        return await _context.PropertyMast.AnyAsync(x => 
            x.WardId == wardId && 
            x.PropertyNo == propertyNo && 
            (x.PartitionNo == "" || x.PartitionNo == null) && x.MarkedForDeletion==false &&
            (!propertyId.HasValue || x.Id != propertyId.Value));
    }

    private static CreateNewPropertyResponseDto Failure(string message)
    {
        return new CreateNewPropertyResponseDto
        {
            Success = false,
            Message = message
        };
    }

    private static bool ContainsAny(string source, params string[] values)
    {
        return values.Any(v =>
            source.Contains(v, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets all RoomWiseMinusData entities by list of RoomWiseSubmissionId values.
    /// Used during property deletion to mark all minus data records for deletion.
    /// This entity only has RoomWiseSubmissionId column (no PropertyId), so we query by parent RoomWiseSubmissionDetails IDs.
    /// </summary>
    public async Task<List<RoomWiseMinusDataEntity>> GetRoomWiseMinusBySubmissionIdsAsync(List<int> roomWiseSubmissionIds, CancellationToken cancellationToken = default)
    {
        return await _context.RoomWiseMinusData
            .Where(x => roomWiseSubmissionIds.Contains(x.RoomWiseSubmissionId))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets PropertyDetails entities for a property.
    /// Used as the first step in property deletion to identify related PropertyDetailsId values.
    /// </summary>
    public async Task<List<PropertyDetailsEntity>> GetPropertyDetailsByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _context.PropertyDetails
            .Where(pd => pd.PropertyId == propertyId)
            .ToListAsync(cancellationToken);
    }

    #region PropertyTaxCalculationRVResults - Entity has BOTH PropertyId AND PropertyDetailsId

    /// <summary>
    /// Gets all PropertyTaxCalculationRVResults for a property by PropertyId.
    /// USED FOR DELETION: PropertyId alone is sufficient because it's the primary FK relationship.
    /// All RV results for a property MUST have PropertyId, so this query guarantees complete coverage.
    /// </summary>
    public async Task<List<PropertyTaxCalculationRVResultsEntity>> GetRvResultsByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _context.PropertyTaxCalculationRVResults
            .Where(x => x.PropertyId == propertyId)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region PropertyTaxCalculationSection129Results - Entity has BOTH PropertyId AND PropertyDetailsId

    /// <summary>
    /// Gets all PropertyTaxCalculationSection129Results for a property by PropertyId.
    /// USED FOR DELETION: PropertyId alone is sufficient because it's the primary FK relationship.
    /// All Section129 results for a property MUST have PropertyId, so this query guarantees complete coverage.
    /// </summary>
    public async Task<List<PropertyTaxCalculationSection129ResultsEntity>> GetSection129ResultsByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _context.PropertyTaxCalculationSection129Results
            .Where(x => x.PropertyId == propertyId)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Entities with ONLY PropertyDetailsId (no PropertyId column)

    /// <summary>
    /// Gets PropertyOccupancyDetails by PropertyDetailsId list.
    /// This entity only has PropertyDetailId column (no PropertyId), so simple query is sufficient.
    /// </summary>
    public async Task<List<PropertyOccupancyDetailsEntity>> GetPropertyOccupancyByPropertyDetailIdsAsync(List<int> propertyDetailIds, CancellationToken cancellationToken = default)
    {
        return await _context.PropertyOccupancyDetails
            .Where(x => propertyDetailIds.Contains(x.PropertyDetailId))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets RenterMast by PropertyDetailsId list.
    /// This entity only has PropertyDetailsId column (no PropertyId), so simple query is sufficient.
    /// </summary>
    public async Task<List<RenterMastEntity>> GetRentersByPropertyDetailIdsAsync(List<int> propertyDetailIds, CancellationToken cancellationToken = default)
    {
        return await _context.RenterMast
            .Where(x => propertyDetailIds.Contains(x.PropertyDetailsId))
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region RoomWiseSubmissionDetails - Entity has BOTH PropertyId AND PropertyDetailsId (nullable)

    /// <summary>
    /// Gets all RoomWiseSubmissionDetails for a property by PropertyId.
    /// USED FOR DELETION: PropertyId alone is sufficient to catch all records.
    /// Catches all records regardless of PropertyDetailsId state (NULL, valid, or orphaned).
    /// Use this method when deleting a property to ensure no orphaned records remain.
    /// </summary>
    public async Task<List<RoomWiseSubmissionDetailsEntity>> GetRoomWiseSubmissionByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _context.RoomWiseSubmissionDetails
            .Where(x => x.PropertyId == propertyId)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Entities with ONLY PropertyId - BaseEntity only (no IHardDeletable)

    /// <summary>
    /// Gets PropertySocialDetails by PropertyId.
    /// This entity extends BaseEntity but does NOT implement IHardDeletable.
    /// Used for deactivation (IsActive=false) during property deletion.
    /// </summary>
    public async Task<List<PropertySocialDetailsEntity>> GetPropertySocialDetailsByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<PropertySocialDetailsEntity>()
            .Where(x => x.PropertyId == propertyId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets WaterConnectionMaster by PropertyId.
    /// This entity extends BaseEntity but does NOT implement IHardDeletable.
    /// Used for deactivation (IsActive=false) during property deletion.
    /// </summary>
    public async Task<List<WaterConnectionMasterEntity>> GetWaterConnectionsByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<WaterConnectionMasterEntity>()
            .Where(x => x.PropertyId == propertyId)
            .ToListAsync(cancellationToken);
    }

    #endregion

    // TODO: Uncomment when database table structure is finalized for PropertyTaxCalculationCVResultsEntity
    //public async Task<List<PropertyTaxCalculationCVResultsEntity>> GetCvResultsByPropertyDetailIdsAsync(List<int> propertyDetailIds, CancellationToken cancellationToken = default)
    //{
    //    return await _context.PropertyTaxCalculationCVResults
    //        .AsNoTracking()
    //        .Where(x => propertyDetailIds.Contains(x.PropertyDetailsIds))
    //        .ToListAsync(cancellationToken);
    //}

    /// <summary>
    /// Gets RenterDetail entities by PropertyDetailsId list.
    /// This entity only has PropertyDetailsId column (no PropertyId), so simple query is sufficient.
    /// Used during property deletion to identify and mark all renter detail records.
    /// </summary>
    public async Task<List<RenterDetailEntity>> GetRenterDetailsByPropertyDetailIdsAsync(List<int> propertyDetailIds, CancellationToken cancellationToken = default)
    {
        return await _context.RenterDetails
            .AsNoTracking()
            .Where(x => propertyDetailIds.Contains(x.PropertyDetailsId))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets all related entities for a property that need to be marked for deletion.
    /// Returns entities implementing IHardDeletable.
    /// 
    /// NOTE: Queries are executed sequentially to avoid DbContext concurrency issues.
    /// EF Core's DbContext is not thread-safe and cannot handle parallel queries on the same instance.
    /// </summary>
    public async Task<List<IHardDeletable>> GetRelatedEntitiesForDeletionAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        // Build the list of related entities
        var relatedEntities = new List<IHardDeletable>();

        // Execute queries sequentially to avoid DbContext concurrency issues
        // Each query is independent but must run one at a time on the same DbContext

        var applyTaxes = await _context.ApplyTaxesMaster.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(applyTaxes);

        var flags = await _context.FlagMaster.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(flags);

        var plots = await _context.PlotDetails.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(plots);

        var policyTax = await _context.PolicyTaxDetails.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(policyTax);

        var assessmentDetails = await _context.PropertyAssessmentDetails.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(assessmentDetails);

        var images = await _context.PropertyImagesMast.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(images);

        // Note: PropertySocialDetails and WaterConnectionMaster do not implement IHardDeletable.
        // These entities are now handled using DeactivatePropertyEntities() in PropertyService.MarkPropertyDetailsAndRelatedAsync().
        // They only get IsActive=false and UpdatedDate set, without MarkedForDeletion flags.

        // Note: PropertyTaxCalculationCVResultsEntity and RenterDetailEntity use PropertyDetailsId (not PropertyId).
        // They are handled in PropertyService.MarkPropertyDetailsAndRelatedAsync() method with TODO comments there.

        var taxPending = await _context.TaxPendingDetails.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(taxPending);

        var taxPendingArchive = await _context.TaxPendingDetailsArchive.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(taxPendingArchive);

        var taxPendingCV = await _context.TaxPendingDetailsCV.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(taxPendingCV);

        var taxPendingLookup = await _context.TaxPendingDetailsLookup.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(taxPendingLookup);

        var taxPendingRetro = await _context.TaxPendingDetailsRetro.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(taxPendingRetro);

        var taxPendingRV = await _context.TaxPendingDetailsRV.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(taxPendingRV);

        var transMast = await _context.TransMast.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(transMast);

        var transMastArchive = await _context.TransMastArchive.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(transMastArchive);

        var transMastLookup = await _context.TransMastLookup.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(transMastLookup);

        var transMastRV = await _context.TransMastRV.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(transMastRV);

        var transMastCV = await _context.TransMastCV.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(transMastCV);

        // TODO: Uncomment when database table structure is finalized

        //var propertyCertificates = await _context.PropertyCertificates.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        //relatedEntities.AddRange(propertyCertificates);

        var propertyAssessments = await _context.PropertyMastDetails.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(propertyAssessments);

        //var societyDetails = await _context.SocietyDetails.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        //relatedEntities.AddRange(societyDetails);

        return relatedEntities;
    }

    /// <summary>
    /// Marks a collection of entities for soft deletion using the same logic as Repository.DeleteAsync.
    /// Sets MarkedForDeletion to true, MarkedForDeletionDate to current time (if not already set),
    /// IsActive to false, and UpdatedDate to current time for entities implementing BaseEntity.
    /// This method ensures consistency with the deletion logic in the base Repository class.
    /// </summary>
    /// <typeparam name="T">Entity type that implements IHardDeletable</typeparam>
    /// <param name="entities">The entities to mark for deletion</param>
    public void MarkEntitiesForDeletion<T>(IEnumerable<T> entities) where T : class, IHardDeletable
    {
        var deletionTime = DateTime.Now;

        foreach (var entity in entities)
        {
            // Set hard deletion flags
            entity.MarkedForDeletion = true;

            // Only set deletion date if not already set (preserves original deletion timestamp)
            if (!entity.MarkedForDeletionDate.HasValue)
            {
                entity.MarkedForDeletionDate = deletionTime;
            }

            // Set IsActive and UpdatedDate if the entity is a BaseEntity
            if (entity is BaseEntity baseEntity)
            {
                baseEntity.IsActive = false;
                baseEntity.UpdatedDate = deletionTime;
            }

            // Mark entity as modified in EF Core
            _context.Entry(entity).State = EntityState.Modified;
        }
    }
    /// <summary>
    /// Deactivates a collection of BaseEntity-derived entities by setting IsActive = false and UpdatedDate = now.
    /// Does NOT touch MarkedForDeletion or MarkedForDeletionDate.
    /// Used for entities that don't implement IHardDeletable (e.g., PropertySocialDetails, WaterConnectionMaster).
    /// </summary>
    /// <param name="entities">The entities to deactivate</param>
    public void DeactivatePropertyEntities(IEnumerable<BaseEntity> entities)
    {
        var now = DateTime.Now;
        foreach (var entity in entities)
        {
            entity.IsActive = false;
            entity.UpdatedDate = now;
            _context.Entry(entity).State = EntityState.Modified;
        }
    }

    public async Task<CreateBulkPropertyResponseDto?> CreateBulkPropertyAsync(CreateBulkPropertyDto dto, CancellationToken cancellationToken = default)
    {

        // Transaction
        PropertyEntity? property = null;
        PropertyAssessmentEntity? propertyMastDetails = null;
        try
        {

            // Property insert
            property = new PropertyEntity
            {
                TaxZoneId = dto.TaxZoneId,
                WardId = dto.WardId,
                PropertyNo = dto.PropertyNo.Trim(),
                PartitionNo = dto.PartitionNo.Trim(),
                PropertySeqNo = dto.PropertySeqNo,
                PropertyTypeId = dto.PropertyTypeId,
                CategoryId = dto.CategoryId,
                OwnerTitle = string.Empty,
                OwnerTitleEnglish = string.Empty,
                OpenPlot = dto.OpenPlot,
                OwnerName = dto.OwnerName,
                OwnerNameEnglish = dto.OwnerNameEnglish,
                FlatOrShopNo = dto.FlatOrShopNo,
                FlatOrShopNoEnglish = dto.FlatOrShopNoEnglish,
                Address = dto?.Address,
                AddressEnglish = dto?.AddressEnglish,
                Location = dto?.Location,
                LocationEnglish = dto?.LocationEnglish,
                SocietyDetailId = dto?.SocietyDetailId,
                PropertyFloorId = dto?.PropertyFloorId,

                IsActive = true,
                MarkedForDeletion = false,
                CreatedBy = dto?.CreatedBy
            };

            _context.PropertyMast.Add(property);
            var propertySaveResult = await _context.SaveChangesAsync(cancellationToken);

            // Assessment insert 
            propertyMastDetails = new PropertyAssessmentEntity
            {
                PropertyId = property.Id,
                IsActive = true,
                MarkedForDeletion = false,
                CreatedBy = dto?.CreatedBy
            };

            _context.PropertyMastDetails.Add(propertyMastDetails);
            var assessmentSaveResult = await _context.SaveChangesAsync(cancellationToken);

            if (dto != null && dto.ConstructionTypeId != null && dto.TypeOfUseId != null && dto.SubTypeOfUseId != null && dto.ConstructionYear != null) 
            {
                // PropertyDetails insert
                var propertyDetails = new PropertyDetailsEntity
                {
                    PropertyId = property.Id,
                    FloorId = property!.PropertyFloorId!.Value,
                    ConstructionTypeId = dto!.ConstructionTypeId!.Value,
                    TypeOfUseId = dto.TypeOfUseId!.Value,
                    SubTypeOfUseId = dto.SubTypeOfUseId,
                    ConstructionYear = dto.ConstructionYear,
                    IsActive = true,
                    MarkedForDeletion = false,
                    CreatedBy = dto?.CreatedBy
                };

                _context.PropertyDetails.Add(propertyDetails);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return new CreateBulkPropertyResponseDto
            {
                PropertyId = property!.Id,
                Success = true,
                Message = "Property generated successfully."
            };
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Another user modified the same record mid-transaction
            return new CreateBulkPropertyResponseDto
            {
                Success = false,
                Message = $"A concurrency conflict occurred. Please retry. detail: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            // Any unexpected error
            return new CreateBulkPropertyResponseDto
            {
                Success = false,
                Message = $"An unexpected error occurred : {ex.Message}"
            };
        }
    }
    public async Task<PropertyEntity?> CheckBuildingIfExists(CreateBulkPropertyDto dto, CancellationToken cancellationToken = default)
    {
        return await _context.PropertyMast.FirstOrDefaultAsync(x => x.WardId == dto.WardId && x.PropertyNo == dto.PropertyNo && x.PartitionNo == "" && x.MarkedForDeletion==false, cancellationToken);
    }
    public async Task<PropertyCategoryEntity?> GetBuildingCategory(int CategoryId, CancellationToken cancellationToken = default)
    {
        return await _context.PropertyCategoryMaster.FirstOrDefaultAsync(x => x.Id == CategoryId, cancellationToken);
    }
    public async Task<PropertyTypeMasterEntity?> GetAmenityPropertyType(CancellationToken cancellationToken = default)
    {
        return await _context.PropertyTypeMasters.FirstOrDefaultAsync(x => x.PartType == PartTypeConstants.Amenity, cancellationToken);
    }
    public async Task<bool> CheckPropertyIfExists(
     CreateBulkPropertyDto dto,
     CancellationToken cancellationToken = default)
    {
        return await _context.PropertyMast.AnyAsync(
            x => x.WardId == dto.WardId
              && x.PropertyNo == dto.PropertyNo
              && x.PartitionNo == dto.PartitionNo && x.MarkedForDeletion==false,
            cancellationToken);
    }
    public async Task<bool> CheckPropertyFlatIfExists(
  CreateBulkPropertyDto dto,
  CancellationToken cancellationToken = default)
    {
        return await _context.PropertyMast.AnyAsync(
            x => x.WardId == dto.WardId
              && x.PropertyNo == dto.PropertyNo
              && x.SocietyDetailId == dto.SocietyDetailId
              && x.FlatOrShopNo == dto.FlatOrShopNo && x.MarkedForDeletion == false,
            cancellationToken);
    }

}

