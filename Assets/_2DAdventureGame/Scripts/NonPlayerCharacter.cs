using UnityEngine;

public class NonPlayerCharacter : MonoBehaviour
{
    public GameObject dialogueBubble;
    private void Start() { SetBubbleVisible(false); }
    public void SetBubbleVisible(bool visible)
    {
        if (dialogueBubble != null) dialogueBubble.SetActive(visible);
    }
}
