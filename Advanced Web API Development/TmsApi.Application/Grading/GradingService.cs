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
        return score switch
        {
            >= 90 => GradeLevel.A,
            >= 80 => GradeLevel.B,
            >= 70 => GradeLevel.C,
            >= 60 => GradeLevel.D,
            _ => GradeLevel.F
        };
    }

    public decimal CalculateGradePoint(decimal score)
    {
        return score switch
        {
            >= 90 => 4.0m,
            >= 80 => 3.0m,
            >= 70 => 2.0m,
            >= 60 => 1.0m,
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
}
