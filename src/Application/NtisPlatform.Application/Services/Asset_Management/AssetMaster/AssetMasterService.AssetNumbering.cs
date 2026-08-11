using Microsoft.EntityFrameworkCore;

namespace NtisPlatform.Application.Services.Asset_Management
{
    public partial class AssetMasterService
    {
        private static readonly SemaphoreSlim _sequenceLock = new SemaphoreSlim(1, 1);

        #region Endpoint: Generate Asset Numbers

        public async Task<string> GenerateAssetNoAsync(int assetCategoryId, int assetTypeId, CancellationToken cancellationToken = default)
        {
            var result = await GenerateAssetNosAsync(assetCategoryId, assetTypeId, 1, null, cancellationToken);
            return result.First();
        }

        public async Task<List<string>> GenerateAssetNosAsync( int assetCategoryId,int assetTypeId, int count,string? subunitPrefix = null,CancellationToken cancellationToken = default)
        {
            if (count <= 0)
            {
                return new List<string>();
            }

            var ulb = await _ulbRepository.GetQueryable()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            var ulbCode = !string.IsNullOrWhiteSpace(ulb?.UlbCode) ? ulb.UlbCode : "AMC";

            var category = await _assetCategoryRepository.GetQueryable()
                .AsNoTracking()
                .Where(x => x.Id == assetCategoryId && x.IsActive && !x.MarkedForDeletion)
                .Select(x => new { x.CategoryCode, x.CategoryName })
                .FirstOrDefaultAsync(cancellationToken);

            if (category == null)
            {
                throw new InvalidOperationException($"Asset category {assetCategoryId} not found or inactive.");
            }

            var type = await _assetTypeRepository.GetQueryable()
                .AsNoTracking()
                .Where(x => x.Id == assetTypeId && x.AssetCategoryId == assetCategoryId && x.IsActive && !x.MarkedForDeletion)
                .Select(x => new { x.TypeCode, x.TypeName })
                .FirstOrDefaultAsync(cancellationToken);

            if (type == null)
            {
                throw new InvalidOperationException($"Asset type {assetTypeId} not found, inactive, or not mapped to category {assetCategoryId}.");
            }

            var (categorySegment, typeSegment) = GetCategoryAndTypeSegments(
                category.CategoryCode,
                category.CategoryName,
                assetCategoryId,
                type.TypeCode,
                type.TypeName,
                assetTypeId);

            var prefix = $"{ulbCode}-{categorySegment}-{typeSegment}-";
            if (!string.IsNullOrWhiteSpace(subunitPrefix))
            {
                var subunitSegment = SanitizeAssetNoSegment(subunitPrefix);
                if (!string.IsNullOrWhiteSpace(subunitSegment))
                {
                    prefix = $"{ulbCode}-{categorySegment}-{typeSegment}-{subunitSegment}-";
                }
            }

            return await GenerateAssetNosWithPrefixAsync(prefix, count, 4, cancellationToken);
        }

        public async Task<string> GetUlbCodeAsync(CancellationToken cancellationToken = default)
        {
            var ulb = await _ulbRepository.GetQueryable()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            return !string.IsNullOrWhiteSpace(ulb?.UlbCode) ? ulb.UlbCode : "AMC";
        }

        public async Task<List<string>> GenerateAssetNosWithPrefixAsync(
            string prefix,
            int count,
            int padding,
            CancellationToken cancellationToken = default)
        {
            if (count <= 0)
            {
                return new List<string>();
            }

            int nextSequence = 0;

            await _sequenceLock.WaitAsync(cancellationToken);
            try
            {
                int maxExistingSeq = await GetMaxExistingAssetNoSequenceAsync(prefix, padding, cancellationToken);
                nextSequence = maxExistingSeq + 1;
            }
            finally
            {
                _sequenceLock.Release();
            }

            var assetNos = new List<string>();
            var paddingFormat = $"D{padding}";
            for (int i = 0; i < count; i++)
            {
                assetNos.Add($"{prefix}{(nextSequence + i).ToString(paddingFormat)}");
            }

            return assetNos;
        }

        /// <summary>
        /// Detects a violation of the AssetNo unique index (<c>UQ_AssetMaster_AssetNo</c> in
        /// <c>ApplicationDbContext</c>). <see cref="GenerateAssetNosWithPrefixAsync"/>'s lock only
        /// guards the max-sequence read, not persistence (coverage roadmap Section B item 5) -- two
        /// concurrent callers can compute the same number before either commits. This index is the
        /// DB-level safety net; callers that persist a freshly generated AssetNo should catch
        /// <see cref="DbUpdateException"/>, check this, and retry with a new number instead of
        /// surfacing the race as a client-facing failure.
        /// </summary>
        private static bool IsUniqueAssetNoViolation(DbUpdateException ex)
        {
            var message = ex.InnerException?.Message ?? ex.Message;
            return message.Contains("UQ_AssetMaster_AssetNo", StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region Asset Numbering Helper Methods

        private async Task<int> GetMaxExistingAssetNoSequenceAsync(
            string prefix,
            int padding,
            CancellationToken cancellationToken = default)
        {
            var existingAssetNos = await _repository.GetQueryable()
                .AsNoTracking()
                .Where(x => x.AssetNo != null && x.AssetNo.StartsWith(prefix))
                .Select(x => x.AssetNo!)
                .ToListAsync(cancellationToken);

            int maxSeq = 0;
            foreach (var assetNo in existingAssetNos)
            {
                if (assetNo.Length >= prefix.Length + padding)
                {
                    var seqStr = assetNo.Substring(prefix.Length);
                    if (int.TryParse(seqStr, out var seq) && seq > maxSeq)
                        maxSeq = seq;
                }
            }

            return maxSeq;
        }

        public static string SanitizeAssetNoSegment(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            var cleaned = new string(input
                .Trim()
                .ToUpperInvariant()
                .Where(c => char.IsLetterOrDigit(c))
                .ToArray());

            return cleaned;
        }

        public static (string CategorySegment, string TypeSegment) GetCategoryAndTypeSegments(
            string? categoryCode,
            string? categoryName,
            int categoryId,
            string? typeCode,
            string? typeName,
            int typeId)
        {
            var categoryCodeRaw = !string.IsNullOrWhiteSpace(categoryCode)
                ? categoryCode
                : (!string.IsNullOrWhiteSpace(categoryName) ? categoryName : categoryId.ToString());

            if (categoryCodeRaw.StartsWith("CAT-", StringComparison.OrdinalIgnoreCase))
            {
                categoryCodeRaw = categoryCodeRaw.Substring(4);
            }
            else if (categoryCodeRaw.StartsWith("CAT", StringComparison.OrdinalIgnoreCase))
            {
                categoryCodeRaw = categoryCodeRaw.Substring(3);
            }

            var categorySegment = SanitizeAssetNoSegment(categoryCodeRaw);

            var typeCodeRaw = !string.IsNullOrWhiteSpace(typeCode)
                ? typeCode
                : (!string.IsNullOrWhiteSpace(typeName) ? typeName : typeId.ToString());

            if (typeCodeRaw.StartsWith("TYPE-", StringComparison.OrdinalIgnoreCase))
            {
                typeCodeRaw = typeCodeRaw.Substring(5);
            }
            else if (typeCodeRaw.StartsWith("TYPE", StringComparison.OrdinalIgnoreCase))
            {
                typeCodeRaw = typeCodeRaw.Substring(4);
            }

            if (!string.IsNullOrWhiteSpace(categoryCodeRaw))
            {
                var categoryPrefixWithDash = categoryCodeRaw + "-";
                if (typeCodeRaw.StartsWith(categoryPrefixWithDash, StringComparison.OrdinalIgnoreCase))
                {
                    typeCodeRaw = typeCodeRaw.Substring(categoryPrefixWithDash.Length);
                }
                else if (typeCodeRaw.StartsWith(categoryCodeRaw, StringComparison.OrdinalIgnoreCase) && typeCodeRaw.Length > categoryCodeRaw.Length)
                {
                    typeCodeRaw = typeCodeRaw.Substring(categoryCodeRaw.Length);
                }
            }

            var typeSegment = SanitizeAssetNoSegment(typeCodeRaw);

            if (!string.IsNullOrWhiteSpace(categorySegment) &&
                !string.IsNullOrWhiteSpace(typeSegment) &&
                typeSegment.Length > categorySegment.Length &&
                typeSegment.StartsWith(categorySegment, StringComparison.OrdinalIgnoreCase))
            {
                typeSegment = typeSegment.Substring(categorySegment.Length);
            }

            if (string.IsNullOrWhiteSpace(categorySegment))
            {
                categorySegment = categoryId.ToString();
            }

            if (string.IsNullOrWhiteSpace(typeSegment))
            {
                typeSegment = typeId.ToString();
            }

            return (categorySegment, typeSegment);
        }

        #endregion
    }
}
