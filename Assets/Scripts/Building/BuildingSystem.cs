using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BuildingSystem : MonoBehaviour
{
    public BuildGrid worldGrid;
    [SerializeField] Transform objectsContainer;
    private BuildObject currentBuildObject;
    [SerializeField] private GameObject buildTemplate;
    [SerializeField] private GameObject backBuildTemplate;
    public BuildCatalog buildCatalog;
    [SerializeField] private GameObject placeholderPrefab;
    private GameObject placeholder;
    [SerializeField] private GameObject destroyParticlesPrefab;

    float holdToDestroyTime = 0.2f;

    MenuManager menuManager;
    BuildingUI buildUI;
    public static BuildingSystem Instance;

    private void Awake()
    {
        Instance = this;
        menuManager = MenuManager.Instance;
        buildUI = GetComponent<BuildingUI>();
    }
    private void Start()
    {
        if (SceneManager.GetActiveScene().name == "VirtualHangar")
        {
            worldGrid = new BuildGrid(new Vector2Int(-10, -10), 20, 20);
        }
        else
        {
            worldGrid = new BuildGrid(new Vector2Int(-100, -1));
        }
        buildUI.SetCatalog(buildCatalog);

        if (objectsContainer == null)
        {
            Debug.Log("Building system error: Please assign the objects container");
        }
        placeholder = Instantiate(placeholderPrefab);

        currentBuildObject = new BuildObject(null);
    }

    public void StartBuilding()
    {
        menuManager.ShowMenu(menuManager.buildMenu);
    }

    public Build SetBuildObject(int index)
    {
        Build newBuild = buildCatalog.GetBuild(index);
        currentBuildObject.build = newBuild;
        currentBuildObject.rot = 0;
        UpdatePlaceholder();

        return newBuild;
    }
    public Category SetCategory(int index)
    {
        return buildCatalog.SetCategory(index);
    }

    void PlaceObject(Vector3 worldPos, BuildGrid thisGrid, Transform parent)
    {
        if (!thisGrid.PositionIsWithinGrid(worldPos) || thisGrid.GetValueAtPosition(worldPos) != null) return;

        GameObject obj = PlaceBlock(worldPos, currentBuildObject, parent);
        // Save object placement in the grid
        BuildObject buildObjectCopy = currentBuildObject.Clone();
        buildObjectCopy.gridObject = obj;
        thisGrid.SetValueAtPosition(worldPos, buildObjectCopy);
    }
    GameObject PlaceBlock(Vector3 worldPos, BuildObject thisBuildObject, Transform parent)
    {
        // Determine the object template to use
        Build thisBuild = thisBuildObject.build;
        Rotation thisRotation = thisBuildObject.GetRotation();
        GameObject clone = thisRotation.Object;
        if (clone == null)
        {
            if (thisBuild.depth == Build.DepthLevel.MidGround)
                clone = buildTemplate;
            else
                clone = backBuildTemplate;
        }

        // Place the object in world
        GameObject obj = Instantiate(clone, Vector2.zero, Quaternion.identity, parent);
        obj.name = thisBuild.name;
        obj.transform.position = worldPos;
        obj.transform.localRotation = Quaternion.Euler(0, 0, thisRotation.DegRotation);

        if (thisRotation.sprite != null)
        {
            obj.GetComponent<SpriteRenderer>().sprite = thisRotation.sprite;
        }
        if (thisBuild.depth == Build.DepthLevel.MidGround)
        {
            PolygonCollider2D collider = obj.AddComponent<PolygonCollider2D>();
            collider.usedByComposite = true;
        }
        if (thisRotation.flipX)
        {
            obj.transform.localScale = new Vector3(-1, obj.transform.localScale.y, 1);
        }
        if (thisRotation.flipY)
        {
            obj.transform.localScale = new Vector3(obj.transform.localScale.x, -1, 1);
        }
        return obj;
    }
    void DeleteObject(Vector3 worldPos, BuildGrid thisGrid)
    {
        BuildObject buildObj = thisGrid.GetValueAtPosition(worldPos);
        if (!thisGrid.PositionIsWithinGrid(worldPos) || !thisGrid.RemoveValueAtPosition(worldPos)) return;

        // Delete object from world
        Destroy(buildObj.gridObject);

        // Particles
        Instantiate(destroyParticlesPrefab, worldPos, Quaternion.identity);
    }
    void RotateObject()
    {
        currentBuildObject.AdvanceRotation();
        UpdatePlaceholder();
    }
    public void SpawnObjects(BuildGrid thisGrid, Transform parent)
    {
        foreach (KeyValuePair<Vector2Int, BuildObject> p in thisGrid.gridObjects)
        {
            GameObject obj = PlaceBlock(thisGrid.GridtoWorldAligned(p.Key), p.Value, parent);
            p.Value.gridObject = obj;
        }
    }
    public void ShiftObjects(BuildGrid thisGrid, Vector3 offset)
    {
        foreach (KeyValuePair<Vector2Int, BuildObject> p in thisGrid.gridObjects)
        {
            p.Value.gridObject.transform.localPosition += offset;
        }
    }
    void UpdatePlaceholder()
    {
        Rotation thisRotation = currentBuildObject.GetRotation();
        SpriteRenderer renderer = placeholder.GetComponent<SpriteRenderer>();
        renderer.sprite = thisRotation.sprite;
        renderer.flipX = thisRotation.flipX;
        renderer.flipY = thisRotation.flipY;
    }

    IEnumerator deleteTimer;
    bool isPlacing;
    bool isDeleting;

    IEnumerator DeleteDelay()
    {
        yield return new WaitForSeconds(holdToDestroyTime);
        isDeleting = true;
    }
    void InterruptDeleteTimer()
    {
        if (deleteTimer != null)
            StopCoroutine(deleteTimer);
    }
    private void Update()
    {
        if (objectsContainer == null) return;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        BuildGrid selectedGrid = worldGrid;
        Transform selectedParent = objectsContainer;

        Ship thisShip = null;
        ///This searches for ships that might be where the cursor is located, allowing the player to build on them instead.
        ///This should be reworked to let the play place on the closest ship to the mouse, just in case too many ships/builds are competing for player placement attention
        foreach (Ship ship in ShipBuilding.loadedShips)
        {
            BuildGrid shipGrid = ship.ship;
            if (shipGrid.PositionIsWithinGrid(mousePos))
            {
                thisShip = ship;
                selectedParent = ship.transform;
                selectedGrid = ship.ship;
                break;
            }
        }
        Vector3 alignedPos = selectedGrid.WorldtoAligned(mousePos);
        bool canPlace = currentBuildObject.build != null && selectedGrid.GetValueAtPosition(alignedPos) == null;

        if (Input.GetMouseButtonDown(0))
        {
            if (canPlace)
            {
                isPlacing = true;
            }
            else
            {
                deleteTimer = DeleteDelay();
                StartCoroutine(deleteTimer);
            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            isPlacing = isDeleting = false;
            InterruptDeleteTimer();
        }

        if (canPlace && !isDeleting)
        {
            placeholder.transform.position = alignedPos;
            Rotation thisRotation = currentBuildObject.GetRotation();
            placeholder.transform.rotation = Quaternion.Euler(0, 0, thisRotation.DegRotation + selectedGrid.rotation);
            placeholder.SetActive(true);
        }
        else
        {
            placeholder.SetActive(false);
        }

        if (!menuManager.IsOnUI())
        {
            if (isPlacing)
            {
                PlaceObject(alignedPos, selectedGrid, selectedParent);
                if (thisShip != null && selectedGrid.PositionIsAtEdge(alignedPos))
                    thisShip.UpdateShip();
            }
            else if (isDeleting)
            {
                DeleteObject(alignedPos, selectedGrid);
                if (thisShip != null && selectedGrid.PositionIsAtEdge(alignedPos))
                    thisShip.UpdateShip();
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            RotateObject();
        }
    }
}
