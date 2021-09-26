using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManagerScript : MonoBehaviour
{

    #region Singleton

    private static BoardManagerScript _instance = null;

    public static BoardManagerScript Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<BoardManagerScript>();

                if (_instance == null)
                {
                    Debug.LogError("Fatal Error: BoardManager not Found");
                }
            }
            return _instance;
        }
    }

    #endregion

    [Header("Board")]
    public Vector2Int size;
    public Vector2 offsetTile;
    public Vector2 offsetBoard;

    [Header("Tile")]
    public List<Sprite> tileTypes = new List<Sprite>();
    public GameObject tilePrefab;


    private Vector2 startPosition;

    private Vector2 endPosition;

    private TileControllerScript[,] tiles;
    Vector2 tileSize;

    private int combo;


    private void Start()
    {
        tileSize = tilePrefab.GetComponent<SpriteRenderer>().size;
        CreateBoard(tileSize);
        IsProcessing = false;
        IsSwapping = false;
    }

    private void CreateBoard(Vector2 tileSize)
    {
        tiles = new TileControllerScript[size.x, size.y];

        Vector2 totalSize = (tileSize + offsetTile) * (size - Vector2.one);

        startPosition = (Vector2)transform.position - (totalSize / 2) + offsetBoard;
        endPosition = startPosition + totalSize;

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                TileControllerScript newTile =
                    Instantiate(tilePrefab,
                    new Vector2(startPosition.x + ((tileSize.x + offsetTile.x) * x),
                    startPosition.y + ((tileSize.y + offsetTile.y) * y)),
                    tilePrefab.transform.rotation, transform).GetComponent<TileControllerScript>();
                tiles[x, y] = newTile;
                newTile.ChangeId(Random.Range(0, tileTypes.Count), x, y);
            }
        }

        //Jika ada yang match maka fungsi ini diulangi
        if(GetAllMatches().Count > 0)
        {
            foreach(TileControllerScript item in tiles) Destroy(item.gameObject);
            CreateBoard(tileSize);
        }

        //jika sudah tidak ada yang match maka kita cek posible move
        if (!HavePosibleMove()) 
        {
            StartCoroutine(countDountNoMove());
        }
    }

    public bool IsAnimating
    {
        get
        {
            return IsProcessing || IsSwapping;
        }
    }

    public bool IsProcessing { get; set; }

    public void Process()
    {
        combo = 0;
        IsProcessing = true;
        ProcessMatches();
    }

    #region Match
    private void ProcessMatches()
    {
        List<TileControllerScript> matchingTiles = GetAllMatches();
        
        combo++;
        ScoreManagerScript.Instance.IncrementCurrentScore(matchingTiles.Count, combo);


        // stop locking if no match found
        if (matchingTiles == null || matchingTiles.Count == 0)
        {
            //Mengecek Gerakan Yang Posible
            if (!HavePosibleMove()) {
                StartCoroutine(countDountNoMove());
            }
            
            IsProcessing = false;
            return;
        }

        StartCoroutine(ClearMatches(matchingTiles, ProcessDrop));
    }

    private IEnumerator ClearMatches(List<TileControllerScript> matchingTiles, System.Action onCompleted)
    {
        List<bool> isCompleted = new List<bool>();

        for (int i = 0; i < matchingTiles.Count; i++)
        {
            isCompleted.Add(false);
        }

        for (int i = 0; i < matchingTiles.Count; i++)
        {
            int index = i;
            StartCoroutine(matchingTiles[i].SetDestroyed(() => { isCompleted[index] = true; }));
        }

        yield return new WaitUntil(() => { return IsAllTrue(isCompleted); });

        onCompleted?.Invoke();
    }
    #endregion

    public bool IsAllTrue(List<bool> list)
    {
        foreach (bool status in list)
        {
            if (!status) return false;
        }

        return true;
    }

    public bool IsSwapping { get; set; }

    #region Swapping
    public IEnumerator SwapTilePosition(TileControllerScript a, TileControllerScript b, System.Action onCompleted)
    {
        IsSwapping = true;

        Vector2Int indexA = GetTileIndex(a);
        Vector2Int indexB = GetTileIndex(b);

        tiles[indexA.x, indexA.y] = b;
        tiles[indexB.x, indexB.y] = a;

        a.ChangeId(a.id, indexB.x, indexB.y);
        b.ChangeId(b.id, indexA.x, indexA.y);

        bool isRoutineACompleted = false;
        bool isRoutineBCompleted = false;

        StartCoroutine(a.MoveTilePosition(GetIndexPosition(indexB), () => { isRoutineACompleted = true; }));
        StartCoroutine(b.MoveTilePosition(GetIndexPosition(indexA), () => { isRoutineBCompleted = true; }));

        yield return new WaitUntil(() => { return isRoutineACompleted && isRoutineBCompleted; });

        onCompleted?.Invoke();

        IsSwapping = false;
    }
    #endregion

    public Vector2Int GetTileIndex(TileControllerScript tile)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                if (tile == tiles[x, y]) return new Vector2Int(x, y);
            }
        }

        return new Vector2Int(-1, -1);
    }

    public Vector2 GetIndexPosition(Vector2Int index)
    {
        Vector2 tileSize = tilePrefab.GetComponent<SpriteRenderer>().size;
        return new Vector2(startPosition.x + ((tileSize.x + offsetTile.x) * index.x), startPosition.y + ((tileSize.y + offsetTile.y) * index.y));
    }

    public List<TileControllerScript> GetAllMatches()
    {
        List<TileControllerScript> matchingTiles = new List<TileControllerScript>();

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                List<TileControllerScript> tileMatched = tiles[x, y].GetAllMatches();

                // just go to next tile if no match
                if (tileMatched == null || tileMatched.Count == 0)
                {
                    continue;
                }

                foreach (TileControllerScript item in tileMatched)
                {
                    // add only the one that is not added yet
                    if (!matchingTiles.Contains(item))
                    {
                        matchingTiles.Add(item);
                    }
                }
            }
        }

        return matchingTiles;
    }


    #region Drop
    private void ProcessDrop()
    {
        Dictionary<TileControllerScript, int> droppingTiles = GetAllDrop();
        StartCoroutine(DropTiles(droppingTiles, ProcessDestroyAndFill));
    }

    private Dictionary<TileControllerScript, int> GetAllDrop()
    {
        Dictionary<TileControllerScript, int> droppingTiles = new Dictionary<TileControllerScript, int>();

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                if (tiles[x, y].IsDestroyed)
                {
                    // process for all tile on top of destroyed tile
                    for (int i = y + 1; i < size.y; i++)
                    {
                        if (tiles[x, i].IsDestroyed)
                        {
                            continue;
                        }

                        // if this tile already on drop list, increase its drop range
                        if (droppingTiles.ContainsKey(tiles[x, i]))
                        {
                            droppingTiles[tiles[x, i]]++;
                        }
                        // if not on drop list, add it with drop range one
                        else
                        {
                            droppingTiles.Add(tiles[x, i], 1);
                        }
                    }
                }
            }
        }

        return droppingTiles;
    }

    private IEnumerator DropTiles(Dictionary<TileControllerScript, int> droppingTiles, System.Action onCompleted)
    {
        foreach (KeyValuePair<TileControllerScript, int> pair in droppingTiles)
        {
            Vector2Int tileIndex = GetTileIndex(pair.Key);

            TileControllerScript temp = pair.Key;
            tiles[tileIndex.x, tileIndex.y] = tiles[tileIndex.x, tileIndex.y - pair.Value];
            tiles[tileIndex.x, tileIndex.y - pair.Value] = temp;

            temp.ChangeId(temp.id, tileIndex.x, tileIndex.y - pair.Value);
        }

        yield return null;

        onCompleted?.Invoke();
    }
    #endregion

    #region Destroy & Fill
    private void ProcessDestroyAndFill()
    {
        List<TileControllerScript> destroyedTiles = GetAllDestroyed();
        StartCoroutine(DestroyAndFillTiles(destroyedTiles, ProcessReposition));
    }

    private List<TileControllerScript> GetAllDestroyed()
    {
        List<TileControllerScript> destroyedTiles = new List<TileControllerScript>();

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                if (tiles[x, y].IsDestroyed)
                {
                    destroyedTiles.Add(tiles[x, y]);
                }
            }
        }

        return destroyedTiles;
    }

    private IEnumerator DestroyAndFillTiles(List<TileControllerScript> destroyedTiles, System.Action onCompleted)
    {
        List<int> highestIndex = new List<int>();

        for (int i = 0; i < size.x; i++)
        {
            highestIndex.Add(size.y - 1);
        }

        float spawnHeight = endPosition.y + tilePrefab.GetComponent<SpriteRenderer>().size.y + offsetTile.y;

        foreach (TileControllerScript tile in destroyedTiles)
        {
            Vector2Int tileIndex = GetTileIndex(tile);
            Vector2Int targetIndex = new Vector2Int(tileIndex.x, highestIndex[tileIndex.x]);
            highestIndex[tileIndex.x]--;

            tile.transform.position = new Vector2(tile.transform.position.x, spawnHeight);
            tile.GenerateRandomTile(targetIndex.x, targetIndex.y);
        }

        yield return null;

        onCompleted?.Invoke();
    }
    #endregion

    #region Reposition
    private void ProcessReposition()
    {
        StartCoroutine(RepositionTiles(ProcessMatches));
    }

    private IEnumerator RepositionTiles(System.Action onCompleted)
    {
        List<bool> isCompleted = new List<bool>();

        int i = 0;
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2 targetPosition = GetIndexPosition(new Vector2Int(x, y));

                // skip if already on position
                if ((Vector2)tiles[x, y].transform.position == targetPosition)
                {
                    continue;
                }

                isCompleted.Add(false);

                int index = i;
                StartCoroutine(tiles[x, y].MoveTilePosition(targetPosition, () => { isCompleted[index] = true; }));

                i++;
            }
        }

        yield return new WaitUntil(() => { return IsAllTrue(isCompleted); });

        onCompleted?.Invoke();
    }
    #endregion

    private bool HavePosibleMove()
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                int curentId = tiles[x, y].id;

                //mengencek tetangga horizontal
                if (x + 1 < size.x)
                {
                    //jika tetangga disebelahnya sama
                    if (curentId == tiles[x + 1, y].id)
                    {
                        //mengecek terlebih dahulu index tersebut ada (tidak melewati batas max atau min)
                        //Kemudian cek idnya
                        
                        if (x + 3 < size.x) 
                            if (tiles[x + 3, y].id == curentId) return true;

                        if (x + 2 < size.x && y + 1 < size.y) 
                            if (tiles[x + 2, y + 1].id == curentId) return true;

                        if (x + 2 < size.x && y - 1 > 0) 
                            if (tiles[x + 2, y - 1].id == curentId) return true;

                        //Mengecek tetangga sebelumnya
                        if (x - 1 > 0)
                        {
                            if (y - 1 > 0)
                                if (tiles[x - 1, y - 1].id == curentId) return true;

                            if (y + 1 < size.y)
                                if (tiles[x - 1, y + 1].id == curentId) return true;
                        }
                    }
                    else if (x + 2 < size.x)
                    {
                        //jika tetangga sebelah tidak memiliki id yang sama,
                        //namun tetangga ke-2 memiliki id yang sama
                        if (curentId == tiles[x + 2, y].id)
                        {
                            if (y + 1 < size.y) 
                                if (tiles[x + 1, y + 1].id == curentId) return true;

                            if (y - 1 > 0) 
                                if (tiles[x + 1, y - 1].id == curentId) return true;

                            if (x + 3 < size.x) 
                                if (tiles[x + 3, y].id == curentId) return true;
                        }
                    }
                }

                //mengencek tetangga vecrtical
                if (y + 1 < size.y)
                {
                    //jika tetangga disebelahnya sama
                    if (curentId == tiles[x, y + 1].id)
                    {
                        //mengecek terlebih dahulu index tersebut ada (tidak melewati batas max atau min)
                        //Kemudian cek idnya

                        if (y + 3 < size.y)
                            if (tiles[x, y + 3].id == curentId) return true;

                        if (y + 2 < size.y && x + 1 < size.x)
                            if (tiles[x + 1, y + 2].id == curentId) return true;

                        if (y + 2 < size.y && x - 1 > 0)
                            if (tiles[x - 1, y + 2].id == curentId) return true;

                        //Mengecek tetangga sebelumnya
                        if (y - 1 > 0)
                        {
                            if (x - 1 > 0)
                                if (tiles[x - 1, y - 1].id == curentId) return true;

                            if (x + 1 < size.x)
                                if (tiles[x + 1, y - 1].id == curentId) return true;
                        }
                    }
                    else if (y + 2 < size.y)
                    {
                        //jika tetangga sebelah tidak memiliki id yang sama,
                        //namun tetangga ke-2 memiliki id yang sama
                        if (curentId == tiles[x, y + 2].id)
                        {
                            if (x + 1 < size.x) 
                                if (tiles[x + 1, y + 1].id == curentId) return true;

                            if (x - 1 > 0) 
                                if (tiles[x - 1, y + 1].id == curentId) return true;

                            if (y + 3 < size.y)
                                if (tiles[x, y + 3].id == curentId) return true;
                        }
                    }
                }
            }
        }
        Debug.Log("Xcute");

        return false;
    }

    private IEnumerator countDountNoMove()
    {
        yield return new WaitForSeconds(3);
        foreach (TileControllerScript item in tiles) Destroy(item.gameObject);
        CreateBoard(tileSize);
    }
}
