namespace Quill.Core;

// After a write the agent must not do again, a failure must name what quill wrote,
// or the agent fixes the cause, runs the command again and makes a second one.
public static class LaterStep
{
    public static async Task RunAsync(string written, string step, Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        try
        {
            await action();
        }
        catch (QuillException exception)
        {
            throw exception.WithMessage(Failed(written, step, exception));
        }
    }

    public static async Task<T> RunAsync<T>(string written, string step, Func<Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        try
        {
            return await action();
        }
        catch (QuillException exception)
        {
            throw exception.WithMessage(Failed(written, step, exception));
        }
    }

    private static string Failed(string written, string step, QuillException exception) =>
        $"{written}, but {step} failed: {exception.Message}";
}
