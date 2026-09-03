using System.Text.Json.Serialization;

namespace TmsApi.Domain.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EnrollmentStatus
{
    Pending = 0,
    Active = 1,
    Approved = 1,
    Rejected = 2,
    Completed = 3,
    Dropped = 4,
    Waitlisted = 5
}
