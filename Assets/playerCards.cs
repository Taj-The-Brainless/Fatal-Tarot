using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerHand : MonoBehaviour
{
    // ── External references ──────────────────────────────────────────
    [Header("References")]
    public Game game;
    public GameObject cardPrefab;

    // ── Layout ───────────────────────────────────────────────────────
    [Header("Layout")]
    public Transform handAnchor;            // starting position/rotation/scale for first card
    public float     xOffset       = 1.5f;  // horizontal spacing between cards
    public float     rotationStep  = 5f;    // degrees per card slot, curves the fan
                                            // negative = fan left, positive = fan right
    public Transform onTableTransform;      // placement anchor for played card

    // ── Hand position ────────────────────────────────────────────────
    [Header("Hand Position")]
    public Transform activeHandTransform;   // where this GameObject moves on player turn start
    public Transform inactiveHandTransform; // where this GameObject moves on player turn end
    public float     handMoveSpeed = 4f;    // lerp speed (units/sec feel), higher = snappier

    // ── Transition ───────────────────────────────────────────────────
    [Header("Card Transition")]
    public float          transitionDuration = 0.4f;
    public AnimationCurve transitionCurve    = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Table Reference")]
    public Transform tableRoot;   // drag in your /OnTable/ GameObject

    // ── State ────────────────────────────────────────────────────────
    [Header("State (read-only at runtime)")]
    public bool       playerTurn         = false;
    public int        selectedCardIndex  = -1;
    public GameObject selectedCardObject = null;

    // ── UI ────────────────────────────────────────────────────────
    [Header("UI")]
    public TMP_Text cardIndexLabel; 
    // ── Private ───────────────────────────────────────────────────────
    private List<GameObject> handCards   = new List<GameObject>();
    private List<int>        cardIndices = new List<int>();
    private int              highlightedSlot = 0;

    private Coroutine activeTransition;
    private Coroutine handMoveCoroutine;

    private const int LAYER_CARD        = 2;
    private const int LAYER_HIGHLIGHTED = 3;

    [Header("Hand Sprites")]
    public SpriteRenderer handBack;
    public SpriteRenderer handFront;

    [Header("Cheating")]
    public bool isCheating = false;
    public Cheating cheatingScript;




    public void Start()
    {
    }
    // ── Public API ────────────────────────────────────────────────────

    public void DealCards(int[] arcanaIndices)
    {
        ClearHand();

        for (int i = 0; i < arcanaIndices.Length && i < 3; i++)
        {
            int arcana = arcanaIndices[i];

            // Instantiate at root first, then parent without modifying world transform,
            // then assign local transform values directly
            GameObject card = Instantiate(cardPrefab);
            card.transform.SetParent(transform, false);

            // Local position: offset along local X from handAnchor's local position
            Vector3 localPos = handAnchor.localPosition
                            + Vector3.right * (xOffset * i);
            localPos.z = -i * 0.01f;
            card.transform.localPosition = localPos;

            // Local rotation: fan from handAnchor's local rotation
            Vector3 localEuler = handAnchor.localEulerAngles;
            localEuler.z += rotationStep * i;
            card.transform.localRotation = Quaternion.Euler(localEuler);

            // Local scale: inherit from handAnchor
            card.transform.localScale = handAnchor.localScale;

            SpriteRenderer sr = card.GetComponent<SpriteRenderer>();
            if (sr != null && game != null && arcana >= 0 && arcana < game.cards.Count)
            {
                sr.sprite       = game.cards[arcana];
                sr.sortingOrder = LAYER_CARD;
            }

            handCards.Add(card);
            cardIndices.Add(arcana);
        }

        highlightedSlot = 0;
        ApplyHighlight();
    }

    public void BeginPlayerTurn()
    {
        playerTurn         = true;
        selectedCardIndex  = -1;
        selectedCardObject = null;
        highlightedSlot    = 0;
        ApplyHighlight();                    // this now also calls UpdateCardLabel
        SetHandFrontAlpha(0.8f);

        if (activeHandTransform != null)
            MoveHandTo(activeHandTransform);
    }


    public void EndPlayerTurn()
    {
        playerTurn = false;
        ResetAllCardLayers();
        SetHandFrontAlpha(1f);


        if (inactiveHandTransform != null)
            MoveHandTo(inactiveHandTransform);
    }

    // ── Unity lifecycle ───────────────────────────────────────────────

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            MoveHandTo(inactiveHandTransform);
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            MoveHandTo(activeHandTransform);
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (isCheating){
                MoveHandTo(activeHandTransform);
                cheatingScript.DeactivateCheating();
                isCheating = false;
            }
            else {
                MoveHandTo(inactiveHandTransform);
                cheatingScript.ActivateCheating();
                isCheating = true;
            }
        }
        if (!playerTurn || handCards.Count == 0) return;

        if (!isCheating){
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                highlightedSlot = (highlightedSlot - 1 + handCards.Count) % handCards.Count;
                ApplyHighlight();
            }

            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                highlightedSlot = (highlightedSlot + 1) % handCards.Count;
                ApplyHighlight();
            }

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                PlaceHighlightedCard();
            }
        }
    }

    // ── Private helpers ───────────────────────────────────────────────

    private void ApplyHighlight()
    {
        for (int i = 0; i < handCards.Count; i++)
        {
            SpriteRenderer sr = handCards[i].GetComponent<SpriteRenderer>();
            if (sr == null) continue;

            if (i == highlightedSlot)
            {
                sr.sortingOrder = LAYER_HIGHLIGHTED;

                Vector3 p = handCards[i].transform.position;
                p.z = -(handCards.Count) * 0.01f;
                handCards[i].transform.position = p;
            }
            else
            {
                sr.sortingOrder = LAYER_CARD;

                Vector3 p = handCards[i].transform.position;
                p.z = -i * 0.01f;
                handCards[i].transform.position = p;
            }
        }

        // Update label to show the currently highlighted arcana index
        if (handCards.Count > 0)
            UpdateCardLabel(cardIndices[highlightedSlot]);
    }

    private void UpdateCardLabel(int arcanaIndex)
    {
        if (cardIndexLabel == null) return;
        cardIndexLabel.text = arcanaIndex.ToString();
    }

    private void PlaceHighlightedCard()
    {
        if (highlightedSlot < 0 || highlightedSlot >= handCards.Count) return;

        GameObject card = handCards[highlightedSlot];

        selectedCardIndex  = cardIndices[highlightedSlot];
        selectedCardObject = card;

        handCards.RemoveAt(highlightedSlot);
        cardIndices.RemoveAt(highlightedSlot);

        if (handCards.Count > 0)
            highlightedSlot = Mathf.Clamp(highlightedSlot, 0, handCards.Count - 1);

        // Reparent into /OnTable/ while keeping exact world position/rotation/scale.
        // true = worldPositionStays, so nothing jumps on screen.
        if (tableRoot != null)
            card.transform.SetParent(tableRoot, true);

        if (activeTransition != null) StopCoroutine(activeTransition);
        activeTransition = StartCoroutine(MoveToTable(card));

        EndPlayerTurn();
    }

    private IEnumerator MoveToTable(GameObject card)
    {
        Vector3    startPos   = card.transform.position;
        Quaternion startRot   = card.transform.rotation;
        Vector3    startScale = card.transform.localScale;

        Vector3    endPos     = onTableTransform.position;
        Quaternion endRot     = onTableTransform.rotation;
        Vector3    endScale   = onTableTransform.localScale;

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            if (card == null) yield break;

            elapsed += Time.deltaTime;
            float t = transitionCurve.Evaluate(Mathf.Clamp01(elapsed / transitionDuration));

            card.transform.position   = Vector3.Lerp(startPos, endPos, t);
            card.transform.rotation   = Quaternion.Slerp(startRot, endRot, t);
            card.transform.localScale = Vector3.Lerp(startScale, endScale, t);

            yield return null;
        }

        if (card != null)
        {
            card.transform.position   = endPos;
            card.transform.rotation   = endRot;
            card.transform.localScale = endScale;
        }

        activeTransition = null;
    }

    public void MoveHandTo(Transform target)
    {
        if (handMoveCoroutine != null) StopCoroutine(handMoveCoroutine);
        handMoveCoroutine = StartCoroutine(SmoothMoveHand(target));
    }

    private IEnumerator SmoothMoveHand(Transform target)
    {
        // Runs every frame, lerping toward the target transform.
        // handMoveSpeed controls how quickly it closes the gap:
        // higher values snap faster, lower values feel floaty.
        while (true)
        {
            float step = handMoveSpeed * Time.deltaTime;

            transform.position = Vector3.Lerp(
                transform.position, target.position, step);

            transform.rotation = Quaternion.Slerp(
                transform.rotation, target.rotation, step);

            transform.localScale = Vector3.Lerp(
                transform.localScale, target.localScale, step);

            // Close enough — snap and stop
            if (Vector3.Distance(transform.position, target.position) < 0.001f &&
                Quaternion.Angle(transform.rotation, target.rotation)  < 0.05f)
            {
                transform.position   = target.position;
                transform.rotation   = target.rotation;
                transform.localScale = target.localScale;
                break;
            }

            yield return null;
        }

        handMoveCoroutine = null;
    }

    private void ResetAllCardLayers()
    {
        for (int i = 0; i < handCards.Count; i++)
        {
            SpriteRenderer sr = handCards[i].GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = LAYER_CARD;

            // Restore Z depth order
            Vector3 p = handCards[i].transform.position;
            p.z = -i * 0.01f;
            handCards[i].transform.position = p;
        }
    }

    private void SetHandFrontAlpha(float alpha)
    {
        if (handFront == null) return;
        Color c = handFront.color;
        c.a = alpha;
        handFront.color = c;
    }

    private void ClearHand()
    {
        foreach (GameObject card in handCards)
            if (card != null) Destroy(card);

        handCards.Clear();
        cardIndices.Clear();
    }
}