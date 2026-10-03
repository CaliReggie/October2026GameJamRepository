using UnityEngine;

public class Liquid : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        Vector3 scale = transform.localScale;
        scale.y += Time.deltaTime;
        scale.y = Mathf.Clamp(scale.y,0, 3);
        transform.localScale = scale;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent<LiquidFilling>(out var l))
        {
            l.filling = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.TryGetComponent<LiquidFilling>(out var l))
        {
            l.filling = false;
        }
    }
}
