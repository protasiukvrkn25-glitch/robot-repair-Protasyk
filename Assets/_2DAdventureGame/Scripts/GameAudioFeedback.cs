using UnityEngine;

public static class GameAudioFeedback
{
    private static AudioClip robotHit;
    private static AudioClip robotFixed;
    private static AudioClip pickup;
    private static AudioClip playerHit;

    private static AudioClip Load(ref AudioClip clip, string path)
    {
        if (clip == null) clip = Resources.Load<AudioClip>(path);
        return clip;
    }

    public static void PlayRobotHit(Vector3 position) => Play(Load(ref robotHit, "AudioFeedback/EnemyHit1"), position);
    public static void PlayRobotFixed(Vector3 position) => Play(Load(ref robotFixed, "AudioFeedback/EnemyFix"), position);
    public static void PlayPickup(Vector3 position) => Play(Load(ref pickup, "AudioFeedback/Collectible"), position);
    public static void PlayPlayerHit(Vector3 position) => Play(Load(ref playerHit, "AudioFeedback/PlayerHit"), position);

    private static void Play(AudioClip clip, Vector3 position)
    {
        if (clip != null) AudioSource.PlayClipAtPoint(clip, position, 0.8f);
    }
}
