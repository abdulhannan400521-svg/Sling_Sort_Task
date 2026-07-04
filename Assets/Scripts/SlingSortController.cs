using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlingSortController : MonoBehaviour
{
    [SerializeField] private Peg sourcePegDefault;
    [SerializeField] private Peg targetPegDefault;
    [SerializeField] private Ring ringPrefab;
    [SerializeField] private int ringCount = 10;
    [SerializeField] private Material[] ringMaterials;
    [SerializeField] private Camera inputCamera;

    [SerializeField] private float moveDuration = 0.6f;
    [SerializeField] private float arcHeight = 1.2f;
    [SerializeField] private float springFrequency = 4f;
    [SerializeField] private float springDamping = 4f;
    [SerializeField] private float staggerDelay = 0.08f;
    [SerializeField] private float liftDuration = 0.12f;
    [SerializeField] private AudioClip ringMoveSound;
    [SerializeField] private AudioClip pegTapSound;
    [SerializeField] private AudioSource audioSource;

    private Peg pegA;
    private Peg pegB;
    private Peg selectedSourcePeg;
    private bool isTransferring;
    private Vector2 touchStartPos;
    private float touchStartTime;
    [SerializeField] private float maxTapTime = 0.3f;
    [SerializeField] private float maxTapMovement = 20f;
    private Coroutine selectionAnimCoroutine;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        pegA = sourcePegDefault;
        pegB = targetPegDefault;

        if (inputCamera == null)
            inputCamera = Camera.main;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        SpawnInitialRings();
    }

    private void Update()
    {
        if (isTransferring)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            TryHandleSelection(Input.mousePosition);
        }
        else if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                touchStartPos = t.position;
                touchStartTime = Time.time;
            }
            else if (t.phase == TouchPhase.Ended)
            {
                float dt = Time.time - touchStartTime;
                float move = (t.position - touchStartPos).magnitude;
                if (dt <= maxTapTime && move <= maxTapMovement)
                    TryHandleSelection(t.position);
            }
        }
    }

    private void TryHandleSelection(Vector2 screenPosition)
    {
        Ray ray = inputCamera.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Peg clickedPeg = hit.collider.GetComponentInParent<Peg>();

            if (clickedPeg != null)
                HandlePegSelection(clickedPeg);
        }
    }

    private void HandlePegSelection(Peg clickedPeg)
    {
        if (selectedSourcePeg == null)
        {
            if (clickedPeg.RingCount > 0)
            {
                selectedSourcePeg = clickedPeg;
                PlayPegTapSound();
                if (selectionAnimCoroutine != null)
                    StopCoroutine(selectionAnimCoroutine);
                selectionAnimCoroutine = StartCoroutine(AnimatePegTouch(selectedSourcePeg));
            }
        }
        else if (clickedPeg != selectedSourcePeg)
        {
            StartCoroutine(TransferRings(selectedSourcePeg, clickedPeg));
            selectedSourcePeg = null;
        }
        else
        {
            selectedSourcePeg = null;
        }
    }

    private IEnumerator AnimatePegTouch(Peg peg)
    {
        if (peg == null)
            yield break;

        Transform t = peg.transform;
        Vector3 original = t.localScale;
        Vector3 target = original * 1.08f;
        float dur = 0.12f;
        float elapsed = 0f;

        while (elapsed < dur)
        {
            t.localScale = Vector3.Lerp(original, target, elapsed / dur);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        t.localScale = target;

        elapsed = 0f;
        while (elapsed < dur)
        {
            t.localScale = Vector3.Lerp(target, original, elapsed / dur);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        t.localScale = original;
        selectionAnimCoroutine = null;
    }

    private void SpawnInitialRings()
    {
        for (int i = 0; i < ringCount; i++)
        {
            Vector3 spawnPosition = pegA.GetSlotPosition(i);
            Quaternion spawnRotation = Quaternion.Euler(-90f, 0f, 0f);
            Ring newRing = Instantiate(ringPrefab, spawnPosition, spawnRotation);
            newRing.SetStackIndex(i);

            if (ringMaterials != null && ringMaterials.Length > 0)
            {
                Renderer ringRenderer = newRing.GetComponentInChildren<Renderer>();
                if (ringRenderer != null)
                    ringRenderer.material = ringMaterials[i % ringMaterials.Length];
            }

            pegA.RegisterRing(newRing);
        }
    }

    private IEnumerator TransferRings(Peg fromPeg, Peg toPeg)
    {
        isTransferring = true;

        List<Ring> ringsToMove = fromPeg.PopAllRings();
        List<Coroutine> activeMoves = new List<Coroutine>();

        for (int i = 0; i < ringsToMove.Count; i++)
        {
            Ring ring = ringsToMove[i];
            Vector3 targetPosition = toPeg.GetSlotPosition(i);
            float delay = i * staggerDelay;

            Vector3 pegLift = fromPeg != null ? fromPeg.LiftOffset : Vector3.up * 0.5f;
            Coroutine move = StartCoroutine(TransferSingleRing(ring, targetPosition, delay, pegLift));

            activeMoves.Add(move);
            toPeg.RegisterRing(ring);
        }

        float totalDuration = liftDuration + moveDuration + (ringsToMove.Count * staggerDelay);
        yield return new WaitForSeconds(totalDuration);

        isTransferring = false;
    }

    private IEnumerator TransferSingleRing(Ring ring, Vector3 targetPosition, float startDelay, Vector3 pegLiftOffset)
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        Vector3 startPos = ring.transform.position;
        Vector3 liftPos = startPos + pegLiftOffset;

        float elapsed = 0f;
        while (elapsed < liftDuration)
        {
            ring.transform.position = Vector3.Lerp(startPos, liftPos, elapsed / liftDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        ring.transform.position = liftPos;
        PlayRingMoveSound();

        yield return StartCoroutine(RingMover.MoveWithSpring(
            ring.transform,
            targetPosition,
            moveDuration,
            arcHeight,
            springFrequency,
            springDamping,
            0f));
    }

    private void PlayRingMoveSound()
    {
        if (ringMoveSound == null)
            return;

        PlaySound(ringMoveSound);
    }

    private void PlayPegTapSound()
    {
        if (pegTapSound == null)
            return;

        PlaySound(pegTapSound);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null)
            return;

        if (audioSource != null)
            audioSource.PlayOneShot(clip);
        else
            AudioSource.PlayClipAtPoint(clip, transform.position);
    }
}
