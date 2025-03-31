using DataStorageService.Models;
using Microsoft.EntityFrameworkCore;
using Prometheus;

namespace DataStorageService.Data.Repositories;

public class ArbitrageRepository : IArbitrageRepository
    {
        private static readonly Counter savedRecords = Metrics.CreateCounter("datastorage_saved_records_total", "Number of saved arbitrage records");
        private readonly AppDbContext _dbContext;
        private readonly ILogger<ArbitrageRepository> _logger;

        public ArbitrageRepository(
            AppDbContext dbContext,
            ILogger<ArbitrageRepository> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task SaveArbitrageDataAsync(ArbitrageData data)
        {
            try
            {
                _logger.LogInformation("Saving arbitrage data with ID {Id}", data.Id);
                
                // Устанавливаем время создания записи, если оно не установлено
                if (data.CreatedAt == default)
                {
                    data.CreatedAt = DateTime.UtcNow;
                }
                else
                {
                    data.CreatedAt = data.CreatedAt.Kind == DateTimeKind.Unspecified 
                        ? DateTime.SpecifyKind(data.CreatedAt, DateTimeKind.Utc) 
                        : data.CreatedAt.ToUniversalTime();
                }
                data.Timestamp = data.Timestamp.Kind == DateTimeKind.Unspecified 
                    ? DateTime.SpecifyKind(data.Timestamp, DateTimeKind.Utc) 
                    : data.Timestamp.ToUniversalTime();
                
                // Если ID пустой, генерируем новый
                if (data.Id == Guid.Empty)
                {
                    data.Id = Guid.NewGuid();
                }
                
                await _dbContext.ArbitrageData.AddAsync(data);
                await _dbContext.SaveChangesAsync();
                savedRecords.Inc();
                _logger.LogInformation("Successfully saved arbitrage data with ID {Id}", data.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving arbitrage data");
                throw;
            }
        }
        
        public async Task SaveArbitrageDataBatchAsync(List<ArbitrageData> dataList)
        {
            try
            {
                _logger.LogInformation("Saving batch of {Count} arbitrage data points", dataList.Count);
        
                await _dbContext.Database.BeginTransactionAsync();
                foreach (var data in dataList)
                {
                    if (data.Id == Guid.Empty)
                        data.Id = Guid.NewGuid();
                    if (data.CreatedAt == default)
                    {
                        data.CreatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        data.CreatedAt = data.CreatedAt.Kind == DateTimeKind.Unspecified 
                            ? DateTime.SpecifyKind(data.CreatedAt, DateTimeKind.Utc) 
                            : data.CreatedAt.ToUniversalTime();
                    }
                    data.Timestamp = data.Timestamp.Kind == DateTimeKind.Unspecified 
                        ? DateTime.SpecifyKind(data.Timestamp, DateTimeKind.Utc) 
                        : data.Timestamp.ToUniversalTime();
                    
                    await _dbContext.ArbitrageData.AddAsync(data);
                    savedRecords.Inc();
                }
                await _dbContext.SaveChangesAsync();
                await _dbContext.Database.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                await _dbContext.Database.RollbackTransactionAsync();
                _logger.LogError(ex, "Error saving batch of arbitrage data");
                throw;
            }
        }

        public async Task<IEnumerable<ArbitrageData>> GetArbitrageDataAsync(
            string firstSymbol = null,
            string secondSymbol = null,
            string timeFrame = null,
            DateTime? startTime = null,
            DateTime? endTime = null)
        {
            try
            {
                _logger.LogInformation("Getting arbitrage data with filters");
                
                var query = _dbContext.ArbitrageData.AsQueryable();
                
                // Применяем фильтры если они заданы
                if (!string.IsNullOrEmpty(firstSymbol))
                {
                    query = query.Where(d => d.FirstSymbol == firstSymbol);
                }
                
                if (!string.IsNullOrEmpty(secondSymbol))
                {
                    query = query.Where(d => d.SecondSymbol == secondSymbol);
                }
                
                if (!string.IsNullOrEmpty(timeFrame))
                {
                    query = query.Where(d => d.TimeFrame == timeFrame);
                }
                
                if (startTime.HasValue)
                {
                    query = query.Where(d => d.Timestamp >= startTime.Value);
                }
                
                if (endTime.HasValue)
                {
                    query = query.Where(d => d.Timestamp <= endTime.Value);
                }
                
                // Сортируем по времени
                query = query.OrderBy(d => d.Timestamp);
                
                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving arbitrage data");
                throw;
            }
        }

        public async Task<ArbitrageData> GetArbitrageDataByIdAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Getting arbitrage data by ID {Id}", id);
                
                return await _dbContext.ArbitrageData
                    .FirstOrDefaultAsync(d => d.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving arbitrage data by ID {Id}", id);
                throw;
            }
        }
    }