using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class Cheating : MonoBehaviour
{
    // get button
    public List<GameObject> cheatingOptions; 
    public TMP_Text cheatingLabel;

    public bool  active = false;

    //stone
    public GameObject stonePrefab;
    public Transform stoneSpawnPoint;
    public Transform stoneTargetPoint;
    public float stoneSpeed = 5f;

    public bool cheatedThisTurn = false;

    public TMP_Text opponentHandIndexText;


    public OpponentHand opponentHandScript;

    public PlayerHand playerCardsScript;
    public Game gameScript;

    public UIController uiControllerScript;

    public GameObject timerChallenge;

    public GameObject anim;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(active && !cheatedThisTurn){
            cheatingLabel.text = "Options";

            if(Input.GetKeyDown(KeyCode.V)){
                // throw stone
                ThrowStone();
                cheatedThisTurn = true;
                DeactivateCheating();
                playerCardsScript.isCheating = false;
                

            }
            if (Input.GetKeyDown(KeyCode.B)){
                //heal health
                gameScript.playerHealth++;
                uiControllerScript.UpdateHealthDisplay( gameScript.playerHealth, gameScript.opponentHealth);
                cheatedThisTurn = true;
                DeactivateCheating();
                playerCardsScript.isCheating = false;
                playerCardsScript.MoveHandTo(playerCardsScript.activeHandTransform);
            }
            if (Input.GetKeyDown(KeyCode.N)){
                // damage opponent
                timerChallenge.SetActive(true);
                timerChal timerScript = timerChallenge.GetComponent<timerChal>();

                //wait few second for result 
                timerScript.timerRange = (0.15f - (uiControllerScript.opponentIndex*0.05f)*0.9f)*0.3f;
                anim.SetActive(true);
                anim.GetComponent<Animator>().Play("sword", -1, 0f);
                bool success = timerScript.OnEnable(); // smaller time range for success
                //wait few second for result 
                StartCoroutine(WaitSecond());
                
                
            }
                
            

        }
        else{
            cheatingLabel.text = "Press C to Cheat";
        }
    }
    
    IEnumerator WaitSecond()
    {
        yield return new WaitForSeconds(1.2f);
        bool success = timerChallenge.GetComponent<timerChal>().success;
        
        if (!success){
            anim.SetActive(false);
            gameScript.playerHealth--;
            uiControllerScript.UpdateHealthDisplay( gameScript.playerHealth, gameScript.opponentHealth);
            if(gameScript.playerHealth == 0){
                timerChallenge.SetActive(false);
                gameScript.OnGameOver();
            }
        }
        if(success){
            uiControllerScript.opHandindex.text = "";
            uiControllerScript.trialIndex.text = "";
            yield return new WaitForSeconds(3f);
            timerChallenge.SetActive(false);
            gameScript.opponentHealth = 0;
            gameScript.OnGameOver();
        }
        cheatedThisTurn = true;
        DeactivateCheating();
        playerCardsScript.isCheating = false;
        playerCardsScript.MoveHandTo(playerCardsScript.activeHandTransform);
        
    }
    public void ThrowStone(){
        GameObject stone = Instantiate(stonePrefab, stoneSpawnPoint.position, Quaternion.identity);
        StartCoroutine(MoveStone(stone));
        opponentHandScript.PeekAllCards();
        opponentHandIndexText.gameObject.SetActive(true);
        // set each opponent card indexes in text "00 11 13"
        string indexesText = "";
        foreach(int index in opponentHandScript.cardIndices){
            indexesText += index.ToString("D2") + " ";
        }
        opponentHandIndexText.text = indexesText;

    }

    private IEnumerator MoveStone(GameObject stone){
        while(stone != null && Vector3.Distance(stone.transform.position, stoneTargetPoint.position) > 0.1f){
            stone.transform.position = Vector3.MoveTowards(stone.transform.position, stoneTargetPoint.position, stoneSpeed * Time.deltaTime);
            stone.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, Vector3.Distance(stone.transform.position, stoneTargetPoint.position) / Vector3.Distance(stoneSpawnPoint.position, stoneTargetPoint.position));
            yield return null;
        }
        if(stone != null){
            Destroy(stone);
        }
        //wait 2 seconds
        yield return new WaitForSeconds(2f);
        playerCardsScript.MoveHandTo(playerCardsScript.activeHandTransform);
        
    }

    public void ActivateCheating()
    {
        active = true;
        foreach (GameObject btn in cheatingOptions)
        {
            btn.gameObject.SetActive(true);
        }
    }
    public void DeactivateCheating()
    {
        active = false;
        foreach (GameObject btn in cheatingOptions)
        {
            btn.gameObject.SetActive(false);
        }
    }
}
