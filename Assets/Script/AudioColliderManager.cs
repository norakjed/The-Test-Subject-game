using UnityEngine;

public class AudioColliderManager : MonoBehaviour
{
    public Collider audio1Collider; // if player walk past
    public Collider audio2Collider; // if player pressed only 1 button
    public Collider audio3Collider; // if player pressed all buttons

    void Update()
    {
        int presses = JumpscareButton.pressCount;

        if (presses == 0)
        {
            // Enable audio1, disable others
            SetColliders(true, false, false);
        }
        else if (presses == 1)
        {
            // Enable audio2, disable others
            SetColliders(false, true, false);
        }
        else if (presses > 1)
        {
            // Enable audio3, disable others
            SetColliders(false, false, true);
        }
    }

    void SetColliders(bool a1, bool a2, bool a3)
    {
        if (audio1Collider != null) audio1Collider.enabled = a1;
        if (audio2Collider != null) audio2Collider.enabled = a2;
        if (audio3Collider != null) audio3Collider.enabled = a3;
    }
}