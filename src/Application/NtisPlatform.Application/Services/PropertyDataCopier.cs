using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Handles copying property data from combined properties to main property
/// </summary>
public class PropertyDataCopier : IPropertyDataCopier
{
    private readonly IRepository<PropertyDetailsEntity, int> _propertyDetailsRepository;
    private readonly IRepository<PropertyAssessmentEntity, int> _propertyAssessmentRepository;
    private readonly IRepository<RoomWiseSubmissionDetailsEntity, int> _roomWiseSubmissionRepository;
    private readonly IRepository<RoomWiseMinusDataEntity, int> _roomWiseMinusDataRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PropertyDataCopier> _logger;

    public PropertyDataCopier(
        IRepository<PropertyDetailsEntity, int> propertyDetailsRepository,
        IRepository<PropertyAssessmentEntity, int> propertyAssessmentRepository,
        IRepository<RoomWiseSubmissionDetailsEntity, int> roomWiseSubmissionRepository,
        IRepository<RoomWiseMinusDataEntity, int> roomWiseMinusDataRepository,
        IUnitOfWork unitOfWork,
        ILogger<PropertyDataCopier> logger)
    {
        _propertyDetailsRepository = propertyDetailsRepository;
        _propertyAssessmentRepository = propertyAssessmentRepository;
        _roomWiseSubmissionRepository = roomWiseSubmissionRepository;
        _roomWiseMinusDataRepository = roomWiseMinusDataRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task CopyPropertyDataAsync(
        int mainPropertyId,
        List<int> combinePropertyIds,
        int? createdBy,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Update toilet counts
        await UpdateMainPropertyToiletCountsAsync(mainPropertyId, combinePropertyIds, cancellationToken);

        // Step 2: Copy PropertyDetails records
        var propertyDetailsMap = await CopyPropertyDetailsToMainAsync(mainPropertyId, combinePropertyIds, createdBy, cancellationToken);

        // Step 3: Copy RoomWiseSubmissionDetails with proper ID mapping
        var roomWiseSubmissionMap = await CopyRoomWiseSubmissionDetailsAsync(mainPropertyId, combinePropertyIds, propertyDetailsMap, createdBy, cancellationToken);

        // Step 4: Copy RoomWiseMinusData with proper ID mapping
        await CopyRoomWiseMinusDataAsync(roomWiseSubmissionMap, createdBy, cancellationToken);
    }

    public async Task UpdateMainPropertyToiletCountsAsync(
        int mainPropertyId,
        List<int> combinePropertyIds,
        CancellationToken cancellationToken)
    {
        var toiletSums = await _propertyAssessmentRepository.GetQueryable()
            .Where(pmd => combinePropertyIds.Contains(pmd.PropertyId) && pmd.IsActive == true)
            .GroupBy(pmd => 1)
            .Select(g => new
            {
                TotalResidentialToilets = g.Sum(x => x.NoOfResidentialToilets ?? 0),
                TotalCommercialToilets = g.Sum(x => x.NoOfCommercialToilets ?? 0)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (toiletSums == null)
        {
            return;
        }

        var mainPropertyAssessment = await _propertyAssessmentRepository.GetQueryable()
            .Where(pmd => pmd.PropertyId == mainPropertyId && pmd.IsActive == true)
            .FirstOrDefaultAsync(cancellationToken);

        if (mainPropertyAssessment == null)
        {
            return;
        }

        await _propertyAssessmentRepository.GetQueryable()
            .Where(pmd => pmd.PropertyId == mainPropertyId && pmd.IsActive == true)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.NoOfResidentialToilets,
                    (mainPropertyAssessment.NoOfResidentialToilets ?? 0) + toiletSums.TotalResidentialToilets)
                .SetProperty(p => p.NoOfCommercialToilets,
                    (mainPropertyAssessment.NoOfCommercialToilets ?? 0) + toiletSums.TotalCommercialToilets),
                cancellationToken);
    }

    private async Task<Dictionary<int, int>> CopyPropertyDetailsToMainAsync(
        int mainPropertyId,
        List<int> combinePropertyIds,
        int? createdBy,
        CancellationToken cancellationToken)
    {
        var propertyDetailsMap = new Dictionary<int, int>();

        var sourcePropertyDetails = await _propertyDetailsRepository.GetQueryable()
            .Where(pd => combinePropertyIds.Contains(pd.PropertyId) && pd.IsActive == true)
            .ToListAsync(cancellationToken);

        if (sourcePropertyDetails.Count == 0)
        {
            return propertyDetailsMap;
        }

        var newPropertyDetailsList = new List<PropertyDetailsEntity>();
        var sourceIdList = new List<int>();

        foreach (var source in sourcePropertyDetails)
        {
            var newPropertyDetails = new PropertyDetailsEntity
            {
                PropertyId = mainPropertyId,
                FloorId = source.FloorId,
                SubFloorId = source.SubFloorId,
                ConstructionYear = source.ConstructionYear,
                AssessmentYear = source.AssessmentYear,
                ConstructionTypeId = source.ConstructionTypeId,
                TypeOfUseId = source.TypeOfUseId,
                CarpetAreaSqMeter = source.CarpetAreaSqMeter,
                CarpetAreaSqFeet = source.CarpetAreaSqFeet,
                BuiltupAreaSqMeter = source.BuiltupAreaSqMeter,
                BuiltupAreaSqFeet = source.BuiltupAreaSqFeet,
                NoOfRooms = source.NoOfRooms,
                IsTaxable = true,
                MarkedForDeletion = false,
                IsActive = true,
                CreatedBy = createdBy
            };

            newPropertyDetailsList.Add(newPropertyDetails);
            sourceIdList.Add(source.Id);
        }

        await _propertyDetailsRepository.AddRangeAsync(newPropertyDetailsList, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        for (int i = 0; i < sourceIdList.Count; i++)
        {
            propertyDetailsMap[sourceIdList[i]] = newPropertyDetailsList[i].Id;
        }

        return propertyDetailsMap;
    }

    private async Task<Dictionary<int, int>> CopyRoomWiseSubmissionDetailsAsync(
        int mainPropertyId,
        List<int> combinePropertyIds,
        Dictionary<int, int> propertyDetailsMap,
        int? createdBy,
        CancellationToken cancellationToken)
    {
        var roomWiseSubmissionMap = new Dictionary<int, int>();

        // Use projection to avoid SqlNullValueException when IsActive column has NULL in database
        var sourceSubmissionsData = await _roomWiseSubmissionRepository.GetQueryable()
            .Where(rwsd => rwsd.PropertyId.HasValue &&
                          combinePropertyIds.Contains(rwsd.PropertyId.Value) &&
                          rwsd.PropertyDetailsId.HasValue &&
                          rwsd.IsActive == true)
            .Select(rwsd => new
            {
                rwsd.Id,
                rwsd.PropertyDetailsId,
                rwsd.LengthMtr,
                rwsd.WidthMtr,
                rwsd.AreaSqMtr,
                rwsd.HeightMtr,
                rwsd.Base1Mtr,
                rwsd.Base2Mtr,
                rwsd.NoOfRooms,
                rwsd.TotalAreaSqMtr,
                rwsd.Shape,
                rwsd.RoomNo,
                rwsd.OuterYesNo,
                rwsd.RoomType,
                rwsd.SubmissionType,
                rwsd.MinusYesNo
            })
            .ToListAsync(cancellationToken);

        if (sourceSubmissionsData.Count == 0)
        {
            return roomWiseSubmissionMap;
        }

        var newSubmissionsList = new List<RoomWiseSubmissionDetailsEntity>();
        var sourceIdList = new List<int>();

        foreach (var source in sourceSubmissionsData)
        {
            if (!source.PropertyDetailsId.HasValue)
            {
                _logger.LogWarning(
                    "Skipping RoomWiseSubmissionDetails Id={SubmissionId} - PropertyDetailsId is NULL",
                    source.Id);
                continue;
            }

            if (!propertyDetailsMap.TryGetValue(source.PropertyDetailsId.Value, out var newPropertyDetailsId))
            {
                _logger.LogWarning(
                    "Skipping RoomWiseSubmissionDetails Id={SubmissionId} - PropertyDetailsId={PropertyDetailsId} not found in mapping",
                    source.Id, source.PropertyDetailsId.Value);
                continue;
            }

            var newSubmission = new RoomWiseSubmissionDetailsEntity
            {
                PropertyId = mainPropertyId,
                PropertyDetailsId = newPropertyDetailsId,
                LengthMtr = source.LengthMtr,
                WidthMtr = source.WidthMtr,
                AreaSqMtr = source.AreaSqMtr,
                HeightMtr = source.HeightMtr,
                Base1Mtr = source.Base1Mtr,
                Base2Mtr = source.Base2Mtr,
                NoOfRooms = source.NoOfRooms,
                TotalAreaSqMtr = source.TotalAreaSqMtr,
                Shape = source.Shape,
                RoomNo = source.RoomNo,
                OuterYesNo = source.OuterYesNo,
                RoomType = source.RoomType,
                SubmissionType = source.SubmissionType,
                MinusYesNo = source.MinusYesNo,
                MarkedForDeletion = false,
                IsActive = true,
                CreatedBy = createdBy
            };

            newSubmissionsList.Add(newSubmission);
            sourceIdList.Add(source.Id);
        }

        if (newSubmissionsList.Count > 0)
        {
            await _roomWiseSubmissionRepository.AddRangeAsync(newSubmissionsList, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            for (int i = 0; i < sourceIdList.Count; i++)
            {
                roomWiseSubmissionMap[sourceIdList[i]] = newSubmissionsList[i].Id;
            }
        }

        return roomWiseSubmissionMap;
    }

    private async Task CopyRoomWiseMinusDataAsync(
        Dictionary<int, int> roomWiseSubmissionMap,
        int? createdBy,
        CancellationToken cancellationToken)
    {
        if (roomWiseSubmissionMap.Count == 0)
        {
            return;
        }

        var oldSubmissionIds = roomWiseSubmissionMap.Keys.ToList();

        // Use projection to avoid SqlNullValueException when IsActive column has NULL in database
        var sourceMinusData = await _roomWiseMinusDataRepository.GetQueryable()
            .Where(rwmd => oldSubmissionIds.Contains(rwmd.RoomWiseSubmissionId) && rwmd.IsActive == true)
            .Select(rwmd => new
            {
                rwmd.RoomWiseSubmissionId,
                rwmd.LengthMtr,
                rwmd.WidthMtr,
                rwmd.AreaSqMtr,
                rwmd.HeightMtr,
                rwmd.Base1Mtr,
                rwmd.Base2Mtr,
                rwmd.Shape
            })
            .ToListAsync(cancellationToken);

        if (sourceMinusData.Count == 0)
        {
            return;
        }

        var newMinusDataList = new List<RoomWiseMinusDataEntity>();

        foreach (var source in sourceMinusData)
        {
            if (!roomWiseSubmissionMap.TryGetValue(source.RoomWiseSubmissionId, out var newRoomWiseSubmissionId))
            {
                continue;
            }

            var newMinusData = new RoomWiseMinusDataEntity
            {
                RoomWiseSubmissionId = newRoomWiseSubmissionId,
                LengthMtr = source.LengthMtr,
                WidthMtr = source.WidthMtr,
                AreaSqMtr = source.AreaSqMtr,
                HeightMtr = source.HeightMtr,
                Base1Mtr = source.Base1Mtr,
                Base2Mtr = source.Base2Mtr,
                Shape = source.Shape,
                MarkedForDeletion = false,
                IsActive = true,
                CreatedBy = createdBy
            };

            newMinusDataList.Add(newMinusData);
        }

        if (newMinusDataList.Count > 0)
        {
            await _roomWiseMinusDataRepository.AddRangeAsync(newMinusDataList, cancellationToken);
            // Note: Final SaveChanges removed - will be called once at transaction commit
        }
    }
}