using UnityEngine;

public class particle_spring : MonoBehaviour
{
    public ParticleSystem particleSystem;
    float spiralSpeed = 2f;
    float fallSpeed = 0.2f;
    float spiralRadius = 0.4f;

    private ParticleSystem.Particle[] particles;
    private Vector3[] spiralOffsets;

    void Start()
    {
        if (particleSystem == null)
            particleSystem = GetComponent<ParticleSystem>();

        particles = new ParticleSystem.Particle[particleSystem.main.maxParticles];
        spiralOffsets = new Vector3[particles.Length];
    }

    void Update()
    {
        int count = particleSystem.GetParticles(particles);

        for (int i = 0; i < count; i++)
        {
            float lifePercent = 1f - (particles[i].remainingLifetime / particles[i].startLifetime);
            float angle = lifePercent * 360f * spiralSpeed;
            float radius = spiralRadius * (1f - lifePercent);

            Vector3 offset = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                -fallSpeed * Time.deltaTime,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius
            );

            particles[i].position += offset;
        }

        particleSystem.SetParticles(particles, count);
    }
}
