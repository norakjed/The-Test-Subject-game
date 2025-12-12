using System.Collections;
using UnityEngine;

// ProximitySpike: keep the visible spike object hidden until the player comes within activationRange.
// When activated the spike will face the player and extend toward their position over time, optionally stay extended,
// then retract. Use spikeChild (disabled by default) or spikePrefab to instantiate.
[DisallowMultipleComponent]
public class ProximitySpike : MonoBehaviour
{
    [Header("Detection")]
    public string playerTag = "Player";
    public float activationRange = 3f;
    public bool requireLineOfSight = true; // raycast to player before activating

    [Header("Spike Setup")]
    [Tooltip("Existing child spike object (should be disabled initially). The script will enable/disable it and move it relative to this GameObject.")]
    public GameObject spikeChild;
    [Tooltip("If spikeChild is not set you can assign a prefab which will be instantiated when triggered.")]
    public GameObject spikePrefab;
    public Transform spawnPoint; // where prefab will be spawned (defaults to this transform)

    [Header("Motion")]
    public float extensionDistance = 1.5f; // how far the spike extends from its retracted position
    public float extendSpeed = 6f; // units per second while extending
    public float retractSpeed = 4f; // units per second while retracting
    public float extendedHoldTime = 1.25f; // how long spike stays extended before retracting (0 = no retract if oneShot)

    [Header("Behavior")]
    public bool oneShot = false; // if true spike triggers only once
    public bool startHidden = true; // if true the spikeChild will be disabled at start

    // runtime
    Transform playerTransform;
    GameObject activeSpike;
    Vector3 retractedLocalPos;
    Vector3 extendedLocalPos;
    bool isActive = false;
    bool hasTriggered = false;

    void Start()
    {
        // find player
        var playerObj = GameObject.FindWithTag(playerTag);
        if (playerObj != null) playerTransform = playerObj.transform;
        if (playerTransform != null)
            Debug.Log($"ProximitySpike: found player '{playerTransform.name}' via tag '{playerTag}'", this);
        else
            Debug.LogWarning($"ProximitySpike: could not find player with tag '{playerTag}' — spike won't activate until player is present.", this);

        // Setup spike object
        if (spikeChild != null)
        {
            activeSpike = spikeChild;
            if (startHidden) activeSpike.SetActive(false);
            // record local positions
            retractedLocalPos = activeSpike.transform.localPosition;
            extendedLocalPos = retractedLocalPos + Vector3.forward * extensionDistance; // forward in spike child's local space
        }
        else if (spikePrefab != null)
        {
            // try to find an existing scene object that matches the prefab name
            var sceneObj = GameObject.Find(spikePrefab.name);
            if (sceneObj != null)
            {
                Debug.Log($"ProximitySpike: found scene object '{spikePrefab.name}', using it as spikeChild.", this);
                activeSpike = sceneObj;
                spikeChild = sceneObj;
                if (startHidden) SetSpikeActive(activeSpike, false);
                // record local positions if possible
                retractedLocalPos = activeSpike.transform.localPosition;
                extendedLocalPos = retractedLocalPos + Vector3.forward * extensionDistance;
            }
            else
            {
                // Instantiate the prefab now as a hidden child so we always have a reference and visuals in the scene.
                Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
                Quaternion rot = spawnPoint != null ? spawnPoint.rotation : transform.rotation;
                activeSpike = Instantiate(spikePrefab, pos, rot, transform);
                spikeChild = activeSpike;
                if (startHidden) SetSpikeActive(activeSpike, false);
                // record local positions
                retractedLocalPos = activeSpike.transform.localPosition;
                extendedLocalPos = retractedLocalPos + Vector3.forward * extensionDistance;
                Debug.Log($"ProximitySpike: instantiated spike prefab '{spikePrefab.name}' at start (hidden).", this);
            }
        }
        else
        {
            Debug.LogWarning("ProximitySpike: No spikeChild or spikePrefab assigned.", this);
        }
    }

    void Update()
    {
        if (hasTriggered && oneShot) return;
        if (isActive) return; // already extending/extended

        // lazy find player if missing
        if (playerTransform == null)
        {
            var playerObj = GameObject.FindWithTag(playerTag);
            if (playerObj != null) playerTransform = playerObj.transform;
            if (playerTransform == null) return;
        }

        float dist = Vector3.Distance(playerTransform.position, transform.position);
        if (dist <= activationRange)
        {
            Debug.Log($"ProximitySpike: player within range ({dist:F2}). checking LOS...", this);
            if (requireLineOfSight)
            {
                Vector3 origin = transform.position + Vector3.up * 0.5f;
                Vector3 toPlayer = playerTransform.position - origin;
                float rayLen = Mathf.Max(0.1f, toPlayer.magnitude - 0.1f);
                Vector3 dir = toPlayer.normalized;

                if (Physics.Raycast(origin, dir, out RaycastHit hit, rayLen))
                {
                    // hit something between spike and player
                    if (hit.transform == playerTransform || hit.transform.IsChildOf(playerTransform))
                    {
                        Debug.Log("ProximitySpike: LOS clear (ray hit player).", this);
                        StartCoroutine(ActivateSpike());
                    }
                    else
                    {
                        // allow activation when the blocking object is part of the spike/spawn geometry
                        bool allowedBlock = false;
                        if (activeSpike != null)
                        {
                            if (hit.transform == activeSpike.transform || hit.transform.IsChildOf(activeSpike.transform))
                                allowedBlock = true;
                        }
                        if (!allowedBlock && spawnPoint != null)
                        {
                            if (hit.transform == spawnPoint.transform || hit.transform.IsChildOf(spawnPoint.transform))
                                allowedBlock = true;
                        }
                        if (!allowedBlock)
                        {
                            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                                allowedBlock = true;
                        }

                        if (allowedBlock)
                        {
                            Debug.Log($"ProximitySpike: LOS blocked by own geometry ({hit.transform.name}) — allowing activation.", this);
                            StartCoroutine(ActivateSpike());
                        }
                        else
                        {
                            // Fallback: if the player is within activationRange and roughly in front, allow activation even if blocked.
                            float angleToPlayer = Vector3.Angle(playerTransform.position - transform.position, transform.forward);
                            if (dist <= activationRange && angleToPlayer <= 60f)
                            {
                                Debug.Log($"ProximitySpike: LOS blocked by {hit.transform.name}, but player is near/front (angle {angleToPlayer:F1}) — activating as fallback.", this);
                                StartCoroutine(ActivateSpike());
                            }
                            else
                            {
                                Debug.Log($"ProximitySpike: LOS blocked by {hit.transform.name}", this);
                            }
                        }
                    }
                }
                else
                {
                    Debug.Log("ProximitySpike: LOS raycast hit nothing — treating as clear.", this);
                    StartCoroutine(ActivateSpike());
                }
            }
            else
            {
                StartCoroutine(ActivateSpike());
            }
        }
    }

    IEnumerator ActivateSpike()
    {
        if (isActive) yield break;
        isActive = true;
        // spawn or enable spike
        if (activeSpike == null && spikePrefab != null)
        {
            Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
            Quaternion rot = spawnPoint != null ? spawnPoint.rotation : transform.rotation;
            activeSpike = Instantiate(spikePrefab, pos, rot, transform);
        }

        if (activeSpike == null)
        {
            Debug.LogWarning("ProximitySpike: no spike object available to activate", this);
            isActive = false;
            yield break;
        }

        // enable visuals (colliders will be toggled during motion)
        SetSpikeActive(activeSpike, true);
        var cols = activeSpike.GetComponentsInChildren<Collider>(true);
        foreach (var c in cols) c.enabled = false;

        // orient spike to face player
        Vector3 toPlayer = (playerTransform.position - activeSpike.transform.position);
        if (toPlayer.sqrMagnitude > 0.001f)
        {
            Quaternion look = Quaternion.LookRotation(toPlayer, Vector3.up);
            activeSpike.transform.rotation = look;
        }

        // perform extension in world space toward the player's current position. This avoids wrong extension
        // when the prefab pivot/forward is misaligned with the visible model or collider.
        Vector3 startWorldPos = activeSpike.transform.position;
        Vector3 toPlayerDir = (playerTransform.position - startWorldPos);
        if (toPlayerDir.sqrMagnitude < 0.0001f) toPlayerDir = activeSpike.transform.forward;
        Vector3 extensionDir = toPlayerDir.normalized;
        Vector3 targetWorldPos = startWorldPos + extensionDir * extensionDistance;

        Debug.Log($"ProximitySpike: activating. start={startWorldPos}, target={targetWorldPos}", this);

        // extend
        while ((activeSpike.transform.position - targetWorldPos).sqrMagnitude > 0.0001f)
        {
            float step = extendSpeed * Time.deltaTime;
            activeSpike.transform.position = Vector3.MoveTowards(activeSpike.transform.position, targetWorldPos, step);

            // enable colliders once spike has moved a small amount
            if (cols.Length > 0 && Vector3.Distance(activeSpike.transform.position, startWorldPos) > 0.05f)
            {
                foreach (var c in cols) c.enabled = true;
            }

            yield return null;
        }

        activeSpike.transform.position = targetWorldPos;

        // hold extended
        if (extendedHoldTime > 0f)
            yield return new WaitForSeconds(extendedHoldTime);

        // one-shot behavior
        if (oneShot)
        {
            hasTriggered = true;
            isActive = false;
            yield break;
        }

        // retract
        while ((activeSpike.transform.position - startWorldPos).sqrMagnitude > 0.0001f)
        {
            float step = retractSpeed * Time.deltaTime;
            activeSpike.transform.position = Vector3.MoveTowards(activeSpike.transform.position, startWorldPos, step);
            yield return null;
        }

        activeSpike.transform.position = startWorldPos;

        // disable colliders and visuals if configured
        foreach (var c in cols) c.enabled = false;
        if (startHidden) SetSpikeActive(activeSpike, false);

        isActive = false;
    }

    // Toggle visuals and colliders on the spike root and all children.
    void SetSpikeActive(GameObject spikeRoot, bool active)
    {
        if (spikeRoot == null) return;
        // When hiding, avoid deactivating the root GameObject (some editors/prefabs nest visuals under children).
        // Always keep the root active; control visibility via renderers and colliders so we can reliably toggle visuals at runtime.
        if (active)
        {
            spikeRoot.SetActive(true);
        }

        // Ensure child renderers and colliders are enabled/disabled in case the prefab nests visuals
        var renderers = spikeRoot.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
            r.enabled = active;

        var colliders = spikeRoot.GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders)
            c.enabled = active;

        // Also set the layer of all children to Default when activating to avoid camera culling issues
        if (active)
        {
            SetLayerRecursively(spikeRoot.transform, 0);
        }
    }

    void SetLayerRecursively(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        for (int i = 0; i < t.childCount; i++)
            SetLayerRecursively(t.GetChild(i), layer);
    }

    // Editor/test helper: force-show the spike (can be called from code or via Inspector with a small editor script)
    public void PreviewShowSpike()
    {
        if (activeSpike != null) SetSpikeActive(activeSpike, true);
    }
}
