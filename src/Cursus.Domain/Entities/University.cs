
namespace Cursus.Domain.Entities
{
    public class University
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ICollection<Department> Departments { get; set; } = new List<Department>();
        public ICollection<GradeScale> GradeScales { get; set; } = new List<GradeScale>();
    }
}