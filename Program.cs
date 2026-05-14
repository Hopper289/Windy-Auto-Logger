using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace WindyDataLogger
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Bat dau chay Robot lay du lieu Windy cho 4 goc Da Nang...");

            string apiKey = "TQLTvjSeFdlMd27Dxni8aniBKcoumA83"; 
            string apiUrl = "https://api.windy.com/api/point-forecast/v2";

            // Dùng mảng để không bị lỗi trên GitHub Actions
            var points = new Dictionary<string, double[]>
            {
                { "NW", new double[] { 16.20, 108.10 } },
                { "NE", new double[] { 16.20, 108.30 } },
                { "SW", new double[] { 15.90, 108.10 } },
                { "SE", new double[] { 15.90, 108.30 } }  
            };

            var rainData = new Dictionary<string, double>();

            using (HttpClient client = new HttpClient())
            {
                foreach (var pt in points)
                {
                    var requestData = new { lat = pt.Value[0], lon = pt.Value[1], model = "gfs", parameters = new[] { "precip" }, levels = new[] { "surface" }, key = apiKey };
                    string jsonPayload = JsonSerializer.Serialize(requestData);
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync(apiUrl, content);
                    double amount = 0;

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        var rawData = JsonSerializer.Deserialize<WindyForecastResponse>(responseBody);

                        if (rawData != null && rawData.Past3hPrecipSurface != null && rawData.Past3hPrecipSurface.Count > 0)
                        {
                            amount = rawData.Past3hPrecipSurface[0] ?? 0;
                            amount = Math.Round(amount, 2);
                        }
                    }
                    
                    rainData[pt.Key] = amount;
                    Console.WriteLine($"Diem {pt.Key}: {amount} mm");
                    await Task.Delay(1000); 
                }
            }

            string currentTime = DateTime.UtcNow.AddHours(7).ToString("dd/MM/yyyy HH:mm"); 
            string filePath = "WindyHistory.csv";
            bool fileExists = File.Exists(filePath);

            using (StreamWriter writer = new StreamWriter(filePath, true, Encoding.UTF8))
            {
                // Tiêu đề 5 cột chuẩn xác
                if (!fileExists) 
                {
                    writer.WriteLine("Thời gian,Mưa_TâyBắc(NW),Mưa_ĐôngBắc(NE),Mưa_TâyNam(SW),Mưa_ĐôngNam(SE)");
                }
                writer.WriteLine($"{currentTime},{rainData["NW"]},{rainData["NE"]},{rainData["SW"]},{rainData["SE"]}");
            }
        }
    }

    public class WindyForecastResponse
    {
        [JsonPropertyName("ts")] public List<long> Timestamps { get; set; }
        [JsonPropertyName("past3hprecip-surface")] public List<double?> Past3hPrecipSurface { get; set; }
    }
}
