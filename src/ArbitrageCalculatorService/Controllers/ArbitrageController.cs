using System.ComponentModel.DataAnnotations;
using ArbitrageCalculatorService.Interfaces;
using ArbitrageCalculatorService.Models;
using Microsoft.AspNetCore.Mvc;
using Prometheus;

namespace ArbitrageCalculatorService.Controllers;

[ApiController]
[Route("api/arbitrage")]
public class ArbitrageController : ControllerBase
{
    private static readonly Counter arbitrageCalculationsTotal = Metrics.CreateCounter("arbitrage_calculations_total", "Number of arbitrage calculations", new string[] { "first_symbol", "second_symbol", "time_frame" });
    private static readonly Histogram arbitrageCalculationDuration = Metrics.CreateHistogram("arbitrage_calculation_duration_seconds", "Duration of arbitrage calculations", new string[] { "first_symbol", "second_symbol", "time_frame" });
    private static readonly Counter arbitrageErrorsTotal = Metrics.CreateCounter("arbitrage_errors_total", "Number of arbitrage calculation errors", new string[] { "error_type", "first_symbol", "second_symbol", "time_frame" });
    
    private readonly IArbitrageCalculator _arbitrageCalculator;
    private readonly ILogger<ArbitrageController> _logger;

    public ArbitrageController(
        IArbitrageCalculator arbitrageCalculator,
        ILogger<ArbitrageController> logger)
    {
        _arbitrageCalculator = arbitrageCalculator ?? throw new ArgumentNullException(nameof(arbitrageCalculator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet("calculate")]
    public async Task<ActionResult<ArbitrageResult>> CalculateArbitrage(
        [Required][FromQuery] string firstSymbol,
        [Required][FromQuery] string secondSymbol,
        [Required][FromQuery] string timeFrame,
        [FromQuery] DateTime startTime,
        [FromQuery] DateTime? endTime = null)
    {
        var timer = arbitrageCalculationDuration.WithLabels(firstSymbol, secondSymbol, timeFrame).NewTimer();
        try
        {
            _logger.LogInformation(
                "Calculating arbitrage between {FirstSymbol} and {SecondSymbol}, TimeFrame: {TimeFrame}, StartTime: {StartTime}",
                firstSymbol, secondSymbol, timeFrame, startTime);

            var result = await _arbitrageCalculator.CalculateArbitrage(
                firstSymbol,
                secondSymbol,
                timeFrame,
                startTime,
                endTime ?? DateTime.UtcNow);
            arbitrageCalculationsTotal.WithLabels(firstSymbol, secondSymbol, timeFrame).Inc();
            return Ok(result);
        }
        catch (Exception ex)
        {
            arbitrageErrorsTotal.WithLabels(ex.GetType().Name, firstSymbol, secondSymbol, timeFrame).Inc();
            _logger.LogError(ex, "Error calculating arbitrage");
            return StatusCode(500, new { message = "An error occurred while calculating arbitrage" });
        }
        finally
        {
            timer.Dispose();
        }
    }
}