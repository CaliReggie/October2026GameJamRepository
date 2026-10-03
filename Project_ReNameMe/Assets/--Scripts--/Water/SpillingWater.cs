using UnityEngine;

public class SpillingWater : MonoBehaviour
{
    LiquidFilling cup;

    [SerializeField] float threshold = 60;

    [SerializeField] ParticleSystem waterParticles;

    private void Start()
    {
        cup = GetComponentInChildren<LiquidFilling>();
    }

    private void Update()
    {
        cup.spilling = IsTippedOver();

        if (IsTippedOver())
        {
            if (!waterParticles.isPlaying && cup.fillAmount > 0) waterParticles.Play();
            else if (cup.fillAmount <= 0) waterParticles.Stop();
        }
        else
        {
            waterParticles.Stop();
        }
    }

    bool IsTippedOver()
    {
        return (Vector3.Angle(transform.up, Vector3.up) > threshold);
    }
}
