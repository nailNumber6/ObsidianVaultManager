namespace ObsidianScanner.Services
{
	public interface IFileDeserializer
	{
		T? Deserialize<T>(string filePath);

		void Serialize<T>(string filePath, T value);
	}
}
