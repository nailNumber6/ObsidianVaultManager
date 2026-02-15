using Newtonsoft.Json;
using System.IO;

namespace ObsidianScanner.Services
{
	public class JsonFileDeserializer : IFileDeserializer
	{
		readonly JsonSerializer _jsonSerializer;

		public JsonFileDeserializer()
		{
			_jsonSerializer = new JsonSerializer();
		}

		public T? Deserialize<T>(string filePath)
		{
			if (!File.Exists(filePath))
			{
				throw new FileNotFoundException($"File not found at path {filePath}");
			}
			using var streamReader = new StreamReader(filePath);
			using var reader = new JsonTextReader(streamReader);
			var deserializedObject = _jsonSerializer.Deserialize<T>(reader);
			return deserializedObject;
		}
	}
}
