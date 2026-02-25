namespace Shared.Core.Repositories;

public class RepositoryInviteOptions : RepositoryOptions
{
    public string Id { get; set; }
    public bool? Sender { get; set; }
    public bool? Accepted { get; set; }
    public string Owner { get; set; }
    public string Guest { get; set; }
    public string Entity { get; set; }
    public string EntityType { get; set; }
}
