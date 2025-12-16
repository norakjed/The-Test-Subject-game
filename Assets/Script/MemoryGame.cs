using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MemoryGame : MonoBehaviour
{
    public static MemoryGame Instance;

    [Header("Tiles")]
    public Tile[] tiles = new Tile[12];

    [Header("Sequence")]
    public int sequenceLength = 6;
    public float glowDuration = 0.8f;
    public float betweenGlowDelay = 0.3f;

    [Header("Player")]
    public GameObject player;
    public float fallForce = 10f;

    private List<int> sequence = new List<int>();
    private int inputIndex = 0;
    private bool acceptingInput = false;
    private HashSet<int> collected = new HashSet<int>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    // Starts a new memory game
    public void StartGame()
    {
        StopAllCoroutines();
        GenerateSequence();
        StartCoroutine(PlaySequence());
    }

    void GenerateSequence()
    {
        // Build a sequence where each step selects one tile from a pair.
        // Pairs are (0,1), (2,3), (4,5), ... so for step i we pick from pair i.
        sequence.Clear();
        int numPairs = tiles.Length / 2;
        for (int i = 0; i < sequenceLength; i++)
        {
            int pairIndex = i;
            if (pairIndex >= numPairs)
            {
                // fallback: choose a random pair if sequenceLength exceeds number of pairs
                pairIndex = Random.Range(0, numPairs);
            }

            int left = pairIndex * 2;
            int right = left + 1;

            // safety bounds
            if (left < 0 || right >= tiles.Length)
            {
                // choose any valid tile as fallback
                sequence.Add(Random.Range(0, tiles.Length));
                continue;
            }

            int pick = (Random.Range(0, 2) == 0) ? left : right;
            sequence.Add(pick);
        }
    }

    IEnumerator PlaySequence()
    {
        acceptingInput = false;
        yield return new WaitForSeconds(0.5f);

        // While glowing: set all tiles to trigger=true
        for (int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i] != null) tiles[i].SetTrigger(true);
        }

        // Play sequence: after each tile's glow ends, make that tile solid (trigger=false)
        foreach (int idx in sequence)
        {
            if (idx >= 0 && idx < tiles.Length && tiles[idx] != null)
            {
                tiles[idx].Glow(glowDuration);
            }

            // wait for the glow period
            yield return new WaitForSeconds(glowDuration);

            // after glow stops for this tile, make it solid (trigger=false)
            if (idx >= 0 && idx < tiles.Length && tiles[idx] != null)
            {
                tiles[idx].SetTrigger(false);
            }

            // wait the between-glow delay before next
            yield return new WaitForSeconds(betweenGlowDelay);
        }

        // Wrong tiles remain trigger=true (they were set to true at the start and never changed)
        // Correct tiles are now solid (trigger=false)

        acceptingInput = true;
        inputIndex = 0;
        collected.Clear();
        Debug.Log("MemoryGame: player's turn");
    }

    // Called by tiles when player steps on them
    public void TilePressed(Tile tile)
    {
        if (!acceptingInput) return;
        int tileIndex = System.Array.IndexOf(tiles, tile);
        if (tileIndex < 0) return;
        // If tile is part of the sequence and not yet collected, mark collected.
        if (sequence.Contains(tileIndex))
        {
            if (collected.Contains(tileIndex)) return; // already collected

            collected.Add(tileIndex);
            // ensure it's solid
            tiles[tileIndex]?.SetTrigger(false);

            Debug.Log($"MemoryGame: collected {tileIndex} ({collected.Count}/{sequence.Count})");

            if (collected.Count >= sequence.Count)
            {
                Win();
            }
        }
        else
        {
            // wrong tile
            Debug.Log($"MemoryGame: wrong tile {tileIndex}");
            Fail();
        }
    }

    void Win()
    {
        acceptingInput = false;
        Debug.Log("MemoryGame: Win!");
        // Additional win behavior can be placed here
    }

    void Fail()
    {
        acceptingInput = false;
        Debug.Log("MemoryGame: Wrong tile - player fell");
        // Player naturally fell through trigger - don't need to force anything
    }
}
