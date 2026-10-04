using UnityEngine;

public class SpinningAcorns : MonoBehaviour
{
    [SerializeField] private float spinSpeed = 5;

    // Update is called once per frame
    void Update()
    {
        Vector3 rot = transform.eulerAngles;
        rot.y += Time.deltaTime * spinSpeed;
        transform.eulerAngles = rot;
    }
}
