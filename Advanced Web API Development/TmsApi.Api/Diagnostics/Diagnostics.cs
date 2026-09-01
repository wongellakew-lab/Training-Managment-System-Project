using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace TmsApi.Api.Diagnostics;

public static class Diagnostics
{
    public const string ServiceName = "TmsApi";
    public static readonly ActivitySource ActivitySource = new(ServiceName);
    public static readonly Meter Meter = new(ServiceName);

    public static readonly Counter<long> EnrollmentsCounter = Meter.CreateCounter<long>(
        "tms_enrollments_total",
        description: "Total number of student enrollments processed");

    public static readonly Counter<long> CertificatesCounter = Meter.CreateCounter<long>(
        "tms_certificates_total",
        description: "Total number of certificates issued");

    public static readonly Counter<long> TranscriptsCounter = Meter.CreateCounter<long>(
        "tms_transcripts_requested_total",
        description: "Total number of transcripts requested");
}
