namespace WebTranslator.Services.Validation
{
    public interface IValidator<T> where T : class
    {
        ValidationResult Validate(T instance);
        IValidator<T> RuleFor(Func<T, object> property, Func<object, bool> predicate, string errorMessage);
    }
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public Dictionary<string, List<string>> PropertyErrors { get; set; } = new();
        public void AddErrors(string error)
        {
            Errors.Add(error);
        }
        public void AddPropertyError(string property, string error)
        {
            if (!PropertyErrors.ContainsKey(property))
                PropertyErrors[property] = new List<string>();
            PropertyErrors[property].Add(error);
        }
    }
}
