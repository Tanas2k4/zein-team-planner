//CHECK CONSTRAINTS ABOUT TIME 
using System.ComponentModel.DataAnnotations;

namespace ZEIN_TeamPlanner.Shared
{
    public class FutureDateAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success!;
            }

            if (value is DateTime date)
            {
                if (date <= DateTime.UtcNow)
                {
                    return new ValidationResult(ErrorMessage ?? "Ngày phải lớn hơn thời điểm hiện tại.");
                }
            }
            else if (value is DateTimeOffset dateOffset)
            {
                if (dateOffset <= DateTimeOffset.UtcNow)
                {
                    return new ValidationResult(ErrorMessage ?? "Thời gian phải lớn hơn thời điểm hiện tại.");
                }
            }

            return ValidationResult.Success!;
        }
    }

    public class EndTimeGreaterThanStartTimeAttribute : ValidationAttribute
    {
        private readonly string _startTimePropertyName;

        public EndTimeGreaterThanStartTimeAttribute(string startTimePropertyName)
        {
            _startTimePropertyName = startTimePropertyName;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success!;
            }

            var startTimeProperty = validationContext.ObjectType.GetProperty(_startTimePropertyName);
            if (startTimeProperty == null)
            {
                return new ValidationResult($"Unknown property: {_startTimePropertyName}");
            }

            var startTimeValue = startTimeProperty.GetValue(validationContext.ObjectInstance);
            if (startTimeValue == null)
            {
                return new ValidationResult("Thời gian bắt đầu không được để trống.");
            }

            if (value is DateTimeOffset endTime && startTimeValue is DateTimeOffset startTime)
            {
                if (endTime <= startTime)
                {
                    return new ValidationResult(ErrorMessage ?? "Thời gian kết thúc phải lớn hơn thời gian bắt đầu.");
                }
            }

            return ValidationResult.Success!;
        }
    }
}