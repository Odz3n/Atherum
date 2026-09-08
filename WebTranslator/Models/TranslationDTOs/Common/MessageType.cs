namespace WebTranslator.Models.TranslationDTOs.Common
{
    public enum MessageType
    {
        // Idle/Empty states
        Idle,
        Empty,
        ReadyForInput,
        AwaitingInput,

        // User interaction states
        Typing,
        Clearing,

        // Validation states
        Valid,
        ValidationError,
        NullError,
        EmptyError, // Only when input is empty but expected
        TooLong,
        TooShort,
        InvalidCharacters,

        // Translation pipeline
        Configuring,
        DetectingLanguage,
        LanguageDetected,
        Translating,
        Translated,
        TranslationPartial,

        // Error states
        TranslationFailed,
        NetworkError,
        TimeoutError,
        ApiError,
        ServerError,

        // System
        Info,
        Warning,
        Success,

        Saving
    }
}
