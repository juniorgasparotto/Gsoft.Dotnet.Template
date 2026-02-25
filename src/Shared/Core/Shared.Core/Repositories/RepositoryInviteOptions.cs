namespace Shared.Core.Repositories;

public class RepositoryInviteOptions : RepositoryOptions
{
    public string Id { get; set; } = null!;
    public bool? Sender { get; set; }
    public bool? Accepted { get; set; }
    public string Owner { get; set; } = null!;
    public string Guest { get; set; } = null!;
    public string Entity { get; set; } = null!;
    public string EntityType { get; set; } = null!;
}
