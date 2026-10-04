using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Generator : MonoBehaviour
{
    public bool generatorOn = false;

    [Header("ButtonPressing")]
    [SerializeField] private GeneratorButton[] buttonOrder;
    [SerializeField] private List<GeneratorButton> buttonsPressed;
    [SerializeField] private GeneratorButton[] buttonList;

    [SerializeField] private Renderer[] lights;

    [SerializeField] Material GreenLight;
    [SerializeField] Material RedLight;
    [SerializeField] private int nextButtonIndex;
    [SerializeField] GeneratorButton nextButton;

    [SerializeField] AudioSource victorySFX;

    public ParticleSystem brokenSparks;

    private void Start()
    {
        nextButtonIndex = 0;
        nextButton = buttonOrder[nextButtonIndex];
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
        Invoke(nameof(GameWon), 2.5f);
    }
    
    private void GameWon()
    {
        AudioSource source = Instantiate(victorySFX);
        GameManager.Instance?.GameOver(true);
    }

    void ButtonPressing()
    {
        if (buttonOrder[nextButtonIndex].pushed && buttonOrder.Length -1 > nextButtonIndex)
        {
            nextButtonIndex++;
            nextButton = buttonOrder[nextButtonIndex];
        }
        
        if (CorrectOrder()) 
        {
            WaterOnGenerator();
        }
    }

    public bool IsPressedButtonNextButton(GeneratorButton button)
    {
        if (nextButton == button)
        {
            buttonsPressed.Add(button);
            lights[nextButtonIndex].material = GreenLight;
            return true;
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(ResetButtons());
            return false;
        }
    }

    IEnumerator ResetButtons()
    {
        yield return new WaitForSeconds(.5f);

        foreach (var button in buttonList)
        {
            button.pushed = false;
            nextButtonIndex = 0;
            nextButton = buttonOrder[nextButtonIndex];
            buttonsPressed.Clear();
        }

        foreach (var light in lights)
        {
            light.material = RedLight;
        }
    }

    bool CorrectOrder()
    {
        int buttonsCorrect = 0;
        for (int i = 0; i < buttonsPressed.Count; i++)
        {
            if (buttonsPressed[i] == buttonOrder[i])
            {
                buttonsCorrect++;
            }
        }

        return buttonsCorrect >= buttonOrder.Length;
    }
}
