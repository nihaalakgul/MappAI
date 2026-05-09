using System;

namespace MappAI.Services
{
    public static class CityData
    {
        public static readonly (string Name, double Lat, double Lng)[] NodeData = {
            ("Adalar", 40.875, 29.133), ("Arnavutköy", 41.184, 28.736), ("Ataşehir", 40.984, 29.106),
            ("Avcılar", 40.980, 28.718), ("Bağcılar", 41.033, 28.844), ("Bahçelievler", 41.002, 28.832),
            ("Bakırköy", 40.983, 28.872), ("Başakşehir", 41.097, 28.806), ("Bayrampaşa", 41.034, 28.905),
            ("Beşiktaş", 41.042, 29.008), ("Beykoz", 41.117, 29.097), ("Beylikdüzü", 41.002, 28.641),
            ("Beyoğlu", 41.036, 28.977), ("Büyükçekmece", 41.021, 28.579), ("Çatalca", 41.143, 28.461),
            ("Çekmeköy", 41.035, 29.178), ("Esenler", 41.040, 28.861), ("Esenyurt", 41.034, 28.680),
            ("Eyüpsultan", 41.046, 28.925), ("Fatih", 41.014, 28.943), ("Gaziosmanpaşa", 41.057, 28.915),
            ("Güngören", 41.021, 28.874), ("Kadıköy", 40.990, 29.025), ("Kağıthane", 41.080, 28.973),
            ("Kartal", 40.888, 29.186), ("Küçükçekmece", 41.000, 28.780), ("Maltepe", 40.931, 29.135),
            ("Pendik", 40.876, 29.234), ("Sancaktepe", 40.990, 29.227), ("Sarıyer", 41.168, 29.050),
            ("Silivri", 41.074, 28.248), ("Sultanbeyli", 40.963, 29.264), ("Sultangazi", 41.106, 28.882),
            ("Şile", 41.174, 29.613), ("Şişli", 41.066, 28.990), ("Tuzla", 40.816, 29.303),
            ("Ümraniye", 41.025, 29.099), ("Üsküdar", 41.026, 29.015), ("Zeytinburnu", 40.989, 28.903)
        };

        public static readonly string[] Nodes = new string[39];
        public static readonly double[,] DistanceMatrix = new double[39, 39];
        public static readonly double[,] TrafficMatrix = new double[39, 39];
        public static readonly double[,] WeatherMatrix = new double[39, 39];

        static CityData()
        {
            int n = NodeData.Length;
            Random rnd = new Random(42);

            for (int i = 0; i < n; i++) Nodes[i] = NodeData[i].Name;

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (i == j) continue;

                    double dist = CalculateDistance(NodeData[i].Lat, NodeData[i].Lng, NodeData[j].Lat, NodeData[j].Lng);

                    bool isEurope1 = NodeData[i].Lng < 28.99;
                    bool isEurope2 = NodeData[j].Lng < 28.99;
                    bool isCrossContinent = isEurope1 != isEurope2;

                    
                    bool isValidConnection = false;

                    if (isCrossContinent) {
                        isValidConnection = dist < 25.0; 
                    } else {
                        
                        isValidConnection = dist < 45.0; 
                    }

                    if (isValidConnection)
                    {
                        DistanceMatrix[i, j] = Math.Round(dist, 1);
                        TrafficMatrix[i, j] = Math.Round(rnd.NextDouble() * 0.7, 2);
                        WeatherMatrix[i, j] = Math.Round(rnd.NextDouble() * 0.4, 2);
                    }
                }
            }
        }

        private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371d;
            var dLat = (lat2 - lat1) * (Math.PI / 180d);
            var dLon = (lon2 - lon1) * (Math.PI / 180d);
            var a = Math.Sin(dLat / 2d) * Math.Sin(dLat / 2d) + Math.Cos(lat1 * (Math.PI / 180d)) * Math.Cos(lat2 * (Math.PI / 180d)) * Math.Sin(dLon / 2d) * Math.Sin(dLon / 2d);
            return R * (2d * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1d - a)));
        }
    }
}