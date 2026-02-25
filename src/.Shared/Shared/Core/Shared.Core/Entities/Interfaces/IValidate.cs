using FluentValidation.Results;

namespace Shared.Core.Entities.Interfaces
{
    public interface IValidate
    {
        ValidationResult Validate();
    }
}
