using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Rigidbody))]
public class BallSound : MonoBehaviour
{
    [Header("��������� �����")]
    [SerializeField] private AudioClip collisionSound; // ���� ��� ���������������
    [SerializeField] private float minVolume = 0.2f;   // ����������� ���������
    [SerializeField] private float maxVolume = 1.0f;   // ������������ ���������
    [SerializeField] private float minSpeed = 2f;      // ����������� �������� ��� �����
    [SerializeField] private float maxSpeed = 15f;     // ������������ �������� ��� �����
    [SerializeField] private float randomPitchRange = 0.1f; // �������� ������ ����

    private AudioSource audioSource;
    private Rigidbody rb;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();

        // ��������� AudioSource
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        // ���� ���� �� �������� � ����������, ������� ��������������
        if (collisionSound == null)
        {
            Debug.LogWarning("���� �� ��������! ����������, �������� AudioClip � ��������� BallSound.");
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // ���������, ��� ������������ ���������� � ������������ (��������, � �����)
        if (collisionSound != null)
        {
            PlayCollisionSound(collision);
        }
    }

    void PlayCollisionSound(Collision collision)
    {
        // �������� �������� ����
        float speed = rb.linearVelocity.magnitude;

        // ����������� �������� ��� ������� ���������
        float volume = CalculateVolume(speed);

        // ��������� ��������� �������� ������ ���� ��� ������������ ������
        float pitch = 1f + Random.Range(-randomPitchRange, randomPitchRange);

        // ������������� ���� � ������������� �����������
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(collisionSound, volume);

        // �����������: ������� ���������� � ������� ��� �������
        Debug.Log($"�������� ����: {speed:F2}, ���������: {volume:F2}, ������ ����: {pitch:F2}");
    }

    float CalculateVolume(float speed)
    {
        // ���� �������� ������ ����������� - ���� �� �������������
        if (speed < minSpeed)
            return 0f;

        // ���� �������� ������ ������������ - ���������� ������������ ���������
        if (speed >= maxSpeed)
            return maxVolume;

        // ������������� ��������� ����� minVolume � maxVolume
        float t = (speed - minSpeed) / (maxSpeed - minSpeed);
        return Mathf.Lerp(minVolume, maxVolume, t);
    }

    // ������������ ���������� � ���������
    void OnDrawGizmosSelected()
    {
        // ���������� ��������� ��������� � ���� ����
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, minSpeed * 0.1f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxSpeed * 0.1f);
    }
}