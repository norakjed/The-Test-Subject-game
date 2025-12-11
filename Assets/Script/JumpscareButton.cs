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
                StartCoroutine(ShowJumpscareRoutine(playerObj));
            }
        }
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
    }
}
