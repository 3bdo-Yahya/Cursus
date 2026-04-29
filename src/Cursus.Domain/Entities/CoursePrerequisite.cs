namespace Cursus.Domain.Entities
{
    public class CoursePrerequisite
    {
        public int CourseId { get; set; }
        public int PrerequisiteId { get; set; }

        public Course? Course { get; set; }
        public Course? Prerequisite { get; set; }
    }
}