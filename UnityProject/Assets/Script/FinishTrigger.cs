using UnityEngine;

public class FinishTrigger : MonoBehaviour
{
    public GameFinish gameFinish;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            gameFinish.FinishGame();
        }
    }
}
