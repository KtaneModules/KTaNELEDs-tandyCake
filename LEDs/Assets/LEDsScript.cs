using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class LEDsScript : MonoBehaviour {

    enum Col
    {
        Red,
        Orange,
        Yellow,
        Green,
        Blue,
        Purple,
        Black,
        White
    }

    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMBombModule Module;
    public KMColorblindMode Colorblind;

    public KMSelectable[] buttons;
    public GameObject[] backings;
    public MeshRenderer[] leds;
    public Material[] backingCols;
    public Material[] ledCols;
    public TextMesh[] cbTexts;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;
    private Col[][] diagrams = new Col[][]
    {
        new[]{Col.Green, Col.Black, Col.Blue, Col.Purple },
        new[]{Col.Yellow, Col.Green, Col.White, Col.Orange },
        new[]{Col.White, Col.Blue,Col.Green,Col.Yellow },
        new[]{Col.Purple,Col.Red,Col.Blue,Col.White },
        new[]{Col.Purple,Col.Orange,Col.Black,Col.Red },
        new[]{Col.White, Col.Green,Col.Orange,Col.Red },
        new[]{Col.Orange,Col.White,Col.Black,Col.Yellow },
        new[]{Col.White,Col.Yellow,Col.Blue,Col.Black },
        new[]{Col.Yellow,Col.Red,Col.Orange,Col.Blue },
        new[]{Col.Yellow,Col.Purple,Col.Blue,Col.Red },
        new[]{Col.Red,Col.Black,Col.Purple,Col.White },
        new[]{Col.Purple,Col.Black, Col.Orange,Col.Green }
    };
    Col[] chosenDiagram;
    int[] correctPositions = new int[] { 2, 0, 1, 3, 0, 1, 3, 2, 2, 3, 1, 1, 0, 3 };

    int diagramNumber;
    int offset;
    int changedButton;
    Col colorChangedTo;
    int pointer = 0;
    bool CBon;
    Col currentDisplayOnChanged;

    string[] positions = new[] { "top", "right", "bottom", "left" };
    string[] rotations = new[] { "not rotated", "rotated 90 degrees clockwise", "rotated 180 degrees", "rotated 90 degrees counterclockwise" };

    void Awake () {
        moduleId = moduleIdCounter++;
        
        foreach (KMSelectable button in buttons) 
            button.OnInteract += delegate () { ButtonPress(Array.IndexOf(buttons, button)); return false; };
        Material bg = backingCols.PickRandom();
        for (int i = 0; i < 4; i++)
            backings[i].GetComponentInChildren<MeshRenderer>().material = bg;
    }
    int[] cycleColors = Enumerable.Range(0, 8).ToArray();

    void Start ()
    {
         GetDiagram();
         GetChange();
         DisplayThings();
         DoLogging();

        /*
        int count = 12;
        int colorCount = 8;

        string colorNames = "ROYGBPKW";
        int[][] diagrams = new int[count][].Select(x => new int[4]).ToArray();
        List<int> edges = new List<int>();
        List<int> opposites = new List<int>();
        int iterations = 0;
        for (int i = 0; i < count; i++)
        {
            int[] pair;
            int[] opposite;
            do
            {
                iterations++;
                int[] numbers = Enumerable.Range(0, colorCount).ToArray().Shuffle();
                diagrams[i] = numbers.Take(4).ToArray();
                pair = new int[4];
                opposite = new int[2];
                for (int j = 0; j < 4; j++)
                    pair[j] = colorCount * diagrams[i][j] + diagrams[i][(j + 1) % 4];
                opposite[0] = colorCount * diagrams[i][0] + diagrams[i][2];
                opposite[1] = colorCount * diagrams[i][1] + diagrams[i][3];

            } while (pair.Any(x => edges.Contains(x)) || opposite.Any(x => opposites.Contains(x)));
            opposites.AddRange(opposite);
            edges.AddRange(pair);
        }
        Debug.Log(diagrams.Select(dia => dia.Select(x => colorNames[x]).Join("")).Join());
        Debug.LogFormat("Script took {0} iterations.", iterations);
        */
        //Generates a series of grids. 12 is the maximum without allowing duplicates. Allowing duplicates, 15 is the maximum but some of the diagrams are pretty gross. 

    }

    void GetDiagram()
    {
        diagramNumber = UnityEngine.Random.Range(0, diagrams.Length);
        chosenDiagram = diagrams[diagramNumber].ToArray();
        offset = UnityEngine.Random.Range(0, 4);
    }
    void GetChange()
    {
        do
        {
            changedButton = UnityEngine.Random.Range(0, 4);
            cycleColors.Shuffle();
            colorChangedTo = (Col)cycleColors[0];
        } while (changedButton == correctPositions[diagramNumber] || colorChangedTo == chosenDiagram[changedButton]);
        chosenDiagram[changedButton] = colorChangedTo;
        currentDisplayOnChanged = colorChangedTo;
    }
    void DisplayThings()
    {
        for (int i = 0; i < 4; i++)
            leds[i].material = ledCols[(int)chosenDiagram[(i - offset + 4) % 4]];
        if (Colorblind.ColorblindModeActive)
            ToggleCB();
    }
    void DoLogging()
    {
        Debug.LogFormat("[LEDs #{0}] The chosen diagram is diagram {1} in reading order.", moduleId, diagramNumber + 1);
        Debug.LogFormat("[LEDs #{0}] The initially {1} LED is changed to {2}.", moduleId, positions[changedButton], colorChangedTo.ToString());
        Debug.LogFormat("[LEDs #{0}] The initially circled LED is in position {1}.", moduleId, positions[correctPositions[diagramNumber]]);
        Debug.LogFormat("[LEDs #{0}] The diagram is then {1}.", moduleId, rotations[offset]);
        Debug.LogFormat("[LEDs #{0}] Set the {1} LED to {2}, and then press the {3} LED.", 
            moduleId, positions[(changedButton + offset) % 4], diagrams[diagramNumber][changedButton].ToString(), positions[(correctPositions[diagramNumber] + offset) % 4]);

    }

    void ButtonPress(int pos)
    {
        buttons[pos].AddInteractionPunch(0.5f);
        Audio.PlaySoundAtTransform("tink", buttons[pos].transform);
        if (moduleSolved)
            return;
        int actualButton = (pos - offset + 4) % 4;
        if (actualButton == correctPositions[diagramNumber])
            Submit();
        else if (actualButton == changedButton)
        {
            pointer++;
            pointer %= 8;
            currentDisplayOnChanged = (Col)cycleColors[pointer];
            leds[pos].material = ledCols[(int)currentDisplayOnChanged];
            SetCB(pos, currentDisplayOnChanged);
        }
        else
        {
            Module.HandleStrike();
            Debug.LogFormat("[LEDs #{0}] Pressed a button that was not the changed button or submit button. Strike!", moduleId);
        }
    }
    void Submit()
    {
        if (currentDisplayOnChanged == diagrams[diagramNumber][changedButton])
        {
            Debug.LogFormat("[LEDs #{0}] Submitted when the changed button was {1}. Module solved!", moduleId, currentDisplayOnChanged.ToString());
            moduleSolved = true;
            Module.HandlePass();
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
        }
        else
        {
            Debug.LogFormat("[LEDs #{0}] Submitted when the changed button was {1}. Strike!", moduleId, currentDisplayOnChanged.ToString());
            Module.HandleStrike();
            pointer = 0;
            currentDisplayOnChanged = (Col)cycleColors[pointer];
            leds[(changedButton + offset) % 4].material = ledCols[(int)currentDisplayOnChanged];
        }
    }

    void ToggleCB()
    {
        CBon = !CBon;
        for (int i = 0; i < 4; i++)
            SetCB(i, chosenDiagram[(i - offset + 4) % 4]);
    }

    void SetCB(int pos, Col color)
    {
        if (color != Col.Black && color != Col.White && CBon)
            cbTexts[pos].text = color.ToString().Substring(0, 1);
        else cbTexts[pos].text = string.Empty;
    }

    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use [!{0} set bottom green] to set that LED to that color. Use [!{0} press right] to press that LED once. Use [!{0} colorblind] to toggle colorblind mode.";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand (string command)
    {
        string[] colornames = new[] { "RED", "ORANGE", "YELLOW", "GREEN", "BLUE", "PURPLE", "BLACK" };
        command = command.Trim().ToUpperInvariant();
        string[] parameters = command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (new string[] { "COLORBLIND", "COLOURBLIND", "COLOR-BLIND", "COLOUR-BLIND", "CB" }.Contains(command))
        {
            yield return null;
            ToggleCB();
            yield break;
        }
        if (parameters.Length == 3 && parameters[0] == "SET" && positions.Contains(parameters[1].ToLower()) && colornames.Contains(parameters[2]))
        {
            int pressedPos = Array.IndexOf(positions, parameters[1].ToLower());
            Col submittedColor = (Col)Array.IndexOf(colornames, parameters[2]);
            yield return null;
            while (!leds[pressedPos].GetComponent<MeshRenderer>().material.name.ToUpper().StartsWith(submittedColor.ToString().ToUpper()))
            {
                buttons[pressedPos].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
        }
        else if (parameters.Length == 2 && parameters[0] == "PRESS" && positions.Contains(parameters[1].ToLower()))
        {
            yield return null;
            buttons[Array.IndexOf(positions, parameters[1].ToLower())].OnInteract();
        }
    }

    IEnumerator TwitchHandleForcedSolve ()
    {
        while (currentDisplayOnChanged != diagrams[diagramNumber][changedButton])
        {
            buttons[(changedButton + offset) % 4].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        buttons[(correctPositions[diagramNumber] + offset) % 4].OnInteract();
    }
}
