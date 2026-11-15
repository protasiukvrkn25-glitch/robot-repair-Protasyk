using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuController : MonoBehaviour
{
    private Button startButton;

    private void Start()
    {
        Time.timeScale = 0f;
        RegisterStartButtonOnAllDocuments();
    }

    private void OnDisable()
    {
        if (startButton != null) startButton.clicked -= OnStartButtonClicked;
    }

    private void RegisterStartButtonOnAllDocuments()
    {
        UIDocument[] documents = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
        foreach (UIDocument document in documents)
        {
            if (document == null) continue;
            Button button = document.rootVisualElement.Q<Button>("start-button");
            if (button == null) continue;
            button.clicked -= OnStartButtonClicked;
            button.clicked += OnStartButtonClicked;
            if (startButton == null) startButton = button;
        }
    }

    private void OnStartButtonClicked()
    {
        Debug.Log("Кнопку натиснуто! Запускаємо гру...");
        Time.timeScale = 1f;

        UIDocument[] documents = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
        foreach (UIDocument document in documents)
        {
            if (document == null) continue;
            Button button = document.rootVisualElement.Q<Button>("start-button");
            if (button != null) button.style.display = DisplayStyle.None;
            VisualElement startOverlay = document.rootVisualElement.Q<VisualElement>("StartScreenOverlay");
            if (startOverlay != null) startOverlay.style.display = DisplayStyle.None;
        }
    }
}
