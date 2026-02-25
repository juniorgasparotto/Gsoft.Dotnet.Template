namespace Shared.Core.Entities;

using Shared.Core.Entities.Interfaces;
using System;

public class User : IEntityId
{
    public Guid Id { get; set; }
    public string UserName { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PictureUrl { get; set; }
    public Gender? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }

    public string GetFullName()
    {
        return $"{string.Join(' ', new string[] { FirstName, LastName })}";
    }
}