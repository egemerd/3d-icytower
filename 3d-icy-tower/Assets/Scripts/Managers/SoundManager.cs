using System;
using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

public enum SoundType
{
    PLAYERATTACK,
    PLAYERDASH,
    PLAYERJUMP,
    PLAYERWALLBOUNCE,
    BOSS1WALLBOUNCE,
    BOSS1ENTRANCE,
    BOSS2DEATH,
    BOSS2LASERSOUND,
    BOSS2PROJECTILE,
    BOSS2START,
    MAINMENUIUSOUND,
    PLAYERGETDAMAGE
}

[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    [SerializeField] private SoundList[] soundList;
    private static SoundManager instance;
    private AudioSource audioSource;

    private void Awake()
    {
        // Eðer halihazýrda hafýzada bir SoundManager varsa ve bu gelen yenisiyse (örn: menüye geri döndün)
        if (instance != null && instance != this)
        {
            Destroy(gameObject); // Ýkincisini yok et, kirlilik yapmasýn
            return;
        }

        instance = this;

        // Bu objenin sahneler deðiþtikçe yok olmasýný ENGELLER
        DontDestroyOnLoad(gameObject);
    }
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public static void PlaySound(SoundType sound, float volume = 1)
    {
        AudioClip[] clips = instance.soundList[(int)sound].Sounds;
        AudioClip randomClip = clips[UnityEngine.Random.Range(0, clips.Length)];
        instance.audioSource.PlayOneShot(randomClip, volume);
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        string[] names = Enum.GetNames(typeof(SoundType));
        Array.Resize(ref soundList, names.Length);
        for (int i = 0; i < names.Length; i++)
        {
            soundList[i].name = names[i];
        }
    }
#endif
}

[Serializable]
public struct SoundList
{
    public AudioClip[] Sounds { get => sounds; }
    [HideInInspector] public string name;
    [SerializeField] private AudioClip[] sounds;
}
