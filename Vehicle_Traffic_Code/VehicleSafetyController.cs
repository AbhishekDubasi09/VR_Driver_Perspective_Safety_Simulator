using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

public class VehicleSafetyController : MonoBehaviour
{
    [Header("Input System")]
    public InputActionAsset inputActions;
    private InputAction steerAction, accelAction, brakeAction, toggleAEBAction;

    [Header("Driving Parameters")]
    public float acceleration = 12f;
    public float deceleration = 8f;
    public float maxForwardSpeed = 25f;
    public float maxReverseSpeed = -10f;
    public float brakeForce = 40f;

    [Header("Steering Parameters")]
    public float steerSensitivity = 100f;
    public float minSteerSpeed = 0.1f;

    [Header("AEB Parameters")]
    public bool isAEBEnabled = false;
    public float aebDetectionRange = 20f;
    public float aebDetectionRadius = 1.5f;
    public LayerMask obstacleLayers;

    [Header("Runtime State")]
    [SerializeField] private float throttleInput;
    [SerializeField] private float steerInput;
    [SerializeField] private float currentSpeed;
    [SerializeField] private bool isBraking;
    [SerializeField] private bool aebBraking;

    [Header("Engine State")]
    public int isEngineRunning = 0;
    private float speedClamped;
    private float rawSpeed;

    [Header("Animation")]
    public Animator steeringAnimator;
    public float steeringLerpSpeed = 5f;
    private float animBlendValue;

    [Header("Steering Wheel")]
    public Transform steeringWheel;
    public float maxSteeringAngle = 180f;
    public float wheelLerpSpeed = 5f;
    private float currentWheelAngle;
    private Quaternion initialWheelRotation;

    [Header("Crash Logic")]
    public float restartDelay = 1.5f;
    public CanvasGroup crashCanvas;
    public TMP_Text crashMessage;
    private bool isRestarting = false;

    [Header("Driver Feedback UI")]
    public TMP_Text overspeedText;
    public TMP_Text aebCountText;

    private int aebActivationCount = 0;
    private float obstacleDetectedTime = -1f;
    private float brakePressTime = -1f;
    private bool driverReacted = false;
    private bool hasCollided = false;

    private bool hasWarnedOverspeed = false;
    public float speedWarningThresholdKPH = 60f;

    private bool wasBrakingLastFrame = false;
    private float pedestrianAudioCooldown = 0f;

    void Start()
    {
        if (steeringWheel != null)
            initialWheelRotation = steeringWheel.localRotation;

        crashCanvas.alpha = 0;
        crashCanvas.blocksRaycasts = false;

        var driving = inputActions.FindActionMap("Driving");
        steerAction = driving.FindAction("Steer");
        accelAction = driving.FindAction("Accelerate");
        brakeAction = driving.FindAction("Brake");
        toggleAEBAction = driving.FindAction("ToggleAEB");
        driving.Enable();

        if (toggleAEBAction != null)
        {
            toggleAEBAction.performed += ctx =>
            {
                isAEBEnabled = !isAEBEnabled;
                Debug.Log("AEB is now " + (isAEBEnabled ? "ENABLED" : "DISABLED"));

                if (isAEBEnabled)
                    AudioManager.Instance?.Play("AEB Activated");
                else
                {
                    if (aebBraking) aebBraking = false;
                    AudioManager.Instance?.Play("AEB intervention successful");
                }
            };
        }

        if (overspeedText != null)
            overspeedText.gameObject.SetActive(false);

        if (aebCountText != null)
            aebCountText.text = "AEB Activations: 0";

        string levelName = SceneManager.GetActiveScene().name;
        AudioManager.Instance?.PlayLevelIntro(levelName);
    }

    void Update()
    {
        GetPlayerInput();
        DetectObstacle();

        // Driver reaction timing (non-AEB)
        if (!isAEBEnabled && !driverReacted && isBraking && obstacleDetectedTime >= 0f)
        {
            brakePressTime = Time.time;
            float reactionTime = brakePressTime - obstacleDetectedTime;

            DriverAnalyticsManager.Instance.RecordReactionTime();
            DriverAnalyticsManager.Instance.RecordTimeToAct();
            DriverAnalyticsManager.Instance.AddBrakingEvent();

            Debug.Log($"Driver Reaction Time: {reactionTime:F2} seconds");

            if (!hasCollided)
            {
                EvaluateStopPerformance(reactionTime);
                DriverAnalyticsManager.Instance.RegisterAccurateStop();
                AudioManager.Instance?.Play("Collision avoided");
            }

            driverReacted = true;
        }

        // Feedback on brake press
        if (isBraking && !wasBrakingLastFrame)
        {
            DriverAnalyticsManager.Instance.AddBrakingEvent();
            AudioManager.Instance?.Play("Brake input delayed pay attention");
        }
        wasBrakingLastFrame = isBraking;

        // Movement & visuals
        HandleMovement();
        HandleSteering();
        UpdateSteeringAnimation();
        UpdateSteeringWheelVisual();
        UpdateSpeedClamped();

        // Overspeed warning
        float currentKPH = GetCurrentSpeedKPH();
        if (currentKPH > speedWarningThresholdKPH)
        {
            if (!hasWarnedOverspeed)
            {
                AudioManager.Instance?.Play("Speed exceeds safety protocol");
                hasWarnedOverspeed = true;
            }
            overspeedText?.gameObject.SetActive(true);
            if (overspeedText != null)
                overspeedText.text = $"Overspeed!\nSpeed: {currentKPH:F1} km/h";
        }
        else if (currentKPH <= speedWarningThresholdKPH - 5f)
        {
            hasWarnedOverspeed = false;
            overspeedText?.gameObject.SetActive(false);
        }
    }

    void GetPlayerInput()
    {
        throttleInput = accelAction?.ReadValue<float>() ?? 0f;
        steerInput = steerAction?.ReadValue<float>() ?? 0f;
        float brakeVal = brakeAction?.ReadValue<float>() ?? 0f;
        isBraking = brakeVal > 0.2f;

        if (throttleInput > 0 && isEngineRunning == 0)
            StartCoroutine(GetComponent<EngineAudio>().StartEngine());
    }

    void HandleMovement()
    {
        if (isEngineRunning < 2) return;
        if (isAEBEnabled && aebBraking)
            currentSpeed = Mathf.MoveTowards(currentSpeed, throttleInput < 0 ? maxReverseSpeed : 0f, brakeForce * Time.deltaTime);
        else if (isBraking)
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, brakeForce * Time.deltaTime);
        else if (throttleInput > 0)
            currentSpeed = Mathf.MoveTowards(currentSpeed, maxForwardSpeed, acceleration * Time.deltaTime);
        else if (throttleInput < 0)
            currentSpeed = Mathf.MoveTowards(currentSpeed, maxReverseSpeed, acceleration * Time.deltaTime);
        else
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime);

        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
    }

    void HandleSteering()
    {
        if (Mathf.Abs(currentSpeed) > minSteerSpeed)
        {
            float steerAmount = steerInput * steerSensitivity * Time.deltaTime * (currentSpeed / maxForwardSpeed);
            transform.Rotate(Vector3.up * steerAmount);
        }
    }

    void UpdateSteeringAnimation()
    {
        if (steeringAnimator == null) return;
        animBlendValue = Mathf.Lerp(animBlendValue, steerInput, steeringLerpSpeed * Time.deltaTime);
        steeringAnimator.SetFloat("SteeringAmount", animBlendValue);
    }

    void UpdateSteeringWheelVisual()
    {
        if (steeringWheel == null) return;
        float targetAngle = steerInput * maxSteeringAngle;
        currentWheelAngle = Mathf.Lerp(currentWheelAngle, targetAngle, Time.deltaTime * wheelLerpSpeed);
        steeringWheel.localRotation = initialWheelRotation * Quaternion.Euler(0f, -currentWheelAngle, 0f);
    }

    void UpdateSpeedClamped()
    {
        rawSpeed = Mathf.Abs(currentSpeed);
        speedClamped = Mathf.Lerp(speedClamped, rawSpeed, Time.deltaTime);
    }

    void DetectObstacle()
    {
        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        if (Physics.SphereCast(origin, aebDetectionRadius, transform.forward, out hit, aebDetectionRange, obstacleLayers))
        {
            bool isPed = hit.collider.CompareTag("People");
            bool isCar = hit.collider.CompareTag("Car");

            if (isPed || isCar)
            {
                Debug.DrawLine(origin, hit.point, Color.red);

                if (obstacleDetectedTime < 0f)
                {
                    obstacleDetectedTime = Time.time;
                    driverReacted = false;
                    hasCollided = false;

                    if (isPed)
                    {
                        DriverAnalyticsManager.Instance.RegisterPedestrianDetected();
                        AudioManager.Instance?.Play("Approaching pedestrian crossing");
                    }
                    else
                    {
                        // Optionally track vehicle detections:
                        // DriverAnalyticsManager.Instance.RegisterVehicleDetected();
                        AudioManager.Instance?.Play("Approaching vehicle ahead");
                    }
                }

                if (isAEBEnabled && !aebBraking)
                {
                    aebActivationCount++;
                    DriverAnalyticsManager.Instance.aebActivations++;
                    aebBraking = true;
                    if (aebCountText != null)
                        aebCountText.text = $"AEB Activations: {aebActivationCount}";
                    AudioManager.Instance?.Play("AEB Activated");
                }
            }
        }
        else
        {
            // No relevant obstacle in range → reset
            obstacleDetectedTime = -1f;
            aebBraking = false;
        }
    }

    void EvaluateStopPerformance(float reactionTime)
    {
        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(origin, transform.forward, out hit, aebDetectionRange, obstacleLayers))
        {
            if (reactionTime <= 0.75f)
                AudioManager.Instance?.Play("Driver response is satisfactory");
            else if (reactionTime > 1.5f)
                AudioManager.Instance?.Play("Driver failed to respond in time");
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasCollided) return;

        if (((1 << collision.gameObject.layer) & obstacleLayers) != 0)
        {
            hasCollided = true;
            if (obstacleDetectedTime > 0f)
            {
                float ttc = Time.time - obstacleDetectedTime;
                DriverAnalyticsManager.Instance.RecordTimeToCollision(ttc);
                Debug.Log($"Time to Collision: {ttc:F2}s");
            }

            if (collision.gameObject.CompareTag("People"))
            {
                AudioManager.Instance?.Play("Detected a crossing pedestrian");
                DriverAnalyticsManager.Instance.RegisterCollision("pedestrian");
            }
            else if (collision.gameObject.CompareTag("Car"))
            {
                AudioManager.Instance?.Play("Collision detected with vehicle");
                DriverAnalyticsManager.Instance.RegisterCollision("car");
            }
            else
            {
                AudioManager.Instance?.Play("Collision detected");
                DriverAnalyticsManager.Instance.RegisterCollision("car");
            }

            if (!isRestarting)
            {
                isRestarting = true;
                StartCoroutine(FadeAndRestart("Collision occurred. Respawning..."));
            }
        }
    }

    IEnumerator FadeAndRestart(string message)
    {
        crashCanvas.blocksRaycasts = true;
        crashMessage.text = message;

        float t = 0f;
        while (t < restartDelay)
        {
            t += Time.deltaTime;
            crashCanvas.alpha = Mathf.Lerp(0, 1, t / restartDelay);
            yield return null;
        }
        yield return new WaitForSeconds(1f);

        // Respawn at current level spawn
        var lm = FindObjectOfType<LevelManager>();
        if (lm != null)
            lm.SpawnPlayerAtLevel(LevelManager.currentLevel);
        else
            Debug.LogError("[VehicleSafetyController] LevelManager not found!");

        crashCanvas.alpha = 0;
        crashCanvas.blocksRaycasts = false;
        isRestarting = false;
    }

    // Utility getters
    public float GetSpeedRatio() => (speedClamped * Mathf.Clamp01(Mathf.Abs(throttleInput))) / maxForwardSpeed;
    public float GetCurrentSpeedKPH() => Mathf.Abs(currentSpeed) * 3.6f;
    public float ThrottleInput => throttleInput;
    public float SteerInput => steerInput;
    public float CurrentSpeed => currentSpeed;
    public bool IsBraking => isBraking;
    public bool AebBraking => aebBraking;
}