namespace ObsidianScanner.Services
{
	public interface IFileDeserializer
	{
		T? Deserialize<T>(string filePath);
	}
}
