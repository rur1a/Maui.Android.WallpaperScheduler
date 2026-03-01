using Android.Content;
using MAUI.Domain.Models;
using System.Text.Json;

namespace MAUI.Infrastructure.Android.Scheduling;

internal static class SchedulerStorage
{
	private const string PrefsName = "wallpaper_scheduler_prefs";
	private const string KeySchedulersJson = "schedulers_json";
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

	internal static List<SchedulerEntry> LoadUnsafe(Context context)
	{
		var raw = GetPreferences(context).GetString(KeySchedulersJson, "[]") ?? "[]";
		try
		{
			var items = JsonSerializer.Deserialize<List<SchedulerEntry>>(raw, JsonOptions) ?? new List<SchedulerEntry>();
			var validItems = items.Where(item => item.IsValid).ToList();
			return SchedulerRules.DeduplicateSchedules(validItems);
		}
		catch (JsonException)
		{
			return new List<SchedulerEntry>();
		}
	}

	internal static void PersistUnsafe(Context context, IReadOnlyList<SchedulerEntry> entries)
	{
		var json = JsonSerializer.Serialize(entries, JsonOptions);
		var editor = GetPreferences(context).Edit();
		if (editor is null)
		{
			return;
		}

		editor.PutString(KeySchedulersJson, json);
		editor.Apply();
	}

	private static ISharedPreferences GetPreferences(Context context) =>
		context.GetSharedPreferences(PrefsName, FileCreationMode.Private)!;
}
