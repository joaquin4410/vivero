using System.Collections.Generic;

namespace vivero.Models
{
    public class WeatherData
    {
        public string Name { get; set; } // Ciudad o lugar
        public WeatherMain Main { get; set; }
        public List<WeatherDescription> Weather { get; set; }
    }
    public class WeatherMain
    {
        public double Temp { get; set; }
        public double Feels_like { get; set; }
        public int Humidity { get; set; }
    }

    public class WeatherDescription
    {
        public string Main { get; set; }
        public string Description { get; set; }
    }
}
