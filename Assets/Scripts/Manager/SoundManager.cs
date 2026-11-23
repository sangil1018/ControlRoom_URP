using AYellowpaper.SerializedCollections;
using Common;
using Common.Extension;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;



/// <summary>
/// 사운드 매니져
/// </summary>
public class SoundManager : MonoSingleton<SoundManager>
{
    public class SoundChannel
    {
        public SoundTableData soundData = null;
        public AudioClip audioClip = null;
                
        public int GetTimeFromMilliseconds()
        {
            if (audioClip == null)
            {
                return 0;
            }
            float duration = audioClip.length / Mathf.Max(0.001f, soundData.pitch);
            return Mathf.RoundToInt(duration * 1000);
        }
    }

    public Dictionary<int, SoundChannel> mSoundChannelPool = new();

    private AudioSource mBgmAudio = null;
    private AudioSource mOneshotAudio = null;
    private GameObject mSfxAudioObj = null;

    private void Start()
    {
        //if (mBgmAudio == null)
        //{
        //    var bgmObj = new GameObject("BGM Audio");
        //    bgmObj.transform.SetParent(transform);
        //    mBgmAudio = bgmObj.AddComponent<AudioSource>();
        //}
        //if (mOneshotAudio == null)
        //{
        //    var oneShotAudioObj = new GameObject("Oneshot Audio");
        //    oneShotAudioObj.transform.SetParent(transform);
        //    mOneshotAudio = oneShotAudioObj.AddComponent<AudioSource>();
        //}
        //if (mSfxAudioObj == null)
        //{
        //    mSfxAudioObj = new GameObject("SFX Audio");
        //    mSfxAudioObj.transform.SetParent(transform);
        //}
    }

    public void Initialized()
    {
        if (mBgmAudio == null)
        {
            var bgmObj = new GameObject("BGM Audio");
            bgmObj.transform.SetParent(transform);
            mBgmAudio = bgmObj.AddComponent<AudioSource>();
        }
        if (mOneshotAudio == null)
        {
            var oneShotAudioObj = new GameObject("Oneshot Audio");
            oneShotAudioObj.transform.SetParent(transform);
            mOneshotAudio = oneShotAudioObj.AddComponent<AudioSource>();
        }
        if (mSfxAudioObj == null)
        {
            mSfxAudioObj = new GameObject("SFX Audio");
            mSfxAudioObj.transform.SetParent(transform);
        }
    }

    public void StopAllSounds()
    {
        if (mBgmAudio != null && mBgmAudio.isPlaying)
            mBgmAudio.Stop();

        if (mSfxAudioObj != null)
        {
            var audios = mSfxAudioObj.GetComponents<AudioSource>();
            if (audios != null)
            {
                foreach (var audio in audios)
                {
                    if (audio != null)
                        audio.Stop();
                }
            }
        }
    }  

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="GameObj"></param>
    /// <param name="type"></param>
    /// <param name="delay"></param>
    public SoundChannel PlaySound(int index)
    {
        var sndChannel = GetSoundChannel(index);
        if (sndChannel == null || sndChannel.audioClip == null)
        {           
            return null;
        }

        var soundData = sndChannel.soundData;
        if (soundData.soundType != SoundType.BGM)
        {
            if (soundData.isOneshot)
            {
                mOneshotAudio.PlayOneShot(sndChannel.audioClip);
            }
            else
            {
                var audioSource = mSfxAudioObj.AddComponent<AudioSource>();
                audioSource.loop = soundData.isLoop;
                audioSource.pitch = soundData.pitch;
                audioSource.clip = sndChannel.audioClip;               
                audioSource.Play();

                if (audioSource.loop == false)
                {
                    PlaybackAsync(audioSource).Forget();
                }
            }
        }

        return sndChannel;
    }

    public SoundChannel PlayBgm()
    {
        var sndChannel = GetSoundChannel(1);
        if (sndChannel == null || 
            sndChannel.audioClip == null || 
            sndChannel.soundData.soundType != SoundType.BGM)
        {
            return null;
        }

        if (mBgmAudio.isPlaying)
        {
            mBgmAudio.Stop();
        }

        var soundData = sndChannel.soundData;        
        mBgmAudio.loop = soundData.isLoop;
        mBgmAudio.pitch = soundData.pitch;
        mBgmAudio.clip = sndChannel.audioClip;
        mBgmAudio.Play();
        return sndChannel;
    }

    public void StopSound(AudioClip clip)
    {
        if (clip == null) return;

        var audioSources = mSfxAudioObj.GetComponents<AudioSource>();
        if (audioSources != null)
        {
            audioSources.Where(src => src != null && src.clip == clip && src.isPlaying)
                .ToList()
                .ForEach(src => src.Stop());
        }
    }

    public void StopBgm()
    {
        if (mBgmAudio.isPlaying)
        {
            mBgmAudio.Stop();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="GameObj"></param>
    /// <param name="type"></param>
    /// <param name="delay"></param>
    public SoundChannel PlayNarrationSound(string audioPath)
    {
        AudioClip audioClip = Resources.Load<AudioClip>(Path.ChangeExtension(audioPath, null));
        if (audioClip == null)
        {
            DLogger.Error($"[{audioPath}] 나레이션 오디오 클립을 불러 올수 없습니다!");
            return null;
        }

        var soundChannel = new SoundChannel()
        {
            audioClip = audioClip,
            soundData = new SoundTableData()
            {
                pitch = 1f,
            }
        };
      
        var audioSource = mSfxAudioObj.AddComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.pitch = 1f;
        audioSource.clip = audioClip;       
        audioSource.Play();
    
        PlaybackAsync(audioSource).Forget();

        return soundChannel;
    }

    private SoundChannel GetSoundChannel(int index)
    {
        if (mSoundChannelPool.ContainsKey(index) == false)
        {
            var soundData = TableManager.Instance.GetData<SoundTableData>(index);
            if (soundData == null)
            {
                DLogger.Error($"[{index}] 미등록된 사운드 인덱스가 호출 되었습니다!");
                return null;
            }
   
            AudioClip audioClip = Resources.Load<AudioClip>(Path.ChangeExtension(soundData.audioPath, null));
            if (audioClip == null)
            {
                DLogger.Error($"[{soundData.audioPath}] 오디오 클립을 불러 올수 없습니다!");
                return null;
            }

            mSoundChannelPool.Add(index, new SoundChannel()
            {
                soundData = soundData,
                audioClip = audioClip
            });
        }     

        return mSoundChannelPool[index];
    }

    private async UniTaskVoid PlaybackAsync(AudioSource audioSource)
    {
        float durationSec = audioSource.clip.length / Mathf.Max(0.001f, audioSource.pitch);
        int durationMs = Mathf.RoundToInt(durationSec * 1000);

        var cancellationToken = audioSource.gameObject.GetCancellationTokenOnDestroy();

        try
        {
            await UniTask.Delay(durationMs, ignoreTimeScale: true, cancellationToken: cancellationToken);

            audioSource.Stop();
            audioSource.clip = null;

            Destroy(audioSource);
        }
        catch (OperationCanceledException)
        {

        }
    }
}