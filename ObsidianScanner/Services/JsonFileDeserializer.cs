using Newtonsoft.Json;
using System.IO;

namespace ObsidianScanner.Services
{
	public class JsonFileDeserializer : IFileDeserializer
	{
		readonly JsonSerializer _jsonSerializer;

		public JsonFileDeserializer()
		{
			_jsonSerializer = JsonSerializer.Create(new JsonSerializerSettings
			{
				Formatting = Formatting.Indented,
				NullValueHandling = NullValueHandling.Ignore,
			});
		}

		public T? Deserialize<T>(string filePath)
		{
			if (!File.Exists(filePath))
			{
				throw new FileNotFoundException($"File not found at path {filePath}");
			}

			using var streamReader = new StreamReader(filePath);
			using var reader = new JsonTextReader(streamReader);
			return _jsonSerializer.Deserialize<T>(reader);
		}

		public void Serialize<T>(string filePath, T value)
		{
			string? directory = Path.GetDirectoryName(filePath);
			if (!string.IsNullOrEmpty(directory))
			{
				Directory.CreateDirectory(directory);
			}

			using var streamWriter = new StreamWriter(filePath);
			using var writer = new JsonTextWriter(streamWriter)
			{
				Formatting = Formatting.Indented,
				Indentation = 1,
				IndentChar = '\t',
			};
			_jsonSerializer.Serialize(writer, value);
		}
	}
}
