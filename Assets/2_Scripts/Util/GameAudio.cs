using UnityEngine;

[CreateAssetMenu(menuName = "Break The Wax World/Game Audio", fileName = "GameAudio")]
public class GameAudio : ScriptableObject
{
    public AudioClip bgm;
    public AudioClip click;
    public AudioClip sfxPreview; // 설정에서 효과음 막대를 움직일 때 들려줄 소리 (파편 줍는 소리)

    private static GameAudio _instance;

    public static GameAudio Instance => _instance != null ? _instance : (_instance = Resources.Load<GameAudio>("GameAudio"));
}
