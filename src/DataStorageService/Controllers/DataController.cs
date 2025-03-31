using System.ComponentModel.DataAnnotations;
using DataStorageService.Data.Repositories;
using DataStorageService.Models;
using Microsoft.AspNetCore.Mvc;
using Prometheus;

namespace DataStorageService.Controllers;

[ApiController]
[Route("api/data")]
public class DataController : ControllerBase
{
    private static readonly Counter datastorageSaveRequestsTotal = Metrics.CreateCounter("datastorage_save_requests_total", "Number of data storage save requests", new string[] { "table_name" });
    private static readonly Histogram datastorageSaveDuration = Metrics.CreateHistogram("datastorage_save_duration_seconds", "Duration of data storage save operations", new string[] { "table_name" });
    private static readonly Counter datastorageErrorsTotal = Metrics.CreateCounter("datastorage_errors_total", "Number of data storage errors", new string[] { "error_type", "table_name" });
    
    private readonly IArbitrageRepository _repository;
    private readonly ILogger<DataController> _logger;

    public DataController(
        IArbitrageRepository repository,
        ILogger<DataController> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("arbitrage")]
    public async Task<ActionResult> SaveArbitrageData([FromBody] ArbitrageData data)
    {
        var timer = datastorageSaveDuration.WithLabels("DataArbitrage").NewTimer();
        try
        {
            _logger.LogInformation(
                "Saving arbitrage data between {FirstSymbol} and {SecondSymbol}, TimeFrame: {TimeFrame}",
                data.FirstSymbol, data.SecondSymbol, data.TimeFrame);
            
            
            await _repository.SaveArbitrageDataAsync(data);
            datastorageSaveRequestsTotal.WithLabels("DataArbitrage").Inc();
            return Ok(new { message = "Data saved successfully" });
        }
        catch (Exception ex)
        {
            datastorageErrorsTotal.WithLabels(ex.GetType().Name, "DataArbitrage").Inc();
            _logger.LogError(ex, "Error saving arbitrage data");
            return StatusCode(500, new { message = "An error occurred while saving data" });
        }
        finally
        {
            timer.Dispose();
        }
    }
    
    [HttpPost("arbitrage/batch")]
    public async Task<ActionResult> SaveArbitrageDataBatch([FromBody] List<ArbitrageData> dataList)
    {
        var timer = datastorageSaveDuration.WithLabels("DataArbitrage").NewTimer();
        try
        {
            _logger.LogInformation("Saving batch of {Count} arbitrage data points", dataList.Count);
            await _repository.SaveArbitrageDataBatchAsync(dataList);
            datastorageSaveRequestsTotal.WithLabels("DataArbitrage").Inc();
            return Ok(new { message = "Data saved successfully" });
        }
        catch (Exception ex)
        {
            datastorageErrorsTotal.WithLabels(ex.GetType().Name, "DataArbitrage").Inc();
            _logger.LogError(ex, "Error saving batch of arbitrage data");
            return StatusCode(500, new { message = "An error occurred while saving data" });
        }
        finally
        {
            timer.Dispose();
        }
    }

    [HttpGet("arbitrage")]
    public async Task<ActionResult<IEnumerable<ArbitrageData>>> GetArbitrageData(
        [FromQuery] string firstSymbol = null,
        [FromQuery] string secondSymbol = null,
        [FromQuery] string timeFrame = null,
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null)
    {
        try
        {
            _logger.LogInformation("Getting arbitrage data with filters: FirstSymbol={FirstSymbol}, SecondSymbol={SecondSymbol}, TimeFrame={TimeFrame}",
                firstSymbol, secondSymbol, timeFrame);
            if (startTime != null)
            {
                startTime = ((DateTime)startTime).Kind == DateTimeKind.Unspecified 
                    ? DateTime.SpecifyKind((DateTime)startTime, DateTimeKind.Utc) 
                    : ((DateTime)startTime).ToUniversalTime();
            }

            if (endTime != null)
            {
                endTime = ((DateTime)endTime).Kind == DateTimeKind.Unspecified 
                    ? DateTime.SpecifyKind((DateTime)endTime, DateTimeKind.Utc) 
                    : ((DateTime)endTime).ToUniversalTime();
            }
            
            var data = await _repository.GetArbitrageDataAsync(
                firstSymbol, 
                secondSymbol, 
                timeFrame, 
                startTime, 
                endTime);
            
            return Ok(data);
        }
        catch (Exception ex)
        {
            datastorageErrorsTotal.WithLabels(ex.GetType().Name, "DataArbitrage").Inc();
            _logger.LogError(ex, "Error retrieving arbitrage data");
            return StatusCode(500, new { message = "An error occurred while retrieving data" });
        }
    }

    [HttpGet("arbitrage/{id}")]
    public async Task<ActionResult<ArbitrageData>> GetArbitrageDataById([Required] Guid id)
    {
        try
        {
            _logger.LogInformation("Getting arbitrage data by ID: {Id}", id);

            var data = await _repository.GetArbitrageDataByIdAsync(id);

            if (data == null)
            {
                return NotFound(new { message = $"Arbitrage data with ID {id} not found" });
            }

            return Ok(data);
        }
        catch (Exception ex)
        {
            datastorageErrorsTotal.WithLabels(ex.GetType().Name, "DataArbitrage").Inc();
            _logger.LogError(ex, "Error retrieving arbitrage data by ID");
            return StatusCode(500, new { message = "An error occurred while retrieving data" });
        }
    }
}