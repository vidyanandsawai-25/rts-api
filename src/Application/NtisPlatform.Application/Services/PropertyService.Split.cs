using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Application.Services;

public partial class PropertyService
{
    public async Task<PropertySplitResultDto> SplitProperty(PropertySplitCreateDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.PropertyNo))
        {
            throw new ArgumentException("PROPERTY_NO_REQUIRED");
        }

        if (dto.NoOfSplit <= 0)
        {
            throw new ArgumentException("NO_OF_SPLITS_GREATER_THAN_ZERO");
        }

        if (dto.UserId <= 0)
        {
            throw new ArgumentException("USER_ID_GREATER_THAN_ZERO");
        }

        if (dto.WardId <= 0)
        {
            throw new ArgumentException("WARD_ID_GREATER_THAN_ZERO");
        }

        // Optimized: Check user existence and ward allocation in a single DB query
        var validationStatus = await _userRepository.GetQueryable()
            .AsNoTracking()
            .Where(u => u.Id == dto.UserId)
            .Select(u => new
            {
                IsUserValid = u.IsActive && !u.MarkedForDeletion,
                HasWard = _wardAllocationRepository.GetQueryable()
                            .Any(w => w.UserId == u.Id && w.WardId == dto.WardId && w.IsActive)
            })
            .FirstOrDefaultAsync(ct);

        if (validationStatus == null || !validationStatus.IsUserValid)
        {
            throw new UnauthorizedAccessException("USER_NOT_EXIST_OR_INACTIVE");
        }

        if (!validationStatus.HasWard)
        {
            throw new UnauthorizedAccessException("WARD_NOT_ALLOCATED_TO_USER");
        }

        await _unitOfWork.BeginTransactionAsync(ct);

        try
        {
            var mainPropertyQuery = _repository.GetQueryable()
                .Where(x => x.PropertyNo == dto.PropertyNo && x.WardId == dto.WardId && x.IsActive);

            if (dto.IsPartitionProperty && !string.IsNullOrWhiteSpace(dto.PartitionNo))
            {
                mainPropertyQuery = mainPropertyQuery.Where(x => x.PartitionNo == dto.PartitionNo);
            }

            var mainProperty = await mainPropertyQuery.FirstOrDefaultAsync(ct);

            if (mainProperty == null)
            {
                throw new InvalidOperationException("BASE_PROPERTY_NOT_EXIST_OR_INACTIVE");
            }

            // Fetch Ward for UPIC generation
            var ward = await _wardRepository.GetByIdAsync(mainProperty.WardId, ct);

            var newProperties = new List<PropertyEntity>();
            var skippedList = new List<PropertySpiltResponseDto>();
            var createdList = new List<PropertySpiltResponseDto>();

            if (dto.IsPartitionProperty)
            {
                if (string.IsNullOrWhiteSpace(dto.PartitionNo))
                {
                    throw new ArgumentException("PARTITION_NO_REQUIRED");
                }

                // Base partition string. Usually determined from dto.PartitionNo if specified.
                string basePartitionNo = dto.PartitionNo;

                // --- ALPHABETIC INCREMENT LOGIC ---
                // 1. Determine the true Base Partition Number by stripping ALL trailing uppercase letters
                int suffixStartIndex = basePartitionNo.Length;

                while (suffixStartIndex > 0 && char.IsLetter(basePartitionNo[suffixStartIndex - 1]) && char.IsUpper(basePartitionNo[suffixStartIndex - 1]))
                {
                    suffixStartIndex--;
                }
                basePartitionNo = basePartitionNo.Substring(0, suffixStartIndex);

                // 2. Fetch all existing properties for partition numbers that start with the base number
                var relatedProperties = await _repository.GetQueryable()
                    .Where(x => x.WardId == dto.WardId &&
                                x.PropertyNo == mainProperty.PropertyNo &&
                                x.IsActive &&
                                x.PartitionNo!.StartsWith(basePartitionNo))
                    .Select(x => new { x.Id, x.PropertyNo, x.PartitionNo })
                    .ToListAsync(ct);

                // 3. Extract existing string suffixes and find the maximum one
                string currentMaxSuffix = string.Empty;

                var existingSuffixes = new List<string>();

                foreach (var prop in relatedProperties)
                {
                    if (prop.PartitionNo != null && prop.PartitionNo.Length > basePartitionNo.Length)
                    {
                        var suffix = prop.PartitionNo.Substring(basePartitionNo.Length);
                        if (suffix.All(char.IsLetter) && suffix.All(char.IsUpper))
                        {
                            existingSuffixes.Add(suffix);

                            // Add existing split to the skipped list
                            skippedList.Add(new PropertySpiltResponseDto
                            {
                                PropertyId = prop.Id,
                                GeneratedPropertyNo = prop.PropertyNo!,
                                ParentPropertyNo = mainProperty.PropertyNo!,
                                GeneratedPartitionNo = prop.PartitionNo,
                                ParentPartitionNo = mainProperty.PartitionNo,
                                IsSplit = true,
                                IsPartitionProperty = true
                            });
                        }
                    }
                }

                if (existingSuffixes.Any())
                {
                    currentMaxSuffix = existingSuffixes
                        .OrderBy(s => s.Length)
                        .ThenBy(s => s)
                        .Last();
                }

                // 4. Determine the starting suffix for our loop
                string nextSuffix = GetNextAlphabeticSuffix(currentMaxSuffix);

                // 5. Generate the requested number of split partitions
                for (int i = 0; i < dto.NoOfSplit; i++)
                {
                    if (nextSuffix.Length > 3)
                    {
                        throw new InvalidOperationException("MAX_SPLIT_LIMIT_REACHED");
                    }

                    string newPartitionNo = $"{basePartitionNo}{nextSuffix}";

                    if (dto.IsMainPropertyDataAttach)
                    {
                        // Clone main property and copy all its details for property split
                        var newProperty = _mapper.Map<PropertyEntity>(mainProperty);

                        // Override cloned fields with specific values for the new split property
                        newProperty.PropertyNo = mainProperty.PropertyNo;
                        newProperty.UPICId = $"{ward?.WardNo}{mainProperty.WardId}{mainProperty.PropertyNo}{newPartitionNo}{DateTime.Now:HHmmss}";
                        newProperty.IsActive = dto.IsActive;
                        newProperty.PartitionNo = newPartitionNo;
                        newProperty.MarkedForDeletion = false;
                        newProperty.CreatedBy = dto.CreatedBy;
                        newProperty.UpdatedBy = null;

                        newProperties.Add(newProperty);
                    }
                    else
                    {
                        // Create basic split property data without copying main property details
                        var newProperty = new PropertyEntity
                        {
                            WardId = mainProperty.WardId,
                            TaxZoneId = mainProperty.TaxZoneId,
                            PropertyNo = mainProperty.PropertyNo,
                            PropertySeqNo = mainProperty.PropertySeqNo,
                            OwnerName = "धारक",
                            OwnerNameEnglish = "The Holder",
                            PartitionNo = newPartitionNo,
                            UPICId = $"{ward?.WardNo}{mainProperty.WardId}{mainProperty.PropertyNo}{newPartitionNo}{DateTime.Now:HHmmss}",
                            IsActive = dto.IsActive,
                            MarkedForDeletion = false,
                            CreatedBy = dto.CreatedBy
                        };

                        newProperties.Add(newProperty);
                    }

                    createdList.Add(new PropertySpiltResponseDto
                    {
                        GeneratedPropertyNo = mainProperty.PropertyNo!,
                        ParentPropertyNo = mainProperty.PropertyNo!,
                        GeneratedPartitionNo = newPartitionNo,
                        ParentPartitionNo = mainProperty.PartitionNo,
                        IsSplit = true,
                        IsPartitionProperty = true
                    });

                    // Advance the suffix for the next iteration
                    nextSuffix = GetNextAlphabeticSuffix(nextSuffix);
                }
            }
            else
            {
                // 1. Determine the true Base Property Number by stripping ALL trailing uppercase letters
                string basePropertyNo = dto.PropertyNo;
                int suffixStartIndex = basePropertyNo.Length;

                while (suffixStartIndex > 0 && char.IsLetter(basePropertyNo[suffixStartIndex - 1]) && char.IsUpper(basePropertyNo[suffixStartIndex - 1]))
                {
                    suffixStartIndex--;
                }
                basePropertyNo = basePropertyNo.Substring(0, suffixStartIndex);

                // 2. Fetch all existing properties for property numbers that start with the base number
                var relatedProperties = await _repository.GetQueryable()
                    .Where(x => x.WardId == dto.WardId &&
                                x.IsActive &&
                                x.PropertyNo!.StartsWith(basePropertyNo))
                    .Select(x => new { x.Id, x.PropertyNo, x.PartitionNo })
                    .ToListAsync(ct);

                // 3. Extract existing string suffixes and find the maximum one
                string currentMaxSuffix = string.Empty;

                var existingSuffixes = new List<string>();

                foreach (var prop in relatedProperties)
                {
                    if (prop.PropertyNo != null && prop.PropertyNo.Length > basePropertyNo.Length)
                    {
                        var suffix = prop.PropertyNo.Substring(basePropertyNo.Length);
                        if (suffix.All(char.IsLetter) && suffix.All(char.IsUpper))
                        {
                            existingSuffixes.Add(suffix);

                            // Add existing split to the skippedList 
                            skippedList.Add(new PropertySpiltResponseDto
                            {
                                PropertyId = prop.Id,
                                GeneratedPropertyNo = prop.PropertyNo,
                                ParentPropertyNo = mainProperty.PropertyNo!,
                                GeneratedPartitionNo = prop.PartitionNo,
                                ParentPartitionNo = mainProperty.PartitionNo,
                                IsSplit = true,
                                IsPartitionProperty = false
                            });
                        }
                    }
                }

                if (existingSuffixes.Any())
                {
                    currentMaxSuffix = existingSuffixes
                        .OrderBy(s => s.Length)
                        .ThenBy(s => s)
                        .Last();
                }

                // 4. Determine the starting suffix for our loop
                string nextSuffix = GetNextAlphabeticSuffix(currentMaxSuffix);

                // 5. Generate the requested number of split properties
                for (int i = 0; i < dto.NoOfSplit; i++)
                {
                    if (nextSuffix.Length > 3)
                    {
                        throw new InvalidOperationException("MAX_SPLIT_LIMIT_REACHED");
                    }

                    string newPropertyNo = $"{basePropertyNo}{nextSuffix}";

                    if (dto.IsMainPropertyDataAttach)
                    {
                        // Clone main property and copy all its details for property split
                        var newProperty = _mapper.Map<PropertyEntity>(mainProperty);

                        // Override cloned fields with specific values for the new split property
                        newProperty.PropertyNo = newPropertyNo;
                        newProperty.UPICId = $"{ward?.WardNo}{mainProperty.WardId}{newPropertyNo}{mainProperty.PartitionNo}{DateTime.Now:HHmmss}";
                        newProperty.IsActive = dto.IsActive;
                        newProperty.MarkedForDeletion = false;
                        newProperty.CreatedBy = dto.CreatedBy;
                        newProperty.UpdatedBy = null;

                        newProperties.Add(newProperty);
                    }
                    else
                    {
                        // Create basic split property data without copying main property details
                        var newProperty = new PropertyEntity
                        {
                            WardId = mainProperty.WardId,
                            TaxZoneId = mainProperty.TaxZoneId,
                            PropertyNo = newPropertyNo,
                            PropertySeqNo = mainProperty.PropertySeqNo,
                            OwnerName = "धारक",
                            OwnerNameEnglish = "The Holder",
                            PartitionNo = string.Empty,
                            UPICId = $"{ward?.WardNo}{mainProperty.WardId}{newPropertyNo}{mainProperty.PartitionNo}{DateTime.Now:HHmmss}",
                            IsActive = dto.IsActive,
                            MarkedForDeletion = false,
                            CreatedBy = dto.CreatedBy
                        };

                        newProperties.Add(newProperty);
                    }

                    createdList.Add(new PropertySpiltResponseDto
                    {
                        GeneratedPropertyNo = newPropertyNo,
                        ParentPropertyNo = mainProperty.PropertyNo!,
                        GeneratedPartitionNo = dto.IsMainPropertyDataAttach ? mainProperty.PartitionNo : string.Empty,
                        ParentPartitionNo = mainProperty.PartitionNo,
                        IsSplit = true,
                        IsPartitionProperty = false
                    });

                    // Advance the suffix for the next iteration
                    nextSuffix = GetNextAlphabeticSuffix(nextSuffix);
                }
            }

            // 6. Bulk insert the new properties
            await _repository.AddRangeAsync(newProperties, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            // 7. Get Map PropertyMapMaster (Do not add to master table)
            var propertyMapMaster = await _propertyMapMasterRepository.GetQueryable()
                .FirstOrDefaultAsync(x => x.MappingCategory == "SPLIT" && x.IsActive, ct);

            int propertyMapId = propertyMapMaster?.Id ?? 2;

            // Assign the newly generated IDs to the newly created items
            var propertyMapDetails = new List<PropertyMapDetailEntity>();
            for (int i = 0; i < createdList.Count; i++)
            {
                createdList[i].PropertyId = newProperties[i].Id;

                // Add Map PropertyMapDetails 
                var propertyMapDetail = new PropertyMapDetailEntity
                {
                    PropertyMapId = propertyMapId,
                    PropertySide = mainProperty.PropertyMastOldId.HasValue ? "OLD" : "NEW",
                    PropertyIdNew = newProperties[i].Id,
                    PropertyIdOld = mainProperty.PropertyMastOldId,
                    PropertyNoNew = $"New-{newProperties[i].PropertyNo}",
                    Status = "MODIFIED",
                    Remark = $"SPLIT: property {mainProperty.PropertyNo} mapped to new property {newProperties[i].PropertyNo}{(string.IsNullOrEmpty(newProperties[i].PartitionNo) ? "" : $"-{newProperties[i].PartitionNo}")}",
                    IsCurrent = true,
                    CreatedBy = dto.CreatedBy,
                    CreatedDate = DateTime.Now
                };

                propertyMapDetails.Add(propertyMapDetail);
            }

            if (propertyMapDetails.Count > 0)
            {
                await _propertyMapDetailRepository.AddRangeAsync(propertyMapDetails, ct);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);

            return new PropertySplitResultDto { Skipped = skippedList, Created = createdList };
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }

        // --- LOCAL HELPER FUNCTION ---
        string GetNextAlphabeticSuffix(string currentSuffix)
        {
            if (string.IsNullOrEmpty(currentSuffix)) return "A";

            char[] chars = currentSuffix.ToCharArray();
            for (int i = chars.Length - 1; i >= 0; i--)
            {
                if (chars[i] < 'Z')
                {
                    chars[i]++;
                    return new string(chars);
                }

                chars[i] = 'A';
            }

            return "A" + new string(chars);
        }
    }

    public async Task<List<PropertyHierarchyResponseDto>> GetPropertyListAsync(PropertyLookupRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.PropertyNo))
            throw new ArgumentException("PROPERTY_NO_REQUIRED");

        if (request.UserId <= 0)
            throw new ArgumentException("USER_ID_GREATER_THAN_ZERO");

        if (request.WardId <= 0)
            throw new ArgumentException("WARD_ID_GREATER_THAN_ZERO");

        var validationStatus = await _userRepository.GetQueryable()
            .AsNoTracking()
            .Where(u => u.Id == request.UserId)
            .Select(u => new
            {
                IsUserValid = u.IsActive && !u.MarkedForDeletion,
                HasWard = _wardAllocationRepository.GetQueryable()
                            .Any(w => w.UserId == u.Id && w.WardId == request.WardId && w.IsActive)
            })
            .FirstOrDefaultAsync(ct);

        if (validationStatus == null || !validationStatus.IsUserValid)
            throw new UnauthorizedAccessException("USER_NOT_EXIST_OR_INACTIVE");

        if (!validationStatus.HasWard)
            throw new UnauthorizedAccessException("WARD_NOT_ALLOCATED_TO_USER");

        string basePropertyNo = request.PropertyNo;
        int suffixStartIndex = basePropertyNo.Length;

        while (suffixStartIndex > 0 && char.IsLetter(basePropertyNo[suffixStartIndex - 1]) && char.IsUpper(basePropertyNo[suffixStartIndex - 1]))
        {
            suffixStartIndex--;
        }
        basePropertyNo = basePropertyNo.Substring(0, suffixStartIndex);

        var propertiesQuery = from p in _repository.GetQueryable()
                              join c in _categoryRepository.GetQueryable() on p.CategoryId equals c.Id into catGroup
                              from c in catGroup.DefaultIfEmpty()
                              where p.WardId == request.WardId &&
                                    p.IsActive &&
                                    p.PropertyNo != null &&
                                    p.PropertyNo.StartsWith(basePropertyNo)
                              select new
                              {
                                  PropertyId = p.Id,
                                  p.PropertyNo,
                                  p.PartitionNo,
                                  p.OwnerName,
                                  PropCategoryDesc = c != null ? c.PropertyCategoryName : string.Empty
                              };

        var relatedProperties = await propertiesQuery.ToListAsync(ct);

        var result = new List<PropertyHierarchyResponseDto>();
        var mainPropDict = new Dictionary<string, PropertyHierarchyResponseDto>();

        foreach (var prop in relatedProperties)
        {
            if (prop.PropertyNo == null) continue;

            bool isBaseProperty = prop.PropertyNo == basePropertyNo;
            bool isPropertySplit = false;

            if (prop.PropertyNo.Length > basePropertyNo.Length)
            {
                var propSuffix = prop.PropertyNo.Substring(basePropertyNo.Length);
                if (propSuffix.All(char.IsLetter) && propSuffix.All(char.IsUpper))
                {
                    isPropertySplit = true;
                }
            }

            if (!isBaseProperty && !isPropertySplit) continue;

            if (!string.IsNullOrWhiteSpace(request.PartitionNo))
            {
                string basePartitionNo = request.PartitionNo;
                int partSuffixIndex = basePartitionNo.Length;
                while (partSuffixIndex > 0 && char.IsLetter(basePartitionNo[partSuffixIndex - 1]) && char.IsUpper(basePartitionNo[partSuffixIndex - 1]))
                {
                    partSuffixIndex--;
                }
                basePartitionNo = basePartitionNo.Substring(0, partSuffixIndex);

                if (prop.PartitionNo == null || !prop.PartitionNo.StartsWith(basePartitionNo))
                    continue;

                bool isBasePartition = prop.PartitionNo == basePartitionNo;
                bool isPartitionSplit = false;
                if (prop.PartitionNo.Length > basePartitionNo.Length)
                {
                    var partSuffix = prop.PartitionNo.Substring(basePartitionNo.Length);
                    if (partSuffix.All(char.IsLetter) && partSuffix.All(char.IsUpper))
                    {
                        isPartitionSplit = true;
                    }
                }

                if (!isBasePartition && !isPartitionSplit) continue;

                string dictKey = $"{basePropertyNo}_{basePartitionNo}";
                if (!mainPropDict.TryGetValue(dictKey, out var hierarchy))
                {
                    hierarchy = new PropertyHierarchyResponseDto
                    {
                        PropertyNo = basePropertyNo,
                        PartitionNo = basePartitionNo
                    };
                    mainPropDict[dictKey] = hierarchy;
                    result.Add(hierarchy);
                }

                if (isBasePartition)
                {
                    hierarchy.PropertyId = prop.PropertyId;
                    hierarchy.PropCategoryDesc = prop.PropCategoryDesc;
                    hierarchy.OwnerName = prop.OwnerName;
                }
                else if (isPartitionSplit)
                {
                    var splitObj = hierarchy.Splits.FirstOrDefault(s => s.PartitionNo == prop.PartitionNo);
                    if (splitObj == null)
                    {
                        splitObj = new PropertySplitHierarchyDto
                        {
                            PropertyNo = prop.PropertyNo,
                            PartitionNo = prop.PartitionNo,
                            PropertyId = prop.PropertyId,
                            PropCategoryDesc = prop.PropCategoryDesc,
                            OwnerName = prop.OwnerName
                        };
                        hierarchy.Splits.Add(splitObj);
                    }
                }
            }
            else
            {
                if (!mainPropDict.TryGetValue(basePropertyNo, out var hierarchy))
                {
                    hierarchy = new PropertyHierarchyResponseDto
                    {
                        PropertyNo = basePropertyNo
                    };
                    mainPropDict[basePropertyNo] = hierarchy;
                    result.Add(hierarchy);
                }

                if (isBaseProperty)
                {
                    if (string.IsNullOrWhiteSpace(prop.PartitionNo))
                    {
                        hierarchy.PropertyId = prop.PropertyId;
                        hierarchy.PropCategoryDesc = prop.PropCategoryDesc;
                        hierarchy.OwnerName = prop.OwnerName;
                    }
                    else
                    {
                        hierarchy.Partitions.Add(new PropertyPartitionDetailDto
                        {
                            PropertyId = prop.PropertyId,
                            PartitionNo = prop.PartitionNo,
                            PropCategoryDesc = prop.PropCategoryDesc,
                            OwnerName = prop.OwnerName
                        });
                    }
                }
                else if (isPropertySplit)
                {
                    var splitObj = hierarchy.Splits.FirstOrDefault(s => s.PropertyNo == prop.PropertyNo);
                    if (splitObj == null)
                    {
                        splitObj = new PropertySplitHierarchyDto
                        {
                            PropertyNo = prop.PropertyNo
                        };
                        hierarchy.Splits.Add(splitObj);
                    }

                    if (string.IsNullOrWhiteSpace(prop.PartitionNo))
                    {
                        splitObj.PropertyId = prop.PropertyId;
                        splitObj.PropCategoryDesc = prop.PropCategoryDesc;
                        splitObj.OwnerName = prop.OwnerName;
                    }
                    else
                    {
                        splitObj.Partitions.Add(new PropertyPartitionDetailDto
                        {
                            PropertyId = prop.PropertyId,
                            PartitionNo = prop.PartitionNo,
                            PropCategoryDesc = prop.PropCategoryDesc,
                            OwnerName = prop.OwnerName
                        });
                    }
                }
            }
        }

        if (!result.Any())
            throw new InvalidOperationException("No split properties found for the given criteria.");

        foreach (var h in result)
        {
            h.Splits = h.Splits.OrderBy(s => s.PropertyNo).ToList();
            foreach (var s in h.Splits)
            {
                s.Partitions = s.Partitions.OrderBy(p => p.PartitionNo).ToList();
            }
            h.Partitions = h.Partitions.OrderBy(p => p.PartitionNo).ToList();
        }

        return result;
    }
}