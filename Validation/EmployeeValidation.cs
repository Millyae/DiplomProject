using DiplomProject.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiplomProject.Validation
{
    public static class EmployeeValidation
    {
        public static ValidationResult ValidateHireDate(DateOnly? hireDate)
        {
            if (hireDate == null)
                return ValidationResult.Error("Дата приема обязательна");

            if (hireDate > DateOnly.FromDateTime(DateTime.Now))
                return ValidationResult.Error("Дата приема не может быть в будущем");

            if (hireDate < new DateOnly(2000, 1, 1))
                return ValidationResult.Error("Дата приема не может быть раньше 2000 года");

            return ValidationResult.Success;
        }

        public static ValidationResult ValidateEmail(string email, diplomContext context, int? currentEmployeeId = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                return ValidationResult.Error("Email обязателен");

            try
            {
                var mailAddress = new System.Net.Mail.MailAddress(email);
            }
            catch
            {
                return ValidationResult.Error("Некорректный формат email");
            }

            bool emailExists = context.Employees
                .Any(e => e.Email == email && e.IdEmployee != currentEmployeeId);

            if (emailExists)
                return ValidationResult.Error("Такой email уже существует");

            return ValidationResult.Success;
        }

        public static ValidationResult ValidatePhone(string phone, diplomContext context, int? currentEmployeeId = null)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return ValidationResult.Error("Номер телефона обязателен");

            if (phone.Length < 5)
                return ValidationResult.Error("Номер телефона слишком короткий");

            if (!phone.All(c => char.IsDigit(c) || c == '+' || c == '-' || c == '(' || c == ')' || c == ' '))
                return ValidationResult.Error("Некорректный формат телефона");

            bool phoneExists = context.Employees
                .Any(e => e.Phone == phone && e.IdEmployee != currentEmployeeId);

            if (phoneExists)
                return ValidationResult.Error("Такой номер телефона уже существует");

            return ValidationResult.Success;
        }
    }
}