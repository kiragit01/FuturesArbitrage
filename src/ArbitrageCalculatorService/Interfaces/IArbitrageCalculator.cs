using ArbitrageCalculatorService.Models;

namespace ArbitrageCalculatorService.Interfaces;

public interface IArbitrageCalculator
{
    Task<ArbitrageResult> CalculateArbitrage(
        string firstSymbol, 
        string secondSymbol, 
        string timeFrame, 
        DateTime startTime, 
        DateTime endTime);
}