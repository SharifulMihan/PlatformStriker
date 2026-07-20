using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Controls the overall penalty shootout: 5 shots, tracks goals,
/// resolves each shot as GOAL or SAVE.
/// On WIN: loads the "End" scene.
/// On LOSE: shows a panel with Restart / Quit to Main Menu buttons.
/// Attach this to an empty "GameManager" GameObject.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("References")]
    public BallController ball;
    public GoalkeeperAI keeper;
    public Transform goalLeft;
    public Transform goalRight;

    [Header("UI")]
    public TMP_Text scoreText;
    public TMP_Text messageText;
    public TMP_Text resultText;      // only used briefly for "GOAL!/SAVED!" style feedback, optional for a final win/lose caption
    public GameObject losePanel;     // a UI Panel with Restart + Quit buttons, disabled by default in the scene

    [Header("Scene Names")]
    public string endSceneName = "End";
    public string mainMenuSceneName = "MainMenu";

    [Header("Match Settings")]
    public int totalShots = 5;
    public int goalsNeededToWin = 3;
    public float saveRadius = 0.6f;         // how close the keeper must be to the ball's target to save it
    public float delayBetweenShots = 1.5f;  // pause between round N and round N+1
    public float finalResultDelay = 3f;     // pause after the LAST shot before End scene / LosePanel

    private int shotsTaken = 0;
    private int goalsScored = 0;
    private bool roundActive = false;
    private bool matchOver = false;

    void Start()
    {
        if (resultText != null) resultText.gameObject.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        StartRound();
    }

    void Update()
    {
        if (!roundActive || matchOver) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0f;

            // Clamp the click to within the goal posts so shots always aim at the goal
            float clampedX = Mathf.Clamp(mouseWorld.x, goalLeft.position.x, goalRight.position.x);
            Vector2 target = new Vector2(clampedX, goalLeft.position.y);

            TakeShot(target);
        }
    }

    void StartRound()
    {
        roundActive = true;
        messageText.text = "Click inside the goal to shoot!";
        ball.ResetBall();
        keeper.ResetKeeper();
    }

    void TakeShot(Vector2 target)
    {
        roundActive = false;
        shotsTaken++;

        ball.Shoot(target);
        keeper.ReactToShot(target);

        // Wait for the kick animation to finish, then resolve the outcome
        Invoke(nameof(ResolveShot), ball.kickDuration);
    }

    void ResolveShot()
    {
        float distance = Vector2.Distance(keeper.transform.position, ball.transform.position);
        bool saved = distance <= saveRadius;

        if (saved)
        {
            messageText.text = "SAVED!";
        }
        else
        {
            goalsScored++;
            messageText.text = "GOAL!";
        }

        scoreText.text = $"Goals: {goalsScored}   Shots: {shotsTaken}/{totalShots}";

        if (shotsTaken >= totalShots)
        {
            ShowFinalResult();
        }
        else
        {
            Invoke(nameof(StartRound), delayBetweenShots);
        }
    }

    void ShowFinalResult()
    {
        matchOver = true;
        bool won = goalsScored >= goalsNeededToWin;

        // Show the outcome right away...
        if (resultText != null)
        {
            resultText.gameObject.SetActive(true);
            resultText.text = won
                ? $"YOU WIN!  {goalsScored}/{totalShots} goals"
                : $"YOU LOSE.  {goalsScored}/{totalShots} goals";
        }

        // ...then wait 3 seconds before moving on to the next action.
        Invoke(nameof(ProceedAfterResult), finalResultDelay);
    }

    void ProceedAfterResult()
    {
        bool won = goalsScored >= goalsNeededToWin;

        if (won)
        {
            SceneManager.LoadScene(endSceneName);
        }
        else
        {
            if (losePanel != null) losePanel.SetActive(true);
        }
    }

    /// <summary>
    /// Hook this up to the "Restart" button's OnClick() in the Inspector.
    /// </summary>
    public void RestartMatch()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Hook this up to the "Quit" button's OnClick() in the Inspector.
    /// </summary>
    public void QuitToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}