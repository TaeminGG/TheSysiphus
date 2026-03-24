using UnityEngine;

public class LevelManager : MonoBehaviour
{
    private ChunkLoader chunkLoader;

    public int currentLevel = 1;           //

    private void Awake()
    {
        chunkLoader = GetComponent<ChunkLoader>();
    }

    private void Start()
    {
        LoadChunk(0, 50);
    }

    public void LoadChunk(int index, int height)
    {
        chunkLoader.AddChunk(index, height, currentLevel, true);
        chunkLoader.AddChunk(index, height, currentLevel, false);
        currentLevel += 1;
    }

}
