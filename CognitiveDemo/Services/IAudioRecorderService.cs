using System;
namespace CognitiveDemo.Services
{
	public interface IAudioRecorderService
	{
		void StartRecording();
		void StopRecording();
	}
}
