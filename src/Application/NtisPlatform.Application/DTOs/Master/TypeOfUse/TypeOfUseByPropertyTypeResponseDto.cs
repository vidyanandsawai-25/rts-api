using System.Collections.Generic;

namespace NtisPlatform.Application.DTOs;

public class TypeOfUseByPropertyTypeResponseDto : List<TypeOfUseByPropertyTypeItemDto>
{
}

public class TypeOfUseByPropertyTypeItemDto : BaseDtos
{
    public string TypeOfUseCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int TypeOfUseGroupId { get; set; }
    public int? SearchSequence { get; set; }
    public int? TypeOfUseCategoryId { get; set; }
}
