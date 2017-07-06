using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using Forge3D;

[CustomEditor(typeof(F3DPoolManager))]
[CanEditMultipleObjects()]
[Serializable]
public class F3DPoolManagerEditor : Editor
{
    [MenuItem("FORGE3D/Pool Manager")]
    public static void OpenPoolManager()
    {
        F3DPoolManager manager = GameObject.FindObjectOfType<F3DPoolManager>();
        if (manager != null)
        {
            Selection.activeGameObject = manager.gameObject;
        }
        else
        {
            GameObject newManager = new GameObject("PoolManager");
            newManager.AddComponent<F3DPoolManager>();
            Selection.activeGameObject = newManager;
        }
    }

    F3DPoolManager menu; // Menu script that we have to attach somewhere 
    List<F3DPoolContainer> pools = new List<F3DPoolContainer>(); //our local pools.  
    public Dictionary<string, F3DPoolContainer> poolsDict = new Dictionary<string, F3DPoolContainer>(); //dicti 
    public ScriptableObject database;
    private string databaseName;
    public string[] poolNames = new string[0];
    public int index = 0;
    private bool[] haveToShow = new bool[0];


    //Current variables,that inputed by keybord. This need for ENTER/CLICK changing
    string currentDatabaseName = "";
    string currentPoolName = "";
    string currentSpawnName = "";
    string currentDespawnName = "";
    int currentTargetFPS = 0;
    int currentOPS = 0;
    int currentPrefabsCount = 0;
    int[] currentBaseCount = new int[0];
    int[] currentMaxCount = new int[0];
    bool lastClickChanged = false;
    bool unClickedTemplates = false;

    string lastLoadedDatabaseName = "";

    private string assetPath = "Assets/FORGE3D/Resources/F3DPoolManagerCache/";

    /// <summary>
    /// Update info and data
    /// </summary>
    void OnEnable()
    {
        menu = (F3DPoolManager)target;
        databaseName = menu.databaseName;
        index = menu.selectedItem;
        haveToShow = menu.haveToShowArr;
    }

    /// <summary>
    /// This function loads pools from asset
    /// </summary>
    void LoadPools()
    {
        if (Application.isPlaying)
            return;
        pools.Clear();
        F3DPoolManagerDB myManager = database as F3DPoolManagerDB;
        if (myManager != null)
        {
            databaseName = myManager.namer;
            menu.databaseName = databaseName;
            if (myManager.pools != null)
            {
                string[] poolNamesTemp = new string[myManager.pools.Count];
                int n = 0;
                foreach (F3DPoolContainer cont in myManager.pools)
                {
                    pools.Add(cont);
                    poolNamesTemp[n] = cont.poolName;
                    n++;
                }
                poolNames = poolNamesTemp;
            }
        }
    }

    /// <summary>
    /// Saving current manager
    /// </summary>
    void SaveManager()
    {
        if (Application.isPlaying)
            return;
        F3DPoolManagerDB newManager = ScriptableObject.CreateInstance<F3DPoolManagerDB>();
        newManager.pools = pools;
        if (databaseName == "")
            databaseName = "DefaultDatabase";
        if (lastLoadedDatabaseName != "")
        {
            if (lastLoadedDatabaseName != databaseName)
            {
                AssetDatabase.DeleteAsset(assetPath + lastLoadedDatabaseName + ".asset");
            }
        }
        newManager.namer = databaseName;
        AssetDatabase.CreateAsset(newManager, assetPath + databaseName + ".asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        database = newManager;
        pools = newManager.pools;
    }

    /// <summary>
    /// Creation of new pool and adding this to our local pools, then saving manager
    /// </summary>
    void CreateNewPoolPressed()
    {
        string newName = "GeneratedPool";
        string addingPrefix = "";
        int j = 0;
        int i = 0;
        bool founded = true;
        while (founded)
        {
            founded = false;
            for (i = 0; i < pools.Count; i++)
            {
                if (pools[i].poolName == newName + addingPrefix)
                {
                    founded = true;
                    j++;
                    addingPrefix = j.ToString();
                    break;
                }
            }
        }
        F3DPoolContainer newCont = new F3DPoolContainer();
        newCont.poolName = newName + addingPrefix;
        currentPoolName = newName + addingPrefix;
        pools.Add(newCont);
        string[] poolNamesTemp = new string[poolNames.Length + 1];
        int n = 0;
        for (n = 0; n < poolNamesTemp.Length - 1; n++)
        {
            poolNamesTemp[n] = poolNames[n];
        }
        poolNamesTemp[n] = newName + addingPrefix;
        poolNames = poolNamesTemp;
        SaveManager();

        if (pools.Count == 1)
        {
            index = 0;
        }
        else
        {
            index = pools.Count - 1;
        }
        LoadAllVariables();
    }

    /// <summary>
    /// Creation of new database
    /// </summary>
    void CreateNewDatabasePressed()
    {
        if (Application.isPlaying)
            return;
        F3DPoolManagerDB newManager = ScriptableObject.CreateInstance<F3DPoolManagerDB>();
        newManager.pools = new List<F3DPoolContainer>();
        string namer = currentDatabaseName;
        if (namer == "")
        {
            namer = "GeneratedDataBase";
            databaseName = namer;
            currentDatabaseName = namer;
        }
        if (AssetDatabase.LoadAssetAtPath(assetPath + namer + ".asset", typeof(ScriptableObject)) == null)
        {
            databaseName = namer;
            currentDatabaseName = namer;
            newManager.namer = databaseName;
            AssetDatabase.CreateAsset(newManager, assetPath + databaseName + ".asset");
            AssetDatabase.SaveAssets();
            pools = new List<F3DPoolContainer>();
            database = newManager;
            LoadPools();
        }
        else
        {
            int j = 0;
            string disNumberedName = namer;
            int toCut = 0;
            int endPos = 0;
            if (disNumberedName[disNumberedName.Length - 1] == ')')
            {
                while (disNumberedName[disNumberedName.Length - 1 - toCut] != '(')
                {
                    toCut++;
                    if (toCut <= 0)
                    {
                        toCut = -1;
                        break;
                    }
                }
                toCut++;
                if (toCut >= 0)
                {
                    endPos = disNumberedName.Length - 1 - toCut;
                    if (disNumberedName[endPos] == ' ')
                    {
                    }
                }
                else
                {
                    endPos = disNumberedName.Length - 1;
                }
                disNumberedName = disNumberedName.Substring(0, endPos);
            }
            j = 0;
            while (AssetDatabase.LoadAssetAtPath(assetPath + disNumberedName + " (" + j.ToString() + ")" + ".asset", typeof(ScriptableObject)) != null)
            {
                j++;
            }
            databaseName = disNumberedName + " (" + j.ToString() + ")";
            currentDatabaseName = databaseName;
            newManager.namer = databaseName;
            AssetDatabase.CreateAsset(newManager, assetPath + databaseName + ".asset");
            AssetDatabase.SaveAssets();
            pools = new List<F3DPoolContainer>();
            database = newManager;
            LoadPools();
        }
    }

    /// <summary>
    /// Updating pool names(need for popup of pools
    /// </summary>
    void UpdateListOfNames()
    {
        string[] poolNamesTemp = new string[pools.Count];
        int n = 0;
        for (n = 0; n < poolNamesTemp.Length; n++)
        {
            poolNamesTemp[n] = pools[n].poolName;
        }
        poolNames = poolNamesTemp;
    }

    /// <summary>
    /// Updating local manager
    /// </summary>
    void LoadDatabase()
    {
        F3DPoolManagerDB newManager = Resources.Load("F3DPoolManagerCache/" + databaseName) as F3DPoolManagerDB;
        if (newManager != null)
        {
            database = newManager;
            databaseName = newManager.namer;
            lastLoadedDatabaseName = databaseName;
            menu.databaseName = databaseName;
            LoadPools();
            UpdateHaveToShowArray();
            LoadAllVariables();
        }
    }

    /// <summary>
    /// This function update showing opt array
    /// </summary>
    void UpdateHaveToShowArray()
    {
        if (index < pools.Count)
        {
            if (pools.Count < 1)
            {
                haveToShow = new bool[0];
                menu.haveToShowArr = haveToShow;
            }
            else
            {
                bool[] tempHaveToShowArray = new bool[pools[index].templates.Length];
                int i;
                for (i = 0; i < tempHaveToShowArray.Length; i++)
                {
                    tempHaveToShowArray[i] = false;
                }
                haveToShow = tempHaveToShowArray;
                menu.haveToShowArr = haveToShow;
            }
        }
    }

    /// <summary>
    /// Loading all variables to local variables that can be inputed by keyboard from db
    /// </summary>
    void LoadAllVariables()
    {
        currentDatabaseName = databaseName;
        if (pools.Count < 1)
        {
            currentPoolName = "My pool";
            currentSpawnName = "SpawnFunction";
            currentDespawnName = "DespawnFunction";
            currentTargetFPS = 50; 
            currentOPS = 8;
            currentPrefabsCount = 0;
            currentBaseCount = new int[0];
            currentMaxCount = new int[0];
        }
        else
        {
            currentPoolName = pools[index].poolName;
            currentSpawnName = pools[index].broadcastSpawnName;
            currentDespawnName = pools[index].broadcastDespawnName;
            currentSpawnName = pools[index].broadcastSpawnName;
            currentDespawnName = pools[index].broadcastDespawnName;
            currentTargetFPS = pools[index].targetFPS;
            currentOPS = pools[index].objectsPerUpdate;
            currentPrefabsCount = pools[index].templates.Length;
            currentBaseCount = pools[index].poolLength;
            currentMaxCount = pools[index].poolLengthMax;
        }
    }

    /// <summary>
    /// Main function of UI drawing
    /// </summary>
    public override void OnInspectorGUI()
    {
        // Our styles
        GUIStyle smallFont = new GUIStyle();
        smallFont.fontSize = 9;
        smallFont.wordWrap = true;

        smallFont.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

        GUIStyle headerFont = new GUIStyle();
        headerFont.fontSize = 11;
        headerFont.fontStyle = FontStyle.Bold;
        headerFont.normal.textColor = new Color(0.75f, 0.75f, 0.75f);

        GUIStyle subHeaderFont = new GUIStyle();
        subHeaderFont.fontSize = 10;
        subHeaderFont.fontStyle = FontStyle.Bold;
        subHeaderFont.margin = new RectOffset(1, 0, 0, 0);
        subHeaderFont.padding = new RectOffset(1, 0, 3, 0);
        subHeaderFont.normal.textColor = new Color(0.70f, 0.70f, 0.70f);

        bool entered = false;
        //Rect prefabRect = new Rect(0, 0, 0, 0);
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
        {
            entered = true;
        }

        bool somethingDropped = false;
        Rect baseRect = new Rect(0, 0, 1, 1);
        DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
        if (Event.current.type == EventType.DragExited)
        {
            if (DragAndDrop.objectReferences.Length > 1)
            {
                somethingDropped = true;
            }
        }
        EditorGUILayout.Space();
        if (!Application.isPlaying)
        {
            bool changed = false;
            bool isAddedDatabase = false;
            if (database != null)
            {
                isAddedDatabase = true;
            }
            ScriptableObject oldDb = database;
            //Here we getting our database
            database = EditorGUILayout.ObjectField("DataBase:", oldDb, typeof(ScriptableObject), true) as ScriptableObject;
            if (isAddedDatabase == false && database != null)
            {
                LoadPools();
                UpdateHaveToShowArray();
                LoadAllVariables();
            }
            else if (oldDb != database && database != null)
            {
                menu.databaseName = databaseName;
                LoadPools();
                UpdateHaveToShowArray();
                LoadAllVariables();
            }
            else if (oldDb != null && database == null)
            {
                oldDb = null;
                database = null;
                LoadPools();
                databaseName = "";
                menu.databaseName = databaseName;
                UpdateHaveToShowArray();
                LoadAllVariables();
            }
            else if (database == null)
            {
                LoadDatabase();
            }
            if (pools.Count <= index)
            {
                index = pools.Count - 1;
            }

            EditorGUILayout.BeginHorizontal("Box");
            {
                string newName = EditorGUILayout.TextField(currentDatabaseName);
                if (newName != currentDatabaseName && newName != "")
                {
                    currentDatabaseName = newName;
                    lastClickChanged = true;
                }
                // Button, that CREATE DB  
                if (GUILayout.Button("Create database"))
                {
                    CreateNewDatabasePressed();
                    UpdateHaveToShowArray();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginVertical("Box");
            {

                if (pools != null && pools.Count > 0)
                {
                    if (index >= pools.Count)
                    {
                        index = pools.Count - 1;
                    }
                    index = EditorGUILayout.Popup(index, poolNames);
                    if (menu.selectedItem != index)
                    {
                        menu.selectedItem = index;
                        UpdateHaveToShowArray();
                        LoadAllVariables();
                    }
                    // NAme of current pool
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Pool name:");// + pools[index].poolName, headerFont); 
                        string newName1 = EditorGUILayout.TextField(currentPoolName);
                        if (newName1 != currentPoolName && newName1 != "")
                        {
                            lastClickChanged = true;
                            currentPoolName = newName1;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.BeginHorizontal();
                {
                    // Button, that CREATE our pool 
                    if (GUILayout.Button("Create pool"))
                    {
                        CreateNewPoolPressed();
                        changed = true;
                    }

                    // Button, that DELETE our pool 
                    if (GUILayout.Button("Delete pool"))
                    {
                        DeletePoolPressed();
                        changed = true;
                        return;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndHorizontal();

            if (index == -1)
            {
                index = 0;

            }
            if (pools.Count <= 0)
            {
                if (entered)
                {
                    ApplyChanges();
                    changed = true;
                    lastClickChanged = false;
                }
                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
                {
                    ApplyChanges();
                    changed = true;
                    lastClickChanged = false;
                }
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    if (lastClickChanged)
                    {
                        ApplyChanges();
                        changed = true;
                    }
                    lastClickChanged = false;
                }
                return;
            }
            EditorGUILayout.BeginVertical("Box");
            {

                // Start parent option  
                bool oldParentOption = pools[index].needParenting;
                pools[index].needParenting = EditorGUILayout.Toggle("Initial parenting:", pools[index].needParenting);
                if (oldParentOption != pools[index].needParenting)
                {
                    changed = true;
                }
                // Sorting option 
                bool oldSortOption = pools[index].needSort;
                pools[index].needSort = EditorGUILayout.Toggle("Runtime sorting:", pools[index].needSort);
                if (oldSortOption != pools[index].needSort)
                {
                    changed = true;
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("Box");
            {

                // Broadcasting option 
                bool oldBroadcastingOption = pools[index].needBroadcasting;
                pools[index].needBroadcasting = EditorGUILayout.Toggle("Broadcasting:", pools[index].needBroadcasting);
                if (oldBroadcastingOption != pools[index].needBroadcasting)
                {
                    changed = true;
                }
                if (pools[index].needBroadcasting)
                {
                    EditorGUILayout.BeginVertical("Box");
                    //Name of OnSpawned function
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.PrefixLabel("Spawn function name:");
                        string oldOnSpanwedFunctionName = pools[index].broadcastSpawnName;
                        string newSpawnName = EditorGUILayout.TextField(pools[index].broadcastSpawnName);
                        if (oldOnSpanwedFunctionName != newSpawnName)
                        {
                            currentSpawnName = newSpawnName;
                            lastClickChanged = true;
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    //Name of OnDespawned function 
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.PrefixLabel("Despawn function name:");
                        string oldOnDespanwedFunctionName = pools[index].broadcastDespawnName;
                        string newDespawnName = EditorGUILayout.TextField(pools[index].broadcastDespawnName);
                        if (oldOnDespanwedFunctionName != newDespawnName)
                        {
                            currentDespawnName = newDespawnName;
                            lastClickChanged = true;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("Box");
            {
                if (pools[index].delayedSpawnInInstall)
                {
                    if (pools[index].optimizeSpawn)
                    {
                        //Delayed spawn option
                        bool oldDelayedSpawnOption = pools[index].delayedSpawnInInstall;
                        pools[index].delayedSpawnInInstall = EditorGUILayout.Toggle("Load control:", pools[index].delayedSpawnInInstall);
                        if (oldDelayedSpawnOption != pools[index].delayedSpawnInInstall)
                        {
                            changed = true;
                        }
                        // FPS option
                        pools[index].optimizeSpawn = EditorGUILayout.Toggle("Load balancer:", pools[index].optimizeSpawn);
                        EditorGUILayout.BeginVertical("Box");
                        {
                            EditorGUILayout.BeginHorizontal();
                            {
                                EditorGUILayout.PrefixLabel("Keep target FPS:");
                                int oldTargetFps = -1;
                                string curTargetFPS = EditorGUILayout.TextField(currentTargetFPS.ToString());
                                int.TryParse(curTargetFPS, out oldTargetFps);
                                if (oldTargetFps >= 0)
                                {
                                    lastClickChanged = true;
                                    currentTargetFPS = oldTargetFps;
                                }
                                else
                                {
                                    currentTargetFPS = pools[index].targetFPS;
                                }
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        EditorGUILayout.EndVertical();
                    }
                    else
                    {
                        //Delayed spawn option
                        bool oldDelayedSpawnOption = pools[index].delayedSpawnInInstall;
                        pools[index].delayedSpawnInInstall = EditorGUILayout.Toggle("Load control:", pools[index].delayedSpawnInInstall);
                        if (oldDelayedSpawnOption != pools[index].delayedSpawnInInstall)
                        {
                            changed = true;
                        }
                        pools[index].optimizeSpawn = EditorGUILayout.Toggle("Load balancer:", pools[index].optimizeSpawn);
                        EditorGUILayout.BeginVertical("Box");
                        {
                            // Else, obj per frame option
                            EditorGUILayout.BeginHorizontal();
                            {
                                EditorGUILayout.PrefixLabel("Limit per frame:");
                                int oldObjPerUpdate = -1;
                                string curObjPerUpdate = EditorGUILayout.TextField(currentOPS.ToString());
                                int.TryParse(curObjPerUpdate, out oldObjPerUpdate);
                                if (oldObjPerUpdate >= 0)
                                {
                                    lastClickChanged = true;
                                    currentOPS = oldObjPerUpdate;
                                }
                                else
                                {
                                    currentOPS = pools[index].objectsPerUpdate;
                                }
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        EditorGUILayout.EndVertical();
                    }
                }
                else
                {
                    //Delayed spawn option
                    bool oldDelayedSpawnOption = pools[index].delayedSpawnInInstall;
                    pools[index].delayedSpawnInInstall = EditorGUILayout.Toggle("Load control:", pools[index].delayedSpawnInInstall);
                    if (oldDelayedSpawnOption != pools[index].delayedSpawnInInstall)
                    {
                        changed = true;
                    }
                }
            }
            EditorGUILayout.EndVertical();

            int n = 0;

            if (pools != null && pools.Count > 0)
            {
                EditorGUILayout.BeginVertical("Box");
                // Debugging option 
                bool oldDebugOption = pools[index].needDebug;
                pools[index].needDebug = EditorGUILayout.Toggle("Debug:", pools[index].needDebug);
                if (oldDebugOption != pools[index].needDebug)
                {
                    changed = true;
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical("Box");
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        // Count of templates
                        EditorGUILayout.PrefixLabel("Prefab items:");
                        int curCount = 0;
                        string curCountString = EditorGUILayout.TextField(currentPrefabsCount.ToString());
                        int.TryParse(curCountString, out curCount);
                        if (pools[index].templates.Length != curCount && (curCount >= 0))
                        {
                            currentPrefabsCount = curCount;
                            lastClickChanged = true;
                        }
                        else
                        {
                            currentPrefabsCount = pools[index].templates.Length;
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    baseRect = GUILayoutUtility.GetLastRect();
                    if (GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                    {
                        unClickedTemplates = true;
                    }

                    //Head of prefabs 
                    EditorGUIUtility.labelWidth = 50;
                    n++;
                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Create new", GUILayout.Width(Screen.width / 3 - 15)))
                        {
                            CreateNewPrefab();
                            LoadAllVariables();
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUIUtility.labelWidth = 50;
                    int i;
                    EditorGUILayout.BeginVertical("Box");
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUIUtility.labelWidth = (1f * (Screen.width) / 100f * 55);
                        EditorGUILayout.BeginHorizontal();
                        {
                            // Current tempalte
                            EditorGUILayout.BeginVertical();
                            {
                                EditorGUILayout.LabelField("Prefab:", GUILayout.Width((1f * (Screen.width) / 100f * 55)));
                            }
                            EditorGUILayout.EndVertical();
                            EditorGUIUtility.labelWidth = (1f * (Screen.width) / 100f * 12);
                            EditorGUILayout.BeginVertical();
                            {
                                //Base item count
                                EditorGUILayout.LabelField("Base:", GUILayout.Width((1f * (Screen.width) / 100f * 12)));
                            }
                            EditorGUILayout.EndVertical();
                            EditorGUIUtility.labelWidth = (1f * (Screen.width) / 100f * 12);
                            EditorGUILayout.BeginVertical();
                            {
                                //Max item count
                                EditorGUILayout.LabelField("Max:", GUILayout.Width((1f * (Screen.width) / 100f * 12)));
                            }
                            EditorGUILayout.EndVertical();
                            EditorGUILayout.LabelField("", GUILayout.Width((1f * (Screen.width) / 100f * 6)));
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndHorizontal();
                    for (i = 0; i < pools[index].templates.Length; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            // Current tempalte
                            EditorGUILayout.BeginVertical();
                            {
                                Transform oldTemp = pools[index].templates[i];
                                pools[index].templates[i] = (Transform)EditorGUILayout.ObjectField("", pools[index].templates[i], typeof(Transform), true, GUILayout.Width(1f * (Screen.width) / 100f * 55));
                                if (oldTemp != pools[index].templates[i])
                                {
                                    changed = true;
                                }
                            }
                            EditorGUILayout.EndVertical();
                            EditorGUILayout.BeginVertical();
                            {
                                //Base item count
                                if (currentBaseCount == null)
                                {
                                    currentBaseCount = pools[index].poolLength;
                                }
                                int curCountPoolLength = 0, oldCount = currentBaseCount[i];
                                int.TryParse(EditorGUILayout.TextField(currentBaseCount[i].ToString(), GUILayout.Width(1f * (Screen.width) / 100f * 12)), out curCountPoolLength);
                                if (curCountPoolLength != oldCount)
                                {
                                    currentBaseCount[i] = curCountPoolLength;
                                    lastClickChanged = true;
                                    break;
                                }
                            }
                            EditorGUILayout.EndVertical();

                            EditorGUILayout.BeginVertical();
                            {
                                //Max item count
                                int curCountPoolLengthMax = 0;
                                int oldCountMax = currentMaxCount[i];
                                int.TryParse(EditorGUILayout.TextField(currentMaxCount[i].ToString(), GUILayout.Width(1f * (Screen.width) / 100f * 12)), out curCountPoolLengthMax);
                                if (curCountPoolLengthMax != oldCountMax)
                                {
                                    currentMaxCount[i] = curCountPoolLengthMax;
                                    lastClickChanged = true;
                                    break;
                                }
                            }
                            EditorGUILayout.EndVertical();
                            EditorGUILayout.BeginVertical();
                            {
                                if (GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(1f * (Screen.width) / 100f * 6)))
                                {
                                    DeletePrefab(i);

                                    LoadAllVariables();
                                }
                            }
                            EditorGUILayout.EndVertical();
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
            }
            else if (pools.Count == 0 && database != null)
            {

                if (GUILayout.Button("Create pool"))
                {
                    CreateNewPoolPressed();
                    changed = true;
                }
            }
            if (somethingDropped)
            {
                if (unClickedTemplates)
                {
                    SetNewTemplates();
                    SetItemsCount(pools[index], pools[index].templates.Length);
                    SaveManager();
                    LoadAllVariables();
                }
            }
            else
            {
                Rect nullRect = new Rect(0, 0, 1, 1);
                if (baseRect != nullRect)
                {
                    if (!baseRect.Contains(Event.current.mousePosition))
                    {
                        unClickedTemplates = false;
                    }
                }
                if (entered)
                {
                    ApplyChanges();
                    changed = true;
                    lastClickChanged = false;
                }
                if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.Tab))
                {
                    ApplyChanges();
                    changed = true;
                    lastClickChanged = false;
                }
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    if (lastClickChanged)
                    {
                        ApplyChanges();
                        changed = true;
                    }
                    lastClickChanged = false;
                }
                if (changed)
                {
                    SaveManager();
                }
            }
        }
    }

    /// <summary>
    /// This function used for setting dragged objects to templates of current pool
    /// </summary>
    void SetNewTemplates()
    {
        List<Transform> tempObjects = new List<Transform>();
        object[] objects = DragAndDrop.objectReferences;
        if (objects != null)
        {
            if (objects.Length >= 1)
            {
                for (int i = 0; i < objects.Length; i++)
                {
                    GameObject addingGO = objects[i] as GameObject;
                    if (addingGO != null)
                    {
                        tempObjects.Add(addingGO.transform);
                    }
                }
                pools[index].templates = tempObjects.ToArray();
            }
        }

    }

    /// <summary>
    /// Transmission of changes from local variables to db. And checking this info too
    /// </summary>
    void ApplyChanges()
    {
        if (database == null)
        {
            CreateNewDatabasePressed();
        }
        else
        {
            databaseName = currentDatabaseName;
            menu.databaseName = databaseName;
            if (pools.Count > 0)
            {
                pools[index].poolName = currentPoolName;
                UpdateListOfNames();
                pools[index].broadcastSpawnName = currentSpawnName;
                pools[index].broadcastDespawnName = currentDespawnName;
                pools[index].targetFPS = currentTargetFPS; 
                pools[index].objectsPerUpdate = currentOPS;
                SetItemsCount(pools[index], currentPrefabsCount);
                pools[index].poolLength = currentBaseCount;
                pools[index].poolLengthMax = currentMaxCount;
                SaveManager();
                CheckForArraysData(pools[index]);
            }
            SaveManager();
            LoadAllVariables();
        }
    }

    /// <summary>
    /// Function that deletes current template with it's legths
    /// </summary>
    /// <param name="point">Index of tempalte</param>
    void DeletePrefab(int point)
    {
        Transform[] tempTemplates = new Transform[pools[index].templates.Length - 1];
        int[] tempLength = new int[tempTemplates.Length];
        int[] tempMaxLength = new int[tempTemplates.Length];
        int i;
        for (i = 0; i < tempTemplates.Length; i++)
        {
            if (i < point)
            {
                tempTemplates[i] = pools[index].templates[i];
                tempLength[i] = pools[index].poolLength[i];
                tempMaxLength[i] = pools[index].poolLengthMax[i];
            }
            else
            {
                tempTemplates[i] = pools[index].templates[i + 1];
                tempLength[i] = pools[index].poolLength[i + 1];
                tempMaxLength[i] = pools[index].poolLengthMax[i + 1];
            }
        }
        pools[index].templates = tempTemplates;
        pools[index].poolLength = tempLength;
        currentBaseCount = tempLength;
        pools[index].poolLengthMax = tempMaxLength;
        currentMaxCount = tempMaxLength;
        SaveManager();
    }

    /// <summary>
    /// Function of creation new epty template
    /// </summary>
    void CreateNewPrefab()
    {
        SetItemsCount(pools[index], pools[index].templates.Length + 1);
        UpdateHaveToShowArray();
    }

    /// <summary>
    /// Checking inputed data of local pools
    /// </summary>
    /// <param name="pool">Selected pool</param>
    void CheckForArraysData(F3DPoolContainer pool)
    {
        int i;

        int[] newArray1 = new int[pool.templates.Length];
        for (i = 0; i < pool.templates.Length; i++)
        {
            if (i < pool.poolLength.Length)
            {
                newArray1[i] = pool.poolLength[i];
            }
            else
            {
                newArray1[i] = 0;
            }
        }

        int[] newArray2 = new int[pool.templates.Length];
        for (i = 0; i < pool.templates.Length; i++)
        {
            if (i < pool.poolLengthMax.Length)
            {
                newArray2[i] = pool.poolLengthMax[i];
                if (newArray2[i] < pool.poolLength[i])
                {
                    newArray2[i] = pool.poolLength[i];
                }
            }
            else
            {
                newArray2[i] = pool.poolLength[i];
            }

        }
        pool.poolLengthMax = newArray2;
        currentMaxCount = pool.poolLengthMax;
    }

    /// <summary>
    /// Deleting of current pool
    /// </summary>
    void DeletePoolPressed()
    {
        if (pools.Count > 0)
        {
            pools.RemoveAt(index);
            SaveManager();
            UpdateListOfNames();
            UpdateHaveToShowArray();
            if (index < 0)
            {
                index = 0;
            }
        }
    }

    /// <summary>
    /// Checking length of pool's arrays
    /// </summary>
    /// <param name="pool">Selected pool</param>
    void CheckForArraysLength(F3DPoolContainer pool)
    {
        int i;
        if (pool.templates.Length != pool.poolLength.Length)
        {
            int[] newArray = new int[pool.templates.Length];
            for (i = 0; i < pool.templates.Length; i++)
            {
                if (i < pool.poolLength.Length)
                {
                    newArray[i] = pool.poolLength[i];
                }
                else
                {
                    newArray[i] = 0;
                }
            }
            pool.poolLength = newArray;
            currentBaseCount = newArray;
        }
        if (pool.templates.Length != pool.poolLengthMax.Length)
        {
            int[] newArray = new int[pool.templates.Length];
            for (i = 0; i < pool.templates.Length; i++)
            {
                if (i < pool.poolLengthMax.Length)
                {
                    newArray[i] = pool.poolLengthMax[i];
                    if (newArray[i] < pool.poolLength[i])
                    {
                        newArray[i] = pool.poolLength[i];
                    }
                }
                else
                {
                    newArray[i] = pool.poolLength[i];
                }

            }
            pool.poolLengthMax = newArray;
            currentMaxCount = pool.poolLengthMax;
        }
    }

    /// <summary>
    /// Setting templates count in pool.
    /// </summary>
    /// <param name="pool">Selected pool</param>
    /// <param name="count">New count</param>
    void SetItemsCount(F3DPoolContainer pool, int count)
    {
        int i;
        if (pool == null)
        {
            pool = new F3DPoolContainer();
        }
        Transform[] newTemplates = new Transform[count];
        for (i = 0; i < count; i++)
        {
            if (i < pool.templates.Length)
            {
                newTemplates[i] = pool.templates[i];
            }
            else
            {
                newTemplates[i] = null;
            }
        }
        pool.templates = newTemplates;
        CheckForArraysLength(pool);
    }
}
