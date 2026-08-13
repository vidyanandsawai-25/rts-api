using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;
using NtisPlatform.Application.DTOs.Asset_Management.SubUnitsDetails;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Asset_Management;

/// <summary>
/// Tests for DirectRoomRegistrationDto and PropertyGroupDto (direct room registration action DTOs).
/// ParentAssetId/FloorId/ConstructionTypeId/TypeOfUseId are non-nullable ints - a struct can never
/// be "missing", so a bare [Required] can never fire for them; each now carries a paired
/// [Range(1, int.MaxValue)] (reusing the existing "_Required" error key, no new key introduced) so
/// that omitted/zero/negative values are actually rejected.
/// ConstructionYear is a non-nullable string defaulting to string.Empty, so its [Required]
/// IS meaningful (RequiredAttribute.AllowEmptyStrings defaults to false).
/// PropertyGroups/Rooms are non-nullable List&lt;T&gt; properties defaulting to `new()`, so their
/// [Required] only ever fires if the list reference is explicitly nulled out (e.g. by JSON binding
/// an explicit `null`) - it provides no protection against an empty (but non-null) list. That gap
/// is out of scope here (it wasn't part of the reviewed issue set for this file).
/// </summary>
public class DirectRoomRegistrationDtoTests
{
    #region DirectRoomRegistrationDto

    [Fact]
    public void DirectRoomRegistrationDto_WithValidData_IsValid()
    {
        var dto = new DirectRoomRegistrationDto
        {
            ParentAssetId = 1,
            FloorId = 2,
            DepartmentId = 3,
            RentInformation = new RentInformationDto { LeaseRentType = "Fixed" },
            PropertyGroups = new List<PropertyGroupDto>
            {
                new() { ConstructionYear = "2020", ConstructionTypeId = 1, TypeOfUseId = 1 }
            }
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void DirectRoomRegistrationDto_WithZeroParentAssetId_IsInvalid()
    {
        var dto = new DirectRoomRegistrationDto
        {
            ParentAssetId = 0,
            FloorId = 2,
            PropertyGroups = new List<PropertyGroupDto>
            {
                new() { ConstructionYear = "2020", ConstructionTypeId = 1, TypeOfUseId = 1 }
            }
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(DirectRoomRegistrationDto.ParentAssetId))
            && r.ErrorMessage == "AMS_DirectRoomRegistration_ParentAssetId_Required");
    }

    [Fact]
    public void DirectRoomRegistrationDto_WithNegativeParentAssetId_IsInvalid()
    {
        var dto = new DirectRoomRegistrationDto
        {
            ParentAssetId = -1,
            FloorId = 2,
            PropertyGroups = new List<PropertyGroupDto>
            {
                new() { ConstructionYear = "2020", ConstructionTypeId = 1, TypeOfUseId = 1 }
            }
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(DirectRoomRegistrationDto.ParentAssetId))
            && r.ErrorMessage == "AMS_DirectRoomRegistration_ParentAssetId_Required");
    }

    [Fact]
    public void DirectRoomRegistrationDto_WithValidParentAssetId_IsValid()
    {
        var dto = new DirectRoomRegistrationDto
        {
            ParentAssetId = 1,
            FloorId = 2,
            PropertyGroups = new List<PropertyGroupDto>
            {
                new() { ConstructionYear = "2020", ConstructionTypeId = 1, TypeOfUseId = 1 }
            }
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void DirectRoomRegistrationDto_WithZeroFloorId_IsInvalid()
    {
        var dto = new DirectRoomRegistrationDto
        {
            ParentAssetId = 1,
            FloorId = 0,
            PropertyGroups = new List<PropertyGroupDto>
            {
                new() { ConstructionYear = "2020", ConstructionTypeId = 1, TypeOfUseId = 1 }
            }
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(DirectRoomRegistrationDto.FloorId))
            && r.ErrorMessage == "AMS_DirectRoomRegistration_FloorId_Required");
    }

    [Fact]
    public void DirectRoomRegistrationDto_WithNegativeFloorId_IsInvalid()
    {
        var dto = new DirectRoomRegistrationDto
        {
            ParentAssetId = 1,
            FloorId = -5,
            PropertyGroups = new List<PropertyGroupDto>
            {
                new() { ConstructionYear = "2020", ConstructionTypeId = 1, TypeOfUseId = 1 }
            }
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(DirectRoomRegistrationDto.FloorId))
            && r.ErrorMessage == "AMS_DirectRoomRegistration_FloorId_Required");
    }

    [Fact]
    public void DirectRoomRegistrationDto_WithValidFloorId_IsValid()
    {
        var dto = new DirectRoomRegistrationDto
        {
            ParentAssetId = 1,
            FloorId = 7,
            PropertyGroups = new List<PropertyGroupDto>
            {
                new() { ConstructionYear = "2020", ConstructionTypeId = 1, TypeOfUseId = 1 }
            }
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void DirectRoomRegistrationDto_WithNullPropertyGroups_IsInvalid()
    {
        var dto = new DirectRoomRegistrationDto
        {
            ParentAssetId = 1,
            FloorId = 2,
            PropertyGroups = null!
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(DirectRoomRegistrationDto.PropertyGroups))
            && r.ErrorMessage == "AMS_DirectRoomRegistration_PropertyGroups_Required");
    }

    [Fact]
    public void DirectRoomRegistrationDto_Defaults_PropertyGroupsIsEmptyList_DepartmentIdAndRentInformationAreNull()
    {
        var dto = new DirectRoomRegistrationDto();

        Assert.NotNull(dto.PropertyGroups);
        Assert.Empty(dto.PropertyGroups);
        Assert.Null(dto.DepartmentId);
        Assert.Null(dto.RentInformation);
    }

    [Fact]
    public void DirectRoomRegistrationDto_PropertiesGetAndSetCorrectly()
    {
        var rentInformation = new RentInformationDto { LeaseRentType = "Fixed", RentAmount = 5000m };
        var propertyGroups = new List<PropertyGroupDto>
        {
            new() { ConstructionYear = "2021", ConstructionTypeId = 2, TypeOfUseId = 3 }
        };

        var dto = new DirectRoomRegistrationDto
        {
            ParentAssetId = 10,
            FloorId = 20,
            DepartmentId = 30,
            RentInformation = rentInformation,
            PropertyGroups = propertyGroups
        };

        Assert.Equal(10, dto.ParentAssetId);
        Assert.Equal(20, dto.FloorId);
        Assert.Equal(30, dto.DepartmentId);
        Assert.Same(rentInformation, dto.RentInformation);
        Assert.Same(propertyGroups, dto.PropertyGroups);
    }

    #endregion

    #region PropertyGroupDto

    [Fact]
    public void PropertyGroupDto_WithValidData_IsValid()
    {
        var dto = new PropertyGroupDto
        {
            ConstructionYear = "2020",
            ConstructionTypeId = 1,
            TypeOfUseId = 1,
            SubTypeOfUseId = 2,
            Rooms = new List<RoomDetailDto> { new() { RoomNo = "R-1" } }
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void PropertyGroupDto_WithDefaultConstructionYear_IsInvalid()
    {
        // ConstructionYear is a non-nullable string defaulting to string.Empty - unlike the int
        // [Required] properties above, RequiredAttribute.AllowEmptyStrings defaults to false, so
        // an untouched (empty-string) ConstructionYear genuinely fails [Required] here.
        var dto = new PropertyGroupDto
        {
            ConstructionTypeId = 1,
            TypeOfUseId = 1,
            Rooms = new List<RoomDetailDto>()
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(PropertyGroupDto.ConstructionYear))
            && r.ErrorMessage == "AMS_PropertyGroup_ConstructionYear_Required");
    }

    [Fact]
    public void PropertyGroupDto_WithZeroConstructionTypeId_IsInvalid()
    {
        var dto = new PropertyGroupDto
        {
            ConstructionYear = "2020",
            ConstructionTypeId = 0,
            TypeOfUseId = 1,
            Rooms = new List<RoomDetailDto>()
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(PropertyGroupDto.ConstructionTypeId))
            && r.ErrorMessage == "AMS_PropertyGroup_ConstructionTypeId_Required");
    }

    [Fact]
    public void PropertyGroupDto_WithNegativeConstructionTypeId_IsInvalid()
    {
        var dto = new PropertyGroupDto
        {
            ConstructionYear = "2020",
            ConstructionTypeId = -2,
            TypeOfUseId = 1,
            Rooms = new List<RoomDetailDto>()
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(PropertyGroupDto.ConstructionTypeId))
            && r.ErrorMessage == "AMS_PropertyGroup_ConstructionTypeId_Required");
    }

    [Fact]
    public void PropertyGroupDto_WithZeroTypeOfUseId_IsInvalid()
    {
        var dto = new PropertyGroupDto
        {
            ConstructionYear = "2020",
            ConstructionTypeId = 1,
            TypeOfUseId = 0,
            Rooms = new List<RoomDetailDto>()
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(PropertyGroupDto.TypeOfUseId))
            && r.ErrorMessage == "AMS_PropertyGroup_TypeOfUseId_Required");
    }

    [Fact]
    public void PropertyGroupDto_WithNegativeTypeOfUseId_IsInvalid()
    {
        var dto = new PropertyGroupDto
        {
            ConstructionYear = "2020",
            ConstructionTypeId = 1,
            TypeOfUseId = -3,
            Rooms = new List<RoomDetailDto>()
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(PropertyGroupDto.TypeOfUseId))
            && r.ErrorMessage == "AMS_PropertyGroup_TypeOfUseId_Required");
    }

    [Fact]
    public void PropertyGroupDto_WithNullRooms_IsInvalid()
    {
        var dto = new PropertyGroupDto
        {
            ConstructionYear = "2020",
            ConstructionTypeId = 1,
            TypeOfUseId = 1,
            Rooms = null!
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(PropertyGroupDto.Rooms))
            && r.ErrorMessage == "AMS_PropertyGroup_Rooms_Required");
    }

    [Fact]
    public void PropertyGroupDto_Defaults_RoomsIsEmptyList_SubTypeOfUseIdIsNull()
    {
        var dto = new PropertyGroupDto();

        Assert.NotNull(dto.Rooms);
        Assert.Empty(dto.Rooms);
        Assert.Null(dto.SubTypeOfUseId);
        Assert.Equal(string.Empty, dto.ConstructionYear);
    }

    #endregion

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
