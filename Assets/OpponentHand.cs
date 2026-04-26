using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpponentHand : MonoBehaviour
{
    [Header("References")]
    public Game       game;
    public GameObject cardPrefab;

    [Header("Layout")]
    public Transform handAnchor;
    public float     xOffset      = 1.5f;
    public float     rotationStep = 5f;

    [Header("Card Transition")]
    public float          transitionDuration = 0.4f;
    public AnimationCurve transitionCurve    = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);


    [Header("Table Reference")]
    public Transform tableRoot;


    [Header("Peek")]
    public float          peekYScale      = 0f;    // Y scale at the flip point (0 = edge-on)
    public float          peekYOffset     = -0.5f; // how much to shift down during peek
    public float          peekDuration    = 2f;
    public float          peekTransition  = 0.4f;

    private bool peekInProgress = false;

    // ── Private ───────────────────────────────────────────────────────
    public List<int>        cardIndices = new List<int>();
    private List<GameObject> handCards   = new List<GameObject>();

    // ─────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────

    public void DealCards(int[] arcanaIndices)
    {
        ClearHand();

        for (int i = 0; i < arcanaIndices.Length && i < 3; i++)
        {
            GameObject card = Instantiate(cardPrefab);
            card.transform.SetParent(transform, false);

            Vector3 localPos = handAnchor.localPosition + Vector3.right * (xOffset * i);
            localPos.z       = -i * 0.01f;
            card.transform.localPosition = localPos;

            Vector3 localEuler = handAnchor.localEulerAngles;
            localEuler.z      += rotationStep * i;
            card.transform.localRotation = Quaternion.Euler(localEuler);
            card.transform.localScale    = handAnchor.localScale;

            SpriteRenderer sr = card.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite       = game.cardBack;
                sr.sortingOrder = 0;
            }

            handCards.Add(card);
            cardIndices.Add(arcanaIndices[i]);
            
        }
    }
    public void PlayCard(int slotIndex, Transform targetSlot, System.Action onComplete,
                        out GameObject cardOut)
    {
        cardOut = null;

        if (slotIndex < 0 || slotIndex >= handCards.Count)
        {
            onComplete?.Invoke();
            return;
        }

        GameObject card   = handCards[slotIndex];
        int        arcana = cardIndices[slotIndex];

        handCards.RemoveAt(slotIndex);
        cardIndices.RemoveAt(slotIndex);

        if (tableRoot != null)
            card.transform.SetParent(tableRoot, true);

        // Stay face-down during travel — Game.cs will flip after player plays
        cardOut = card;
        StartCoroutine(MoveToSlot(card, targetSlot, onComplete));
    }

    public void ClearHand()
    {
        foreach (GameObject card in handCards)
            if (card != null) Destroy(card);

        handCards.Clear();
        cardIndices.Clear();
    }


    public GameObject GetFrontCard()
    {
        if (handCards.Count == 0) return null;
        return handCards[0];
    }
    
    public void PeekAllCards()
    {
        if (peekInProgress)       return;
        if (handCards.Count == 0) return;

        StartCoroutine(PeekAllCoroutine());
    }

    private IEnumerator PeekAllCoroutine()
    {
        peekInProgress = true;

        int count = handCards.Count;

        Vector3[]      startScales = new Vector3[count];
        Vector3[]      startPos    = new Vector3[count];
        SpriteRenderer[] srs       = new SpriteRenderer[count];

        for (int i = 0; i < count; i++)
        {
            startScales[i] = handCards[i].transform.localScale;
            startPos[i]    = handCards[i].transform.localPosition;
            srs[i]         = handCards[i].GetComponent<SpriteRenderer>();
        }

        // ── Squish to Y=0 ─────────────────────────────────────────────────
        float elapsed  = 0f;
        bool  swapped  = false;

        while (elapsed < peekTransition)
        {
            elapsed += Time.deltaTime;
            float t = transitionCurve.Evaluate(Mathf.Clamp01(elapsed / peekTransition));

            for (int i = 0; i < count; i++)
            {
                if (handCards[i] == null) continue;

                handCards[i].transform.localScale = new Vector3(
                    startScales[i].x,
                    Mathf.Lerp(startScales[i].y, 0f, t),
                    startScales[i].z);

                handCards[i].transform.localPosition = new Vector3(
                    startPos[i].x,
                    Mathf.Lerp(startPos[i].y, startPos[i].y + peekYOffset, t),
                    startPos[i].z);
            }

            // Swap sprite exactly when Y scale hits 0
            if (!swapped && t >= 1f)
            {
                for (int i = 0; i < count; i++)
                {
                    if (srs[i] == null) continue;
                    if (cardIndices[i] >= 0 && cardIndices[i] < game.cards.Count)
                        srs[i].sprite = game.cards[cardIndices[i]];
                }
                swapped = true;
            }

            yield return null;
        }

        // Snap to Y=0 and ensure sprite swap happened
        for (int i = 0; i < count; i++)
        {
            if (handCards[i] == null) continue;

            handCards[i].transform.localScale = new Vector3(
                startScales[i].x, 0f, startScales[i].z);

            handCards[i].transform.localPosition = new Vector3(
                startPos[i].x, startPos[i].y + peekYOffset, startPos[i].z);

            if (!swapped && srs[i] != null && cardIndices[i] >= 0 && cardIndices[i] < game.cards.Count)
                srs[i].sprite = game.cards[cardIndices[i]];
        }

        // ── Expand back out ───────────────────────────────────────────────
        elapsed = 0f;
        while (elapsed < peekTransition)
        {
            elapsed += Time.deltaTime;
            float t = transitionCurve.Evaluate(Mathf.Clamp01(elapsed / peekTransition));

            for (int i = 0; i < count; i++)
            {
                if (handCards[i] == null) continue;

                handCards[i].transform.localScale = new Vector3(
                    startScales[i].x,
                    Mathf.Lerp(0f, startScales[i].y, t),
                    startScales[i].z);

                handCards[i].transform.localPosition = new Vector3(
                    startPos[i].x,
                    Mathf.Lerp(startPos[i].y + peekYOffset, startPos[i].y, t),
                    startPos[i].z);
            }

            yield return null;
        }

        // ── Hold ──────────────────────────────────────────────────────────
        yield return new WaitForSeconds(peekDuration);

        // ── Squish back to Y=0 ────────────────────────────────────────────
        elapsed = 0f;
        while (elapsed < peekTransition)
        {
            elapsed += Time.deltaTime;
            float t = transitionCurve.Evaluate(Mathf.Clamp01(elapsed / peekTransition));

            for (int i = 0; i < count; i++)
            {
                if (handCards[i] == null) continue;

                handCards[i].transform.localScale = new Vector3(
                    startScales[i].x,
                    Mathf.Lerp(startScales[i].y, 0f, t),
                    startScales[i].z);
            }

            yield return null;
        }

        // Swap back to card back at Y=0
        for (int i = 0; i < count; i++)
        {
            if (handCards[i] == null) continue;

            handCards[i].transform.localScale = new Vector3(
                startScales[i].x, 0f, startScales[i].z);

            if (srs[i] != null) srs[i].sprite = game.cardBack;
        }

        // ── Expand back to original ───────────────────────────────────────
        elapsed = 0f;
        while (elapsed < peekTransition)
        {
            elapsed += Time.deltaTime;
            float t = transitionCurve.Evaluate(Mathf.Clamp01(elapsed / peekTransition));

            for (int i = 0; i < count; i++)
            {
                if (handCards[i] == null) continue;

                handCards[i].transform.localScale = new Vector3(
                    startScales[i].x,
                    Mathf.Lerp(0f, startScales[i].y, t),
                    startScales[i].z);
            }

            yield return null;
        }

        // Snap back to original
        for (int i = 0; i < count; i++)
        {
            if (handCards[i] == null) continue;
            handCards[i].transform.localScale    = startScales[i];
            handCards[i].transform.localPosition = startPos[i];
        }

        peekInProgress = false;
    }

    // ─────────────────────────────────────────────────────────────────
    // Private
    // ─────────────────────────────────────────────────────────────────

    private IEnumerator MoveToSlot(GameObject card, Transform slot,
                                   System.Action onComplete)
    {
        if (card == null) { onComplete?.Invoke(); yield break; }

        Vector3    startPos   = card.transform.position;
        Quaternion startRot   = card.transform.rotation;
        Vector3    startScale = card.transform.localScale;

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            if (card == null) { onComplete?.Invoke(); yield break; }

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

        onComplete?.Invoke();
    }
}