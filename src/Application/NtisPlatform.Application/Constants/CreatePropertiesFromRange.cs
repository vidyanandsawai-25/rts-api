using System;

namespace NtisPlatform.Application.Constants
{
    public static class CreatePropertiesFromRange
    {
        public static class Numeric
        {
            public const int InitialSuccessCount = 0;
            public const int InitialFailedCount = 0;
            public const int IndexOffset = 1;
        }

        public static class Messages
        {
            public const string TemplateCannotBeNull = "Template cannot be null.";
            public const string PropertyAlreadyExists = "Property already exists.";
            public const string DuplicateCheckFailedTemplate = "Row {0} : Property already exists";
            public const string UnknownErrorOccurred = "Unknown error occurred";
            public const string PropertyCreationFailedTemplate = "Row {0} ({1}): {2}";
            public const string DatabaseErrorTemplate = "Database error: {0}";
            public const string OperationCancelled = "Operation cancelled.";
            public const string InvalidArgumentTemplate = "Invalid argument: {0}";
            public const string RollbackErrorTemplate = "Rollback error: {0}";
            public const string UnexpectedTransactionErrorTemplate = "Unexpected transaction error: {0}: {1}";
            public const string PropertyCreatedSuccessfully = "Property created successfully.";
        }

        public static class CategoryNames
        {
            public const string Apartment = "Apartment";
            public const string Plot = "Plot";
        }

        public static class ComparisonOptions
        {
            public const StringComparison CategoryNameComparison = StringComparison.OrdinalIgnoreCase;
        }
    }
}
