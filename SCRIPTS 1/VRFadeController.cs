using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class VRFadeController : MonoBehaviour
{
    public CanvasGroup fadeCanvas;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI clearedText;

    [System.Serializable]
    public class LevelMessages
    {
        public string startMessage;
        public string clearMessage;
    }

    public LevelMessages[] levelMessages;

    public IEnumerator PlayIntro(int level)
    {
        fadeCanvas.alpha = 5f;
        clearedText.gameObject.SetActive(false);
        levelText.gameObject.SetActive(true);

        string message = GetMessage(level, true);
        levelText.text = message;

        yield return new WaitForSeconds(3f);
        yield return FadeCanvas(0, 3f);

        levelText.gameObject.SetActive(false);
    }

    public IEnumerator PlayCleared(int level)
    {
        levelText.gameObject.SetActive(false);
        clearedText.gameObject.SetActive(true);

        string message = GetMessage(level, false);
        clearedText.text = message;

        yield return new WaitForSeconds(3f);
        yield return FadeCanvas(3, 3f);
    }

    string GetMessage(int level, bool isStart)
    {
        if (levelMessages.Length >= level)
        {
            var msg = levelMessages[level - 1];
            return isStart ? msg.startMessage : msg.clearMessage;
        }
        return isStart ? $"Level {level}" : "Challenge Cleared";
    }

    IEnumerator FadeCanvas(float targetAlpha, float duration)
    {
        float start = fadeCanvas.alpha;
        float time = 0;
        while (time < duration)
        {
            fadeCanvas.alpha = Mathf.Lerp(start, targetAlpha, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        fadeCanvas.alpha = targetAlpha;
    }
}
