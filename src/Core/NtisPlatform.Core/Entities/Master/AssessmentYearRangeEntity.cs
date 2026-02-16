
namespace NtisPlatform.Core.Entities.Master;

    public class AssessmentYearRangeEntity : CommonBaseEntity
    {
        public int YearId { get; set; }
        public int FromYear { get; set; }
        public int ToYear { get; set; }
    }

