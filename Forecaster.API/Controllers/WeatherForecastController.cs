using Forecaster.API.FeatureFlags;
using Forecaster.API.Metrics;
using Forecaster.API.Requests;
using Forecaster.DataContracts;
using Forecaster.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;

namespace Forecaster.API.Controllers
{
    [ApiController]
    [Route("/weather-forecasts")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> _logger;
        private readonly ForecasterMetrics _metrics;
        private readonly IWeatherForecastService _weatherForecastService;
        private readonly IValidator<CreateWeatherForecastRequest> _createValidator;
        private readonly IFeatureManager _featureManager;

        public WeatherForecastController(
            ILogger<WeatherForecastController> logger,
            ForecasterMetrics metrics,
            IWeatherForecastService weatherForecastService,
            IValidator<CreateWeatherForecastRequest> createValidator,
            IFeatureManager featureManager)
        {
            _logger = logger;
            _metrics = metrics;
            _weatherForecastService = weatherForecastService;
            _createValidator = createValidator;
            _featureManager = featureManager;
        }

        [HttpGet("five-day", Name = "Get5DWeatherForecast")]
        public IEnumerable<WeatherForecast> Get5D()
        {
            var result = _weatherForecastService.GetFiveDay().ToArray();
            _metrics.RecordRequest("five-day", result.Length);
            return result;
        }

        [HttpGet("thirty-day", Name = "Get30DWeatherForecast")]
        public IEnumerable<WeatherForecast> Get30D()
        {
            var result = _weatherForecastService.GetThirtyDay().ToArray();
            _metrics.RecordRequest("thirty-day", result.Length);
            return result;
        }

        [FeatureGate(FeatureFlagNames.AllForecast)]
        [HttpGet("all", Name = "GetAllDWeatherForecast")]
        public IEnumerable<WeatherForecast> GetAll(int days, string filter)
        {
            var result = _weatherForecastService.GetAll(days, filter).ToArray();
            _metrics.RecordRequest("all", result.Length);
            return result;
        }

        [HttpPost(Name = "CreateWeatherForecast")]
        [ProducesResponseType(typeof(WeatherForecast), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Create([FromBody] CreateWeatherForecastRequest request)
        {
            if (!await _featureManager.IsEnabledAsync(FeatureFlagNames.CreateForecast))
                throw new NotImplementedException(); 

            var validationResult = await _createValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                    ModelState.AddModelError(error.PropertyName, error.ErrorMessage);

                return ValidationProblem(ModelState);
            }

            var forecast = new WeatherForecast
            {
                Date = request.Date,
                TemperatureC = request.TemperatureC,
                Summary = request.Summary,
                City = request.City
            };

            var created = await _weatherForecastService.CreateAsync(forecast);
            _metrics.RecordRequest("create", 1);

            return CreatedAtRoute("Get5DWeatherForecast", created);
        }
    }
}
