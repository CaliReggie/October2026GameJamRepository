using UnityEngine;

public class LiquidFilling : MonoBehaviour
{
    Material liquidShader;

    [SerializeField] float fillSpeed = 1;

    public bool filling = false;
    public bool spilling = false;

    [Range(0,1)] public float fillAmount;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        liquidShader = GetComponent<Renderer>().material;
    }

    // Update is called once per frame
    void Update()
    {
        liquidShader.SetFloat("_Fill", fillAmount);

        if (filling)
        {
            fillAmount += Time.deltaTime * fillSpeed;
        }

        if (spilling)
        {
            fillAmount -= Time.deltaTime * fillSpeed;
        }

        fillAmount = Mathf.Clamp01(fillAmount);
    }
}
