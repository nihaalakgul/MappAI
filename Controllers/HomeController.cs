using Microsoft.AspNetCore.Mvc;
using MappAI.Services;
using MappAI.Models; 
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json; 

namespace MappAI.Controllers
{
    public class HomeController : Controller
    {
        private readonly IOptimizationService _optimizationService;
        private readonly WeatherApiService _weatherApiService; 

        public HomeController(IOptimizationService optimizationService)
        {
            _optimizationService = optimizationService;
            _weatherApiService = new WeatherApiService(); 
        }

        public IActionResult Index()
        {
            
            var cityNodes = new Dictionary<int, CityNodeModel>();
            
            for (int i = 0; i < CityData.NodeData.Length; i++)
            {
                cityNodes.Add(i, new CityNodeModel 
                { 
                    Name = CityData.NodeData[i].Name, 
                    Lat = CityData.NodeData[i].Lat, 
                    Lng = CityData.NodeData[i].Lng 
                });
            }

            ViewBag.CityNodesJson = JsonSerializer.Serialize(cityNodes);
            return View();
        }

    [HttpGet]
        public async Task<IActionResult> TestAgent(int start, int end)
        {
          
            if (start == end)
            {
                return Json(new { durum = "Hata", aciklama = "Başlangıç ve bitiş noktası aynı olamaz." });
            }

            try
            {
                double realWeatherRisk = await _weatherApiService.GetRealTimeWeatherRiskAsync();
                var bestRoute = _optimizationService.GetOptimizedRoute(start, end, realWeatherRisk);

                if (bestRoute == null || bestRoute.Count == 0)
                {
                    return Json(new { durum = "Hata", aciklama = "Bu iki ilçe arasında kara yolu bağlantısı kurulamadı (Deniz engeli veya mesafe)." });
                }

                double totalCost = _optimizationService.CalculateRouteCost(bestRoute, realWeatherRisk);
                var routeNames = bestRoute.Select(index => CityData.Nodes[index]).ToList();

                DateTime now = DateTime.Now;
                string dayType = (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday) ? "Hafta Sonu" : "Hafta İçi";
                string weatherStatus = realWeatherRisk >= 0.5 ? "Riskli (Yağış/Sis)" : "Açık/Güvenli";

                return Json(new { 
                    durum = "Başarılı",
                    toplamMaliyet = totalCost,
                    rotaIndeksleri = bestRoute,
                    rotaIsimleri = routeNames,
                    havaDurumu = weatherStatus,
                    sistemSaati = now.ToString("HH:mm"),
                    gunTipi = dayType
                });
            }
            catch (Exception ex)
            {
                return Json(new { durum = "Hata", aciklama = "Sistem hatası: " + ex.Message });
            }
        }

    
       
    }
}