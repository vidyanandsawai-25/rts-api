using AutoMapper;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Application.Mappings;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using AppVR = NtisPlatform.Application.Models.ValidationResult;

namespace NtisPlatform.Tests.Application;

public class PenaltyRuleServiceTests
{
    private readonly Mock<IRepository<PenaltyRuleMasterEntity, int>> _repo;
    private readonly Mock<IUnitOfWork> _uow;
    private readonly IMapper _mapper;
    private readonly Mock<IReferenceValidationService> _refVal;
    private readonly Mock<IHardDeleteCleanupService> _cleanup;
    private readonly PenaltyRuleService _svc;

    public PenaltyRuleServiceTests()
    {
        _repo    = new Mock<IRepository<PenaltyRuleMasterEntity, int>>();
        _uow     = new Mock<IUnitOfWork>();
        _refVal  = new Mock<IReferenceValidationService>();
        _cleanup = new Mock<IHardDeleteCleanupService>();

        _refVal.Setup(x => x.ValidateReferencesAsync<PenaltyRuleMasterEntity>(
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(AppVR.Success());
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _mapper = new MapperConfiguration(
            cfg => cfg.AddProfile<PenaltyRuleMappingProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance).CreateMapper();

        _svc = new PenaltyRuleService(_repo.Object, _uow.Object, _mapper, _refVal.Object);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private PenaltyRuleController MakeController(Mock<IPenaltyRuleService>? svc = null) =>
        new(svc?.Object ?? new Mock<IPenaltyRuleService>().Object,
            _cleanup.Object, _refVal.Object,
            new Mock<ILogger<PenaltyRuleController>>().Object);

    private static IList<System.ComponentModel.DataAnnotations.ValidationResult> Validate(object obj)
    {
        var r = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        Validator.TryValidateObject(obj, new ValidationContext(obj), r, true);
        return r;
    }

    private void MockRepo(params PenaltyRuleMasterEntity[] items) =>
        _repo.Setup(r => r.GetQueryable()).Returns(items.ToList().BuildMockDbSet().Object);

    private void MockGetById(int id, PenaltyRuleMasterEntity? entity) =>
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

    // ── MemberData sources ────────────────────────────────────────────────────

    public static IEnumerable<object?[]> CreateCodeCases => new[]
    {
        new object?[] { null,                "Penaltyrule_Code_Required"  },
        new object?[] { "",                  "Penaltyrule_Code_Required"  },
        new object?[] { new string('A', 51), "Penaltyrule_Code_MaxLen_50" },
    };
    public static IEnumerable<object?[]> CreateNameCases => new[]
    {
        new object?[] { null,                 "Penaltyrule_Name_Required"     },
        new object?[] { "",                   "Penaltyrule_Name_Required"     },
        new object?[] { new string('A', 101), "Penaltyrule_Name_MaxLen_100"   },
    };
    public static IEnumerable<object?[]> CreateCalcCases => new[]
    {
        new object?[] { null, "Penaltyrule_CalculationType_Required" },
        new object?[] { "",   "Penaltyrule_CalculationType_Required" },
    };

    // ── Entity ────────────────────────────────────────────────────────────────

    [Fact]
    public void Entity_Properties_WorkCorrectly()
    {
        var d = DateTime.UtcNow;
        var e = new PenaltyRuleMasterEntity
        {
            Id = 1, PenaltyCode = "PEN001", PenaltyName = "Late Payment Penalty",
            CalculationType = "Percentage", PenaltyValue = 10.5m, GracePeriodDays = 5,
            IsActive = true, CreatedBy = 1, CreatedDate = d, UpdatedBy = 2, UpdatedDate = d,
            MarkedForDeletion = true, MarkedForDeletionDate = d
        };
        Assert.Equal(1, e.Id);
        Assert.Equal("PEN001", e.PenaltyCode);
        Assert.Equal("Late Payment Penalty", e.PenaltyName);
        Assert.Equal("Percentage", e.CalculationType);
        Assert.Equal(10.5m, e.PenaltyValue);
        Assert.Equal(5, e.GracePeriodDays);
        Assert.True(e.IsActive);
        Assert.Equal(1, e.CreatedBy);
        Assert.Equal(d, e.CreatedDate);
        Assert.Equal(2, e.UpdatedBy);
        Assert.Equal(d, e.UpdatedDate);
        Assert.True(e.MarkedForDeletion);
        Assert.Equal(d, e.MarkedForDeletionDate);
    }

    [Fact]
    public void Entity_DefaultValues_AreCorrect()
    {
        var e = new PenaltyRuleMasterEntity();
        Assert.Equal(0, e.Id);
        Assert.Equal(string.Empty, e.PenaltyCode);
        Assert.Equal(string.Empty, e.PenaltyName);
        Assert.Equal(string.Empty, e.CalculationType);
        Assert.Equal(0m, e.PenaltyValue);
        Assert.Equal(0, e.GracePeriodDays);
        Assert.True(e.IsActive);
        Assert.Null(e.CreatedBy); Assert.Null(e.CreatedDate);
        Assert.Null(e.UpdatedBy); Assert.Null(e.UpdatedDate);
        Assert.False(e.MarkedForDeletion); Assert.Null(e.MarkedForDeletionDate);
    }

    // ── DTOs ─────────────────────────────────────────────────────────────────

    [Fact]
    public void PenaltyRuleDto_Properties_WorkCorrectly()
    {
        var d = DateTime.UtcNow;
        var dto = new PenaltyRuleDto
        {
            Id = 1, PenaltyCode = "PEN001", PenaltyName = "Late Payment Penalty",
            CalculationType = "Percentage", PenaltyValue = 10.5m, GracePeriodDays = 5,
            IsActive = true, CreatedDate = d, UpdatedDate = d
        };
        Assert.Equal(1, dto.Id);
        Assert.Equal("PEN001", dto.PenaltyCode);
        Assert.Equal("Late Payment Penalty", dto.PenaltyName);
        Assert.Equal("Percentage", dto.CalculationType);
        Assert.Equal(10.5m, dto.PenaltyValue);
        Assert.Equal(5, dto.GracePeriodDays);
        Assert.True(dto.IsActive);
        Assert.Equal(d, dto.CreatedDate);
        Assert.Equal(d, dto.UpdatedDate);
    }

    [Fact]
    public void CreateDto_ValidData_PassesValidation()
    {
        var dto = new CreatePenaltyRuleDto
            { PenaltyCode = "PEN001", PenaltyName = "Late", CalculationType = "Percentage",
              PenaltyValue = 10m, GracePeriodDays = 5, IsActive = true, CreatedBy = 1 };
        Assert.Empty(Validate(dto));
    }

    [Theory, MemberData(nameof(CreateCodeCases))]
    public void CreateDto_PenaltyCode_Validation(string? code, string err)
    {
        var dto = new CreatePenaltyRuleDto { PenaltyCode = code!, PenaltyName = "Name", CalculationType = "Percentage" };
        Assert.Contains(Validate(dto), v => v.ErrorMessage == err);
    }

    [Theory, MemberData(nameof(CreateNameCases))]
    public void CreateDto_PenaltyName_Validation(string? name, string err)
    {
        var dto = new CreatePenaltyRuleDto { PenaltyCode = "PEN001", PenaltyName = name!, CalculationType = "Percentage" };
        Assert.Contains(Validate(dto), v => v.ErrorMessage == err);
    }

    [Theory, MemberData(nameof(CreateCalcCases))]
    public void CreateDto_CalculationType_Validation(string? ct, string err)
    {
        var dto = new CreatePenaltyRuleDto { PenaltyCode = "PEN001", PenaltyName = "Name", CalculationType = ct! };
        Assert.Contains(Validate(dto), v => v.ErrorMessage == err);
    }

    [Fact]
    public void UpdateDto_ValidData_PropertiesAndValidation()
    {
        var dto = new UpdatePenaltyRuleDto
            { PenaltyCode = "PEN002", PenaltyName = "Monthly Penalty", CalculationType = "PerDay",
              PenaltyValue = 100m, GracePeriodDays = 7, IsActive = false, UpdatedBy = 3 };
        Assert.Empty(Validate(dto));
        Assert.Equal("PEN002", dto.PenaltyCode);
        Assert.Equal("Monthly Penalty", dto.PenaltyName);
        Assert.Equal("PerDay", dto.CalculationType);
        Assert.Equal(100m, dto.PenaltyValue);
        Assert.Equal(7, dto.GracePeriodDays);
        Assert.False(dto.IsActive);
        Assert.Equal(3, dto.UpdatedBy);
    }

    [Theory, MemberData(nameof(CreateCodeCases))]
    public void UpdateDto_PenaltyCode_Validation(string? code, string err)
    {
        var dto = new UpdatePenaltyRuleDto { PenaltyCode = code!, PenaltyName = "Name", CalculationType = "Percentage" };
        Assert.Contains(Validate(dto), v => v.ErrorMessage == err);
    }

    [Theory, MemberData(nameof(CreateNameCases))]
    public void UpdateDto_PenaltyName_Validation(string? name, string err)
    {
        var dto = new UpdatePenaltyRuleDto { PenaltyCode = "PEN001", PenaltyName = name!, CalculationType = "Percentage" };
        Assert.Contains(Validate(dto), v => v.ErrorMessage == err);
    }

    [Theory, MemberData(nameof(CreateCalcCases))]
    public void UpdateDto_CalculationType_Validation(string? ct, string err)
    {
        var dto = new UpdatePenaltyRuleDto { PenaltyCode = "PEN001", PenaltyName = "Name", CalculationType = ct! };
        Assert.Contains(Validate(dto), v => v.ErrorMessage == err);
    }

    // ── QueryParameters ───────────────────────────────────────────────────────

    [Fact]
    public void QueryParameters_Properties_WorkCorrectly()
    {
        var qp = new PenaltyRuleQueryParameters
        {
            PenaltyCode = "PEN001", PenaltyName = "Late", CalculationType = "Percentage",
            IsActive = true, MarkedForDeletion = false, PenaltyValue = 10.5m,
            GracePeriodDays = 5, PageNumber = 2, PageSize = 20, SearchTerm = "Test", SortBy = "PenaltyName"
        };
        Assert.Equal("PEN001", qp.PenaltyCode); Assert.Equal("Late", qp.PenaltyName);
        Assert.Equal("Percentage", qp.CalculationType); Assert.True(qp.IsActive);
        Assert.False(qp.MarkedForDeletion); Assert.Equal(10.5m, qp.PenaltyValue);
        Assert.Equal(5, qp.GracePeriodDays); Assert.Equal(2, qp.PageNumber);
        Assert.Equal(20, qp.PageSize); Assert.Equal("Test", qp.SearchTerm);

        var defaults = new PenaltyRuleQueryParameters();
        Assert.Null(defaults.PenaltyCode); Assert.Null(defaults.PenaltyName);
        Assert.Null(defaults.CalculationType); Assert.Null(defaults.IsActive);
        Assert.Null(defaults.MarkedForDeletion); Assert.Null(defaults.PenaltyValue);
        Assert.Null(defaults.GracePeriodDays);
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    [Fact]
    public void Mappings_AllDirections_WorkCorrectly()
    {
        var d = DateTime.UtcNow;
        var entity = new PenaltyRuleMasterEntity
            { Id = 1, PenaltyCode = "PEN001", PenaltyName = "Late", CalculationType = "Percentage",
              PenaltyValue = 10.5m, GracePeriodDays = 5, IsActive = true, CreatedDate = d, UpdatedDate = d };

        // Entity → Dto
        var dto = _mapper.Map<PenaltyRuleDto>(entity);
        Assert.Equal(1, dto.Id); Assert.Equal("PEN001", dto.PenaltyCode);
        Assert.Equal("Late", dto.PenaltyName); Assert.Equal("Percentage", dto.CalculationType);
        Assert.Equal(10.5m, dto.PenaltyValue); Assert.Equal(5, dto.GracePeriodDays);
        Assert.True(dto.IsActive);

        // CreateDto → Entity
        var fromCreate = _mapper.Map<PenaltyRuleMasterEntity>(
            new CreatePenaltyRuleDto { PenaltyCode = "PEN001", PenaltyName = "Late",
                                       CalculationType = "Percentage", PenaltyValue = 10m, CreatedBy = 1 });
        Assert.Equal("PEN001", fromCreate.PenaltyCode);
        Assert.Equal(1, fromCreate.CreatedBy);
        Assert.Null(fromCreate.CreatedDate); Assert.Null(fromCreate.UpdatedDate);

        // UpdateDto → Entity
        var fromUpdate = _mapper.Map<PenaltyRuleMasterEntity>(
            new UpdatePenaltyRuleDto { PenaltyCode = "PEN002", PenaltyName = "Flat",
                                       CalculationType = "FlatAmount", UpdatedBy = 2 });
        Assert.Equal("PEN002", fromUpdate.PenaltyCode);
        Assert.Equal(2, fromUpdate.UpdatedBy);
        Assert.Null(fromUpdate.CreatedDate); Assert.Null(fromUpdate.UpdatedDate);
    }

    // ── Service ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Service_GetById_ReturnsCorrectly()
    {
        var entity = new PenaltyRuleMasterEntity { Id = 1, PenaltyCode = "PEN001", PenaltyName = "Late", CalculationType = "Percentage" };
        MockGetById(1, entity);
        MockGetById(999, null);

        var result = await _svc.GetByIdAsync(1, CancellationToken.None);
        Assert.NotNull(result); Assert.Equal(1, result.Id); Assert.Equal("PEN001", result.PenaltyCode);

        Assert.Null(await _svc.GetByIdAsync(999, CancellationToken.None));
    }

    [Fact]
    public async Task Service_GetAll_ReturnsPaged()
    {
        MockRepo(
            new() { Id = 1, PenaltyCode = "PEN001", PenaltyName = "Late", CalculationType = "Percentage", IsActive = true },
            new() { Id = 2, PenaltyCode = "PEN002", PenaltyName = "Flat", CalculationType = "FlatAmount", IsActive = true });

        var result = await _svc.GetAllAsync(new PenaltyRuleQueryParameters { PageNumber = 1, PageSize = 10 }, CancellationToken.None);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
    }

    [Fact]
    public async Task Service_Create_ValidDto_Succeeds()
    {
        MockRepo(); // empty → no duplicate
        _repo.Setup(r => r.AddAsync(It.IsAny<PenaltyRuleMasterEntity>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((PenaltyRuleMasterEntity e, CancellationToken _) => { e.Id = 1; return e; });

        var result = await _svc.CreateAsync(
            new CreatePenaltyRuleDto { PenaltyCode = "PEN001", PenaltyName = "Late", CalculationType = "Percentage" },
            CancellationToken.None);

        Assert.Equal(1, result.Id); Assert.Equal("PEN001", result.PenaltyCode);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Service_Create_DuplicateCode_ThrowsValidation()
    {
        MockRepo(new PenaltyRuleMasterEntity { PenaltyCode = "PEN001" });

        var ex = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _svc.CreateAsync(
                new CreatePenaltyRuleDto { PenaltyCode = "PEN001", PenaltyName = "Late", CalculationType = "Percentage" },
                CancellationToken.None));

        Assert.Contains(ex.Errors, e => e.Key == "PenaltyCode" && e.Value.Contains("Penaltyrule_Code_Duplicate"));
    }

    [Fact]
    public async Task Service_Update_Succeeds_ActiveToActive_SkipsRefVal()
    {
        var existing = new PenaltyRuleMasterEntity { Id = 1, PenaltyCode = "PEN001", PenaltyName = "Late", CalculationType = "Percentage", IsActive = true };
        MockGetById(1, existing);
        MockRepo(existing);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<PenaltyRuleMasterEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _svc.UpdateAsync(1,
            new UpdatePenaltyRuleDto { PenaltyCode = "PEN001", PenaltyName = "Updated", CalculationType = "Percentage", IsActive = true },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Updated", result.PenaltyName);
        _refVal.Verify(r => r.ValidateReferencesAsync<PenaltyRuleMasterEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Service_Deactivate_Referenced_ThrowsValidation()
    {
        var existing = new PenaltyRuleMasterEntity { Id = 1, PenaltyCode = "PEN001", PenaltyName = "Late", CalculationType = "Percentage", IsActive = true };
        MockGetById(1, existing);
        MockRepo(existing);
        _refVal.Setup(r => r.ValidateReferencesAsync<PenaltyRuleMasterEntity>(1, It.IsAny<CancellationToken>()))
               .ReturnsAsync(AppVR.Failure("Cannot deactivate"));

        await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _svc.UpdateAsync(1,
                new UpdatePenaltyRuleDto { PenaltyCode = "PEN001", PenaltyName = "Late", CalculationType = "Percentage", IsActive = false },
                CancellationToken.None));
    }

    [Fact]
    public async Task Service_Deactivate_AlreadyInactive_SkipsRefVal()
    {
        var existing = new PenaltyRuleMasterEntity { Id = 1, PenaltyCode = "PEN001", PenaltyName = "Late", CalculationType = "Percentage", IsActive = false };
        MockGetById(1, existing);
        MockRepo(existing);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<PenaltyRuleMasterEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _svc.UpdateAsync(1,
            new UpdatePenaltyRuleDto { PenaltyCode = "PEN001", PenaltyName = "Late", CalculationType = "Percentage", IsActive = false },
            CancellationToken.None);

        Assert.NotNull(result);
        _refVal.Verify(r => r.ValidateReferencesAsync<PenaltyRuleMasterEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Service_Delete_Succeeds()
    {
        MockGetById(1, new PenaltyRuleMasterEntity { Id = 1, PenaltyCode = "PEN001" });
        _repo.Setup(r => r.DeleteAsync(It.IsAny<PenaltyRuleMasterEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        Assert.True(await _svc.DeleteAsync(1, CancellationToken.None));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Service_Delete_Referenced_ThrowsValidation()
    {
        MockGetById(1, new PenaltyRuleMasterEntity { Id = 1, PenaltyCode = "PEN001" });
        _refVal.Setup(r => r.ValidateReferencesAsync<PenaltyRuleMasterEntity>(1, It.IsAny<CancellationToken>()))
               .ReturnsAsync(AppVR.Failure("Referenced"));

        await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _svc.DeleteAsync(1, CancellationToken.None));
    }

    // ── Controller ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Controller_GetAll_ReturnsOkWithPagedResult()
    {
        var svc = new Mock<IPenaltyRuleService>();
        var qp  = new PenaltyRuleQueryParameters();
        svc.Setup(s => s.GetAllAsync(qp, It.IsAny<CancellationToken>()))
           .ReturnsAsync(new PagedResult<PenaltyRuleDto>(new List<PenaltyRuleDto>(), 0, 1, 10));

        var ok = Assert.IsType<OkObjectResult>(await MakeController(svc).GetAll(qp, CancellationToken.None));
        Assert.Empty(Assert.IsType<PagedResult<PenaltyRuleDto>>(ok.Value!).Items);
    }

    [Fact]
    public async Task Controller_GetById_ExistingId_ReturnsOk()
    {
        var svc = new Mock<IPenaltyRuleService>();
        svc.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new PenaltyRuleDto { Id = 1 });
        Assert.IsType<OkObjectResult>(await MakeController(svc).GetById(1, CancellationToken.None));
    }

    [Fact]
    public async Task Controller_GetById_NonExistingId_ReturnsNotFound()
    {
        var svc = new Mock<IPenaltyRuleService>();
        svc.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((PenaltyRuleDto?)null);
        Assert.IsType<NotFoundResult>(await MakeController(svc).GetById(999, CancellationToken.None));
    }

    [Fact]
    public async Task Controller_Create_ReturnsOk()
    {
        var svc = new Mock<IPenaltyRuleService>();
        svc.Setup(s => s.CreateAsync(It.IsAny<CreatePenaltyRuleDto>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new PenaltyRuleDto { Id = 1 });
        Assert.IsType<OkObjectResult>(await MakeController(svc).Create(
            new CreatePenaltyRuleDto { PenaltyCode = "PEN001", PenaltyName = "Late", CalculationType = "Percentage" },
            CancellationToken.None));
    }

    [Fact]
    public async Task Controller_Update_ReturnsOk()
    {
        var svc = new Mock<IPenaltyRuleService>();
        svc.Setup(s => s.UpdateAsync(1, It.IsAny<UpdatePenaltyRuleDto>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new PenaltyRuleDto { Id = 1 });
        Assert.IsType<OkObjectResult>(await MakeController(svc).Update(1,
            new UpdatePenaltyRuleDto { PenaltyCode = "PEN001", PenaltyName = "Late", CalculationType = "Percentage" },
            CancellationToken.None));
    }

    [Fact]
    public async Task Controller_Delete_ReturnsOk()
    {
        var svc = new Mock<IPenaltyRuleService>();
        svc.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Assert.IsType<OkObjectResult>(await MakeController(svc).Delete(1, CancellationToken.None));
    }

    [Fact]
    public async Task Controller_Purge_ReturnsOk()
    {
        _cleanup.Setup(c => c.ForceHardDeleteAsync<PenaltyRuleMasterEntity, int>(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
        Assert.IsType<OkObjectResult>(await MakeController().Purge(1, CancellationToken.None));
    }
}
