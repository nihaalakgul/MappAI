using System.Collections.Generic;

namespace MappAI.Services
{
    public interface IOptimizationService
    {
    
        List<int> GetOptimizedRoute(int startIndex, int endIndex, double realWeatherRisk = -1);
        
        double CalculateRouteCost(List<int> route, double realWeatherRisk = -1);
    }
}