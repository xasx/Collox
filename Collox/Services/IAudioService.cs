using System.Speech.Synthesis;

namespace Collox.Services;

public interface IAudioService
{
    Task PlayBeepSoundAsync();
    Task ReadTextAsync(string text, string voiceName = null);
    ICollection<VoiceInfo> GetInstalledVoices();
}
