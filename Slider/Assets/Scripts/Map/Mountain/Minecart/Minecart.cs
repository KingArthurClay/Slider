using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Minecart : Item
{
    [Header("Movement")]
    [SerializeField] private int currentDirection;
    public RailManager railManager;
    [SerializeField] private bool isOnTrack;
    [SerializeField] public bool isMoving {get; private set;} = false;
    private bool canStartMoving = true;
    [SerializeField] public RailTile currentTile;
    [SerializeField] public RailTile targetTile;
    [SerializeField] private float speed = 2.0f; 
    public Vector3 offSet = new Vector3(0.5f, 0.75f, 0.0f);
    [SerializeField] private RailManager borderRM;
    public STile currentSTile;
    public MinecartState mcState;

    [SerializeField] private bool dropOnNextMove = false;
    [SerializeField] private RailManager savedRM = null;

    public Vector3Int currentTilePos;
    public Vector3Int targetTilePos; 
    public Vector3 targetWorldPos;

    [Header("Player")]
    public Transform playerPos;

    [Header("Animation")]
    [SerializeField] private float derailDuration;
    [SerializeField] private AnimationCurve xDerailMotion;
    [SerializeField] private AnimationCurve yDerailMotion;
    [SerializeField] private Animator minecartAnimator;

    [Header("UI")]
    public Sprite trackerSprite;


    private List<GameObject> collidingObjects = new List<GameObject>();
    private bool collisionPause = false;
    

    public void setCanStartMoving(bool canStart)
    {
        canStartMoving = canStart;
    }

    private void Awake() 
    {
        RailManager[] rms = FindObjectsOfType<RailManager>();
        foreach (RailManager r in rms) {
            if(r.isBorderRM)
                borderRM = r;
        }
        UITrackerManager.AddNewTracker(gameObject, trackerSprite);
        minecartAnimator ??= GetComponent<Animator>();
    }

    private void OnEnable()
    {
        SGridAnimator.OnSTileMoveStart += OnMoveStart;
        SGridAnimator.OnSTileMoveEnd += OnMoveEnd;
    }

    private void OnDisable()
    {
        SGridAnimator.OnSTileMoveStart -= OnMoveStart;
        SGridAnimator.OnSTileMoveEnd -= OnMoveEnd;
    }

    private void OnMoveStart(object sender, SGridAnimator.OnTileMoveArgs e)
    {
        if(currentSTile == null || e.stile == null)
            return;
        if(e.stile == currentSTile)
        {
            if(isMoving)
                Derail();
            else
                canStartMoving = false;
        }
    }

    private void OnMoveEnd(object sender, SGridAnimator.OnTileMoveArgs tileMoveArgs)
    {
        if(tileMoveArgs.stile = currentSTile)
            canStartMoving = true;
        //recalculate target position
        if(mcState == MinecartState.Crystal)
            mcState = MinecartState.Empty;
    }

    #region Item

    public override void PickUpItem(Transform pickLocation, System.Action callback = null)
    {
        base.PickUpItem(pickLocation, callback);
        UITrackerManager.RemoveTracker(this.gameObject);
        if(mcState == MinecartState.Crystal || mcState == MinecartState.Lava)
            mcState = MinecartState.Empty;
    }


    public override STile DropItem(Vector3 dropLocation, System.Action callback=null) 
    {
        UITrackerManager.AddNewTracker(this.gameObject, trackerSprite);
        
        STile hitTile = SGrid.GetStileUnderneath(gameObject);

        if(hitTile != null) //Use Stile RM
        {
            Tilemap railmap = hitTile.allTileMaps.GetComponentInChildren<STileTilemap>().minecartRails;
            railManager = railmap.GetComponent<RailManager>();
            if(railManager.railLocations.Contains(railmap.WorldToCell(dropLocation))) //C: If this is dropped onto rails, it will snap into position (change to station only later?)
            {
                StartCoroutine(AnimateDrop(railmap.CellToWorld(railmap.WorldToCell(dropLocation)) + offSet, callback));
                SnapToRail(railmap.WorldToCell(dropLocation));
            }
            else
                StartCoroutine(AnimateDrop(dropLocation, callback));
            
            UpdateParent();
            currentSTile = hitTile;
            return hitTile;
        }
        else if(borderRM) //use border RM
        {
            railManager = borderRM;
            Tilemap railmap = borderRM.railMap;

            if(railManager.railLocations.Contains(railmap.WorldToCell(dropLocation))) //C: If this is dropped onto rails, it will snap into position (change to station only later?)
            {
                StartCoroutine(AnimateDrop(railmap.CellToWorld(railmap.WorldToCell(dropLocation)) + offSet, callback));
                SnapToRail(railmap.WorldToCell(dropLocation));
            }
            else
                StartCoroutine(AnimateDrop(dropLocation, callback));
            UpdateParentBorder();
            currentSTile = null;
        }
        else
            base.DropItem(dropLocation, callback);
        return null;

    }

    #endregion

    public override void OnEquip()
    {
        StopMoving();
        ResetTiles();
        currentSTile = null;
    }

    private void OnCollisionEnter2D(Collision2D other) 
    {
        if(other.gameObject.CompareTag("Player"))
            collisionPause = true;
        else if ((dropOnNextMove && !other.gameObject.tag.Equals("WorldMapCollider")) 
                || (other.gameObject.layer == 14 && !dropOnNextMove) //C: Layer 14 is MinecartIgnore
                || other.gameObject.tag.Equals("MinecartIgnore"))
            return;
        else
            StopMoving();
    }

    private void OnCollisionExit2D(Collision2D other) 
    {
        if(other.gameObject.CompareTag("Player"))
            collisionPause = false;
    }



    #region movement

    public void StartMoving() 
    {
        if(isOnTrack && canStartMoving && !collisionPause)
        {
            isMoving = true; 
            minecartAnimator.SetBool("isMoving", true);
        } 
    }

    public void StopMoving(bool onTrack = false)
    {
        isMoving = false;
        if(!onTrack)
            isOnTrack = false;
        minecartAnimator.SetBool("isMoving", false);
        collisionPause = false;
        collidingObjects.Clear();
    }

    public void ResetTiles()
    {
        currentTile = null;
        targetTile = null;
        currentTilePos = Vector3Int.zero;
        targetTilePos = Vector3Int.zero;
    }

    //Places the minecart on the tile at the given position
    public void SnapToRail(Vector3Int pos, int direction = -1)
    {
        transform.position = railManager.railMap.layoutGrid.CellToWorld(pos) + offSet;
        currentTile = railManager.railMap.GetTile(pos) as RailTile;
        currentTilePos = pos;
        currentDirection = direction == -1? currentTile.defaultDir: direction;
        if(railManager.railLocations.Contains(pos))
        {
            targetTilePos = currentTilePos + GetTileOffsetVector(currentDirection);
            targetTile = railManager.railMap.GetTile(targetTilePos) as RailTile;
            targetWorldPos = railManager.railMap.layoutGrid.CellToWorld(targetTilePos) + offSet;
            isOnTrack = true;
        }
        else
            ResetTiles();
    }

    public void SnapToRail(Vector3 pos, int dir = -1)
    {
        railManager = borderRM;
        Vector3Int newPos = railManager.railMap.WorldToCell(pos);
        SnapToRail(newPos, dir);
    }

    

    private void Update() 
    {
        if(Time.timeScale == 0) return;
        if(isMoving && isOnTrack && !collisionPause)
        {
            if(Vector3.Distance(transform.position, targetWorldPos) < 0.01f)
                GetNextTile();
            else
                transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, Time.deltaTime * speed);
        }
        minecartAnimator.SetInteger("State", ((int)mcState));
    }

    private void GetNextTile()
    {
        if(dropOnNextMove)
        {
            transform.position += (new Vector3Int(0,-1 * MountainGrid.Instance.layerOffset, 0));
            if(savedRM != null)
            {
                Vector3Int targetLoc = savedRM.railMap.layoutGrid.WorldToCell(railManager.railMap.layoutGrid.CellToWorld(targetTilePos));
                railManager = savedRM;
                SnapToRailNewSTile(targetLoc + new Vector3Int(0,-1 * MountainGrid.Instance.layerOffset, 0)); 
                UpdateParent();
                savedRM = null;
            }
            else
            {
                Derail();
                UpdateParent(SGrid.GetSTileUnderneath(transform, null));
            }
            dropOnNextMove = false;
            savedRM = null;
            return;
        }
        currentTile = targetTile;
        currentTilePos = targetTilePos;
        targetTilePos = currentTilePos + GetTileOffsetVector(currentDirection);
        if(railManager.railLocations.Contains(targetTilePos))
        {
            targetTile = railManager.railMap.GetTile(targetTilePos) as RailTile;
            int targetConnection = targetTile.connections[(currentDirection + 2) % 4];
            if(targetConnection == -1) //this is a broken track, derail
            {
                Derail();
                return;
            }    

            targetWorldPos = railManager.railMap.layoutGrid.CellToWorld(targetTilePos) + offSet;
            currentDirection = targetTile.connections[(currentDirection + 2) % 4];
        }
        else
        {
            LookForRailManager();
        }
        
    }

    /*C: looks for a rail manager that has a tile which overlaps with the target position
     * If one is found, rail manager is updated so the minecart can operate on the new STile
     * If one is not found, the minecart derails
     */
    private void LookForRailManager()
    {
        List<STile> stileList = SGrid.Current.GetActiveTiles();
        List<RailManager> rmList = new List<RailManager>();
        Vector3Int targetLoc;
        foreach(STile tile in stileList)
        {
            RailManager[] otherRMs = tile.allTileMaps.GetComponentsInChildren<RailManager>();
            foreach(RailManager rm in otherRMs)
                if(rm != null && rm != railManager)
                    rmList.Add(rm);
        }
        
        foreach(RailManager rm in rmList) //look and see if the next location overlaps with a location of a rail on another STile
        {
            targetLoc = rm.railMap.layoutGrid.WorldToCell(railManager.railMap.layoutGrid.CellToWorld(targetTilePos));
            if(rm.railLocations.Contains(targetLoc))
            {
                railManager = rm;
                SnapToRailNewSTile(targetLoc);
                UpdateParent();
                return;
            }
        }
        foreach(RailManager rm in rmList) //check dropping down onto tile
        {
            targetLoc = rm.railMap.layoutGrid.WorldToCell(railManager.railMap.layoutGrid.CellToWorld(targetTilePos));
            //C: Check if the minecart can drop down to the tile below. Needs to be in seperate loop so will check after trying all non-drops first
            if(MountainGrid.Instance && rm.railLocations.Contains(targetLoc + new Vector3Int(0,-1 * MountainGrid.Instance.layerOffset, 0)))
            {
                dropOnNextMove = true;
                savedRM = rm;
                targetWorldPos += getDirectionAsVector(currentDirection);
                return;
            }
        }
        
        if(borderRM) //check border
        {
            targetLoc = borderRM.railMap.layoutGrid.WorldToCell(railManager.railMap.layoutGrid.CellToWorld(targetTilePos));
            if(borderRM.railLocations.Contains(targetLoc)) //look and see if the next location overlaps with a location of a rail on the outside
            {
                railManager = borderRM;
                SnapToRailNewSTile(targetLoc);
                UpdateParentBorder();
                return;
            }
        }

        if(transform.position.y > 93 && transform.position.y < 124
            && transform.position.x > -7 && transform.position.x < 24) //check dropping down onto world
        {
            bool shouldDrop = true;
            targetWorldPos += getDirectionAsVector(currentDirection);
          //  GameObject temp = gameObject;
            GameObject temp = new GameObject();
            if(SGrid.GetStileUnderneath(temp) != null) //don't drop if there is an adj tile
                shouldDrop = false;
            temp.transform.position = targetWorldPos +  new Vector3Int(0,-1 * MountainGrid.Instance.layerOffset, 0);
            if(SGrid.GetStileUnderneath(temp) == null) //don't drop unless there is a tile to drop down onto
                shouldDrop = false;
            dropOnNextMove = shouldDrop;
            Destroy(temp);
            return;
        }
        StopMoving();
    }

    #endregion

    //Used to snap to a rail tile when moving across STiles
    public void SnapToRailNewSTile(Vector3Int pos)
    {
        currentTile = railManager.railMap.GetTile(pos) as RailTile;
        currentTilePos = pos;
        targetTilePos = currentTilePos + GetTileOffsetVector(currentDirection);
        targetTile = railManager.railMap.GetTile(targetTilePos) as RailTile;
        targetWorldPos = railManager.railMap.layoutGrid.CellToWorld(targetTilePos) + offSet; 
    }

    //makes the minecart fall off of the rails
    public void Derail()
    {
        StopMoving();
        ResetTiles();
        Vector3 derailVector = transform.position 
                               + speed * 0.3f * getDirectionAsVector(currentDirection)   
                               + 0.5f * (new Vector3(Random.onUnitSphere.x, Random.onUnitSphere.y, 0));
       // StartCoroutine(AnimateDerail(derailVector));
    }

    



    protected IEnumerator AnimateDerail(Vector3 target, System.Action callback = null)
    {
        float t = derailDuration;

        Vector3 start = new Vector3(transform.position.x, transform.position.y);
        while (t >= 0)
        {
            float x = xDerailMotion.Evaluate(t / derailDuration);
            float y = yDerailMotion.Evaluate(t / derailDuration);
            Vector3 pos = new Vector3(Mathf.Lerp(target.x, start.x, x),
                                      Mathf.Lerp(target.y, start.y, y));
            
            base.spriteRenderer.transform.position = pos;
            yield return null;
            t -= Time.deltaTime;
        }

        transform.position = target;
        base.spriteRenderer.transform.position = target;
        callback();

    }


    #region Utility

    private void UpdateParent()
    {
        gameObject.transform.parent = railManager.gameObject.GetComponentInParent<STile>().transform.Find("Objects").transform;
        currentSTile = railManager.gameObject.GetComponentInParent<STile>();
    }

    private void UpdateParentBorder()
    {
        gameObject.transform.parent = borderRM.gameObject.GetComponentInParent<STileTilemap>().transform.Find("Objects").transform;
        currentSTile = null;
    }

    private void UpdateParent(STile sTile)
    {
        gameObject.transform.parent = sTile.transform.Find("Objects").transform;
        currentSTile = sTile;
    }

    //C: returns a vector that can be added to the tile position in order to determine the location of the specified point
    public static Vector3Int GetTileOffsetVector(int num)
    {
        int[] arr = {1, 0, -1, 0};
        return new Vector3Int(arr[num], arr[(num+3) % 4], 0);
    }

    private static Vector3 getDirectionAsVector(int dir)
    {
        Vector3[] arr = {Vector3.right, Vector3.up, Vector3.left, Vector3.down};
        return arr[dir];
    }

    public void UpdateState(string stateName){
        if(stateName.Equals("Player"))
            mcState = MinecartState.Player;
        else if(stateName.Equals("Lava"))
            mcState = MinecartState.Lava;
        else if(stateName.Equals("Crystal"))
            mcState = MinecartState.Crystal;
        else if (stateName.Equals("Empty"))
            mcState = MinecartState.Empty;
        else if (stateName.Equals("RepairParts"))
            mcState = MinecartState.RepairParts;
        else
            Debug.LogWarning("Invalid Minecart State. Should be Player, Lava, Empty, RepairParts, or Crystal");
    }

    public void TryAddCrystals()
    {
        if(mcState == MinecartState.Empty)
            mcState = MinecartState.Crystal;
    }

    #endregion

    public void CheckIsEmpty(Condition c){
        c.SetSpec(mcState == MinecartState.Empty);
    }

    public void CheckCrystals(Condition c){
        c.SetSpec(mcState == MinecartState.Crystal);
    }

    public void CheckIsMoving(Condition c){
        c.SetSpec(isMoving);
    }

    public void CheckIsNotMoving(Condition c){
        c.SetSpec(!isMoving);
    }
}
