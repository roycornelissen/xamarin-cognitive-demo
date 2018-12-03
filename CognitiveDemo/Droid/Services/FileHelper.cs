using System;
using System.IO;
using Xamarin.Forms;
using CognitiveDemo.Droid.Services;
using CognitiveDemo.Services;

[assembly: Dependency(typeof(FileHelper))]
namespace CognitiveDemo.Droid.Services
{
	public class FileHelper : IFileHelper
	{
		public string GetLocalFilePath(string filename)
		{
			string path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			return Path.Combine(path, filename);
		}
	}
}