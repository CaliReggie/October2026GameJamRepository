using System.Collections;
using UnityEngine;

public class GateOpener : MonoBehaviour
{
    [SerializeField] GameObject gate;
    bool open = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (!open) StartCoroutine(OpenGate());
    }

    IEnumerator OpenGate()
    {
        open = true;
        float t = 2;
        Vector3 pos = gate.transform.position;
        while (t > 0)
        {
            t -= Time.deltaTime;
            pos.y += Time.deltaTime;
            gate.transform.position = pos;
            yield return null;
        }
    }
}
