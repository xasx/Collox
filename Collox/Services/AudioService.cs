using System.Speech.Synthesis;
using System.Media;
using Windows.ApplicationModel;
using Serilog;

namespace Collox.Services;

public class AudioService : IAudioService
{
    private static readonly ILogger Logger = Log.ForContext<AudioService>();
    private static readonly Lazy<ICollection<VoiceInfo>> _voiceInfos = new(
        () => [.. new SpeechSynthesizer().GetInstalledVoices().Select(iv => iv.VoiceInfo)]);

    public ICollection<VoiceInfo> GetInstalledVoices() => _voiceInfos.Value;

    public async Task PlayBeepSoundAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                var installedPath = Package.Current.InstalledLocation.Path;
                var sp = new SoundPlayer(Path.Combine(installedPath, "Assets", "notify.wav"));
                sp.PlaySync();
            }).ConfigureAwait(false);
            Logger.Debug("Beep sound played successfully");
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to play beep sound");
        }
    }

    public async Task ReadTextAsync(string text, string voiceName = null)
    {
        Logger.Debug("Reading text with voice: {Voice}, TextLength: {Length}", voiceName ?? "Default", text.Length);

        try
        {
            await Task.Run(() =>
            {
                using var speechSynthesizer = new SpeechSynthesizer();
                speechSynthesizer.SetOutputToDefaultAudioDevice();

                if (voiceName == null)
                {
                    speechSynthesizer.SelectVoiceByHints(VoiceGender.Male, VoiceAge.Adult);
                }
                else
                {
                    speechSynthesizer.SelectVoice(voiceName);
                }

                speechSynthesizer.Speak(text);
            }).ConfigureAwait(false);
            Logger.Debug("Text reading completed successfully");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to read text with voice: {Voice}", voiceName);
        }
    }
}
