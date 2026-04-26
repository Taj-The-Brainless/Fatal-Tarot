using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class UIController : MonoBehaviour
{
    public Game game;
    public GameObject titleScreenPanel;


    public Slider slider;//opponent sprite control
    public GameObject opponentSpriteObject;
    public int opponentIndex = 0;
    public Sprite[] opponentSprites; // Assign different sprites for each opponent in the Inspector


    public TMP_Text opHandindex;
    public TMP_Text trialIndex;
    public GameObject gameUI;

    public GameObject howToPlayPanel;

    private List<GameObject> opponentHearts = new List<GameObject>();
    private List<GameObject> playerHearts   = new List<GameObject>();

    public GameObject heartPrefab;
    public Transform opponentHealthAnchor;
    public Transform playerHealthAnchor;
    public float PlayerHeartSpacing = 0.5f;
    public float OpponentHeartSpacing = 0.4f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        titleScreenPanel.SetActive(true);
        opHandindex.text = "";
        trialIndex.text = "";
        gameUI.SetActive(false);
        
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 3f;
            slider.value = 1f;
            slider.onValueChanged.AddListener(UpdateOpponentSprite);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void UpdateOpponentSprite(float value)
    {
        opponentIndex = Mathf.RoundToInt(value);
        if (opponentIndex >= 0 && opponentIndex < opponentSprites.Length && opponentSpriteObject != null)
        {
            SpriteRenderer sr = opponentSpriteObject.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = opponentSprites[opponentIndex];
            }
        }
    }

    public void UpdateHealthDisplay(int playerHealth, int opponentHealth)
    {
        // Clear existing hearts
        foreach (GameObject heart in opponentHearts) Destroy(heart);
        foreach (GameObject heart in playerHearts) Destroy(heart);
        opponentHearts.Clear();
        playerHearts.Clear();

        // Create new hearts for opponent
        for (int i = 0; i < opponentHealth; i++)
        {
            GameObject heart = Instantiate(heartPrefab, opponentHealthAnchor);
            heart.transform.localPosition = Vector3.right * i * OpponentHeartSpacing;
            opponentHearts.Add(heart);
        }

        // Create new hearts for player
        for (int i = 0; i < playerHealth; i++)
        {
            GameObject heart = Instantiate(heartPrefab, playerHealthAnchor);
            heart.transform.localPosition = Vector3.right * i * PlayerHeartSpacing;
            playerHearts.Add(heart);
        }
    }

    public void startGame()
    {
        // Implement logic to start the game, e.g., load the main game scene
        titleScreenPanel.SetActive(false);
        gameUI.SetActive(true);
        StartCoroutine(game.RunGame());
        
    }
    public void LoadTitleScreen()
    {
        // Implement logic to load the title screen
        SceneManager.LoadScene("GameTable");
    }
    public void UpdateOpponentHandIndex(string index)
    {
        opHandindex.text = index;
    }
    
    public void UpdateTrialIndex(string index)
    {
        trialIndex.text = index;

    }

    public void ShowHowToPlay()
    {
        howToPlayPanel.SetActive(true);
    }
    public void HideHowToPlay()
    {
        howToPlayPanel.SetActive(false);
    }
}
