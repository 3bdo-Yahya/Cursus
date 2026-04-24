namespace Cursus.Models.ViewModels
{
    /// <summary>
    /// Represents the data displayed on the admin dashboard.
    /// </summary>
    public class AdminDashboardViewModel
    {
        /// <summary>
        /// Gets or sets the total number of students.
        /// </summary>
        public int TotalStudents { get; set; }

        /// <summary>
        /// Gets or sets the total number of courses.
        /// </summary>
        public int TotalCourses { get; set; }

        /// <summary>
        /// Gets or sets the total number of departments.
        /// </summary>
        public int TotalDepartments { get; set; }
    }
}
