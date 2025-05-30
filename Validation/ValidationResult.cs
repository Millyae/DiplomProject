using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiplomProject.Validation
{
    public class ValidationResult
    {
        public bool IsValid { get; }
        public string ErrorMessage { get; }

        public ValidationResult(bool isValid, string errorMessage = null)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }

        public static ValidationResult Success => new ValidationResult(true);

        public static ValidationResult Error(string message) => new ValidationResult(false, message);
    }
}
