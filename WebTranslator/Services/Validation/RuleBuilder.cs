
namespace WebTranslator.Services.Validation
{
    public class RuleBuilder<T, Tproperty> : IRuleBuilder<T, Tproperty>
    {
        private readonly string _propertyName;

        public IRuleBuilder<T, Tproperty> DependentOn(string propertyName)
        {
            throw new NotImplementedException();
        }

        public IRuleBuilder<T, Tproperty> Length(int min, int max, string? errorMessage = null)
        {
            throw new NotImplementedException();
        }

        public IRuleBuilder<T, Tproperty> Matches(string pattern, string? errorMessage = null)
        {
            throw new NotImplementedException();
        }

        public IRuleBuilder<T, Tproperty> Must(Func<Tproperty, bool> predicate, string? errorMessage = null)
        {
            throw new NotImplementedException();
        }

        public IRuleBuilder<T, Tproperty> MustAsync(Func<Tproperty, bool> predicate, string? errorMessage = null)
        {
            throw new NotImplementedException();
        }

        public IRuleBuilder<T, Tproperty> NotEmpty(string? errorMessage = null)
        {
            throw new NotImplementedException();
        }

        public IRuleBuilder<T, Tproperty> NotNull(string? errorMessage = null)
        {
            throw new NotImplementedException();
        }

        public IRuleBuilder<T, Tproperty> Unless(Func<T, bool> condition)
        {
            throw new NotImplementedException();
        }

        public IRuleBuilder<T, Tproperty> When(Func<T, bool> condition)
        {
            throw new NotImplementedException();
        }

        public IRuleBuilder<T, Tproperty> WithMessage(string? errorMessage = null)
        {
            throw new NotImplementedException();
        }
    }
}
