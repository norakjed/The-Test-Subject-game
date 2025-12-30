
using UnityEngine;

public class ReversePlate : MonoBehaviour
{
	[Tooltip("Tag used to identify the player object.")]
	public string playerTag = "Player";
	[Tooltip("If true the plate will only trigger once and then disable itself.")]
	public bool oneShot = true;

	// Whether this plate has already been used (one-shot)
	private bool used = false;
	private void OnTriggerEnter(Collider other)
	{
		TrySetInvertForCollider(other, true);
	}

	private void OnTriggerExit(Collider other)
	{
		TrySetInvertForCollider(other, false);
	}

	private void OnCollisionEnter(Collision collision)
	{
		TrySetInvertForGameObject(collision.gameObject, true);
	}

	private void OnCollisionExit(Collision collision)
	{
		TrySetInvertForGameObject(collision.gameObject, false);
	}

	private void TrySetInvertForCollider(Collider other, bool invert)
	{
		if (other == null) return;
		TrySetInvertForGameObject(other.gameObject, invert);
	}

	private void TrySetInvertForGameObject(GameObject obj, bool invert)
	{
		if (obj == null) return;
		if (!obj.CompareTag(playerTag)) return;

		// Try direct, children, then parent to find Movement component
		Movement movement = obj.GetComponent<Movement>();
		if (movement == null) movement = obj.GetComponentInChildren<Movement>();
		if (movement == null) movement = obj.GetComponentInParent<Movement>();

		if (movement == null)
		{
			Debug.LogWarning($"ReversePlate: Player tagged '{playerTag}' entered but no Movement component found on '{obj.name}' or relatives.");
			return;
		}

		// If configured as oneShot, only trigger once (on enter). Ignore exits.
		if (oneShot)
		{
			if (used) return;
			if (!invert) return; // only act on enter

			bool newInvertV = !movement.invertVertical;
			bool newInvertH = !movement.invertHorizontal;
			movement.SetInvertVertical(newInvertV);
			movement.SetInvertHorizontal(newInvertH);
			used = true;

			// Mark used but keep collider enabled so the player doesn't fall through.
			Debug.Log($"ReversePlate (one-shot): toggled invertV={newInvertV} invertH={newInvertH} on '{obj.name}'. Plate remains solid.");
		}
		else
		{
			// Normal behavior: set/unset based on enter/exit for both axes
			movement.SetInvertVertical(invert);
			movement.SetInvertHorizontal(invert);
			Debug.Log($"ReversePlate: set invert={invert} on '{obj.name}' (Movement found: {movement.name})");
		}
	}
}

