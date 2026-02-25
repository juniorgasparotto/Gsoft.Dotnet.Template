namespace Shared.Infra.Module.EntityFramework.Options;

using Microsoft.EntityFrameworkCore;

public class EntityFrameworkBehaviorOptions
{
    public bool? AutoApplyMigrations { get; set; }
    public bool? AutoCreateDatabase { get; set; }
    public DeleteBehavior? DefaultDeleteBehavior { get; set; }
}
