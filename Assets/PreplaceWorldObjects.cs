using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Niantic.Lightship.AR.WorldPositioning;
using Niantic.Lightship.AR.XRSubsystems;

// ---------------------------------------------
//  JSON data classes (matches nav_graph.json)
// ---------------------------------------------
[Serializable]
public class NavGraph
{
    public List<NavNode> nodes;
    public List<NavEdge> edges;
}

[Serializable]
public class NavNode
{
    public string id;
    public NavCoord coordinates;
    public string type;
    public List<string> poi;
}

[Serializable]
public class NavCoord
{
    public double lat;
    public double lng;
}

[Serializable]
public class NavEdge
{
    public string id;
    public string from;
    public string to;
    public float distance_m;
    public bool bidirectional;
}

// ---------------------------------------------
//  JSON data classes (matches office_mapping.json)
// ---------------------------------------------
[Serializable]
public class OfficeMapping
{
    public List<OfficeEntry> offices;
}

[Serializable]
public class OfficeEntry
{
    public string office_id;     // e.g. "off_021"
    public string display_name;  // e.g. "Canteen"
    public string node_id;       // e.g. "N33"
    public bool is_active;
}

// ---------------------------------------------
//  API response classes (matches Railway /api/offices response)
// ---------------------------------------------
[Serializable]
public class ApiOfficeResponse
{
    public bool success;
    public List<ApiOfficeEntry> data;
}

[Serializable]
public class ApiOfficeEntry
{
    public string office_id;
    public string name;      // display_name aliased as "name" in SQL query
    public string node;      // node_id aliased as "node" in SQL query
    public string status;
    public string node_type;
}

// ---------------------------------------------
//  Stores spawned nav object with its GPS coords
//  and cached renderers for distance-based fading
// ---------------------------------------------
public class SpawnedNavObject
{
    public GameObject obj;
    public double lat;
    public double lng;
    public Renderer[] renderers; // cached for performance
}

// ---------------------------------------------
//  Main Navigation Manager
// ---------------------------------------------
public class PreplaceWorldObjects : MonoBehaviour
{
    [Header("Lightship")]
    [SerializeField] private ARWorldPositioningManager _positionManager;
    [SerializeField] private ARWorldPositioningObjectHelper _objectHelper;

    [Header("Navigation Prefabs")]
    [SerializeField] private GameObject _arrowPrefab;      // Spawned at midpoint between nodes, faces next node
    [SerializeField] private GameObject _nodeMarkerPrefab;        // Spawned on each path node (except destination)
    [SerializeField] private GameObject _destinationMarkerPrefab; // Spawned only on the final destination node

    [Header("Navigation")]
    [SerializeField] private string _destinationNodeId = "N8"; // Fallback default: Technovation Building
    [SerializeField] private string _officeMappingUrl = "https://your-railway-url.up.railway.app/api/offices"; // Railway API URL

    [Header("Localization Settings")]
    [SerializeField] private float _stableSeconds = 10f;       // All conditions must stay true this long before spawning
    [SerializeField] private float _gpsAccuracyThreshold = 8f; // Max acceptable GPS horizontal accuracy in meters
    [Tooltip("Height of spawned objects relative to WPS origin. Use negative values to push toward ground (e.g. -1.5)")]
    [SerializeField] private float _arrowAltitude = -1.5f; // Altitude of spawned arrows/markers in meters
    [Tooltip("Distance in meters between each spawned arrow along a path edge. 8m is recommended.")]
    [SerializeField] private float _arrowInterval = 8f;    // Meters between each arrow

    [Header("Visibility / Fade")]
    [Tooltip("Objects fully visible within this distance (meters)")]
    [SerializeField] private float _fadeNearDistance = 25f;
    [Tooltip("Objects fully invisible beyond this distance (meters)")]
    [SerializeField] private float _fadeFarDistance = 35f;

    [Header("UI")]
    [SerializeField] private GameObject _destinationPanel;
    [SerializeField] private MinimapToggle _minimapToggle;
    [SerializeField] private DestinationInfoPanelController _destinationInfoPanel;

    // Internal state
    private NavGraph _graph;
    private OfficeMapping _officeMapping;
    private Dictionary<string, NavNode> _nodeMap = new();
    private Dictionary<string, List<(string neighborId, float dist)>> _adjacency = new();
    private Dictionary<string, OfficeEntry> _officeMap = new(); // office_id -> OfficeEntry
    private List<SpawnedNavObject> _spawnedObjects = new();

    private bool _isUserWaitingForSpawn = false;
    private bool _isWpsStable = false;
    private float _readySince = -1f;
    private bool _hasSpawned = false;
    private string _currentOfficeId = "";  // tracks which office_id was clicked
    private bool _isBackgroundWarmup = false; // true during silent localization warmup

    // Exposed for MinimapController — updated after Dijkstra runs
    public List<string> CurrentPath { get; private set; }

    // ---------------------------------------------
    //  Public properties read by ARWarmupManager
    // ---------------------------------------------
    public bool IsWpsStable => _isWpsStable;

    public float GpsAccuracy
    {
        get
        {
            if (Input.location.status == LocationServiceStatus.Running)
                return Input.location.lastData.horizontalAccuracy;
            return -1f;
        }
    }

    public float StableDuration
    {
        get
        {
            if (_readySince < 0f) return 0f;
            return Mathf.Clamp(Time.time - _readySince, 0f, _stableSeconds);
        }
    }

    public float StableSeconds => _stableSeconds;
    public float GpsAccuracyThreshold => _gpsAccuracyThreshold;

    // True once spawning is complete and arrows are in the world
    public bool HasActiveNavigation => !_isBackgroundWarmup && _hasSpawned && _spawnedObjects.Count > 0;
    public string CurrentOfficeId => _currentOfficeId;

    // Display name of current destination for the top nav card
    // Uses the exact office_id that was clicked so shared-node offices show correctly
    public string CurrentDestinationLabel
    {
        get
        {
            // If a specific office_id was clicked, use its display_name directly
            if (!string.IsNullOrEmpty(_currentOfficeId) &&
                _officeMap.TryGetValue(_currentOfficeId, out OfficeEntry clicked))
                return clicked.display_name;
            // Fallback: search by node (for direct node ID navigation)
            foreach (var entry in _officeMap.Values)
                if (entry.node_id == _destinationNodeId && entry.is_active)
                    return entry.display_name;
            return _destinationNodeId;
        }
    }

    // Returns current user GPS position, false if GPS not running
    public bool TryGetCurrentUserLatLng(out double lat, out double lng)
    {
        if (Input.location.status == LocationServiceStatus.Running)
        {
            lat = Input.location.lastData.latitude;
            lng = Input.location.lastData.longitude;
            return true;
        }
        lat = 0; lng = 0;
        return false;
    }

    // Returns destination node coordinates, false if node not found
    public bool TryGetDestinationLatLng(out double lat, out double lng)
    {
        if (_nodeMap.TryGetValue(_destinationNodeId, out NavNode node))
        {
            lat = node.coordinates.lat;
            lng = node.coordinates.lng;
            return true;
        }
        lat = 0; lng = 0;
        return false;
    }

    // Public wrapper so NavigationTopCardUI can call Haversine
    public double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
        => Haversine(lat1, lon1, lat2, lon2);


    // All three gates must pass simultaneously
    public bool IsLocalizationReady()
    {
        if (!_isWpsStable) return false;
        if (Input.location.status != LocationServiceStatus.Running) return false;
        if (Input.location.lastData.horizontalAccuracy > _gpsAccuracyThreshold) return false;
        return true;
    }

    // ---------------------------------------------
    //  Unity lifecycle
    // ---------------------------------------------
    void Start()
    {
        LoadGraph();
        StartCoroutine(LoadOfficeMappingWithFallback());
        _positionManager.OnStatusChanged += OnStatusChanged;

        Input.compass.enabled = true;

        if (!Input.location.isEnabledByUser)
            Debug.LogWarning("Location services not enabled by user.");
        else
            StartCoroutine(StartGPS());

        // Silent background warmup — runs localization pipeline early
        // so GPS/WPS/compass are already converged when user picks a destination
        StartBackgroundWarmup();
    }

    void Update()
    {
        // Distance-based fade runs whenever navigation is active
        if (_hasSpawned && _spawnedObjects.Count > 0)
            UpdateObjectVisibility();

        // Spawn waiting logic
        if (!_isUserWaitingForSpawn || _hasSpawned) return;

#if UNITY_EDITOR
        if (_readySince < 0f) return;
        if (Time.time - _readySince >= _stableSeconds)
        {
            _hasSpawned = true;
            _isUserWaitingForSpawn = false;
            StartCoroutine(SpawnPathObjects());
        }
#else
        if (IsLocalizationReady())
        {
            if (_readySince < 0f)
            {
                _readySince = Time.time;
                Debug.Log("Localization conditions met - starting stability timer.");
            }

            if (Time.time - _readySince >= _stableSeconds)
            {
                _hasSpawned = true;
                _isUserWaitingForSpawn = false;
                StartCoroutine(SpawnPathObjects());
            }
        }
        else
        {
            if (_readySince >= 0f)
            {
                Debug.LogWarning($"Localization condition lost - resetting stability timer. WPS:{_isWpsStable} GPS:{Input.location.status} Accuracy:{GpsAccuracy:F1}m");
                _readySince = -1f;
            }
        }
#endif
    }

    // ---------------------------------------------
    //  Fade objects based on distance from user
    // ---------------------------------------------
    private void UpdateObjectVisibility()
    {
        if (Input.location.status != LocationServiceStatus.Running) return;

        double userLat = Input.location.lastData.latitude;
        double userLng = Input.location.lastData.longitude;

        foreach (var item in _spawnedObjects)
        {
            if (item.obj == null) continue;

            double dist = Haversine(userLat, userLng, item.lat, item.lng);

            // Compute alpha: 1 = fully visible, 0 = invisible
            float alpha;
            if (dist <= _fadeNearDistance)
                alpha = 1f;
            else if (dist >= _fadeFarDistance)
                alpha = 0f;
            else
                alpha = 1f - (float)((dist - _fadeNearDistance) / (_fadeFarDistance - _fadeNearDistance));

            // Disable entirely when invisible (saves GPU)
            bool shouldBeActive = alpha > 0.01f;
            if (item.obj.activeSelf != shouldBeActive)
                item.obj.SetActive(shouldBeActive);

            if (!shouldBeActive) continue;

            // Apply alpha to all cached renderers
            foreach (var rend in item.renderers)
            {
                if (rend == null) continue;
                foreach (var mat in rend.materials)
                {
                    Color c = mat.color;
                    c.a = alpha;
                    mat.color = c;
                }
            }
        }
    }

    // ---------------------------------------------
    //  Public methods - called by UI buttons
    // ---------------------------------------------

    // Original single button - uses _destinationNodeId from Inspector
    public void StartNavigation()
    {
        if (_isUserWaitingForSpawn || _spawnedObjects.Count > 0) return;
        _isBackgroundWarmup = false; // Cancel any running warmup
        _hasSpawned = false;
        Debug.Log($"Navigating to node: {_destinationNodeId}. Waiting for localization...");
        _isUserWaitingForSpawn = true;

#if UNITY_EDITOR
        _readySince = Time.time;
#endif
    }

    // ---------------------------------------------
    //  Background warmup — runs full navigation pipeline
    //  silently on app start. Objects are spawned behind
    //  the UI panels so WPS actively primes by computing
    //  real GPS-to-Unity mappings. ClearNavigation()
    //  is called when user selects a category.
    // ---------------------------------------------
    private void StartBackgroundWarmup()
    {
        _isBackgroundWarmup = true;
        _destinationNodeId = "N8"; // fixed warmup destination
        _hasSpawned = false;
        _isUserWaitingForSpawn = true;
        _readySince = -1f;
        Debug.Log("[Warmup] Background warmup started — navigating to N8 for WPS priming.");

#if UNITY_EDITOR
        _readySince = Time.time;
#endif
    }

    // Multi-destination buttons
    // Accepts EITHER an office_id (e.g. "off_021") OR a direct node ID (e.g. "N33")
    // Both work - office_id resolves via office_mapping.json, node ID goes directly
    //
    // Button OnClick setup:
    //   1. Drag the NavigationManager GameObject into the OnClick slot
    //   2. Select PreplaceWorldObjects -> StartNavigationTo
    //   3. Type the office_id e.g. "off_021" for Canteen
    //      OR type a node ID directly e.g. "N33" (old format still works)
    public void StartNavigationTo(string input)
    {
        // Cancel background warmup if still running or completed - user real destination takes priority
        if (_isBackgroundWarmup || (_spawnedObjects.Count > 0 && _currentOfficeId == ""))
        {
            Debug.Log("[Warmup] Clearing background warmup — user selected a destination.");
            ClearNavigation();
            _isBackgroundWarmup = false;
        }

        if (_isUserWaitingForSpawn || _spawnedObjects.Count > 0) return;

        // Resolve office_id to node_id if needed
        string resolvedNodeId = ResolveToNodeId(input);

        if (resolvedNodeId == null)
        {
            Debug.LogError($"Cannot resolve destination: '{input}'. Check office_mapping.json or node ID.");
            return;
        }

        _destinationNodeId = resolvedNodeId;

        // Store the exact office_id clicked so CurrentDestinationLabel shows the right name
        // for offices that share the same node (e.g. MIS, Studio, OJT all on N8)
        _currentOfficeId = input.StartsWith("N") ? "" : input;

        Debug.Log($"Destination '{input}' resolved to node: {resolvedNodeId}");
        StartNavigation();
    }

    // Hook to a Cancel or Back button
    public void ClearNavigation()
    {
        foreach (var item in _spawnedObjects)
            Destroy(item.obj);
        _spawnedObjects.Clear();
        _isUserWaitingForSpawn = false;
        _hasSpawned = false;
        _isBackgroundWarmup = false;
        _readySince = -1f;
        _currentOfficeId = "";
        CurrentPath = null; // clear minimap path

        if (_minimapToggle != null)
            _minimapToggle.Hide();

        if (_destinationInfoPanel != null)
            _destinationInfoPanel.Hide();

        Debug.Log("Navigation cleared.");
    }

    // ---------------------------------------------
    //  Resolve office_id OR node_id to a valid node_id
    //  Returns null if not found
    // ---------------------------------------------
    private string ResolveToNodeId(string input)
    {
        if (string.IsNullOrEmpty(input)) return null;

        // If it starts with "N" and exists in nodeMap, treat as direct node ID
        if (input.StartsWith("N") && _nodeMap.ContainsKey(input))
        {
            return input;
        }

        // Otherwise treat as office_id and look up in office mapping
        if (_officeMap.TryGetValue(input, out OfficeEntry entry))
        {
            if (!entry.is_active)
            {
                Debug.LogWarning($"Office '{entry.display_name}' (${input}) is marked inactive.");
                return null;
            }

            if (!_nodeMap.ContainsKey(entry.node_id))
            {
                Debug.LogError($"Office '{entry.display_name}' points to node '{entry.node_id}' which does not exist in nav_graph.json!");
                return null;
            }

            return entry.node_id;
        }

        Debug.LogError($"'{input}' is not a valid office_id or node_id.");
        return null;
    }

    // ---------------------------------------------
    //  Load nav_graph.json from Assets/Resources/
    // ---------------------------------------------
    private void LoadGraph()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("nav_graph");
        if (jsonFile == null)
        {
            Debug.LogError("nav_graph.json not found in Assets/Resources/!");
            return;
        }

        _graph = JsonUtility.FromJson<NavGraph>(jsonFile.text);

        foreach (var node in _graph.nodes)
            _nodeMap[node.id] = node;

        foreach (var node in _graph.nodes)
            _adjacency[node.id] = new List<(string, float)>();

        foreach (var edge in _graph.edges)
        {
            _adjacency[edge.from].Add((edge.to, edge.distance_m));
            if (edge.bidirectional)
                _adjacency[edge.to].Add((edge.from, edge.distance_m));
        }

        Debug.Log($"Graph loaded: {_graph.nodes.Count} nodes, {_graph.edges.Count} edges.");
    }

    // ---------------------------------------------
    //  Load office mapping — tries live API first,
    //  falls back to local office_mapping.json if
    //  network fails or URL is unreachable
    // ---------------------------------------------
    private IEnumerator LoadOfficeMappingWithFallback()
    {
        bool loadedFromServer = false;

        // Only attempt network fetch if URL is set
        if (!string.IsNullOrEmpty(_officeMappingUrl) &&
            !_officeMappingUrl.Contains("your-railway-url"))
        {
            Debug.Log("Fetching office mapping from server...");

            using (UnityEngine.Networking.UnityWebRequest req =
                   UnityEngine.Networking.UnityWebRequest.Get(_officeMappingUrl))
            {
                req.timeout = 10; // 10 second timeout
                yield return req.SendWebRequest();

                if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    try
                    {
                        // API returns { success: true, data: [...] }
                        // We need to wrap it into OfficeMapping format
                        string json = req.downloadHandler.text;
                        ApiOfficeResponse apiResponse = JsonUtility.FromJson<ApiOfficeResponse>(json);

                        if (apiResponse != null && apiResponse.success && apiResponse.data != null)
                        {
                            _officeMap.Clear();
                            foreach (var entry in apiResponse.data)
                            {
                                // API returns display_name as "name" and node_id as "node"
                                OfficeEntry mapped = new OfficeEntry
                                {
                                    office_id = entry.office_id,
                                    display_name = entry.name,
                                    node_id = entry.node,
                                    is_active = true
                                };
                                _officeMap[mapped.office_id] = mapped;
                            }
                            loadedFromServer = true;
                            Debug.Log($"Office mapping loaded from server: {_officeMap.Count} offices.");
                        }
                        else
                        {
                            Debug.LogWarning("Server returned invalid office mapping. Falling back to local JSON.");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Failed to parse server office mapping: {e.Message}. Falling back to local JSON.");
                    }
                }
                else
                {
                    Debug.LogWarning($"Failed to fetch office mapping from server: {req.error}. Falling back to local JSON.");
                }
            }
        }
        else
        {
            Debug.Log("No server URL configured — using local office_mapping.json.");
        }

        // Fallback to local JSON if server fetch failed or was skipped
        if (!loadedFromServer)
        {
            TextAsset jsonFile = Resources.Load<TextAsset>("office_mapping");
            if (jsonFile == null)
            {
                Debug.LogWarning("office_mapping.json not found in Assets/Resources/. Office ID resolution will not work.");
                yield break;
            }

            _officeMapping = JsonUtility.FromJson<OfficeMapping>(jsonFile.text);

            foreach (var entry in _officeMapping.offices)
                _officeMap[entry.office_id] = entry;

            Debug.Log($"Office mapping loaded from local JSON: {_officeMapping.offices.Count} offices.");
        }
    }

    // ---------------------------------------------
    //  GPS initialization
    // ---------------------------------------------
    private IEnumerator StartGPS()
    {
        Input.location.Start(1f, 0.5f);
        int maxWait = 10;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (Input.location.status != LocationServiceStatus.Running)
            Debug.LogWarning("GPS failed to start: " + Input.location.status);
        else
        {
            Debug.Log($"GPS running. Initial accuracy: {Input.location.lastData.horizontalAccuracy:F1}m");
            StartCoroutine(EnableCompass());
        }
    }

    // ---------------------------------------------
    //  Compass initialization — retry until available
    // ---------------------------------------------
    private IEnumerator EnableCompass()
    {
        for (int attempt = 1; attempt <= 10; attempt++)
        {
            Input.compass.enabled = true;
            yield return new WaitForSeconds(1f);

            if (Input.compass.enabled)
            {
                Debug.Log($"[Compass] Enabled on attempt {attempt}. heading:{Input.compass.trueHeading:F1}");
                yield break;
            }

            Debug.Log($"[Compass] Attempt {attempt}/10 — still not enabled.");
        }

        Debug.LogWarning("[Compass] Failed to enable after 10 attempts. Device may not have a magnetometer.");
    }

    // ---------------------------------------------
    //  Core: find path then spawn node markers + midpoint arrows
    // ---------------------------------------------
    private IEnumerator SpawnPathObjects()
    {

        float gpsWait = 0f;
        while (Input.location.status != LocationServiceStatus.Running && gpsWait < 5f)
        {
            gpsWait += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }

        double userLat, userLng;
        if (Input.location.status == LocationServiceStatus.Running)
        {
            userLat = Input.location.lastData.latitude;
            userLng = Input.location.lastData.longitude;
        }
        else
        {
            Debug.LogWarning("GPS unavailable, defaulting to N1.");
            userLat = _nodeMap["N1"].coordinates.lat;
            userLng = _nodeMap["N1"].coordinates.lng;
        }

        string startNodeId = FindNearestNode(userLat, userLng);
        Debug.Log($"User is nearest to node: {startNodeId}");

        List<string> path = Dijkstra(startNodeId, _destinationNodeId);

        // Only expose path to minimap during real navigation (not background warmup)
        if (!_isBackgroundWarmup)
            CurrentPath = path;

        if (path == null || path.Count < 2)
        {
            Debug.LogWarning($"No path found from {startNodeId} to {_destinationNodeId}.");
            yield break;
        }

        Debug.Log("Path: " + string.Join(" -> ", path));

        // Spawn node markers on every node in the path
        for (int i = 0; i < path.Count; i++)
        {
            NavNode node = _nodeMap[path[i]];

            float markerBearing = 0f;
            if (i < path.Count - 1)
            {
                NavNode nextNode = _nodeMap[path[i + 1]];
                markerBearing = ComputeBearing(
                    node.coordinates.lat, node.coordinates.lng,
                    nextNode.coordinates.lat, nextNode.coordinates.lng
                );
            }
            else
            {
                NavNode prevNode = _nodeMap[path[i - 1]];
                markerBearing = ComputeBearing(
                    node.coordinates.lat, node.coordinates.lng,
                    prevNode.coordinates.lat, prevNode.coordinates.lng
                );
            }

            bool isDestination = (i == path.Count - 1);

            if (isDestination)
            {
                // Last node — spawn destination marker (pin/pointer) instead of regular arrow
                if (_destinationMarkerPrefab != null)
                {
                    GameObject destMarker = Instantiate(_destinationMarkerPrefab);
                    // Destination marker faces back toward where the user came from
                    Quaternion destRotation = Quaternion.Euler(0, markerBearing, 0);
                    _objectHelper.AddOrUpdateObject(destMarker, node.coordinates.lat, node.coordinates.lng, _arrowAltitude, destRotation);
                    _spawnedObjects.Add(new SpawnedNavObject
                    {
                        obj = destMarker,
                        lat = node.coordinates.lat,
                        lng = node.coordinates.lng,
                        renderers = destMarker.GetComponentsInChildren<Renderer>()
                    });
                    Debug.Log($"Destination marker: {path[i]} at ({node.coordinates.lat:F6}, {node.coordinates.lng:F6})");
                }
            }
            else
            {
                // Regular path node — spawn node marker arrow
                if (_nodeMarkerPrefab != null)
                {
                    GameObject marker = Instantiate(_nodeMarkerPrefab);
                    Quaternion markerRotation = Quaternion.Euler(0, markerBearing, 0);
                    _objectHelper.AddOrUpdateObject(marker, node.coordinates.lat, node.coordinates.lng, _arrowAltitude, markerRotation);
                    _spawnedObjects.Add(new SpawnedNavObject
                    {
                        obj = marker,
                        lat = node.coordinates.lat,
                        lng = node.coordinates.lng,
                        renderers = marker.GetComponentsInChildren<Renderer>()
                    });
                    Debug.Log($"Node marker {i + 1}/{path.Count - 1}: {path[i]} at ({node.coordinates.lat:F6}, {node.coordinates.lng:F6})");
                }
            }
        }

        // Spawn arrows every _arrowInterval meters between each consecutive node pair
        int totalArrows = 0;
        for (int i = 0; i < path.Count - 1; i++)
        {
            NavNode nodeA = _nodeMap[path[i]];
            NavNode nodeB = _nodeMap[path[i + 1]];

            float bearing = ComputeBearing(
                nodeA.coordinates.lat, nodeA.coordinates.lng,
                nodeB.coordinates.lat, nodeB.coordinates.lng
            );

            double edgeLength = Haversine(
                nodeA.coordinates.lat, nodeA.coordinates.lng,
                nodeB.coordinates.lat, nodeB.coordinates.lng
            );

            // How many arrows fit — at least 1 even for short edges
            int steps = Mathf.Max(1, Mathf.FloorToInt((float)edgeLength / _arrowInterval));

            for (int s = 1; s <= steps; s++)
            {
                // t evenly spaces arrows between nodes, not at endpoints
                double t = (double)s / (steps + 1);

                double arrowLat = nodeA.coordinates.lat + t * (nodeB.coordinates.lat - nodeA.coordinates.lat);
                double arrowLng = nodeA.coordinates.lng + t * (nodeB.coordinates.lng - nodeA.coordinates.lng);

                if (_arrowPrefab != null)
                {
                    GameObject arrow = Instantiate(_arrowPrefab);
                    Quaternion rotation = Quaternion.Euler(0, bearing, 0);
                    _objectHelper.AddOrUpdateObject(arrow, arrowLat, arrowLng, _arrowAltitude, rotation);
                    _spawnedObjects.Add(new SpawnedNavObject
                    {
                        obj = arrow,
                        lat = arrowLat,
                        lng = arrowLng,
                        renderers = arrow.GetComponentsInChildren<Renderer>()
                    });
                    totalArrows++;
                }
            }
        }

        int markers = path.Count;
        Debug.Log($"Navigation started! {markers} node markers + {totalArrows} interval arrows ({_arrowInterval}m spacing) = {_spawnedObjects.Count} total objects.");

        // Only hide destination panel during real navigation (not background warmup)
        if (!_isBackgroundWarmup)
        {
            if (_destinationPanel != null)
                _destinationPanel.SetActive(false);
        }
        else
        {
            Debug.Log("[Warmup] Background warmup objects spawned — WPS is now actively priming.");
        }
    }

    // ---------------------------------------------
    //  Find nearest graph node to a GPS coordinate
    // ---------------------------------------------
    private string FindNearestNode(double lat, double lng)
    {
        string nearestId = null;
        double nearestDist = double.MaxValue;

        foreach (var node in _graph.nodes)
        {
            double d = Haversine(lat, lng, node.coordinates.lat, node.coordinates.lng);
            if (d < nearestDist)
            {
                nearestDist = d;
                nearestId = node.id;
            }
        }

        Debug.Log($"Nearest node: {nearestId} ({nearestDist:F1}m away)");
        return nearestId;
    }

    // ---------------------------------------------
    //  Dijkstra's shortest path
    // ---------------------------------------------
    private List<string> Dijkstra(string startId, string endId)
    {
        var dist = new Dictionary<string, float>();
        var prev = new Dictionary<string, string>();
        var visited = new HashSet<string>();
        var queue = new SortedList<float, string>(new DuplicateKeyComparer());

        foreach (var node in _graph.nodes)
            dist[node.id] = float.MaxValue;

        dist[startId] = 0f;
        queue.Add(0f, startId);

        while (queue.Count > 0)
        {
            float currentDist = queue.Keys[0];
            string currentId = queue.Values[0];
            queue.RemoveAt(0);

            if (visited.Contains(currentId)) continue;
            visited.Add(currentId);

            if (currentId == endId) break;

            if (!_adjacency.ContainsKey(currentId)) continue;

            foreach (var (neighborId, edgeDist) in _adjacency[currentId])
            {
                if (visited.Contains(neighborId)) continue;

                float newDist = currentDist + edgeDist;
                if (newDist < dist[neighborId])
                {
                    dist[neighborId] = newDist;
                    prev[neighborId] = currentId;
                    queue.Add(newDist, neighborId);
                }
            }
        }

        if (!prev.ContainsKey(endId) && startId != endId)
        {
            Debug.LogWarning("Destination unreachable.");
            return null;
        }

        var path = new List<string>();
        string current = endId;
        while (current != null)
        {
            path.Insert(0, current);
            prev.TryGetValue(current, out current);
        }

        return path;
    }

    // ---------------------------------------------
    //  Haversine distance (meters)
    // ---------------------------------------------
    private double Haversine(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000;
        double dLat = (lat2 - lat1) * Math.PI / 180.0;
        double dLon = (lon2 - lon1) * Math.PI / 180.0;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Asin(Math.Sqrt(a));
    }

    // ---------------------------------------------
    //  Compute bearing (degrees) A to B
    //  0 = North, 90 = East, 180 = South, 270 = West
    // ---------------------------------------------
    private float ComputeBearing(double lat1, double lon1, double lat2, double lon2)
    {
        double dLon = (lon2 - lon1) * Math.PI / 180.0;
        double lat1Rad = lat1 * Math.PI / 180.0;
        double lat2Rad = lat2 * Math.PI / 180.0;

        double y = Math.Sin(dLon) * Math.Cos(lat2Rad);
        double x = Math.Cos(lat1Rad) * Math.Sin(lat2Rad) -
                   Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(dLon);

        double bearing = Math.Atan2(y, x) * 180.0 / Math.PI;
        return (float)((bearing + 360.0) % 360.0);
    }

    // ---------------------------------------------
    //  WPS status handler
    // ---------------------------------------------
    private void OnStatusChanged(WorldPositioningStatus status)
    {
        Debug.Log("WPS status: " + status);
        bool readyNow = (status == WorldPositioningStatus.Available);
        if (readyNow)
        {
            _isWpsStable = true;
        }
        else
        {
            _isWpsStable = false;
            _readySince = -1f;
        }
    }
}

// ---------------------------------------------
//  Helper: allows duplicate keys in SortedList
// ---------------------------------------------
public class DuplicateKeyComparer : IComparer<float>
{
    public int Compare(float x, float y)
    {
        int result = x.CompareTo(y);
        return result == 0 ? 1 : result;
    }
}
