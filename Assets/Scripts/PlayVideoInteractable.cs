using UnityEngine;
using UnityEngine.Video;
using UnityEngine.XR.Interaction.Toolkit;

public class PlayVideoInteractable : MonoBehaviour
{
    public VideoPlayer video;

    public void PlayVideo()
    {
        if (!video.isPlaying)
            video.Play();
    }
}