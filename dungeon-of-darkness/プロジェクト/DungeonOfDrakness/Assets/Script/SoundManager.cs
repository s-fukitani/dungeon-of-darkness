using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[DefaultExecutionOrder(-100)] // 他より早く初期化されるようにする
public class SoundManager : MonoBehaviour
{
    //インスペクタ設定
    [Header("Audio Mixer / Groups")]
    [Tooltip("全体のAudioMixerアセット")]
    [SerializeField] private AudioMixer audioMixer = null;

    [Tooltip("BGM用のAudioMixerGroup（Mixer内のBGMグループを割り当て）")]
    [SerializeField] private AudioMixerGroup bgmGroup = null;

    [Tooltip("SE用のAudioMixerGroup（Mixer内のSEグループを割り当て）")]
    [SerializeField] private AudioMixerGroup seGroup = null;

    [Header("Exposed Parameter Names (任意)")]
    [Tooltip("MixerでExposeしたBGMのVolumeパラメータ名（例：BGMVolume）")]
    [SerializeField] private string bgmVolumeParam = "BGMVolume";

    [Tooltip("MixerでExposeしたSEのVolumeパラメータ名（例：SEVolume）")]
    [SerializeField] private string seVolumeParam = "SEVolume";

    //定数
    public const string BGM_DIR = "audio/bgm/";         //BGMファイルフォルダ
    public const string SE_DIR = "audio/se/";           //効果音ファイルフォルダ

    //最初のSoundManagerを保存するパラメータ
    public static SoundManager soundManager { get; private set; }

    //再生に使うAudioSource
    private AudioSource bgmSource;  //BGM専用:ループ再生等に使用
    private AudioSource seSource;   //SE専用:PlayOneShotで同時多発にも対応

    private void Awake()
    {
        if (soundManager != null && soundManager != this)
        {
            //ゲームオブジェクトの破棄
            Destroy(gameObject);
            return;
        }

        soundManager = this;

        //シーンを跨いでも破棄しない
        DontDestroyOnLoad(gameObject); 

        //AudioSourceを動的に生成
        bgmSource = gameObject.AddComponent<AudioSource>();
        seSource = gameObject.AddComponent<AudioSource>();

        //Mixerグループの割当
        if (bgmGroup != null)
        {
            bgmSource.outputAudioMixerGroup = bgmGroup;
        }

        if (seGroup != null)
        {
            seSource.outputAudioMixerGroup = seGroup;
        }

        bgmSource.loop = true;
        seSource.loop = false;
        bgmSource.spatialBlend = 0.0f;
        seSource.spatialBlend = 0.0f;
    }

    void Start()
    {
        //BGMおよび効果音のボリューム初期化
        SetBGMVolume01(0.4f);
        SetSEVolume01(1.0f);
    }

    //BGMを再生する関数
    //（すでに再生中のBGMとはフェード付きで切り替え可能）
    //引数
    //bgm:BGMのファイル名
    //fadeSecondsフェード時間（秒）、0なら即切り替え
    //loop:ループ再生するかのフラグ（true:する、false:しない）
    public void PlayBGM(string bgm, float fadeSeconds = 0.0f, bool loop = true)
    {
        string bgmPath;

        bgmPath = BGM_DIR + bgm;

        AudioClip clip = Resources.Load<AudioClip>(bgmPath);

        if (clip == null)
        {
            return;
        }

        if (bgmSource.clip == clip && bgmSource.isPlaying == true)
        {
            return;
        }

        bgmSource.loop = loop;

        if (fadeSeconds > 0.0f && bgmSource.isPlaying == true)
        {
            StopAllCoroutines();
            StartCoroutine(FadeBGMAndSwitch(clip, fadeSeconds));
        }
        else
        {
            bgmSource.clip = clip;
            bgmSource.Play();
        }
    }

    //BGMを停止する関数
    //（現在再生中のBGMを停止します。フェードアウト付きも可）
    //引数
    //fadeSeconds:フェードアウト時間（秒）
    public void StopBGM(float fadeSeconds = 0.0f)
    {
        if (bgmSource.isPlaying == false)
        {
            return;
        }

        if (fadeSeconds > 0.0f)
        {
            StopAllCoroutines();
            StartCoroutine(FadeOutAndStop(bgmSource, fadeSeconds));
        }
        else
        {
            bgmSource.Stop();
        }
    }

    //効果音(SE)を再生する関数
    //（指定したAudioClipを効果音として再生、PlayOneShotを使用するため同時再生が可能）
    //引数
    //se:効果音のファイル名
    //volumeScale:音量スケール（0〜1）、デフォルト1.0
    public void PlaySE(string se, float volumeScale = 1.0f)
    {
        string sePath;

        sePath = SE_DIR + se;

        AudioClip clip = Resources.Load<AudioClip>(sePath);

        if (clip == null)
        {
            return;
        }

        seSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    //BGMが再生中かどうかをチェックする関数
    //戻り値:（true:再生中、false:再生中でない）
    public bool BGMPlayingCheck()
    {
        return bgmSource.isPlaying;
    }

    //効果音が再生中かどうかをチェックする関数
    //戻り値:（true:再生中、false:再生中でない）
    public bool SEPlayingCheck()
    {
        return seSource.isPlaying;
    }

    //BGMの音量を設定する関数（0〜1の線形値）
    //（BGM用のMixerパラメータに音量を設定）
    //引数
    //volume01:0〜1の音量値（0で無音、1で最大）
    public void SetBGMVolume01(float volume01)
    {
        if (audioMixer == null || string.IsNullOrEmpty(bgmVolumeParam) == true)
        {
            return;
        }

        audioMixer.SetFloat(bgmVolumeParam, Linear01ToDecibel(volume01));
    }

    //SEの音量を設定する関数（0〜1の線形値）
    //（効果音用のMixerパラメータに音量を設定）
    //引数
    //volume01:0〜1の音量値（0で無音、1で最大）
    public void SetSEVolume01(float volume01)
    {
        if (audioMixer == null || string.IsNullOrEmpty(seVolumeParam) == true)
        {
            return;
        }

        audioMixer.SetFloat(seVolumeParam, Linear01ToDecibel(volume01));
    }

    //線形音量(0〜1)をdB値（-80〜0dB程度）に変換する関数
    //引数
    ///v:0〜1の音量値
    private float Linear01ToDecibel(float v)
    {
        if (v <= 0.0001f)
        {
            return -80.0f;
        }

        return Mathf.Log10(v) * 20.0f;
    }

    //BGMをフェードアウトして切り替えるコルーチン
    //（現在のBGMをフェードアウトして、新しいクリップに切り替える）
    //引数
    //nextClip:次に再生するAudioClip
    //seconds:フェードにかける秒数
    //戻り値:IEnumerator（コルーチンとして使用）
    private System.Collections.IEnumerator FadeBGMAndSwitch(AudioClip nextClip, float seconds)
    {
        float startVolume = bgmSource.volume;
        float t = 0.0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0.0f, t / seconds);
            yield return null;
        }

        bgmSource.Stop();
        bgmSource.clip = nextClip;
        bgmSource.Play();

        t = 0.0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(0.0f, startVolume, t / seconds);
            yield return null;
        }

        bgmSource.volume = startVolume;
    }

    //AudioSourceをフェードアウトして停止するコルーチン
    //（指定したAudioSourceをフェードアウトして停止します）
    //引数
    //src:停止対象のAudioSource
    //seconds:フェードアウト時間（秒）
    //戻り値:IEnumerator（コルーチンとして使用）
    private System.Collections.IEnumerator FadeOutAndStop(AudioSource src, float seconds)
    {
        float startVolume = src.volume;
        float t = 0.0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(startVolume, 0.0f, t / seconds);
            yield return null;
        }
        src.Stop();
        src.volume = startVolume;
    }

    //音声データの長さ（秒）を取得する関数
    //引数
    //bgm:対象のBGMファイル
    //戻り値:（音声データの長さ）
    public float GetBGMLength(string bgm)
    {
        string bgmPath;

        bgmPath = BGM_DIR + bgm;
        AudioClip clip = Resources.Load<AudioClip>(bgmPath);

        float bgmLength = clip.length;
        return bgmLength;
    }

    #region デバッグ
    //SoundManagerチェック用関数
    public void DebugCheckMixer()
    {
        // 1) ルーティング確認
        Debug.Log($"[SM] bgmGroup assigned? {(bgmGroup != null)}");
        Debug.Log($"[SM] bgmSource.output = {bgmSource.outputAudioMixerGroup?.name}");

        // 2) Mixer 割り当て確認
        Debug.Log($"[SM] audioMixer assigned? {(audioMixer != null)}");

        // 3) Exposed 名確認（GetFloatで存在チェック）
        float val = 0.0f;
        bool hasParam = audioMixer != null && audioMixer.GetFloat(bgmVolumeParam, out val);
        Debug.Log($"[SM] Exposed '{bgmVolumeParam}' exists? {hasParam} (current={ (hasParam ? val.ToString("F1") : "N/A") } dB)");

        // 4) 極端値テスト
        SetBGMVolume01(0f);  // 無音になるはず
        Debug.Log("[SM] SetBGMVolume01(0) → 無音になるか確認してください");
        SetBGMVolume01(1f);  // 元に戻るはず
        Debug.Log("[SM] SetBGMVolume01(1) → 元の音量に戻るか確認してください");

    }
    #endregion
}
