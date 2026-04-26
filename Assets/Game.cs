using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game : MonoBehaviour
{
    // ── Card Sprites ─────────────────────────────────────────────────
    [Header("Card Sprites")]
    public List<Sprite> cards;
    public Sprite       cardBack;

    // ── References ───────────────────────────────────────────────────
    [Header("References")]
    public PlayerHand   playerHand;
    public OpponentHand opponentHand;

    // ── Table Slots ───────────────────────────────────────────────────
    [Header("Table Slots")]
    public Transform playerTableSlot;
    public Transform opponentTableSlot;

    [Header("Trial Card")]
    public Transform trialCardSlot;
    public Transform trialCardSpawnSlot;
    public int       trialCardIndex = -1;

    // Hierarchy roots cards are reparented into when played
    [Header("Table Roots")]
    public Transform playerTableRoot;
    public Transform opponentTableRoot;

    // ── Health (public for UI) ────────────────────────────────────────
    [Header("Health (read for UI)")]
    public int playerHealth   = 3;
    public int opponentHealth = 3;
    
    [Header("UI references")]
    public GameObject endGamePanel;
    public TMPro.TextMeshProUGUI endGameText;
    public TMPro.TextMeshProUGUI endGameDescription;

    public UIController uiController;
    
    public GameObject aud;
    public GameObject sndAud;

    public string playerWinText = "You Win!";
    public string opponentWinText = "You Lose!";
    public List<string> winDescriptions;
    public List<string> loseDescriptions;

    // ── Current turn cards (public for UI) ───────────────────────────
    [Header("Current Turn (read for UI)")]
    public int playerCardIndex   = -1;
    public int opponentCardIndex = -1;

    // ── Transition ────────────────────────────────────────────────────
    [Header("Card Transition")]
    public float          transitionDuration = 0.5f;
    public AnimationCurve transitionCurve    = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    

    public GameObject cheatingLabel;
    public Cheating cheatingScript;
    
    // ── Private round state ───────────────────────────────────────────
    private int playerRoundWins   = 0;
    private int opponentRoundWins = 0;
    private int currentTurn       = 0;

    private int[] playerDeal   = new int[3];
    private int[] opponentDeal = new int[3];

    private GameObject playerTurnCard;
    private GameObject opponentTurnCard;
    private GameObject trialCardObject;

    // ─────────────────────────────────────────────────────────────────
    // Unity lifecycle
    // ─────────────────────────────────────────────────────────────────

    private void Start()
    {
        
    }

    // ─────────────────────────────────────────────────────────────────
    // Main game loop
    // ─────────────────────────────────────────────────────────────────

    public IEnumerator RunGame()
    {
        while (playerHealth > 0 && opponentHealth > 0)
        {
            yield return StartCoroutine(RunRound());
        }

        OnGameOver();
    }

    // ─────────────────────────────────────────────────────────────────
    // Round
    // ─────────────────────────────────────────────────────────────────

    private IEnumerator RunRound()
    {
        uiController.UpdateHealthDisplay(playerHealth, opponentHealth);
        
        playerRoundWins   = 0;
        opponentRoundWins = 0;
        currentTurn       = 0;

        yield return StartCoroutine(PickAnchorCard());

        DealHands();

        while (currentTurn < 3 && playerRoundWins < 2 && opponentRoundWins < 2)
        {
            yield return StartCoroutine(RunTurn());
            currentTurn++;
        }
        

        // ── Apply round result ────────────────────────────────────────
        cheatingLabel.SetActive(false);
        int damage = AnchorDamage(trialCardIndex);

        if (playerRoundWins > opponentRoundWins)
            opponentHealth -= damage;
        else if (opponentRoundWins > playerRoundWins)
            playerHealth -= damage;

        uiController.UpdateHealthDisplay(playerHealth, opponentHealth);
        cheatingScript.cheatedThisTurn = false;

        yield return new WaitForSeconds(1.2f);
        uiController.UpdateTrialIndex("");

        if (trialCardObject != null)
        {
            Destroy(trialCardObject);
            trialCardObject = null;
        }

        playerHand.EndPlayerTurn();
    }

    // ─────────────────────────────────────────────────────────────────
    // Anchor card
    // ─────────────────────────────────────────────────────────────────
private IEnumerator PickAnchorCard()
    {
        while (true)
        {
            trialCardIndex = Random.Range(0, cards.Count);

            if (trialCardObject != null) Destroy(trialCardObject);
            trialCardObject = Instantiate(playerHand.cardPrefab,
                                        trialCardSpawnSlot.position,
                                        trialCardSpawnSlot.rotation);
            trialCardObject.transform.localScale = trialCardSpawnSlot.localScale;

            SpriteRenderer sr = trialCardObject.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite       = cards[trialCardIndex];   // face-up immediately
                sr.sortingOrder = 1;
            }

            // Just slide into view, no flip
            yield return StartCoroutine(MoveToSlot(trialCardObject, trialCardSlot));

            if (trialCardIndex == 0)
            {
                yield return new WaitForSeconds(0.8f);
                (playerHealth, opponentHealth) = (opponentHealth, playerHealth);
                Destroy(trialCardObject);
                trialCardObject = null;
                uiController.UpdateHealthDisplay(playerHealth, opponentHealth);
                yield return new WaitForSeconds(0.3f);
                continue;
            }

            break;
        }
        uiController.UpdateTrialIndex(AnchorDamage(trialCardIndex).ToString());
    }
    // ─────────────────────────────────────────────────────────────────
    // Deal
    // ─────────────────────────────────────────────────────────────────

    private void DealHands()
    {
        // Create pool excluding trial card
        List<int> pool = new List<int>();
        for (int i = 0; i < cards.Count; i++)
            if (i != trialCardIndex) pool.Add(i);
        
        Shuffle(pool);
        
        // Simple distribution logic
        if (uiController.opponentIndex <= 1)
        {
            // Normal deal - first 6 cards split evenly
            for (int i = 0; i < 3; i++)
            {
                playerDeal[i] = pool[i];
                opponentDeal[i] = pool[i + 3];
            }
        }
        else if (uiController.opponentIndex == 2)
        {
            // Opponent gets ONLY high cards (>11)
            int oppCount = 0;
            foreach (int card in pool)
            {
                if (card > 11 && oppCount < 3)
                    opponentDeal[oppCount++] = card;
            }
            
            // Player gets remaining (shuffled)
            List<int> remaining = new List<int>(pool);
            foreach (int card in opponentDeal)
                remaining.Remove(card);
            
            for (int i = 0; i < 3; i++)
                playerDeal[i] = remaining[i];
        }
        else if (uiController.opponentIndex == 3)
        {
            // Opponent = high cards, Player = low cards
            int oppCount = 0, plCount = 0;
            
            foreach (int card in pool)
            {
                if (card > 11 && oppCount < 3)
                    opponentDeal[oppCount++] = card;
                else if (card <= 11 && plCount < 3)
                    playerDeal[plCount++] = card;
            }
        }
        
        // Deal to hands
        opponentHand.DealCards(opponentDeal);
        playerHand.DealCards(playerDeal);
        playerHand.BeginPlayerTurn();
    }

    // ─────────────────────────────────────────────────────────────────
    // Turn
    // ─────────────────────────────────────────────────────────────────

    private IEnumerator RunTurn()
    {
        playerCardIndex   = -1;
        opponentCardIndex = -1;
        playerTurnCard    = null;
        opponentTurnCard  = null;

        // ── Opponent plays first (face-down) ──────────────────────────

        opponentCardIndex = opponentDeal[currentTurn];

        bool opponentDone = false;
        opponentHand.PlayCard(
            0,
            opponentTableSlot,
            () => opponentDone = true,
            out opponentTurnCard);            // grab reference directly

        yield return new WaitUntil(() => opponentDone);

        // ── Player selects ────────────────────────────────────────────
        playerHand.BeginPlayerTurn();
        yield return new WaitUntil(() => playerHand.selectedCardIndex != -1);

        playerCardIndex               = playerHand.selectedCardIndex;
        playerTurnCard                = playerHand.selectedCardObject;
        playerHand.selectedCardIndex  = -1;
        playerHand.selectedCardObject = null;

        yield return StartCoroutine(MoveToSlot(playerTurnCard, playerTableSlot));

        // ── Flip opponent card to reveal ──────────────────────────────
        yield return StartCoroutine(FlipCard(opponentTurnCard, opponentCardIndex));

        // ── Brief pause to read both cards ────────────────────────────
        yield return new WaitForSeconds(0.8f);

        // ── Judge ─────────────────────────────────────────────────────
        if (playerCardIndex > opponentCardIndex)
            playerRoundWins++;
        else if (opponentCardIndex > playerCardIndex)
            opponentRoundWins++;

        // ── Clear table ───────────────────────────────────────────────
        uiController.UpdateOpponentHandIndex("");
        yield return new WaitForSeconds(0.4f);



        if (playerTurnCard   != null) Destroy(playerTurnCard);
        if (opponentTurnCard != null) Destroy(opponentTurnCard);

        playerTurnCard   = null;
        opponentTurnCard = null;
    }

    // ─────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────

    private int AnchorDamage(int index)
    {
        if (index <= 10) return 1;
        if (index <= 20) return 2;
        return 3;
    }
    private IEnumerator FlipCard(GameObject card, int arcanaIndex)
    {
        uiController.UpdateOpponentHandIndex(arcanaIndex.ToString());

        if (card == null) yield break;

        SpriteRenderer sr       = card.GetComponent<SpriteRenderer>();
        float          duration = transitionDuration * 1.5f;
        float          elapsed  = 0f;

        Quaternion startRot = card.transform.rotation;
        Quaternion endRot   = startRot * Quaternion.Euler(60f, 0f, 0f);  // was 180

        bool swapped = false;

        while (elapsed < duration)
        {
            if (card == null) yield break;

            elapsed += Time.deltaTime;
            float t = transitionCurve.Evaluate(Mathf.Clamp01(elapsed / duration));

            card.transform.rotation = Quaternion.Slerp(startRot, endRot, t);

            // Swap at the edge-on point — unchanged, still triggers at 90 degrees total travel
            if (!swapped && Quaternion.Angle(startRot, card.transform.rotation) >= 30f)
            {
                if (sr != null && arcanaIndex >= 0 && arcanaIndex < cards.Count)
                    
                    sr.sprite = cards[arcanaIndex];
                swapped = true;
            }

            yield return null;
        }

        if (card != null)
            card.transform.rotation = endRot;
    }
    private IEnumerator MoveToSlot(GameObject card, Transform slot)
    {
        if (card == null) yield break;

        Vector3    startPos   = card.transform.position;
        Quaternion startRot   = card.transform.rotation;
        Vector3    startScale = card.transform.localScale;

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            if (card == null) yield break;

            elapsed += Time.deltaTime;
            float t = transitionCurve.Evaluate(
                          Mathf.Clamp01(elapsed / transitionDuration));

            card.transform.position   = Vector3.Lerp(startPos,   slot.position,   t);
            card.transform.rotation   = Quaternion.Slerp(startRot,   slot.rotation,   t);
            card.transform.localScale = Vector3.Lerp(startScale, slot.localScale,  t);

            yield return null;
        }

        if (card != null)
        {
            card.transform.position   = slot.position;
            card.transform.rotation   = slot.rotation;
            card.transform.localScale = slot.localScale;
        }
    }

    private GameObject FindTopChildOf(Transform root)
    {
        if (root == null || root.childCount == 0) return null;
        return root.GetChild(root.childCount - 1).gameObject;
    }

    public void OnGameOver()
    {
        aud.GetComponent<AudioSource>().Stop();
        sndAud.GetComponent<AudioSource>().Play();
        
        endGamePanel.SetActive(true);

        if (playerHealth <= 0 && opponentHealth <= 0)
        {
            endGameText.text = "It's a Tie!";
            endGameDescription.text = "Both players have fallen. Try again?";
        }
        else if (playerHealth <= 0)
        {
            endGameText.text = opponentWinText;
            if (loseDescriptions.Count > 0)
                endGameDescription.text = loseDescriptions[uiController.opponentIndex];
        }
        else
        {
            endGameText.text = playerWinText;
            if (winDescriptions.Count > 0)
                endGameDescription.text = winDescriptions[uiController.opponentIndex];
        }
    }
    

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}