using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MovingStartButton : MonoBehaviour
{
    public float detectionRadius = 50f;
    public string gameSceneName = "PlayScene";
    public Text instructionText;
    public float moveSpeed = 5f; // Speed of smooth movement

    // Title position to avoid
    public Vector2 titlePosition = new Vector2(-46, 298);
    public Vector2 titleSize = new Vector2(1000, 50);

    private Button button;
    private RectTransform rectTransform;
    private Canvas canvas;
    private Vector2 targetPosition;
    private bool isMoving = false;

    void Start()
    {
        button = GetComponent<Button>();
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();


        if (instructionText != null)
        {
            instructionText.text = "Press Enter to Start";
            instructionText.gameObject.SetActive(true);
        }

        // Set initial random position
        MoveToRandomPosition();
    }

    void Update()
    {
        // Check mouse proximity
        if (!isMoving && IsMouseNearButton())
        {
            MoveToRandomPosition();
        }

        // Check for Enter key
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    bool IsMouseNearButton()
    {
        if (canvas == null || rectTransform == null) return false;

        Vector2 mousePos = Input.mousePosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            mousePos,
            canvas.worldCamera,
            out Vector2 localMousePos
        );

        Vector2 buttonPos = rectTransform.anchoredPosition;
        float distance = Vector2.Distance(localMousePos, buttonPos);

        return distance < detectionRadius;
    }

    void MoveToRandomPosition()
    {
        Rect canvasRect = canvas.GetComponent<RectTransform>().rect;
        Vector2 newPos;
        do
        {
            float randomX = Random.Range(canvasRect.xMin + rectTransform.rect.width / 2, canvasRect.xMax - rectTransform.rect.width / 2);
            float randomY = Random.Range(canvasRect.yMin + rectTransform.rect.height / 2, canvasRect.yMax - rectTransform.rect.height / 2);
            newPos = new Vector2(randomX, randomY);
        } while (IsInTitleArea(newPos));

        targetPosition = newPos;
        StartCoroutine(SmoothMove());
    }

    IEnumerator SmoothMove()
    {
        isMoving = true;
        Vector2 startPos = rectTransform.anchoredPosition;
        float elapsed = 0f;
        float duration = 0.5f; // Adjust duration

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t);
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPosition, t);
            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
        isMoving = false;
    }

    bool IsInTitleArea(Vector2 pos)
    {
        float left = titlePosition.x - titleSize.x / 2;
        float right = titlePosition.x + titleSize.x / 2;
        float bottom = titlePosition.y - titleSize.y / 2;
        float top = titlePosition.y + titleSize.y / 2;

        return pos.x >= left && pos.x <= right && pos.y >= bottom && pos.y <= top;
    }
}