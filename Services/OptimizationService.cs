using System;
using System.Collections.Generic;
using System.Linq;

namespace MappAI.Services
{
    public class OptimizationService : IOptimizationService
    {
        private readonly double _packagePriority = 0.8;
        private readonly Random _random = new Random(42);

        private const double TRAFFIC_THRESHOLD = 0.7;
        private const double WEATHER_THRESHOLD = 0.5;
        private const double PACKAGE_PRIORITY_THRESHOLD = 0.7;

        private double GetDynamicWeather(double apiWeather) 
        {
            return apiWeather != -1 ? apiWeather : 0.2; 
        }

        
        private double GetDynamicTraffic(int i, int j)
        {
            DateTime now = DateTime.Now;
            bool isWeekend = (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday);
            int hour = now.Hour;

          
            double traffic = CityData.TrafficMatrix[i, j]; 

            if (isWeekend)
            {
                if (hour >= 13 && hour <= 20) traffic += 0.3; 
                else if (hour >= 10 && hour < 13) traffic += 0.1;
            }
            else 
            {
                if (hour >= 7 && hour <= 9) traffic += 0.4; 
                else if (hour >= 17 && hour <= 19) traffic += 0.5;
                else if (hour >= 10 && hour <= 16) traffic += 0.2; 
            }

            bool isSchoolRush = (!isWeekend && hour >= 14 && hour <= 16); 
            if (isSchoolRush && (i == 4 || j == 4 || i == 5 || j == 5 || i == 7 || j == 7))
            {
                traffic += 0.3; 
            }

            bool isEuropeToAsia = (i <= 5 && j >= 6);
            bool isAsiaToEurope = (i >= 6 && j <= 5);

            if (!isWeekend && hour >= 7 && hour <= 9 && isAsiaToEurope) traffic += 0.4; 
            else if (!isWeekend && hour >= 17 && hour <= 19 && isEuropeToAsia) traffic += 0.4; 
            else if (isWeekend && hour >= 14 && hour <= 20 && (isEuropeToAsia || isAsiaToEurope)) traffic += 0.2; 

            return Math.Min(traffic, 1.0); 
        }

        private bool BaglantiVar(int i, int j) => CityData.DistanceMatrix[i, j] > 0;
        private bool YuksekTrafikliYol(int i, int j) => GetDynamicTraffic(i, j) >= TRAFFIC_THRESHOLD;
        private bool RiskliYol(double apiWeather) => GetDynamicWeather(apiWeather) >= WEATHER_THRESHOLD;
        private bool GuvenliYol(int i, int j, double apiWeather) => BaglantiVar(i, j) && !YuksekTrafikliYol(i, j) && !RiskliYol(apiWeather);
        private bool UrgentPackage(double priority) => priority >= PACKAGE_PRIORITY_THRESHOLD;

        private bool AlternatifVar(int current, int excludedNext, int endIndex, double apiWeather)
        {
            var visited = new HashSet<int> { current };
            var queue = new Queue<int>();
            queue.Enqueue(current);

            while (queue.Count > 0)
            {
                int node = queue.Dequeue();
                if (node == endIndex) return true;

                for (int k = 0; k < CityData.Nodes.Length; k++)
                {
                    if (node == current && k == excludedNext) continue;
                    if (CityData.DistanceMatrix[node, k] > 0 && !visited.Contains(k))
                    {
                        visited.Add(k);
                        queue.Enqueue(k);
                    }
                }
            }
            return false;
        }

        private (bool kullanilabilir, string aciklama) ApplyLogicRules(int i, int j, double priority, int endIndex, double apiWeather)
        {
            if (!BaglantiVar(i, j)) return (false, "Bağlantı Yok");
            if (RiskliYol(apiWeather) && YuksekTrafikliYol(i, j)) return (false, "Çift Risk");
            if (YuksekTrafikliYol(i, j) && AlternatifVar(i, j, endIndex, apiWeather) && !UrgentPackage(priority)) return (false, "Yüksek Trafik + Alternatif Var");
            if (!UrgentPackage(priority) && YuksekTrafikliYol(i, j)) return (false, "Paket Acil Değil + Yüksek Trafik");
            if (GuvenliYol(i, j, apiWeather)) return (true, "Güvenli Yol");
            if (YuksekTrafikliYol(i, j)) return (true, "Mecburi Kabul");
            return (true, "Kabul");
        }

        private double EdgeCost(int i, int j, int endIndex, double realWeatherRisk = -1)
        {
            var (kullanilabilir, _) = ApplyLogicRules(i, j, _packagePriority, endIndex, realWeatherRisk);
            if (!kullanilabilir) return double.PositiveInfinity;

            double distance = CityData.DistanceMatrix[i, j]; 
            double traffic = GetDynamicTraffic(i, j); 
            double weather = GetDynamicWeather(realWeatherRisk);

            double cost = distance + (5 * traffic) + (3 * weather);
            if (UrgentPackage(_packagePriority)) cost -= 2 * _packagePriority;
            return cost;
        }

        public double CalculateRouteCost(List<int> route, double realWeatherRisk = -1)
        {
            if (route == null || route.Count == 0) return double.PositiveInfinity;
            double totalCost = 0;
            int endIndex = route.Last();
            for (int i = 0; i < route.Count - 1; i++)
            {
                double cost = EdgeCost(route[i], route[i + 1], endIndex, realWeatherRisk);
                if (double.IsPositiveInfinity(cost)) return double.PositiveInfinity;
                totalCost += cost;
            }
            return totalCost;
        }

       
        private List<int> GenerateRandomRoute(int start, int end, double realWeatherRisk)
        {
            var route = new List<int> { start };
            int current = start;
            var visited = new HashSet<int> { start };
            
            while (current != end)
            {
                var neighbors = new List<int>();
                for (int i = 0; i < CityData.Nodes.Length; i++)
                {
                    if (CityData.DistanceMatrix[current, i] > 0 && !visited.Contains(i) && !double.IsPositiveInfinity(EdgeCost(current, i, end, realWeatherRisk)))
                    {
                        neighbors.Add(i);
                    }
                }
                
                if (neighbors.Count == 0) return null!;


                var endNode = CityData.NodeData[end];
                var orderedNeighbors = neighbors.OrderBy(n => 
                    Math.Pow(CityData.NodeData[n].Lat - endNode.Lat, 2) + 
                    Math.Pow(CityData.NodeData[n].Lng - endNode.Lng, 2)
                ).ToList();

            
                int topK = Math.Min(3, orderedNeighbors.Count);
                int nextNode = orderedNeighbors[_random.Next(topK)];
                
                route.Add(nextNode);
                visited.Add(nextNode);
                current = nextNode;
            }
            return route;
        }

        public List<int> GetOptimizedRoute(int startIndex, int endIndex, double realWeatherRisk = -1)
        {
            int popSize = 100, generations = 250; 
            var population = new List<List<int>>();
            
            for(int attempts=0; attempts < 1000 && population.Count < popSize; attempts++)
            {
                var route = GenerateRandomRoute(startIndex, endIndex, realWeatherRisk);
                if (route != null) population.Add(route);
            }
            
            if(population.Count == 0) return new List<int>();

            var bestRoute = population.OrderBy(r => CalculateRouteCost(r, realWeatherRisk)).First();

            for (int gen = 0; gen < generations; gen++)
            {
                var selected = population.OrderBy(r => CalculateRouteCost(r, realWeatherRisk)).Take(population.Count / 2).ToList();
                var newPop = new List<List<int>>(selected);

                while (newPop.Count < popSize)
                {
                    var p1 = selected[_random.Next(selected.Count)];
                    var p2 = selected[_random.Next(selected.Count)];
                    
                    var commonNodes = p1.Skip(1).Take(p1.Count-2).Intersect(p2.Skip(1).Take(p2.Count-2)).ToList();
                    var child = new List<int>(p1);
                    
                    if(commonNodes.Count > 0) {
                        int crossNode = commonNodes[_random.Next(commonNodes.Count)];
                        child = p1.Take(p1.IndexOf(crossNode)).ToList();
                        child.AddRange(p2.Skip(p2.IndexOf(crossNode)));
                        
                        if(child.Distinct().Count() != child.Count || double.IsPositiveInfinity(CalculateRouteCost(child, realWeatherRisk))) 
                            child = p1;
                    }
                    newPop.Add(child);
                }
                population = newPop;
                var currentBest = population.OrderBy(r => CalculateRouteCost(r, realWeatherRisk)).First();
                
                if (CalculateRouteCost(currentBest, realWeatherRisk) < CalculateRouteCost(bestRoute, realWeatherRisk)) 
                    bestRoute = currentBest;
            }
            return bestRoute;
        }
    }
}