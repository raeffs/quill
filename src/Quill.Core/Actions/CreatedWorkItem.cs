namespace Quill.Core.Actions;

// After the create, a failure must name the new work item, or the agent fixes the
// cause, creates again and makes a second one.
internal static class CreatedWorkItem
{
    public static async Task LinkAsync(IAzureDevOpsClient client, int id, IEnumerable<int> targetIds)
    {
        try
        {
            foreach (var targetId in targetIds)
            {
                await client.AddRelationAsync(id, targetId);
            }
        }
        catch (UnavailableException exception)
        {
            throw new UnavailableException(Failed(id, "linking it failed", exception), exception.RetryAfter, exception);
        }
        catch (RejectedException exception)
        {
            throw new RejectedException(Failed(id, "linking it failed", exception), exception);
        }
        catch (DeniedException exception)
        {
            throw new DeniedException(Failed(id, "linking it failed", exception), exception);
        }
        catch (UsageException exception)
        {
            throw new UsageException(Failed(id, "linking it failed", exception), exception);
        }
    }

    public static string Failed(int id, string step, QuillException exception) =>
        $"Created work item {id}, but {step}: {exception.Message}";
}
