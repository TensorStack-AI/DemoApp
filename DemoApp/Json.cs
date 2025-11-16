using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DemoApp
{
    public static class Json
    {
        public readonly static JsonSerializerOptions DefaultOptions;

        static Json()
        {
            DefaultOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };
        }


        public static T Load<T>(string filePath) where T : class
        {
            try
            {
                using (var jsonReader = File.OpenRead(filePath))
                {
                    return JsonSerializer.Deserialize<T>(jsonReader, DefaultOptions);
                }
            }
            catch (System.Exception ex)
            {
                return default;
            }

        }


        public static async Task<T> LoadAsync<T>(string filePath) where T : class
        {
            try
            {
                using (var jsonReader = File.OpenRead(filePath))
                {
                    return await JsonSerializer.DeserializeAsync<T>(jsonReader, DefaultOptions);
                }
            }
            catch (System.Exception)
            {
                return default;
            }
        }




        public static void Save<T>(T obj, string filePath) 
        {
            using (var jsonWriter = File.OpenWrite(filePath))
            {
                JsonSerializer.Serialize<T>(jsonWriter, obj, DefaultOptions);
            }
        }


        public static async Task SaveAsync<T>(T obj, string filePath) 
        {
            using (var jsonWriter = File.OpenWrite(filePath))
            {
                await JsonSerializer.SerializeAsync<T>(jsonWriter, obj, DefaultOptions);
            }
        }
    }
}
