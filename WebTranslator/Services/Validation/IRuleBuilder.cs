namespace WebTranslator.Services.Validation
{
    public interface IRuleBuilder<T, Tproperty>
    {
        IRuleBuilder<T, Tproperty> NotNull(string? errorMessage = null);
        IRuleBuilder<T, Tproperty> NotEmpty(string? errorMessage = null);
        IRuleBuilder<T, Tproperty> Length(int min, int max, string? errorMessage = null);
        IRuleBuilder<T, Tproperty> Matches(string pattern, string? errorMessage = null);
        IRuleBuilder<T, Tproperty> Must(Func<Tproperty, bool> predicate, string? errorMessage = null);
        IRuleBuilder<T, Tproperty> MustAsync(Func<Tproperty, bool> predicate, string? errorMessage = null);
        IRuleBuilder<T, Tproperty> WithMessage(string? errorMessage = null);
        IRuleBuilder<T, Tproperty> When(Func<T, bool> condition);
        IRuleBuilder<T, Tproperty> Unless(Func<T, bool> condition);
        IRuleBuilder<T, Tproperty> DependentOn(string propertyName);
    }
}
