namespace DLSearch.Search;

public abstract record OutputEvent;

public sealed record WalkFinished(IReadOnlyList<NameMatch> Names) : OutputEvent;

public sealed record FileMatched(ContentMatch Match) : OutputEvent;
