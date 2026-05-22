using System;

namespace NtisPlatform.Application.Services.TaxEngine
{
    /// <summary>
    /// Helper class for validating calculation preconditions
    /// </summary>
    public static class CalculationValidator
    {
        /// <summary>
        /// Checks a condition and throws an exception if false
        /// </summary>
        /// <param name="condition">Condition to check</param>
        /// <param name="errorMessage">Error message if condition is false</param>
        /// <exception cref="InvalidOperationException">Thrown when condition is false</exception>
        public static void CheckCondition(bool condition, string errorMessage)
        {
            if (!condition)
            {
                throw new InvalidOperationException(errorMessage);
            }
        }
    }
}
