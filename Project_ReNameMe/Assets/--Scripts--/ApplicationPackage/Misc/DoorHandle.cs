using System.Collections;
using UnityEngine;

public class DoorHandle : MonoBehaviour
{
    bool turned;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!turned) StartCoroutine(TurnHandle());
    }

    IEnumerator TurnHandle()
    {
        turned = true;
        Vector3 rot = transform.eulerAngles;
        float t = .5f;

        while (t > 0)
        {
            t -= Time.deltaTime;
            rot.z -= Time.deltaTime * 100;
            transform.eulerAngles = rot;
            yield return null;
        }

        Vector3 doorRot = transform.parent.transform.eulerAngles;
        t = .5f;
        while (t > 0)
        {
            t -= Time.deltaTime;
            doorRot.y -= Time.deltaTime * 100;
            transform.parent.eulerAngles = doorRot;
            yield return null;
        }
    }
}
