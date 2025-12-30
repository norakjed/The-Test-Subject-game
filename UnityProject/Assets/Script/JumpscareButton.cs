using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Attach this to a 3D cylinder button GameObject.
// When the player is within `interactionDistance` and presses E, the script will
// display a fullscreen jumpscare image taken from the assigned Material's main texture.
public class JumpscareButton : MonoBehaviour
{
    [Header("Interaction")]
    public string playerTag = "Player";
    public float interactionDistance = 2f;
    public KeyCode interactKey = KeyCode.E;

    [Header("Jumpscare")]
    [Tooltip("Material that contains the jumpscare texture (main texture will be used).")]
    public Material jumpscareMaterial;
    [Tooltip("Seconds the jumpscare image will remain on screen.")]
    public float showDuration = 2f;
    [Tooltip("If true, the player Movement component will be disabled during the jumpscare.")]
    public bool disablePlayerMovement = true;
    [Tooltip("If true, call PlayerHealth.Die(...) after the jumpscare. If CameraFall is true, pass forceFall=true.")]
    public bool killPlayerAfterJumpscare = true;
    [Tooltip("If true, call Die(true) to force fall/ragdoll style death. Otherwise call Die().")]
    public bool forceFallOnDeath = false;

    [Header("Optional Audio")]
    public AudioClip jumpscareSound;
    public float soundVolume = 1f;
    [Header("Button Press")]
    [Tooltip("Local distance to move the button down when pressed.")]
    public float pressDepth = 0.08f;
    [Tooltip("Seconds the press animation takes to move down (and same to move back up).")]
    public float pressTime = 0.06f;

    [Header("Third Person")]
    [Tooltip("If true, temporarily disable common third-person components/cameras during the jumpscare so the third-person view is not shown.")]
    public bool suppressThirdPersonDuringJumpscare = true;

    public static int pressCount = 0;

    Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
        if (mainCam == null)
            Debug.LogWarning("JumpscareButton: No Camera.main found.");

        if (jumpscareMaterial == null)
            Debug.LogWarning("JumpscareButton: No jumpscare material assigned.");
    }

    void Update()
    {
        // Find player by tag
        var playerObj = GameObject.FindWithTag(playerTag);
        if (playerObj == null) return;

        float dist = Vector3.Distance(playerObj.transform.position, transform.position);
        if (dist <= interactionDistance)
        {
            if (Input.GetKeyDown(interactKey))
            {
                StartCoroutine(PressThenJumpscareRoutine(playerObj));
            }
        }
    }

    IEnumerator PressThenJumpscareRoutine(GameObject playerObj)
    {
        pressCount++;
        // Animate local press (move down then back up)
        Vector3 start = transform.localPosition;
        Vector3 down = start + Vector3.down * pressDepth;

        float t = 0f;
        while (t < pressTime)
        {
            t += Time.deltaTime;
            transform.localPosition = Vector3.Lerp(start, down, t / pressTime);
            yield return null;
        }
        transform.localPosition = down;

        // small wait so the press feel is visible before jumpscare
        yield return new WaitForSeconds(0.06f);

        // Run jumpscare
        yield return StartCoroutine(ShowJumpscareRoutine(playerObj));

        // Move back up
        t = 0f;
        while (t < pressTime)
        {
            t += Time.deltaTime;
            transform.localPosition = Vector3.Lerp(down, start, t / pressTime);
            yield return null;
        }
        transform.localPosition = start;
    }

    IEnumerator ShowJumpscareRoutine(GameObject playerObj)
    {
        if (jumpscareMaterial == null)
        {
            Debug.LogWarning("JumpscareButton: jumpscareMaterial is null.");
            yield break;
        }

        // Get texture from material
        Texture tex = jumpscareMaterial.mainTexture;
        if (tex == null)
        {
            tex = jumpscareMaterial.GetTexture("_MainTex");
        }
        if (tex == null)
        {
            Debug.LogWarning("JumpscareButton: jumpscare material has no main texture.");
            yield break;
        }

        // Optionally disable player movement
        Movement movement = null;
        if (disablePlayerMovement && playerObj != null)
        {
            movement = playerObj.GetComponent<Movement>();
            if (movement != null)
                movement.enabled = false;
        }

        // Optionally suppress third-person cameras / controllers while jumpscare is visible
        var disabledBehaviours = new System.Collections.Generic.List<Behaviour>();
        var disabledCameras = new System.Collections.Generic.List<Camera>();
        // Temp camera handles — declared here so they're in scope for cleanup later
        Camera tempCamera = null;
        bool createdTempCamera = false;
        if (suppressThirdPersonDuringJumpscare && playerObj != null)
        {
            // Disable any MonoBehaviours on the player whose type name contains "ThirdPerson"
            var monos = playerObj.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var m in monos)
            {
                if (m == null) continue;
                var name = m.GetType().Name;
                if (name.IndexOf("ThirdPerson", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("ThirdPersonController", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var b = m as Behaviour;
                    if (b != null && b.enabled)
                    {
                        b.enabled = false;
                        disabledBehaviours.Add(b);
                    }
                }
            }

            // Also disable any Cameras that are children of the player (common for third-person setups)
            var cams = playerObj.GetComponentsInChildren<Camera>(true);
            foreach (var c in cams)
            {
                if (c != null && c.enabled)
                {
                    c.enabled = false;
                    disabledCameras.Add(c);
                }
            }
                // Additionally disable any other enabled Cameras in the scene to ensure no third-person view renders
                var sceneCams = GameObject.FindObjectsOfType<Camera>();
                foreach (var sc in sceneCams)
                {
                    if (sc == null) continue;
                    // Skip cameras we already disabled (they'll be in disabledCameras)
                    if (disabledCameras.Contains(sc)) continue;
                    if (sc.enabled)
                    {
                        sc.enabled = false;
                        disabledCameras.Add(sc);
                    }
                }

                // Try to disable CinemachineBrain-like components by name (if Cinemachine is present)
                var allMonos = GameObject.FindObjectsOfType<MonoBehaviour>(true);
                foreach (var m in allMonos)
                {
                    if (m == null) continue;
                    var n = m.GetType().Name;
                    if (n.IndexOf("CinemachineBrain", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var b = m as Behaviour;
                        if (b != null && b.enabled)
                        {
                            b.enabled = false;
                            disabledBehaviours.Add(b);
                        }
                    }
                }

                // If disabling cameras removed all enabled Cameras, create a temporary camera so Unity doesn't show "No cameras rendering".
                bool anyEnabled = false;
                var checkCams = GameObject.FindObjectsOfType<Camera>();
                foreach (var cc in checkCams)
                {
                    if (cc != null && cc.enabled)
                    {
                        anyEnabled = true;
                        break;
                    }
                }
                if (!anyEnabled)
                {
                    var go = new GameObject("JumpscareTempCamera");
                    tempCamera = go.AddComponent<Camera>();
                    tempCamera.cullingMask = 0; // render nothing
                    tempCamera.clearFlags = CameraClearFlags.SolidColor;
                    tempCamera.backgroundColor = Color.black;
                    tempCamera.depth = 10000;
                    createdTempCamera = true;
                }
        }

        // Play sound
        if (jumpscareSound != null)
        {
            AudioSource.PlayClipAtPoint(jumpscareSound, playerObj.transform.position, soundVolume);
        }

        // Create a fullscreen Canvas and RawImage
        GameObject canvasGO = new GameObject("JumpscareCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000; // on top
        CanvasScaler cs = canvasGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject imgGO = new GameObject("JumpscareImage");
        imgGO.transform.SetParent(canvasGO.transform, false);
        RawImage raw = imgGO.AddComponent<RawImage>();
        raw.texture = tex;
        raw.color = Color.white;

        // Stretch to full screen
        RectTransform rt = raw.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Optional: add a quick scale pop animation
        float popTime = Mathf.Min(0.15f, showDuration * 0.25f);
        if (popTime > 0f)
        {
            raw.rectTransform.localScale = Vector3.zero;
            float t = 0f;
            while (t < popTime)
            {
                t += Time.unscaledDeltaTime;
                float s = Mathf.SmoothStep(0f, 1f, t / popTime);
                raw.rectTransform.localScale = Vector3.one * s;
                yield return null;
            }
            raw.rectTransform.localScale = Vector3.one;
        }

        // Wait in unscaled time (so it still shows if timescale changes)
        float timer = 0f;
        while (timer < showDuration)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        // Clean up
        GameObject.Destroy(canvasGO);
        // If configured, kill the player now. Use the forceFall option when calling Die.
        if (killPlayerAfterJumpscare && playerObj != null)
        {
            var ph = playerObj.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                if (forceFallOnDeath)
                    ph.Die(true);
                else
                    ph.Die();
            }
        }

        // Re-enable movement only if player still exists and is not dead
        if (movement != null)
        {
            var phCheck = playerObj != null ? playerObj.GetComponent<PlayerHealth>() : null;
            if (phCheck == null || !phCheck.isDead)
                movement.enabled = true;
        }

        // Re-enable any third-person behaviours/cameras we disabled (if the player is still alive)
        var phCheck2 = playerObj != null ? playerObj.GetComponent<PlayerHealth>() : null;
        if ((phCheck2 == null || !phCheck2.isDead) && disabledBehaviours.Count > 0)
        {
            foreach (var b in disabledBehaviours)
                if (b != null) b.enabled = true;
        }
        if ((phCheck2 == null || !phCheck2.isDead) && disabledCameras.Count > 0)
        {
            foreach (var c in disabledCameras)
                if (c != null) c.enabled = true;
        }
        // Remove the temporary camera if we created one
        if (createdTempCamera && tempCamera != null)
        {
            GameObject.Destroy(tempCamera.gameObject);
        }
    }
}
