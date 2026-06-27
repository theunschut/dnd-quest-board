using System.Text.Json.Serialization;

namespace EuphoriaInn.Service.Services;

public record ResendEmailRecord(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt,
    [property: JsonPropertyName("last_event")] string LastEvent);

public record ResendEmailListResponse(
    [property: JsonPropertyName("data")] List<ResendEmailRecord> Data);

public readonly record struct ResendStatCounts(int Sent, int Delivered, int Bounced, int Failed);

public static class ResendStatsAggregator
{
    public static ResendStatCounts Aggregate(IEnumerable<ResendEmailRecord> records, DateTime cutoffUtc)
    {
        int sent = 0, delivered = 0, bounced = 0, failed = 0;

        foreach (var record in records)
        {
            if (record.CreatedAt < cutoffUtc)
                continue;

            switch (record.LastEvent)
            {
                case "sent":
                    sent++;
                    break;
                case "delivered":
                case "opened":
                case "clicked":
                    delivered++;
                    break;
                case "bounced":
                    bounced++;
                    break;
                case "failed":
                    failed++;
                    break;
                // delivery_delayed, complained, scheduled — excluded
            }
        }

        return new ResendStatCounts(sent, delivered, bounced, failed);
    }
}
