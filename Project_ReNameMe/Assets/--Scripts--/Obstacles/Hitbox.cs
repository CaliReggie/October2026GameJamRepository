using UnityEngine;

public class Hitbox : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent<Rigidbody>(out var rb))
        {
            Debug.Log(other.gameObject.name);
            rb.AddForce((transform.position + Random.insideUnitSphere - transform.position) * 5, ForceMode.Impulse);

            if (other.gameObject.TryGetComponent<PlayerObjectPioComponent>(out var input))
            {
                input.GetHitForDamage(1);
            }

            if (other.gameObject.TryGetComponent<HingeJoint>(out var joint))
            {
                Debug.Log("Hit Cat Door");
                JointSpring newJoint = joint.spring;
                newJoint.spring = 0;
                joint.spring = newJoint;
                if (joint.connectedBody != null)
                {
                    joint.connectedBody.AddForce(transform.right * 10, ForceMode.Impulse);
                }
                joint.connectedBody = null;
            }
        }
    }
}
