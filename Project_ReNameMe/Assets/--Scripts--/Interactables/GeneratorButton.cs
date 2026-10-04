using UnityEngine;

public class GeneratorButton : MonoBehaviour
{
    public bool pushed = false;

    Vector3 ogPos;
    Vector3 pushPos;

    [SerializeField] float pushDistance;
    [SerializeField] Generator generator;

    private void Start()
    {
        ogPos = transform.position;
        Vector3 pos = transform.position;
        pos.y -= pushDistance;
        pushPos = pos;
    }
    private void Update()
    {
        if (pushed)
        {
            transform.position = Vector3.MoveTowards(transform.position, pushPos, Time.deltaTime);
        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, ogPos, Time.deltaTime);
        }
    }

    public void PushButton()
    {
        pushed = true;

        generator.IsPressedButtonNextButton(this);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (pushed) return;
        PushButton();
    }
}
