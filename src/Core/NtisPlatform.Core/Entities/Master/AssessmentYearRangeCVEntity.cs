namespace NtisPlatform.Core.Entities.Master;

public class AssessmentYearRangeCVEntity : CommonBaseEntity
{
   public int YearRangeId { get; set; }
   public int FromYear { get; set; }
   public int ToYear { get; set; }
}

