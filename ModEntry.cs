using System.Text;
using Microsoft.Xna.Framework.Audio;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewValley;

namespace StardewExportAudio;

/// <summary>The main entry point for the mod.</summary>
public class ModEntry : Mod
{
    /*********
    ** Public methods
    *********/
    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        helper.Events.GameLoop.GameLaunched += (_, _) =>
        {
            var outputDict = new Dictionary<string, string[]>();

            foreach (var categoryGroup in GetTracks().GroupBy(p => p.GetCategoryName()).OrderBy(p => p.Key))
            {
                foreach (TrackInfo track in categoryGroup.OrderBy(p => p.Name).ThenBy(p => p.Index))
                    outputDict[track.GetSoundbankId()] = new [] {categoryGroup.Key, track.Name};
            }

            helper.Data.WriteJsonFile("audio-tracks.json", outputDict);
            Monitor.Log($"Wrote {outputDict.Count} entries to audio-tracks.json.", LogLevel.Info);
        };
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Extract the music/sound tracks from the game's soundbank.</summary>
    private IEnumerable<TrackInfo> GetTracks()
    {
        SoundBank soundBank = this.Helper.Reflection.GetField<SoundBank>(Game1.soundBank, "soundBank").GetValue();
        IEnumerable<CueDefinition> cues = this.Helper.Reflection.GetField<Dictionary<string, CueDefinition>>(soundBank, "_cues").GetValue().Values;

        foreach (CueDefinition cue in cues)
        {
            foreach (XactSoundBankSound sound in cue.sounds)
            {
                // simple sound
                if (!sound.complexSound)
                {
                    yield return new TrackInfo(
                        WavebankIndex: sound.waveBankIndex,
                        CategoryId: sound.categoryID,
                        Name: cue.name,
                        Index: sound.trackIndex
                    );
                    continue;
                }

                // complex sound
                bool hasVariants = false;
                if (sound.soundClips != null)
                {
                    foreach (XactClip clip in sound.soundClips)
                    {
                        foreach (ClipEvent rawClipEvent in clip.clipEvents)
                        {
                            if (rawClipEvent is not PlayWaveEvent clipEvent)
                            {
                                this.Monitor.Log($"Unexpected clip event type '{rawClipEvent.GetType().FullName}'.", LogLevel.Error);
                                continue;
                            }

                            foreach (PlayWaveVariant variant in clipEvent.GetVariants())
                            {
                                hasVariants = true;

                                yield return new TrackInfo(
                                    WavebankIndex: variant.waveBank,
                                    CategoryId: sound.categoryID,
                                    Name: cue.name,
                                    Index: variant.track
                                );
                            }
                        }
                    }
                }

                // invalid sound, should never happen
                if (!hasVariants)
                    this.Monitor.Log($"Complex sound '{cue.name}' unexpectedly has no variants.", LogLevel.Error);
            }
        }
    }
}

/// <summary>A sound or music track in a wavebank.</summary>
/// <param name="WavebankIndex">The wavebank which contains the track.</param>
/// <param name="CategoryId">The sound or music category.</param>
/// <param name="Name">The sound name used in the game code.</param>
/// <param name="Index">The offset index in the raw soundbank.</param>
public record TrackInfo(int WavebankIndex, uint CategoryId, string Name, int Index)
{
    /// <summary>Get a human-readable name for the category ID.</summary>
    public string GetCategoryName()
    {
        return this.CategoryId switch
        {
            2 => "Music",
            3 => "Sound",
            4 => "Music (ambient)",
            5 => "Footsteps",
            _ => this.CategoryId.ToString()
        };
    }

    /// <summary>Get a human-readable for the wavebank index.</summary>
    public string GetWavebankName()
    {
        return this.WavebankIndex switch
        {
            0 => "Wavebank",
            1 => "Wavebank(1.4)",
            _ => this.WavebankIndex.ToString()
        };
    }

    /// <summary>Get the hexadecimal soundbank ID which matches the filenames exported by unxwb.</summary>
    public string GetSoundbankId()
    {
        return this.Index
            .ToString("X")
            .ToLower()
            .PadLeft(8, '0');
    }
}
