using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Runtime gameplay bootstrapper for Level 0 — Bahay Kubo sa Daan (Tutorial).
/// Sets up the player, UI, NPC, trigger zones, and game systems.
/// Does NOT generate any environment — use your own scene environment.
/// Attach to an empty GameObject in the scene.
/// </summary>
public class TutorialSceneSetup : MonoBehaviour
{
    [Header("NPC Position")]
    [Tooltip("Where Lola Coring (NPC) will be placed in your scene")]
    public Vector3 npcPosition = new Vector3(0f, 1.5f, 5f);

    [Header("Trigger Zones")]
    [Tooltip("Trigger at the house entrance — walking through this starts Phase 2 (Investigation)")]
    public Vector3 salaEntryPosition = new Vector3(0f, 2f, 3f);
    public Vector3 salaEntrySize = new Vector3(3f, 3f, 1f);

    [Tooltip("Trigger at the exit — walking through this ends the level")]
    public Vector3 exitTriggerPosition = new Vector3(0f, 0.5f, -5f);
    public Vector3 exitTriggerSize = new Vector3(3f, 2f, 2f);

    [Tooltip("Interior zone — covers all indoor rooms")]
    public Vector3 interiorZoneCenter = new Vector3(0f, 2f, 6f);
    public Vector3 interiorZoneSize = new Vector3(12f, 5f, 12f);

    // NPC Material Colors
    private static readonly Color COL_FABRIC       = new Color(0.94f, 0.90f, 0.83f);
    private static readonly Color COL_WOOD_LIGHT   = new Color(0.77f, 0.64f, 0.42f);
    private static readonly Color COL_WALL_INT     = new Color(0.65f, 0.55f, 0.38f);
    private static readonly Color COL_CERAMIC      = new Color(0.72f, 0.45f, 0.25f);

    // Cached references for GamePhaseManager
    private GamePhaseManager phaseManager;
    private PlayerMovement player;
    private LolaCoring lolaNPC;
    private LedgerUI ledgerUI;
    private UIPromptManager promptManager;
    private SubtitleUI subtitleUI;
    private InvestigationChain investigationChain;
    private CollectionSequence collectionSequence;
    private Light mainLight;

    private Dictionary<string, Material> materials = new Dictionary<string, Material>();

    void Awake()
    {
        CreateNPCMaterials();
        SetupPlayer();
        SetupUI();
        SetupNPC();
        SetupTriggerZones();
        SetupGameSystems();
    }

    // ========================================
    //  NPC MATERIALS
    // ========================================

    private void CreateNPCMaterials()
    {
        materials["fabric"]    = CreateMat("Fabric", COL_FABRIC);
        materials["wood_light"] = CreateMat("WoodLight", COL_WOOD_LIGHT);
        materials["wall_int"]  = CreateMat("WallInterior", COL_WALL_INT);
        materials["ceramic"]   = CreateMat("Ceramic", COL_CERAMIC);
    }

    private Material CreateMat(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.name = name;
        mat.color = color;
        return mat;
    }

    // ========================================
    //  PLAYER SETUP
    // ========================================

    private void SetupPlayer()
    {
        // Find existing player capsule
        GameObject existingPlayer = null;
        CharacterController cc = FindAnyObjectByType<CharacterController>();
        if (cc != null)
            existingPlayer = cc.gameObject;
        else
        {
            // Look for Capsule object
            GameObject capsule = GameObject.Find("Capsule");
            if (capsule == null) capsule = GameObject.Find("Player");
            existingPlayer = capsule;
        }

        if (existingPlayer != null)
        {
            // Don't move the player — keep their scene position

            // Ensure it has PlayerMovement
            player = existingPlayer.GetComponent<PlayerMovement>();
            if (player == null)
                player = existingPlayer.AddComponent<PlayerMovement>();

            // Ensure it has CharacterController
            if (existingPlayer.GetComponent<CharacterController>() == null)
                existingPlayer.AddComponent<CharacterController>();

            // Ensure camera exists as child
            Camera cam = existingPlayer.GetComponentInChildren<Camera>();
            if (cam == null)
            {
                GameObject camObj = new GameObject("PlayerCamera");
                camObj.transform.SetParent(existingPlayer.transform, false);
                camObj.transform.localPosition = new Vector3(0f, 0.6f, 0f); // Eye height
                cam = camObj.AddComponent<Camera>();
                cam.nearClipPlane = 0.1f;
                cam.fieldOfView = 65f;

                // Add audio listener
                if (FindAnyObjectByType<AudioListener>() == null)
                    camObj.AddComponent<AudioListener>();
            }
        }
        else
        {
            Debug.LogError("TutorialSceneSetup: No player capsule found in scene!");
        }
    }

    // ========================================
    //  UI SETUP
    // ========================================

    private void SetupUI()
    {
        // Create main UI Canvas
        GameObject canvasObj = new GameObject("GameCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            var es = esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.sendNavigationEvents = false;
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // Create UI systems
        ledgerUI = LedgerUI.CreateLedgerUI(canvas);
        promptManager = UIPromptManager.CreatePromptUI(canvas);

        // Crosshair (small dot at center)
        GameObject crosshair = new GameObject("Crosshair");
        crosshair.transform.SetParent(canvas.transform, false);
        RectTransform chRect = crosshair.AddComponent<RectTransform>();
        chRect.anchorMin = new Vector2(0.5f, 0.5f);
        chRect.anchorMax = new Vector2(0.5f, 0.5f);
        chRect.sizeDelta = new Vector2(4, 4);
        Image chImg = crosshair.AddComponent<Image>();
        chImg.color = new Color(1f, 1f, 1f, 0.5f);
        chImg.raycastTarget = false;

        // Fade overlay (full screen black for transitions)
        GameObject fadeObj = new GameObject("FadeOverlay");
        fadeObj.transform.SetParent(canvas.transform, false);
        RectTransform fadeRect = fadeObj.AddComponent<RectTransform>();
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.offsetMin = Vector2.zero;
        fadeRect.offsetMax = Vector2.zero;
        Image fadeImg = fadeObj.AddComponent<Image>();
        fadeImg.color = Color.black;
        fadeImg.raycastTarget = false;

        // Create Subtitles AFTER fade overlay so it renders on top of the black screen
        subtitleUI = SubtitleUI.CreateSubtitleUI(canvas);

        // Loading text (over fade overlay)
        GameObject loadTextObj = new GameObject("LoadingText");
        loadTextObj.transform.SetParent(fadeObj.transform, false);
        RectTransform loadRect = loadTextObj.AddComponent<RectTransform>();
        loadRect.anchorMin = new Vector2(0.2f, 0.4f);
        loadRect.anchorMax = new Vector2(0.8f, 0.6f);
        loadRect.offsetMin = Vector2.zero;
        loadRect.offsetMax = Vector2.zero;
        TextMeshProUGUI loadTmp = loadTextObj.AddComponent<TextMeshProUGUI>();
        loadTmp.text = "";
        loadTmp.fontSize = 28;
        loadTmp.color = new Color(0.8f, 0.75f, 0.6f);
        loadTmp.alignment = TextAlignmentOptions.Center;
        loadTextObj.SetActive(false);

        // Create Presence Sense and Debt Mark UI on player
        if (player != null)
        {
            PresenceSense.CreatePresenceSenseUI(canvas, player.gameObject);
            DebtMark.CreateDebtMarkUI(canvas, player.gameObject);
        }
    }

    // ========================================
    //  NPC SETUP
    // ========================================

    private void SetupNPC()
    {
        // Create Lola Coring at the configured position
        GameObject npcObj = new GameObject("LolaCoring");
        npcObj.transform.position = npcPosition;

        // NPC body (capsule, hunched over slightly)
        GameObject body = CreatePrimitive("NPC_Body", PrimitiveType.Capsule,
            new Vector3(0, 0.1f, -0.1f),
            new Vector3(0.35f, 0.45f, 0.35f), materials["fabric"], npcObj.transform);
        body.transform.rotation = Quaternion.Euler(15f, 0, 0);
        
        // Shawl/Blanket draped over shoulders
        CreatePrimitive("NPC_Shawl", PrimitiveType.Cylinder, new Vector3(0f, 0.35f, 0f), new Vector3(0.4f, 0.1f, 0.4f), materials["wall_int"], npcObj.transform);

        // Arms (resting on lap)
        GameObject armL = CreatePrimitive("NPC_ArmL", PrimitiveType.Capsule, new Vector3(-0.25f, 0.15f, 0.1f), new Vector3(0.12f, 0.35f, 0.12f), materials["fabric"], npcObj.transform);
        armL.transform.rotation = Quaternion.Euler(-30f, 0, -15f);
        GameObject armR = CreatePrimitive("NPC_ArmR", PrimitiveType.Capsule, new Vector3(0.25f, 0.15f, 0.1f), new Vector3(0.12f, 0.35f, 0.12f), materials["fabric"], npcObj.transform);
        armR.transform.rotation = Quaternion.Euler(-30f, 0, 15f);

        // Make the NPC collider not block the player but be detectable by raycast
        Collider bodyCol = body.GetComponent<Collider>();
        if (bodyCol != null) bodyCol.isTrigger = true;

        // NPC head (sphere, slightly down)
        GameObject head = CreatePrimitive("NPC_Head", PrimitiveType.Sphere,
            new Vector3(0f, 0.55f, 0.05f),
            new Vector3(0.25f, 0.25f, 0.25f), materials["wood_light"], npcObj.transform);
            
        // White hair (bun in back)
        CreatePrimitive("NPC_Hair", PrimitiveType.Sphere, new Vector3(0f, 0.6f, -0.05f), new Vector3(0.26f, 0.22f, 0.28f), materials["ceramic"], npcObj.transform);
        CreatePrimitive("NPC_HairBun", PrimitiveType.Sphere, new Vector3(0f, 0.55f, -0.15f), new Vector3(0.12f, 0.12f, 0.12f), materials["ceramic"], npcObj.transform);

        // Floating label
        GameObject labelObj = new GameObject("NPC_Label");
        labelObj.transform.SetParent(npcObj.transform, false);
        labelObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);

        Canvas labelCanvas = labelObj.AddComponent<Canvas>();
        labelCanvas.renderMode = RenderMode.WorldSpace;
        labelCanvas.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 50);
        labelObj.transform.localScale = Vector3.one * 0.01f;

        GameObject labelTextObj = new GameObject("LabelText");
        labelTextObj.transform.SetParent(labelObj.transform, false);
        RectTransform labelRect = labelTextObj.AddComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(200, 50);
        TextMeshProUGUI labelTmp = labelTextObj.AddComponent<TextMeshProUGUI>();
        labelTmp.text = "Lola Coring";
        labelTmp.fontSize = 24;
        labelTmp.color = new Color(0.9f, 0.85f, 0.75f, 0.8f);
        labelTmp.alignment = TextAlignmentOptions.Center;

        // Add LolaCoring component
        lolaNPC = npcObj.AddComponent<LolaCoring>();
        lolaNPC.npcRenderer = body.GetComponent<Renderer>();
        lolaNPC.npcLabel = labelObj;

        // Create soul light (for collection sequence)
        GameObject soulLight = new GameObject("SoulLight");
        soulLight.transform.position = npcObj.transform.position + Vector3.up * 0.5f;
        Light sl = soulLight.AddComponent<Light>();
        sl.type = LightType.Point;
        sl.color = new Color(1f, 0.9f, 0.7f);
        sl.intensity = 0f;
        sl.range = 5f;
        soulLight.SetActive(false);

        // Store soul light reference for CollectionSequence
        CollectionSequence cs = FindAnyObjectByType<CollectionSequence>();
        if (cs == null)
        {
            GameObject csObj = new GameObject("CollectionSequence");
            cs = csObj.AddComponent<CollectionSequence>();
        }
        collectionSequence = cs;
        cs.soulLight = soulLight;
    }

    // ========================================
    //  TRIGGER ZONES
    // ========================================

    private void SetupTriggerZones()
    {
        // Sala entry trigger (at house entrance — entering starts Investigation phase)
        CreateTriggerZone("SalaEntry", salaEntryPosition, salaEntrySize);

        // Exit trigger (at the exit — entering ends the level)
        CreateTriggerZone("StairBottom", exitTriggerPosition, exitTriggerSize);

        // Interior zone (covers all indoor rooms)
        GameObject interiorZone = CreateTriggerZone("InteriorZone", interiorZoneCenter, interiorZoneSize);
        interiorZone.AddComponent<InteriorZone>();
    }

    private GameObject CreateTriggerZone(string zoneName, Vector3 position, Vector3 size)
    {
        GameObject zoneObj = new GameObject($"TriggerZone_{zoneName}");
        zoneObj.transform.position = position;
        BoxCollider col = zoneObj.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = size;
        TriggerZone trigger = zoneObj.AddComponent<TriggerZone>();
        trigger.zoneName = zoneName;
        return zoneObj;
    }

    // ========================================
    //  GAME SYSTEMS WIRING
    // ========================================

    private void SetupGameSystems()
    {
        // Create InvestigationChain
        InvestigationChain chain = FindAnyObjectByType<InvestigationChain>();
        if (chain == null)
        {
            GameObject chainObj = new GameObject("InvestigationChain");
            chain = chainObj.AddComponent<InvestigationChain>();
        }
        investigationChain = chain;

        // Create GamePhaseManager
        GameObject phaseObj = new GameObject("GamePhaseManager");
        phaseManager = phaseObj.AddComponent<GamePhaseManager>();

        // Find the scene's main directional light
        mainLight = FindAnyObjectByType<Light>();

        // Wire references
        phaseManager.playerMovement = player;
        phaseManager.mainCamera = Camera.main;
        phaseManager.lolaCoring = lolaNPC;
        phaseManager.ledgerUI = ledgerUI;
        phaseManager.promptManager = promptManager;
        phaseManager.subtitleUI = subtitleUI;
        phaseManager.presenceSense = FindAnyObjectByType<PresenceSense>();
        phaseManager.debtMark = FindAnyObjectByType<DebtMark>();
        phaseManager.collectionSequence = collectionSequence;
        phaseManager.investigationChain = investigationChain;
        phaseManager.ambientLight = mainLight;

        // Find and assign fade overlay
        Image fadeOverlay = null;
        GameObject fadeObj = GameObject.Find("FadeOverlay");
        if (fadeObj != null) fadeOverlay = fadeObj.GetComponent<Image>();
        phaseManager.fadeOverlay = fadeOverlay;

        // Find loading text
        GameObject loadTextObj = GameObject.Find("LoadingText");
        if (loadTextObj != null)
        {
            phaseManager.loadingText = loadTextObj.GetComponent<TextMeshProUGUI>();
        }

        // Wire NPC references
        if (lolaNPC != null)
        {
            lolaNPC.playerMovement = player;
            lolaNPC.phaseManager = phaseManager;
            lolaNPC.subtitleUI = subtitleUI;
            lolaNPC.promptManager = promptManager;
            lolaNPC.collectionSequence = collectionSequence;
        }

        // Wire investigation chain
        if (investigationChain != null)
        {
            investigationChain.phaseManager = phaseManager;
            investigationChain.ledgerUI = ledgerUI;
            investigationChain.promptManager = promptManager;
            investigationChain.subtitleUI = subtitleUI;
        }

        // Setup the custom interactable cubes
        SetupInteractables();
    }

    // ========================================
    //  INTERACTABLES SETUP
    // ========================================

    private void SetupInteractables()
    {
        GameObject cube1 = GameObject.Find("InteractableObject1");
        GameObject cube2 = GameObject.Find("InteractableObject2");

        if (cube1 != null && cube2 != null && investigationChain != null)
        {
            // Cube 1 will be the Key that the player needs to find
            InteractableObject key = SetupInteractable(cube1, "Rusty Key", "A small brass key.", "");
            key.isPickup = true;
            key.pickupKeyId = "bedroom_key";
            investigationChain.keyItem = key;

            // Cube 2 will be the Locked Tin Box that holds the contract
            InteractableObject lockedBox = SetupInteractable(cube2, "Locked Tin Box", "You found the Debt Contract inside!", "Debt confirmed. Price: Life.", true, "bedroom_key", "The box is locked tight. It needs a small key.");
            investigationChain.lockedTinBox = lockedBox;
            
            // When the locked box is successfully examined (meaning it was unlocked first), trigger the contract found event
            lockedBox.OnExamined.AddListener(() => investigationChain.OnContractFound());
        }
    }

    private InteractableObject SetupInteractable(GameObject obj, string objName, string examineText,
        string ledgerAnnotation, bool requiresKey = false, string requiredKeyId = "",
        string lockedMessage = "It's locked.")
    {
        InteractableObject interactable = obj.GetComponent<InteractableObject>();
        if (interactable == null) interactable = obj.AddComponent<InteractableObject>();
        
        interactable.objectName = objName;
        interactable.examineText = examineText;
        interactable.ledgerAnnotation = ledgerAnnotation;
        interactable.requiresKey = requiresKey;
        interactable.requiredKeyId = requiredKeyId;
        interactable.lockedMessage = lockedMessage;
        interactable.isLocked = requiresKey;

        // Ensure it has a collider for raycasts
        if (obj.GetComponent<Collider>() == null)
        {
            obj.AddComponent<BoxCollider>();
        }

        // Floating label
        Transform existingLabel = obj.transform.Find("Interactable_Label");
        if (existingLabel == null)
        {
            GameObject labelObj = new GameObject("Interactable_Label");
            labelObj.transform.SetParent(obj.transform, false);
            labelObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);

            Canvas labelCanvas = labelObj.AddComponent<Canvas>();
            labelCanvas.renderMode = RenderMode.WorldSpace;
            labelCanvas.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 50);
            labelObj.transform.localScale = Vector3.one * 0.01f;

            GameObject labelTextObj = new GameObject("LabelText");
            labelTextObj.transform.SetParent(labelObj.transform, false);
            RectTransform labelRect = labelTextObj.AddComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(300, 50);
            TextMeshProUGUI labelTmp = labelTextObj.AddComponent<TextMeshProUGUI>();
            labelTmp.text = $"< {objName} >";
            labelTmp.fontSize = 20;
            labelTmp.color = new Color(0.9f, 0.85f, 0.75f, 0.6f);
            labelTmp.alignment = TextAlignmentOptions.Center;

            labelObj.AddComponent<BillboardLabel>();
        }

        return interactable;
    }

    // ========================================
    //  UTILITY: PRIMITIVE CREATION (for NPC only)
    // ========================================

    private GameObject CreatePrimitive(string name, PrimitiveType type, Vector3 position,
        Vector3 scale, Material material, Transform parent = null)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.position = position;
        obj.transform.localScale = scale;

        if (parent != null)
        {
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
        }

        Renderer rend = obj.GetComponent<Renderer>();
        if (rend != null && material != null)
        {
            rend.material = material;
        }

        return obj;
    }
}

/// <summary>
/// Simple script to make world-space UI always face the main camera.
/// </summary>
public class BillboardLabel : MonoBehaviour
{
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
    }

    void Update()
    {
        if (mainCam != null)
        {
            transform.forward = mainCam.transform.forward;
        }
    }
}

public class SimpleDoor : InteractableObject
{
    public Transform doorPivot;
    private bool isOpen = false;
    private Quaternion closedRot;
    private Quaternion openRot;
    private Coroutine animationCoroutine;

    void Start()
    {
        if (doorPivot == null) doorPivot = transform.parent;
        closedRot = doorPivot.localRotation;
        openRot = closedRot * Quaternion.Euler(0, 90f, 0);
        isInteractable = true;
    }

    public void ToggleDoor()
    {
        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        
        isOpen = !isOpen;
        animationCoroutine = StartCoroutine(AnimateDoor(isOpen ? openRot : closedRot));
        examineText = isOpen ? "An open door." : "A closed door.";
    }

    private System.Collections.IEnumerator AnimateDoor(Quaternion targetRot)
    {
        float duration = 0.4f;
        float elapsed = 0f;
        Quaternion startRot = doorPivot.localRotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            doorPivot.localRotation = Quaternion.Slerp(startRot, targetRot, elapsed / duration);
            yield return null;
        }
        doorPivot.localRotation = targetRot;
    }
}
