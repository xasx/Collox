using System.Speech.Synthesis;

namespace Collox.Services;

public interface IAudioService
{
    Task PlayBeepSoundAsync(CancellationToken cancellationToken = default);
    Task ReadTextAsync(string text, string voiceName = null, CancellationToken cancellationToken = default);
    ICollection<VoiceInfo> GetInstalledVoices();
}
