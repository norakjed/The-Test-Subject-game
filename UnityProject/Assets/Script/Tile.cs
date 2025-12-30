using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Tile : MonoBehaviour
{
    public Renderer rend;
    public Color normalColor = Color.white;
    public Color glowColor = Color.yellow;
    public float emissionIntensity = 1.5f;
    public Material glowMaterial;

    // store original materials so we can restore after swapping
    Material[] originalMaterials;
    // primary box collider that will be toggled on/off
    private BoxCollider primaryBox;

    private Coroutine glowCoroutine;

    void Awake()
    {
        if (rend == null) rend = GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>();
        if (rend != null) normalColor = rend.material.color;

        // Ensure there is a collider present; leave trigger flag to designer.
        var col = GetComponent<Collider>();
        if (col == null) Debug.LogWarning("Tile requires a Collider component.", this);

        if (rend != null)
        {
            // keep a copy of the renderer's original materials
            originalMaterials = rend.sharedMaterials;
        }

        // ensure we have a primary box collider on this object we can toggle
        EnsurePrimaryBoxCollider();
    }

    public void Glow(float duration)
    {
        if (glowCoroutine != null) StopCoroutine(glowCoroutine);
        glowCoroutine = StartCoroutine(GlowRoutine(duration));
    }

    IEnumerator GlowRoutine(float dur)
    {
        if (rend == null) yield break;
        if (rend == null) yield break;

        // If a glow material is provided, swap all materials to it for the duration
        if (glowMaterial != null)
        {
            var mats = rend.materials; // creates instance array
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = new Material(glowMaterial);
                // if glowMaterial supports color/emission, set color
                if (mats[i].HasProperty("_Color")) mats[i].color = glowColor;
                if (mats[i].HasProperty("_EmissionColor"))
                {
                    mats[i].EnableKeyword("_EMISSION");
                    mats[i].SetColor("_EmissionColor", glowColor * emissionIntensity);
                }
            }
            rend.materials = mats;
        }
        else
        {
            var mat = rend.material;
            if (mat == null) yield break;
            mat.color = glowColor;

            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", glowColor * emissionIntensity);
            }
        }

        yield return new WaitForSeconds(dur);

        // restore original state
        if (glowMaterial != null && originalMaterials != null)
        {
            rend.materials = originalMaterials;
        }
        else
        {
            var mat = rend.material;
            if (mat != null)
            {
                mat.color = normalColor;
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.black);
            }
        }

        glowCoroutine = null;
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"Tile {name} OnCollisionEnter by {collision.gameObject.name}");
        if (collision.gameObject.CompareTag("Player"))
        {
            MemoryGame.Instance?.TilePressed(this);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // When tile is trigger, player just falls through - no game logic needed
        Debug.Log($"Tile {name} OnTriggerEnter by {other.gameObject.name} (falling through)");
    }

    // Make the tile non-solid so the player will fall through
    public void Collapse()
    {
        SetTrigger(true);
    }

    // Restore solidity if needed
    public void Restore()
    {
        SetTrigger(false);
    }

    // Set trigger flag on the primary BoxCollider
    public void SetTrigger(bool isTrigger)
    {
        EnsurePrimaryBoxCollider();
        if (primaryBox != null)
        {
            primaryBox.isTrigger = isTrigger;
            Debug.Log($"Tile {name}: BoxCollider.isTrigger = {isTrigger}");
        }
        else
        {
            Debug.LogWarning($"Tile {name}: no primary BoxCollider found!");
        }
    }

    void EnsurePrimaryBoxCollider()
    {
        if (primaryBox != null) return;
        
        primaryBox = GetComponent<BoxCollider>();
        if (primaryBox != null) return;

        // add a BoxCollider if none present, sized to renderer bounds if possible
        primaryBox = gameObject.AddComponent<BoxCollider>();
        if (rend != null)
        {
            var b = rend.bounds.size;
            primaryBox.size = new Vector3(b.x, 0.2f, b.z);
            primaryBox.center = new Vector3(0, primaryBox.size.y / 2f, 0);
        }
        Debug.Log($"Tile {name}: added primary BoxCollider");
    }
}
