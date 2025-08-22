using UnityEngine;

public class FinishZone : MonoBehaviour
{
    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;

            LevelManager lm = FindObjectOfType<LevelManager>();
            if (lm != null)
            {
                lm.GoToNextLevel();
            }
            else
            {
                Debug.LogError("LevelManager not found!");
            }
        }
    }
}
