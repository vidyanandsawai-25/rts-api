using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NtisPlatform.Application.Services.TaxEngine
{
    public class TaxGetterCache<T>
    {
        private readonly Dictionary<int, string> _taxNamesById;
        private readonly Dictionary<int, string> _taxCategoryCodesById;

        public TaxGetterCache(
            IEnumerable<T> rows,
            Func<T, int> idSelector,
            Func<T, string> nameSelector,
            Func<T, string> categoryCodeSelector = null)
        {
            _taxNamesById = rows
                .GroupBy(idSelector)
                .ToDictionary(g => g.Key, g => nameSelector(g.First()));

            // Optional: Store category codes if selector provided
            if (categoryCodeSelector != null)
            {
                _taxCategoryCodesById = rows
                    .GroupBy(idSelector)
                    .ToDictionary(g => g.Key, g => categoryCodeSelector(g.First()));
            }
            else
            {
                _taxCategoryCodesById = new Dictionary<int, string>();
            }
        }

        public string GetTaxName(int taxId)
        {
            return _taxNamesById.TryGetValue(taxId, out var name)
                ? name
                : $"Tax_{taxId}";
        }

        public string GetTaxCategoryCode(int taxId)
        {
            return _taxCategoryCodesById.TryGetValue(taxId, out var code)
                ? code
                : string.Empty;
        }

        public IReadOnlyDictionary<int, string> TaxNames => _taxNamesById;
    }
}
