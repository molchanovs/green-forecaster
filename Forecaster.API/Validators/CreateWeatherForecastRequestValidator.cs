using Forecaster.API.Requests;
using FluentValidation;

namespace Forecaster.API.Validators;

public class CreateWeatherForecastRequestValidator : AbstractValidator<CreateWeatherForecastRequest>
{
    private static readonly string[] ValidSummaries =
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    public CreateWeatherForecastRequestValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("Date is required.")
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.Date))
            .WithMessage("Date must be today or in the future.");

        RuleFor(x => x.TemperatureC)
            .InclusiveBetween(-60, 60)
            .WithMessage("Temperature must be between -60°C and 60°C.");

        RuleFor(x => x.Summary)
            .NotEmpty()
            .WithMessage("Summary is required.")
            .Must(s => ValidSummaries.Contains(s))
            .WithMessage($"Summary must be one of: {string.Join(", ", ValidSummaries)}.");

        RuleFor(x => x.City)
            .NotEmpty()
            .WithMessage("City is required.")
            .MaximumLength(100)
            .WithMessage("City must not exceed 100 characters.");
    }
}

