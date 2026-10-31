namespace Quill.Core.Models;

/// <summary>
/// A Predecessor link. <paramref name="RelationIndex"/> is its position in the
/// server's full relation list, which is how a JSON Patch removes it.
/// </summary>
public sealed record WorkItemPredecessor(int Id, int RelationIndex);
