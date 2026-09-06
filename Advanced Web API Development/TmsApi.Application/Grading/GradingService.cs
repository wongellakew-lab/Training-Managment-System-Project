namespace TmsApi.Application.Grading;

public enum GradeLevel
{
    A,
    B,
    C,
    D,
    F
}

public class GradingService
{
    public GradeLevel CalculateGradeLevel(decimal score)
    {
        var pct = score <= 4.0m && score > 0.0m ? score * 25.0m : score;
        return pct switch
        {
            >= 90m => GradeLevel.A,
            >= 80m => GradeLevel.B,
            >= 70m => GradeLevel.C,
            >= 60m => GradeLevel.D,
            _ => GradeLevel.F
        };
    }

    public decimal CalculateGradePoint(decimal score)
    {
        var pct = score <= 4.0m && score > 0.0m ? score * 25.0m : score;
        return pct switch
        {
            >= 90m => 4.0m,
            >= 80m => 3.0m,
            >= 70m => 2.0m,
            >= 60m => 1.0m,
            _ => 0.0m
        };
    }

    public decimal CalculateGPA(IEnumerable<decimal> scores)
    {
        var scoreList = scores.ToList();
        if (!scoreList.Any()) return 0.0m;

        var totalPoints = scoreList.Sum(CalculateGradePoint);
        return Math.Round(totalPoints / scoreList.Count, 2);
    }

    public decimal CalculateEnrollmentGPA(IEnumerable<(decimal Grade, int Credits)> gradedCourses)
    {
        var list = gradedCourses.ToList();
        if (!list.Any()) return 0.0m;

        decimal totalPoints = 0m;
        int totalCredits = 0;

        foreach (var (grade, credits) in list)
        {
            var cr = credits > 0 ? credits : 3;
            var gp = CalculateGradePoint(grade);
            totalPoints += gp * cr;
            totalCredits += cr;
        }

        return totalCredits > 0 ? Math.Round(totalPoints / totalCredits, 2) : 0.0m;
    }
}
