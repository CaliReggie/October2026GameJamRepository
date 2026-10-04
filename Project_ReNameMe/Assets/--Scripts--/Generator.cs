using System.Linq;
using UnityEngine;

public class Generator : MonoBehaviour
{
    public bool generatorOn = false;

    [Header("ButtonPressing")]
    [SerializeField] private GeneratorButton[] buttonOrder;
    [SerializeField] private int nextButton;

    public ParticleSystem brokenSparks;

    private void Start()
    {
        nextButton = 0;
    }

    private void Update()
    {
        ButtonPressing();
    }

    public void WaterOnGenerator()
    {
        generatorOn = false;
        if (!brokenSparks.isPlaying) brokenSparks.Play();
        Debug.Log("Generator Wet");
    }

    void ButtonPressing()
    {
        if (buttonOrder[nextButton].pushed && buttonOrder.Length -1 > nextButton)
        {
            nextButton++;
        }
        
        if (buttonOrder.All(x => x.pushed)) 
        {
            WaterOnGenerator();
        }
    }

    public bool IsPressedButtonNextButton(GeneratorButton button)
    {
        if (buttonOrder[nextButton] == button)
        {
            return true;
        }
        else
        {
            ResetButtons();
            return false;
        }
    }

    void ResetButtons()
    {
        foreach (var button in buttonOrder)
        {
            button.pushed = false;
            nextButton = 0;
        }
    }
}
