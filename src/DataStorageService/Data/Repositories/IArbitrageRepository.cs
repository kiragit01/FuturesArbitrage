using DataStorageService.Models;

namespace DataStorageService.Data.Repositories;

public interface IArbitrageRepository
{
    Task SaveArbitrageDataAsync(ArbitrageData data);
    Task SaveArbitrageDataBatchAsync(List<ArbitrageData> data);
    Task<IEnumerable<ArbitrageData>> GetArbitrageDataAsync(
        string firstSymbol = null,
        string secondSymbol = null,
        string timeFrame = null,
        DateTime? startTime = null,
        DateTime? endTime = null);
    Task<ArbitrageData> GetArbitrageDataByIdAsync(Guid id);
}