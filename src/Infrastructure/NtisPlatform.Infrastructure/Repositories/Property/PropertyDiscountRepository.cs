using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces.Property;
using NtisPlatform.Core.Models;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories.Property;

/// <summary>Data-access implementation for the Property "Discount" tab (queries and staged inserts only).</summary>
public class PropertyDiscountRepository : PropertyRepositoryBase, IPropertyDiscountRepository
{
    public PropertyDiscountRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<PropertyDiscountInfoResponseDto?> GetDiscountDetailsAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        // Step 1: Existence check only — no full entity load needed for a read-only query.
        var propertyExists = await _context.PropertyMast
            .AsNoTracking()
            .AnyAsync(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion, cancellationToken);

        if (!propertyExists) return null;

        // Step 2: Get all social attributes where IsDiscountApplicable = 1 — read-only.
        var discountAttributes = await _context.Set<SocialAttributeEntity>()
            .AsNoTracking()
            .Where(sa => sa.IsActive && sa.IsDiscountApplicable)
            .OrderBy(sa => sa.DisplayOrder ?? int.MaxValue)
            .ThenBy(sa => sa.SocialAttributeName)
            .ToListAsync(cancellationToken);

        // Step 3: Get existing PropertySocialDetails with document information — read-only join.
        var existingDetailsWithDocs = await (
            from psd in _context.Set<PropertySocialDetailsEntity>().AsNoTracking()
            where psd.PropertyId == propertyId && psd.IsActive && !psd.MarkedForDeletion
            join db in _context.Set<DocumentBindingEntity>() on psd.DocumentBindingId equals db.Id into dbJoin
            from db in dbJoin.DefaultIfEmpty()
            join doc in _context.Set<DocumentEntity>() on db.DocumentId equals doc.Id into docJoin
            from doc in docJoin.DefaultIfEmpty()
            select new
            {
                PropertySocialDetail = psd,
                DocumentGuid = db != null && db.IsActive && !db.MarkedForDeletion && doc != null && doc.IsActive && !doc.MarkedForDeletion ? (Guid?)doc.DocumentGuid : null
            }).ToListAsync(cancellationToken);

        var existingByAttributeId = existingDetailsWithDocs
            .GroupBy(x => x.PropertySocialDetail.SocialAttributeId)
            .ToDictionary(g => g.Key, g => g.First());

        // Step 3.5: Fetch active photo bindings for these details
        var detailIds = existingDetailsWithDocs.Select(x => x.PropertySocialDetail.Id).ToList();
        var photoBindings = new Dictionary<int, (int BindingId, Guid DocumentGuid)>();
        if (detailIds.Any())
        {
            var rawBindings = await (
                from db in _context.Set<DocumentBindingEntity>().AsNoTracking()
                where db.ReferenceTableName == "PropertySocialDetails"
                   && db.ReferenceTableId.HasValue
                   && detailIds.Contains(db.ReferenceTableId!.Value)
                   && db.BindingPurpose == "Photo"
                   && db.IsActive
                   && !db.MarkedForDeletion
                join doc in _context.Set<DocumentEntity>().AsNoTracking() on db.DocumentId equals doc.Id
                where doc.IsActive && !doc.MarkedForDeletion
                select new { DetailId = db.ReferenceTableId!.Value, BindingId = db.Id, DocumentGuid = doc.DocumentGuid }
            ).ToListAsync(cancellationToken);

            photoBindings = rawBindings
                .GroupBy(b => b.DetailId)
                .ToDictionary(g => g.Key, g => (g.First().BindingId, g.First().DocumentGuid));
        }

        // Step 4: Build response DTO
        var result = new PropertyDiscountInfoResponseDto
        {
            PropertyId = propertyId,
            DiscountAttributes = discountAttributes.Select(attr =>
            {
                existingByAttributeId.TryGetValue(attr.Id, out var existingValue);

                int? photoBindingId = null;
                Guid? photoGuid = null;
                if (existingValue != null && photoBindings.TryGetValue(existingValue.PropertySocialDetail.Id, out var pb))
                {
                    photoBindingId = pb.BindingId;
                    photoGuid = pb.DocumentGuid;
                }

                return new DiscountAttributeDto
                {
                    Id = attr.Id,
                    SocialAttributeCode = attr.SocialAttributeCode,
                    SocialAttributeName = attr.SocialAttributeName,
                    DataType = attr.DataType,
                    Unit = attr.Unit,
                    DisplayOrder = attr.DisplayOrder,
                    IsDiscountApplicable = attr.IsDiscountApplicable,
                    IsPhotoRequired = attr.IsPhotoRequired,
                    IsDocumentRequired = attr.IsDocumentRequired,

                    // Map existing values if present
                    PropertySocialDetailId = existingValue?.PropertySocialDetail.Id,
                    BitValue = existingValue?.PropertySocialDetail.BitValue,
                    IntValue = existingValue?.PropertySocialDetail.IntValue,
                    DecimalValue = existingValue?.PropertySocialDetail.DecimalValue,
                    TextValue = existingValue?.PropertySocialDetail.TextValue,
                    DateValue = existingValue?.PropertySocialDetail.DateValue,

                    // Document GUID - only populated if document is valid and active
                    DocumentGuid = existingValue?.DocumentGuid,
                    DocumentBindingId = existingValue?.PropertySocialDetail.DocumentBindingId,
                    PhotoBindingId = photoBindingId,
                    PhotoGuid = photoGuid,
                    Remark = existingValue?.PropertySocialDetail.Remark,
                    IsActive = existingValue != null
                };
            }).ToList()
        };

        return result;
    }

    public async Task<List<PropertySocialDetailsEntity>> GetActiveSocialDetailsAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<PropertySocialDetailsEntity>()
            .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
            .ToListAsync(cancellationToken);
    }

    public async Task<HashSet<int>> GetDiscountApplicableAttributeIdsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<SocialAttributeEntity>()
            .AsNoTracking()
            .Where(sa => sa.IsActive && sa.IsDiscountApplicable)
            .Select(sa => sa.Id)
            .ToHashSetAsync(cancellationToken);
    }

    public void AddSocialDetail(PropertySocialDetailsEntity socialDetail)
    {
        _context.Set<PropertySocialDetailsEntity>().Add(socialDetail);
    }
}
