using System.Threading.Tasks;

namespace CognitiveDemo.Services
{
	public interface ISpeechRecognitionService
	{
		Task<string> Recognize();
	}
}
