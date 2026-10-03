using UnityEngine;

public class Generator : MonoBehaviour
{
    public bool generatorOn = false;

    public ParticleSystem brokenSparks;

    public void WaterOnGenerator()
    {
        generatorOn = false;
        if (!brokenSparks.isPlaying) brokenSparks.Play();
        Debug.Log("Generator Wet");
    }
}
