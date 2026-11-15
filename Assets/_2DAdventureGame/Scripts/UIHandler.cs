using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
public class UIHandler : MonoBehaviour
{
    public static UIHandler instance { get; private set; }
    public float displayTime = 4f;

    private VisualElement dialogue;
    private float dialogueTimer = -1f;
    private PlayerController player;
    private bool defeatShown;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        UIDocument document = GetComponent<UIDocument>();
        if (document == null)
        {
            Debug.LogError("UIHandler must be attached to the UIDocument object.");
            return;
        }

        dialogue = document.rootVisualElement.Q<VisualElement>("NPCDialogue");
        player = FindFirstObjectByType<PlayerController>();
        SetHealthValue(player != null && player.maxHealth > 0 ? player.health / (float)player.maxHealth : 1f);
        if (dialogue != null) dialogue.style.display = DisplayStyle.None;
    }

    public void SetHealthValue(float normalizedHealth)
    {
        float fill = Mathf.Clamp01(normalizedHealth);
        UIDocument[] documents = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);

        foreach (UIDocument document in documents)
        {
            if (document == null) continue;
            VisualElement root = document.rootVisualElement;
            Label healthValue = root.Q<Label>("HealthValue");
            if (healthValue == null)
            {
                root.Query<Label>().ForEach(label =>
                {
                    if (healthValue == null && label.text.EndsWith("%")) healthValue = label;
                });
            }
            if (healthValue != null)
                healthValue.text = $"ХП {Mathf.RoundToInt(fill * 100f)}%";

            VisualElement healthBar = root.Q<VisualElement>("HealthBar") ?? root.Q<VisualElement>("Healthbar");
            if (healthBar == null) continue;

            VisualElement container = root.Q<VisualElement>("HealthBarContainer") ?? healthBar.parent;
            float containerWidth = container != null ? container.resolvedStyle.width : 0f;
            if (containerWidth <= 0f) containerWidth = 82f;

            healthBar.style.flexGrow = 0f;
            healthBar.style.flexShrink = 0f;
            healthBar.style.flexBasis = Length.Pixels(containerWidth * fill);
            healthBar.style.width = Length.Pixels(containerWidth * fill);
        }
    }

    private void Update()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            int maxHealth = Mathf.Max(1, player.maxHealth);
            SetHealthValue(player.health / (float)maxHealth);
            if (player.health <= 0 && !defeatShown)
            {
                defeatShown = true;
                DisplayLoseScreen();
            }
        }

        if (dialogueTimer <= 0) return;
        dialogueTimer -= Time.deltaTime;
        if (dialogueTimer <= 0 && dialogue != null) dialogue.style.display = DisplayStyle.None;
    }

    public void DisplayDialogue()
    {
        if (dialogue == null) return;
        dialogue.style.display = DisplayStyle.Flex;
        dialogueTimer = displayTime;
    }

    public void DisplayWinScreen()
    {
        SetEndScreen("WinScreenContainer", "WinScreen", "ПЕРЕМОГА", false);
    }

    public void DisplayLoseScreen()
    {
        SetEndScreen("LoseScreenContainer", "LoseScreen", "ПОРАЗКА", true);
    }

    private static void RestartThroughGameManager()
    {
        GameManager manager = FindFirstObjectByType<GameManager>();
        if (manager != null) manager.RestartLevel();
        else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private static void SetEndScreen(string containerName, string artName, string titleText, bool isLose)
    {
        UIDocument[] documents = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
        foreach (UIDocument document in documents)
        {
            if (document == null) continue;
            VisualElement root = document.rootVisualElement;
            VisualElement screen = root.Q<VisualElement>(containerName);

            if (screen == null)
            {
                screen = new VisualElement { name = containerName, pickingMode = PickingMode.Ignore };
                screen.style.position = Position.Absolute;
                screen.style.left = 0;
                screen.style.right = 0;
                screen.style.top = 0;
                screen.style.bottom = 0;
                screen.style.backgroundColor = new Color(0f, 0f, 0f, 0.82f);
                root.Add(screen);
            }

            screen.style.display = DisplayStyle.Flex;
            screen.style.opacity = 1f;

            Button restart = root.Q<Button>("RestartButton");
            if (restart == null)
            {
                restart = new Button { name = "RestartButton" };
                screen.Add(restart);
            }
            restart.text = "Грати знову";
            restart.clicked -= RestartThroughGameManager;
            restart.clicked += RestartThroughGameManager;
            restart.style.position = Position.Absolute;
            restart.style.left = Length.Percent(35f);
            restart.style.right = Length.Percent(35f);
            restart.style.top = Length.Percent(66f);
            restart.style.height = 58;
            restart.style.fontSize = 22;

            VisualElement art = root.Q<VisualElement>(artName);
            if (art != null) art.style.display = DisplayStyle.Flex;

            if (isLose)
            {
                Label title = root.Q<Label>("LoseTitle");
                if (title == null)
                {
                    title = new Label { name = "LoseTitle" };
                    screen.Add(title);
                }
                title.text = titleText;
                title.pickingMode = PickingMode.Ignore;
                title.style.display = DisplayStyle.Flex;
                title.style.position = Position.Absolute;
                title.style.left = 0;
                title.style.right = 0;
                title.style.top = Length.Percent(38f);
                title.style.height = 90;
                title.style.fontSize = 72;
                title.style.color = Color.white;
                title.style.unityFontStyleAndWeight = FontStyle.Bold;
                title.style.unityTextAlign = TextAnchor.MiddleCenter;
                title.BringToFront();
            }
        }
    }
}


