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
}
