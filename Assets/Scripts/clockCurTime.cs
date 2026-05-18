using UnityEngine;
using UnityEngine.EventSystems;

public class clockCurTime : MonoBehaviour
{
    public GameObject hourHand;
    public GameObject minuteHand;
    public GameObject secondHand;
    public AudioSource tickSound; // Добавлен компонент для звука

    private int lastSecond = -1; // Для отслеживания изменения секунд

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Проверяем наличие AudioSource
        if (tickSound == null)
        {
            Debug.LogWarning("AudioSource не назначен! Добавьте компонент AudioSource в инспекторе.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateHand();
        PlayTickSound(); // Проверяем и воспроизводим звук
    }

    void UpdateHand()
    {
        hourHand.transform.localRotation =
            Quaternion.Euler((System.DateTime.Now.Hour / 12f) * 360f + 90, 0, -90);
        minuteHand.transform.localRotation =
            Quaternion.Euler((System.DateTime.Now.Minute / 60f) * 360f + 90, 0, -90);
        secondHand.transform.localRotation =
            Quaternion.Euler((System.DateTime.Now.Second / 60f) * 360f + 90, 0, -90);
    }

    void PlayTickSound()
    {
        int currentSecond = System.DateTime.Now.Second;

        // Если секунда изменилась и звуковой компонент существует
        if (currentSecond != lastSecond && tickSound != null)
        {
            tickSound.Play(); // Воспроизводим звук
            lastSecond = currentSecond;
        }
    }
}