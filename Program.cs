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
            Console.WriteLine("Bat dau chay Robot lay du lieu Windy...");

            // 1. Cấu hình
            string apiKey = "TQLTvjSeFdlMd27Dxni8aniBKcoumA83"; // Thay bằng Key thật của bạn
            double lat = 16.0470; // Tọa độ Đà Nẵng
            double lon = 108.2062;
            string apiUrl = "https://api.windy.com/api/point-forecast/v2";

            // 2. Gọi API GFS
            var requestData = new { lat = lat, lon = lon, model = "gfs", parameters = new[] { "precip" }, levels = new[] { "surface" }, key = apiKey };
            string jsonPayload = JsonSerializer.Serialize(requestData);

            using (HttpClient client = new HttpClient())
            {
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var rawData = JsonSerializer.Deserialize<WindyForecastResponse>(responseBody);

                    if (rawData != null && rawData.Past3hPrecipSurface != null && rawData.Past3hPrecipSurface.Count > 0)
                    {
                        // Lấy giá trị dự báo đầu tiên của ngày hôm nay
                        double amount = rawData.Past3hPrecipSurface[0] ?? 0;
                        amount = Math.Round(amount, 2);
                        string currentTime = DateTime.UtcNow.AddHours(7).ToString("dd/MM/yyyy HH:mm"); // Giờ VN

                        // 3. Ghi nối (Append) vào file CSV
                        string filePath = "WindyHistory.csv";
                        bool fileExists = File.Exists(filePath);

                        // Chữ 'true' ở đây có nghĩa là GHI NỐI TIẾP vào dòng cuối cùng, không xóa dữ liệu cũ
                        using (StreamWriter writer = new StreamWriter(filePath, true, Encoding.UTF8))
                        {
                            if (!fileExists) writer.WriteLine("ThoiGianLay,MoHinh,LuongMua(mm)");
                            writer.WriteLine($"{currentTime},GFS,{amount}");
                        }

                        Console.WriteLine($"Da ghi thanh cong: {currentTime} - {amount}mm");
                    }
                }
                else
                {
                    Console.WriteLine("Loi API: " + response.StatusCode);
                }
            }
        }
    }

    // Khuôn hứng dữ liệu (đã gom chung vào đây cho gọn)
    public class WindyForecastResponse
    {
        [JsonPropertyName("ts")] public List<long> Timestamps { get; set; }
        [JsonPropertyName("past3hprecip-surface")] public List<double?> Past3hPrecipSurface { get; set; }
    }
}