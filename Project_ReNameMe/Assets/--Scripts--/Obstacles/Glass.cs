using UnityEngine;

public class Glass : MonoBehaviour
{
    [SerializeField] GameObject brokenMesh;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Projectile"))
        {
            brokenMesh.SetActive(true);
            brokenMesh.transform.SetParent(null);
            gameObject.SetActive(false);
        }
    }
}
