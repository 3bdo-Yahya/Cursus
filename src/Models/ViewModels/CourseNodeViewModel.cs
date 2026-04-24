namespace Cursus.Models.ViewModels
{
    /// <summary>
    /// Represents a course node in the course map.
    /// </summary>
    public class CourseNodeViewModel
    {
        /// <summary>
        /// Gets or sets the course code.
        /// </summary>
        public required string Code { get; set; }

        /// <summary>
        /// Gets or sets the course name.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the number of credit hours for the course.
        /// </summary>
        public int Credits { get; set; }

        /// <summary>
        /// Gets or sets the course status.
        /// </summary>
        public CourseStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the grade achieved in the course.
        /// </summary>
        public required string Grade { get; set; }
    }
}
