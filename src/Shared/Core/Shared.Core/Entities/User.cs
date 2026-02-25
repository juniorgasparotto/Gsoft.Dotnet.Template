namespace Shared.Core.Entities;

using Shared.Core.Entities.Interfaces;
using System;

public class User : IEntityId
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string PictureUrl { get; set; } = null!;
    public Gender? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }

    public string GetFullName()
    {
        return $"{string.Join(' ', new string[] { FirstName, LastName })}";
    }
}