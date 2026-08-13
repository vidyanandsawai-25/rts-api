namespace NtisPlatform.Application.DTOs.CommonDetails;

public class SourceTableLookupDto
{
    public int Id { get; set; }
    public string? TableName { get; set; }
}

public class SourceTableFieldLookupDto
{
    public int Id { get; set; }
    public string? TableFieldName { get; set; }
}
