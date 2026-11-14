using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuController : MonoBehaviour
{
    private UIDocument _uiDocument;
    private Button _startButton;

    private void Start()
    {
        // Ставимо гру на паузу одразу після завантаження сцени
        Time.timeScale = 0f;

        _uiDocument = GetComponent<UIDocument>();
        _startButton = _uiDocument.rootVisualElement.Q<Button>("start-button");

        if (_startButton != null)
        {
            _startButton.clicked += OnStartButtonClicked;
        }
    }

    private void OnDisable()
    {
        if (_startButton != null)
        {
            _startButton.clicked -= OnStartButtonClicked;
        }
    }

    private void OnStartButtonClicked()
    {
        Debug.Log("Кнопку натиснуто! Запускаємо гру...");

        // Запускаємо час у грі
        Time.timeScale = 1f;

        // Ховаємо лише кнопку (решта елементів GameUI залишається на екрані)
        _startButton.style.display = DisplayStyle.None;
    }
}