using UnityEngine;

public class CheckParticleForGenerator : MonoBehaviour
{
    private void OnParticleCollision(GameObject other)
    {
        if (other.gameObject.TryGetComponent<Generator>(out var gen))
        {
            gen.WaterOnGenerator();
        }
    }
}
