using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace FinalProject
{
    public sealed class SmartRoomSettingsMenu : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private VideoPlayer trainingVideo;
        [SerializeField] private SafetyGuideAgent guideAgent;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider guideSpeedSlider;
        [SerializeField] private Toggle routeToggle;
        [SerializeField] private Toggle guideToggle;
        [SerializeField] private TMP_Text statusText;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponentInChildren<CanvasGroup>(true);
        }

        public void Configure(CanvasGroup newCanvasGroup, AudioSource newMusicSource, VideoPlayer newTrainingVideo, SafetyGuideAgent newGuideAgent,
            Slider newMusicVolumeSlider, Slider newGuideSpeedSlider, Toggle newRouteToggle, Toggle newGuideToggle, TMP_Text newStatusText)
        {
            canvasGroup = newCanvasGroup;
            musicSource = newMusicSource;
            trainingVideo = newTrainingVideo;
            guideAgent = newGuideAgent;
            musicVolumeSlider = newMusicVolumeSlider;
            guideSpeedSlider = newGuideSpeedSlider;
            routeToggle = newRouteToggle;
            guideToggle = newGuideToggle;
            statusText = newStatusText;
            SyncControls();
        }

        private void Start()
        {
            SyncControls();
            SetStatus("Комната готова: меню, видео и A* помощник активны.");
        }

        public void SetMusicVolume(float value)
        {
            if (musicSource != null)
                musicSource.volume = Mathf.Clamp01(value);

            SetStatus($"Громкость музыки: {Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%");
        }

        public void SetGuideSpeed(float value)
        {
            if (guideAgent != null)
                guideAgent.MoveSpeed = value;

            SetStatus($"Скорость A* помощника: {value:0.0}");
        }

        public void SetRouteVisible(bool visible)
        {
            if (guideAgent != null)
                guideAgent.ShowRoute = visible;

            SetStatus(visible ? "Маршрут A* показан." : "Маршрут A* скрыт.");
        }

        public void SetGuideEnabled(bool enabled)
        {
            if (guideAgent != null)
            {
                guideAgent.gameObject.SetActive(enabled);
                guideAgent.SetPaused(!enabled);
            }

            SetStatus(enabled ? "Дрон-помощник включен." : "Дрон-помощник выключен.");
        }

        public void PlayTrainingVideo()
        {
            if (trainingVideo != null)
            {
                trainingVideo.Play();
                SetStatus("Видеоинструктаж запущен.");
            }
            else
            {
                SetStatus("Видео в сцене не найдено.");
            }
        }

        public void PauseTrainingVideo()
        {
            if (trainingVideo != null)
            {
                trainingVideo.Pause();
                SetStatus("Видеоинструктаж поставлен на паузу.");
            }
        }

        public void RestartGuideRoute()
        {
            if (guideAgent != null)
                guideAgent.RestartRoute();

            SetStatus("Маршрут A* перестроен.");
        }

        public void ToggleMenu()
        {
            if (canvasGroup == null)
                return;

            var nextVisible = canvasGroup.alpha <= 0.5f;
            canvasGroup.alpha = nextVisible ? 1f : 0f;
            canvasGroup.interactable = nextVisible;
            canvasGroup.blocksRaycasts = nextVisible;
        }

        private void SyncControls()
        {
            if (musicVolumeSlider != null && musicSource != null)
                musicVolumeSlider.SetValueWithoutNotify(musicSource.volume);

            if (guideSpeedSlider != null && guideAgent != null)
                guideSpeedSlider.SetValueWithoutNotify(guideAgent.MoveSpeed);

            if (routeToggle != null && guideAgent != null)
                routeToggle.SetIsOnWithoutNotify(guideAgent.ShowRoute);

            if (guideToggle != null && guideAgent != null)
                guideToggle.SetIsOnWithoutNotify(guideAgent.gameObject.activeSelf);
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }
    }
}
