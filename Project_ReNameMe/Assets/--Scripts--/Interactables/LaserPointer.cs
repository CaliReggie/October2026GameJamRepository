using UnityEngine;

public class LaserPointer : Interactable
{
    [SerializeField] Cat cat;
    [SerializeField] float maxBattery = 100;
    [SerializeField] float currentBattery;

    [SerializeField] float batteryDrainSpeed = 1;

    [SerializeField] Transform laserOrigin;
    [SerializeField] GameObject fakePlayer;
    [SerializeField] LayerMask layers;

    LineRenderer lineRenderer;

    bool laserActive = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Start()
    {
        base.Start();
        currentBattery = maxBattery;
        lineRenderer = GetComponentInChildren<LineRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (laserActive)
        {
            PointLaser();
            currentBattery -= Time.deltaTime * batteryDrainSpeed;
        }

        
    }

    public override void Use()
    {
        base.Use();
        laserActive = !laserActive;
        lineRenderer.enabled = laserActive;
        fakePlayer.SetActive(laserActive);
    }

    public void PointLaser()
    {
        if (GetComponentInParent<PlayerInputObject>())
        {
            PlayerInputObject playerInput = GetComponentInParent<PlayerInputObject>();
            Camera cam = playerInput.GetComponentInChildren<Camera>();
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            Debug.DrawLine(cam.transform.position, cam.transform.position + cam.transform.forward * 1000);
            if (Physics.Raycast(ray, out RaycastHit hit, 100000, layers))
            {
                lineRenderer.SetPosition(0, laserOrigin.position);
                lineRenderer.SetPosition(1, hit.point);
                fakePlayer.transform.position = hit.point;
            }
        }
    }
}
