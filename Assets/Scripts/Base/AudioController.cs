using System;
using System.Collections.Generic;
using UnityEngine;

public class AudioController : MonoBehaviour
{
  [Serializable]
  internal class AudioEntry
  {
    public string type;
    public string group;
    public AudioSource source;
    public AudioClip clip;
    public bool loop;
    [Range(0.1f, 3f)] public float pitch = 1f;
  }

  [SerializeField] private List<AudioEntry> entries = new List<AudioEntry>();
  [SerializeField] private string startupType = "bg";

  private readonly Dictionary<string, AudioEntry> map = new Dictionary<string, AudioEntry>();

  private void Awake()
  {
    foreach (var entry in entries)
    {
      if (string.IsNullOrEmpty(entry.type)) continue;
      map[entry.type] = entry;
    }

    if (!string.IsNullOrEmpty(startupType)) Play(startupType);
  }

  internal void Play(string type)
  {
    if (!map.TryGetValue(type, out var entry)) return;

    if (!string.IsNullOrEmpty(entry.group))
    {
      foreach (var other in entries)
      {
        if (other == entry) continue;
        if (other.group == entry.group) other.source.Stop();
      }
    }

    var source = entry.source;
    source.Stop();
    source.clip = entry.clip;
    source.loop = entry.loop;
    source.pitch = entry.pitch;
    source.Play();
  }

  internal void Stop(string type)
  {
    if (map.TryGetValue(type, out var entry)) entry.source.Stop();
  }

  internal void StopAll()
  {
    foreach (var entry in entries) entry.source.Stop();
  }

  internal void SetMute(string type, bool mute)
  {
    if (map.TryGetValue(type, out var entry)) entry.source.mute = mute;
  }

  private bool userMuted = false;
  private bool isForceMuted = false;
  private bool preFocusUserMuted;

  // Focus-driven mute. Called from BOTH the JS bridge path (UIManager.OnFocusChanged) and the
  // native path (OnApplicationFocus below). Never writes userMuted — regaining focus restores
  // exactly the setting the user last chose. The isForceMuted guard makes it idempotent so a
  // duplicate blur/focus signal from the other path can't clobber the stored restore state.
  internal void SetMuteAll(bool forceMute)
  {
    if (forceMute == isForceMuted) return;
    isForceMuted = forceMute;

    if (forceMute)
    {
      preFocusUserMuted = userMuted;
      foreach (var entry in entries) entry.source.mute = true;
    }
    else
    {
      foreach (var entry in entries) entry.source.mute = preFocusUserMuted;
    }
  }

  // User-toggle-driven — the sound button callback. An explicit user interaction proves the game
  // has real interactive focus, so it clears any stale forced-mute and always applies immediately.
  internal void SetUserMute(bool mute)
  {
    userMuted = mute;
    isForceMuted = false;
    preFocusUserMuted = mute;
    foreach (var entry in entries) entry.source.mute = mute;
  }

  private void OnApplicationFocus(bool focus)
  {
    SetMuteAll(!focus);
  }
}
