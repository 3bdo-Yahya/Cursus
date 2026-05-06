namespace Cursus.Domain.Entities
{

    // Junction table
    public class GraduationRequirementCourse
    {
        public int GraduationRequirementId { get; set; }

        public int CourseId { get; set; }

        public GraduationRequirement? GraduationRequirement { get; set; }
        public Course? Course { get; set; }

    }
}