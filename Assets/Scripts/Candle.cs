using UnityEngine;

public class Candle : MonoBehaviour
{
    public ParticleSystem flame;
    public float shakeThreshold = 2.5f;

    private Vector3 lastPosition;
    private float velocity;

    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        // считаем скорость движени€
        velocity = (transform.position - lastPosition).magnitude / Time.deltaTime;
        lastPosition = transform.position;

        // если свеча тр€сЄтс€ Ч тушим
        if (flame.isPlaying && velocity > shakeThreshold)
        {
            Extinguish();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("LighterFlame"))
        {
            if (!flame.isPlaying)
            {
                flame.Play();
            }
        }
    }

    void Extinguish()
    {
        flame.Stop();
    }
}