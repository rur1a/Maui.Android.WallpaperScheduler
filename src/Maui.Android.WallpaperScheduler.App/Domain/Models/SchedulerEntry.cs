namespace MAUI.Domain.Models;

public sealed record SchedulerEntry
{
	public string Id { get; init; } = string.Empty;
	public string EndpointUrl { get; init; } = string.Empty;
	public long TriggerAtMillis { get; init; }
	public bool RepeatDaily { get; init; }

	public DateTime TriggerAtLocal =>
		DateTimeOffset.FromUnixTimeMilliseconds(TriggerAtMillis).LocalDateTime;

	public string ShortId => Id.Length > 8 ? Id[..8] : Id;

	public bool IsValid =>
		!string.IsNullOrWhiteSpace(Id) &&
		!string.IsNullOrWhiteSpace(EndpointUrl) &&
		TriggerAtMillis > 0;
}
