using UnityEngine;
using System.Collections;

public class LevelManager : MonoBehaviour
{
    [Header("Spawn Points")]
    public Transform level1Spawn;
    public Transform level2Spawn;
    public Transform level3Spawn;

    [Header("Player")]
    [Tooltip("Drag your Player Car GameObject here")]
    public GameObject playerCarObject;

    [Header("Level Paths")]
    [Tooltip("Assign the Level 1 path container (e.g. LineRenderer parent)")]
    public GameObject level1Path;
    [Tooltip("Assign the Level 2 path container")]
    public GameObject level2Path;
    [Tooltip("Assign the Level 3 path container")]
    public GameObject level3Path;

    [Header("Accident Detection")]
    [Tooltip("Angle threshold (degrees) beyond which we consider the car tipped/accident")]
    public float accidentAngleThreshold = 30f;
    private Vector3 initialCarEulerAngles;

    // Tracks which “sub‐level” we’re on (1 through 3)
    public static int currentLevel = 1;

    void Start()
    {
        // Only on the very first entry to Level 1 do we reset analytics
        if (currentLevel == 1)
        {
            Debug.Log("[LevelManager] Resetting analytics at start of Level 1");
            DriverAnalyticsManager.Instance.ResetAllStats();
        }

        SpawnPlayerAtLevel(currentLevel);
    }

    /// <summary>
    /// Moves the player to the correct spawn point, resets physics,
    /// and toggles only the relevant level path.
    /// </summary>
    public void SpawnPlayerAtLevel(int level)
    {
        // Determine which spawn to use
        Transform spawnPoint = level1Spawn;
        switch (level)
        {
            case 2: spawnPoint = level2Spawn; break;
            case 3: spawnPoint = level3Spawn; break;
        }

        if (playerCarObject == null)
        {
            Debug.LogError("[LevelManager] playerCarObject is not assigned!");
            return;
        }

        // Reset any existing physics on the car
        var rb = playerCarObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Teleport & orient the car
        playerCarObject.transform.position = spawnPoint.position;
        playerCarObject.transform.rotation = spawnPoint.rotation;
        playerCarObject.SetActive(true);

        // Record its upright orientation for accident checks
        initialCarEulerAngles = playerCarObject.transform.eulerAngles;

        // Show only this level's guiding path
        if (level1Path != null) level1Path.SetActive(level == 1);
        if (level2Path != null) level2Path.SetActive(level == 2);
        if (level3Path != null) level3Path.SetActive(level == 3);

        // Play any intro fade or audio cue
        var fade = FindObjectOfType<VRFadeController>();
        if (fade != null)
            StartCoroutine(fade.PlayIntro(level));
        AudioManager.Instance?.Play("Intro Level " + level);
    }

    void Update()
    {
        if (playerCarObject == null) return;

        // Check if the car has tipped past our accident threshold
        Vector3 currentAngles = playerCarObject.transform.eulerAngles;
        float dx = Mathf.DeltaAngle(initialCarEulerAngles.x, currentAngles.x);
        float dz = Mathf.DeltaAngle(initialCarEulerAngles.z, currentAngles.z);

        if (Mathf.Abs(dx) > accidentAngleThreshold || Mathf.Abs(dz) > accidentAngleThreshold)
        {
            Debug.Log("[LevelManager] Accident detected—respawning at current level");
            var rb = playerCarObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            SpawnPlayerAtLevel(currentLevel);
        }
    }

    /// <summary>
    /// Called by your FinishZone triggers when the player reaches a finish.
    /// </summary>
    public void GoToNextLevel()
    {
        StartCoroutine(TransitionToNext());
    }

    private IEnumerator TransitionToNext()
    {
        // Play the “level cleared” fade if present
        var fade = FindObjectOfType<VRFadeController>();
        if (fade != null)
            yield return fade.PlayCleared(currentLevel);

        currentLevel++;

        if (currentLevel > 3)
        {
            // All three levels complete → show final summary
            Debug.Log("[LevelManager] All levels complete—showing summary UI");
            SummaryUIManager.Instance.PopulateAndShow();
            yield break;
        }

        // Otherwise, spawn at the next level
        SpawnPlayerAtLevel(currentLevel);
    }
}