namespace NtisPlatform.Core.Constants;

/// <summary>
/// Constants used in Capital Value calculation and processing
/// </summary>
public static class CapitalValueConstants
{
    /// <summary>
    /// Tax-related constants
    /// </summary>
    public static class Tax
    {
        /// <summary>
        /// Name of the tax total head in TaxMaster table
        /// </summary>
        public const string TaxTotalName = "TaxTotal";
    }

    /// <summary>
    /// Policy-related constants
    /// </summary>
    public static class Policy
    {
        /// <summary>
        /// Default policy code for net tax calculation
        /// </summary>
        public const string DefaultPolicyCode = "NETTAX";
     }

 

    /// <summary>
    /// Property details calculation constants
    /// </summary>
    public static class PropertyDetails
    {
        /// <summary>
        /// Value indicating all property details (when PropertyDetailsId is not specified)
        /// </summary>
        public const int AllPropertyDetails = 0;
    }

    /// <summary>
    /// Property category related constants
    /// </summary>
    public static class PropertyCategory
    {
        /// <summary>
        /// Keyword for Apartment property category
        /// </summary>
        public const string ApartmentKeyword = "Apartment";
    }

    /// <summary>
    /// Restricted owner names that cannot be combined
    /// </summary>
    public static class RestrictedOwnerNames
    {
        /// <summary>
        /// "The Holder" placeholder name
        /// </summary>
        public const string TheHolder = "The Holder";

        /// <summary>
        /// "Holder" placeholder name
        /// </summary>
        public const string Holder = "Holder";

        /// <summary>
        /// "धारक" (Holder in Hindi/Marathi) placeholder name
        /// </summary>
        public const string HolderMarathi = "धारक";

        /// <summary>
        /// List of all restricted owner names
        /// </summary>
        public static readonly string[] All = [TheHolder, Holder, HolderMarathi];
    }
}
