namespace Cursus.PL.Models;

public class AiAdvisorViewModel
{
    public required string StudentName { get; init; }
    public required string Initials { get; init; }
    public required string Department { get; init; }
    public int Year { get; init; }
    public double Cgpa { get; init; }
    public required string AcademicStanding { get; init; }
    public string StandingCssClass { get; set; } = "good";
}