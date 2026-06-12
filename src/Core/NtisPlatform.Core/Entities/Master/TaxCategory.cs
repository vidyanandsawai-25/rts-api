// This file is intentionally empty.
// The TaxCategory enum has been removed.
// Tax classification (Education / Employment / General) is now derived directly from
// TaxCategoryMasterEntity.CategoryCode ("EDU" / "EMP") loaded via the TaxMaster navigation
// property — no separate enum or extra DB column is required.
// See: TaxMasterEntity.TaxCategoryMaster, RateableValueService.IsEducationTax / IsEmploymentTax
