using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class GameManager : MonoBehaviour
{
    public PlayerController player;
    public UIHandler uiHandler;
    public GameObject bossPrefab;
    private EnemyController boss;
    private bool bossSpawned;
    private bool gameEnded;

    private void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (uiHandler == null)
        {
            UIDocument document = FindFirstObjectByType<UIDocument>();
            if (document != null)
            {
                uiHandler = document.GetComponent<UIHandler>();
                if (uiHandler == null) uiHandler = document.gameObject.AddComponent<UIHandler>();
            }
        }
        if (player == null) Debug.LogError("GameManager could not find PlayerController.");
        if (uiHandler == null) Debug.LogError("GameManager could not find UIHandler.");
        if (bossPrefab == null) bossPrefab = Resources.Load<GameObject>("RedBoss");
        if (bossPrefab == null) Debug.LogError("RedBoss prefab could not be loaded from the scene or Resources folder.");
        EnsureNPC();
    }

    private void EnsureNPC()
    {
        if (player == null || FindFirstObjectByType<NonPlayerCharacter>() != null) return;
        GameObject npcObject = new GameObject("NPC");
        npcObject.transform.position = player.transform.position + Vector3.right * 1.2f;
        SpriteRenderer npcRenderer = npcObject.AddComponent<SpriteRenderer>();
        npcRenderer.sprite = CreateSprite(new Color(1f, .66f, .18f), 24, 28);
        npcRenderer.sortingOrder = 1;
        BoxCollider2D collider = npcObject.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(.8f, 1f);
        NonPlayerCharacter npc = npcObject.AddComponent<NonPlayerCharacter>();

        GameObject promptObject = new GameObject("TalkPrompt_X");
        promptObject.transform.SetParent(npcObject.transform, false);
        promptObject.transform.localPosition = new Vector3(0f, .9f, 0f);
        promptObject.transform.localScale = new Vector3(.35f, .25f, 1f);
        SpriteRenderer promptRenderer = promptObject.AddComponent<SpriteRenderer>();
        promptRenderer.sprite = CreateSprite(Color.white, 8, 8);
        promptRenderer.sortingOrder = 5;
        TextMesh prompt = promptObject.AddComponent<TextMesh>();
        if (prompt != null)
        {
            prompt.text = "X";
            prompt.anchor = TextAnchor.MiddleCenter;
            prompt.alignment = TextAlignment.Center;
            prompt.characterSize = .08f;
            prompt.fontSize = 32;
            prompt.color = Color.black;
            MeshRenderer textRenderer = prompt.GetComponent<MeshRenderer>();
            if (textRenderer != null) textRenderer.sortingOrder = 6;
        }
        npc.dialogueBubble = prompt != null ? promptObject : null;
        npc.SetBubbleVisible(false);
    }

    private static Sprite CreateSprite(Color color, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color clear = new Color(0, 0, 0, 0);
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            bool inside = x > 1 && x < width - 2 && y > 1 && y < height - 2;
            texture.SetPixel(x, y, inside ? color : clear);
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(.5f, .1f), 16);
    }

    private void Update()
    {
        if (gameEnded) return;
        if (player != null && player.health <= 0)
        {
            player.Die();
            EndGame(false);
            return;
        }

        if (!bossSpawned && AllNormalEnemiesDefeated()) SpawnBoss();
        if (bossSpawned && boss != null && !boss.isBroken) EndGame(true);
    }

    private bool AllNormalEnemiesDefeated()
    {
        EnemyController[] allEnemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        int normalEnemyCount = 0;
        foreach (EnemyController enemy in allEnemies)
        {
            if (enemy == null || enemy.isBoss) continue;
            normalEnemyCount++;
            if (enemy.isBroken) return false;
        }
        return normalEnemyCount > 0;
    }

    private void SpawnBoss()
    {
        if (bossPrefab == null || player == null) return;
        Vector2 spawnPosition = (Vector2)player.transform.position + new Vector2(4f, 1.5f);
        Tilemap[] maps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        Tilemap largestMap = null;
        int largestArea = 0;
        foreach (Tilemap map in maps)
        {
            if (map == null) continue;
            BoundsInt cells = map.cellBounds;
            int area = cells.size.x * cells.size.y;
            if (area > largestArea) { largestArea = area; largestMap = map; }
        }
        if (largestMap != null)
        {
            BoundsInt cells = largestMap.cellBounds;
            Vector3 min = largestMap.CellToWorld(cells.min);
            Vector3 max = largestMap.CellToWorld(cells.max);
            spawnPosition.x = Mathf.Clamp(spawnPosition.x, min.x + 2f, max.x - 2f);
            spawnPosition.y = Mathf.Clamp(spawnPosition.y, min.y + 2f, max.y - 2f);
        }
        GameObject bossObject = Instantiate(bossPrefab, spawnPosition, Quaternion.identity);
        boss = bossObject.GetComponent<EnemyController>();
        if (boss == null)
        {
            Debug.LogError("Boss prefab is missing EnemyController.", bossObject);
            Destroy(bossObject);
            return;
        }
        boss.isBoss = true;
        bossSpawned = true;
        Debug.Log("The red boss has entered the level!");
    }

    private void EndGame(bool won)
    {
        gameEnded = true;
        Time.timeScale = 0f;
        if (uiHandler != null)
        {
            if (won) uiHandler.DisplayWinScreen();
            else uiHandler.DisplayLoseScreen();
        }

    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
