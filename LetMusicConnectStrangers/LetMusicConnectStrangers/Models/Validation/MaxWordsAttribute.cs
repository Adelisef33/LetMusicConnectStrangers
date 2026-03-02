using System;
using System.ComponentModel.DataAnnotations;

namespace LetMusicConnectStrangers.Models.Validation
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class MaxWordsAttribute : ValidationAttribute
    {
        private readonly int _maxWords;

        public MaxWordsAttribute(int maxWords)
        {
            _maxWords = maxWords;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is null) return ValidationResult.Success;

            var str = value as string;
            if (string.IsNullOrWhiteSpace(str)) return ValidationResult.Success;

            var wordCount = str
                .Split((char[])null, StringSplitOptions.RemoveEmptyEntries)
                .Length;

            if (wordCount > _maxWords)
            {
                var msg = ErrorMessage ?? $"The field {validationContext.DisplayName} must be {_maxWords} words or fewer (currently {wordCount}).";
                return new ValidationResult(msg);
            }

            return ValidationResult.Success;
        }
    }
}