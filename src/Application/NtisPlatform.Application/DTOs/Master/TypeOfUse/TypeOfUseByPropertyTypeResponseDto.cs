using System.Collections.Generic;

namespace NtisPlatform.Application.DTOs;

public class TypeOfUseByPropertyTypeResponseDto : List<TypeOfUseByPropertyTypeItemDto>
{
}

public class TypeOfUseByPropertyTypeItemDto
{
    public int Id { get; set; }
    public string TypeOfUseCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
