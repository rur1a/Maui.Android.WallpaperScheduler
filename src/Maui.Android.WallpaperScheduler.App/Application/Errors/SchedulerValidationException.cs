namespace MAUI.Application.Errors;

public sealed class SchedulerValidationException : Exception
{
	public SchedulerValidationException(string message)
		: base(message)
	{
	}
}
