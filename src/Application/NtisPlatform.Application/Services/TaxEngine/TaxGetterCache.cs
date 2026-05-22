using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NtisPlatform.Application.Services.TaxEngine
{
    public class TaxGetterCache<T>
    {
        private readonly Dictionary<int, string> _taxNamesById;

        public TaxGetterCache(
            IEnumerable<T> rows,
            Func<T, int> idSelector,
            Func<T, string> nameSelector)
        {
            _taxNamesById = rows
                .GroupBy(idSelector)
                .ToDictionary(g => g.Key, g => nameSelector(g.First()));
        }

        public string GetTaxName(int taxId)
        {
            return _taxNamesById.TryGetValue(taxId, out var name)
                ? name
                : $"Tax_{taxId}";
        }

        public IReadOnlyDictionary<int, string> TaxNames => _taxNamesById;
    }
}
