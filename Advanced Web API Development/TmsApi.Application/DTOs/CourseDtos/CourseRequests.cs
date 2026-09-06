namespace TmsApi.Application.DTOs.CourseDtos;

public record ModuleRequest(string Title, string? Description);
public record ReorderItem(int Id, int OrderIndex);
public record ReorderRequest(List<ReorderItem> Items);
public record LessonRequest(string Title, string? Description, int DurationMinutes, string? ContentText, string? PdfUrl, string? PdfFileName);
