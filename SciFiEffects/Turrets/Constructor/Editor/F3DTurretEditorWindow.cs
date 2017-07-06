using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Forge3D
{    
    public class F3DTurretEditorWindow : EditorWindow
    {
        F3DTurretScriptable db;                                      //Loaded DataBase
        List<TurretStructure> turrets = new List<TurretStructure>(); //local turrets structures

        int selectedTurret = 0;                                      //Index of currently selected turret

        Vector2 scrollPos = new Vector2(0, 0);                       // Variable for ScrollView

        bool installedStart;
        bool showBases;

        string[] baseNames = new string[0];

        bool showSwivels;
        string[] swivelNames = new string[0];

        bool showHeads;
        string[] headNames = new string[0];

        bool showMounts;
        string[] mountNames = new string[0];
        string[] mountPrefabNames = new string[0];

        bool showBreeches;
        string[] breechNames = new string[0];

        bool showBarrels = false;
        string[] barrelNames = new string[0];

        bool showSwivelPrefixes = false;
        string[] swivelPrefixes = new string[0];

        string[] headPrefixes = new string[0];

        string[] mountPrefixes = new string[0];

        bool lastClickChanged = false;

        string currentNameField = "";
        string currentBasesCountField = "";
        string currentSwivelCountField = "";
        string currentHeadCountField = "";
        string currentMountCountField = "";
        string currentBreechesCountField = "";
        string currentBarrelCountField = "";
         
        string currentSwivelPrefixField = "*SOCKET_SWIVEL"; 
        string currentHeadPrefixField = "*SOCKET_HEAD"; 
        string currentMountPrefixField = "*SOCKET_MOUNT"; 
        string currentWeaponSocketField = "*SOCKET_WEAPON"; 
        string currentBarrelPrefixField = "*SOCKET_BARREL";

        Rect baseRect = new Rect(0, 0, 0, 0), brechRect = new Rect(0, 0, 0, 0), barrelRect = new Rect(0, 0, 0, 0), swivelRect = new Rect(0, 0, 0, 0), mountRect = new Rect(0, 0, 0, 0), headRect = new Rect(0, 0, 0, 0);
        bool unClickedBase;
        bool unClickedSwivel;
        bool unClickedHead;
        bool unClickedMount;
        bool unClickedBreech;
        bool unClickedBarrel ;
        private bool isGUIStylesLoaded;

        [MenuItem("FORGE3D/Turrets/Add default")]
        private static void AddDefault()
        {
            AddToSceneConstructor();
        }

        [MenuItem("FORGE3D/Turrets/Add default at current selection")]
        private static void AddDefaultAtSelection()
        {
            AddToSceneConstructorSelection();
        }

        [MenuItem("FORGE3D/Turrets/Turret Editor...")]
        private static void OpenTurretEditor()
        {
            ShowWindow();
        }

        void UpdateAllCurrentTurrels()
        {
            F3DTurretConstructor[] editors = GameObject.FindObjectsOfType<F3DTurretConstructor>();
            int i;
            for (i = 0; i < editors.Length; i++)
            {
                if (editors[i].GetSelectedType() == selectedTurret)
                { 
                    editors[i].UpdateFullTurret(turrets[selectedTurret]);
                }
            }
        }

        public static int GetCurrentIndex()
        {
            F3DTurretScriptable newManager = AssetDatabase.LoadAssetAtPath("Assets/FORGE3D/Sci-Fi Effects/Turrets/Database/database.asset", typeof(ScriptableObject)) as F3DTurretScriptable;
            if (newManager != null)
            {
                return newManager.SelectedTurret;
            }
            return 0;
        }

        /// <summary>
        /// Use this function for creation new constructor
        /// </summary>
        public static void AddToSceneConstructor()
        {
            int index = GetCurrentIndex();
            if (AssetDatabase.LoadAssetAtPath("Assets/FORGE3D/Sci-Fi Effects/Turrets/Prefabs/ConstructorBase.prefab", typeof(GameObject)))
            {
                GameObject loading = AssetDatabase.LoadAssetAtPath("Assets/FORGE3D/Sci-Fi Effects/Turrets/Prefabs/ConstructorBase.prefab", typeof(GameObject)) as GameObject;
                GameObject newGO = Instantiate(loading, Vector3.zero, Quaternion.identity) as GameObject;
                newGO.name = "Turret";
                newGO.GetComponent<F3DTurretConstructor>().turretIndex = index;
                Selection.activeGameObject = newGO;
            }
            else
            {
                Debug.LogWarning("Not foud turret template:Assets/FORGE3D/Sci-Fi Effects/Turrets/Prefabs/ConstructorBase.prefab");
            } 
        }

        /// <summary>
        /// Use this function for turning on constructor on selected empty gameobject
        /// </summary>
        public static void AddToSceneConstructorSelection()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected != null)
            {
                if (selected.transform.childCount > 0)
                {
                    Debug.LogWarning("F3DTurretEditor: A GAMEOBJECT HAS TO BE EMPTY BEFORE ADDING TURRET CONSTRUCTOR!");
                }
                else
                {
                    F3DTurretConstructor constructor = selected.GetComponent<F3DTurretConstructor>();
                    if (constructor != null)
                    {
                        Debug.LogWarning("F3DTurretEditor: THIS GAMEOBJECT ALREADY HAS THE TURRET CONTRUCTOR!");
                    }
                    else
                    {
                        int index = GetCurrentIndex(); 
                        constructor = selected.AddComponent<F3DTurretConstructor>();
                        constructor.turretIndex = index;
                    }
                }
            }
            else
            {
                Debug.LogWarning("F3DTurretEditor: HERE IS NO SELECTED GAMEOBJECT");
            }
        }

        /// <summary>
        /// This function creates this window
        /// </summary>
        public static void ShowWindow()
        {
            F3DTurretEditorWindow wnd = (F3DTurretEditorWindow)EditorWindow.GetWindow(typeof(F3DTurretEditorWindow));
            wnd.titleContent = new GUIContent("Turret Editor");      
        }

        void OnEnable()
        {
            LoadDatabase();
            UpdateAllVariables();
            if (installedStart)
            {
                installedStart = true;
            }
        }

        /// <summary>
        /// Use this function for updating all available fields
        /// </summary>
        void UpdateAllVariables()
        {
            if (db.Turrets.Count <= 0)
            {
                currentNameField = "";
                currentBarrelCountField = "";
                currentBreechesCountField = db.Breeches.Count.ToString(); 
                currentBarrelCountField = db.Barrels.Count.ToString();

                currentMountPrefixField = "*SOCKET_MOUNT";
                currentSwivelPrefixField = "*SOCKET_SWIVEL";// new List<string>();
                currentHeadPrefixField = "*SOCKET_HEAD";
                currentBarrelPrefixField = "*SOCKET_BARREL";
                currentWeaponSocketField = "*SOCKET_WEAPON";
                return;
            }

            UpdateBaseNames();
            UpdateSwivelNames();
            UpdateHeadNames();
            UpdateMountNames();
            UpdateBreechNames();
            UpdateBarrelNames();
            UpdateSwivelPrefixNames();
            UpdateHeadPrefixNames();
            UpdateMountPrefixNames();
            UpdateBarrelNames();
            UpdateBarrelPrefixNames();
            UpdateWeaponPrefixNames();           
            CheckForMounts();
            CheckForBarrels();

            currentNameField = db.Turrets[selectedTurret].Name;

            currentBasesCountField = db.Bases.Count.ToString();
            currentSwivelCountField = db.Swivels.Count.ToString();
            currentHeadCountField = db.Heads.Count.ToString();
            currentMountCountField = db.Mounts.Count.ToString();
            currentBreechesCountField = db.Breeches.Count.ToString();
            currentBarrelCountField = db.Barrels.Count.ToString();

            currentSwivelPrefixField = db.SwivelPrefix;
            currentHeadPrefixField = db.HeadPrefix;        
            currentMountPrefixField = db.MountPrefix;         
            currentBarrelPrefixField = db.BarrelPrefix;          
            currentWeaponSocketField = db.WeaponSocket;         
        }

        void OnGUI()
        {
            var warningLabel = new GUIStyle("ChannelStripAttenuationBar")
            {
                alignment = TextAnchor.MiddleCenter
            };
          
            var miniLabel = new GUIStyle("MiniLabel")
            {
                padding = new RectOffset(3, 3, 3, 3),
                margin = new RectOffset(2, 1, 1, 3)
            };

            var lodGrpLayout = new GUIStyle("HelpBox")
            {
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 5, 0)
            };
            
            var myBoxEmpty = new GUIStyle
            {
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };

            var myBox = new GUIStyle("CN Box") {padding = new RectOffset(3, 3, 3, 3)};

            var myHelpBox = new GUIStyle("HelpBox") {margin = new RectOffset(1, 1, 10, 1)};

            var myInfoBox = new GUIStyle("CN EntryInfo")
            {
                wordWrap = true,
                margin = new RectOffset(1, 1, 10, 10)
            };

            var weaponTagButton = new GUIStyle("SelectionRect")
            {
                margin = new RectOffset(3, 3, 2, 2),
                alignment = TextAnchor.MiddleCenter
            };

            var myWeapBox = new GUIStyle("CN Box")
            {
                padding = new RectOffset(3, 3, 1, 1),
                margin = new RectOffset(0, 0, 3, 4)
            };

            var myPrefabLayout = new GUIStyle
            {
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 3, 0)
            };

            var mySocketNamesLayout = new GUIStyle
            {
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };

            if (Application.isPlaying)
                return;

            Color baseCol = GUI.backgroundColor;

            int i;
            bool changed = false;
            bool entered = false;
            if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.Tab))
            {
                entered = true;
            }

            bool somethingDropped = false;

            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;

            if (Event.current.type == EventType.DragExited)
            {
                if (DragAndDrop.objectReferences.Length >= 1)
                {
                    somethingDropped = true;
                }
            }

            GUIStyle vertical = GUI.skin.verticalScrollbar;
      
            if (db == null)
            {
                LoadDatabase();
                UpdateAllVariables();
                if (installedStart)
                { 
                    installedStart = true;
                }
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, true, GUIStyle.none, vertical, GUIStyle.none);

            // WINDOW BACKGROUND START
            EditorGUILayout.BeginHorizontal("AnimationCurveEditorBackground");
            {
                //Clearing index of current turret
                if (selectedTurret >= db.Turrets.Count)
                {
                    selectedTurret = db.Turrets.Count - 1;
                }
                if (selectedTurret < 0)
                {
                    selectedTurret = 0;
                }

                // FIRST COLUMN START
                EditorGUILayout.BeginVertical(myBoxEmpty, GUILayout.MinWidth(180), GUILayout.MaxWidth(300));
                {
                    // NAME START
                    EditorGUILayout.BeginVertical("Box");
                    {
                        GUILayout.Label("Edit:", "flow target in");
                   
                        string oldName = currentNameField;

                        GUILayout.BeginHorizontal(myPrefabLayout);
                            GUILayout.Label("Name:", miniLabel);
                                GUILayout.FlexibleSpace();
                            string newName = EditorGUILayout.TextField(currentNameField);
                        GUILayout.EndHorizontal();

                        if (newName != oldName && newName != "")
                        {
                            currentNameField = newName;
                            lastClickChanged = true;
                        }
                        
                        if (GUILayout.Button("Add to scene", "toolbarbutton"))
                        {
                            AddToScene();
                        }
                    }
                    EditorGUILayout.EndVertical();
                    // NAME END

                    // ADD BUTTONS START
                    EditorGUILayout.BeginHorizontal("Box");
                        EditorGUILayout.BeginHorizontal("HelpBox");                   
                        {                           
                            // Button, that Add new turret template 
                            if (GUILayout.Button("Add", "toolbarbutton", GUILayout.MinWidth(50)))
                            {
                                AddNewTurret();
                            }

                            // Button, that Duplicate current turret
                            if (GUILayout.Button("Duplicate", "toolbarbutton", GUILayout.MinWidth(50)))
                            {
                                DuplicateTurret();
                            }

                            // Button, that Remove current turret
                            if (GUILayout.Button("Remove", "toolbarbutton", GUILayout.MinWidth(50)))
                            {
                                DeleteCurrentTurret();
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndHorizontal();
                    // ADD BUTTONS END    
                   
                    // TEMPLATES START
                    EditorGUILayout.BeginVertical("Box");

                    EditorGUILayout.BeginHorizontal(myPrefabLayout, GUILayout.MinHeight(20));
                        GUILayout.Label("Templates:", "flow target in");                   
                    EditorGUILayout.EndHorizontal();
               
                    EditorGUILayout.BeginVertical(myBox, GUILayout.MaxHeight(1));
                    {                                
                        if (db.Turrets.Count > 0)
                        {
                         
                            for (i = 0; i < db.Turrets.Count; i++)
                            {
                                EditorGUILayout.BeginHorizontal();
                                            
                                    if (selectedTurret == i)
                                    {
                                        GUI.backgroundColor = new Color(0.65f, 0.8f, 1.0f);
                                        GUI.contentColor = new Color(1f, 1f, 1f);
                                     
                                        if (GUILayout.Button(db.Turrets[i].Name,  "minibuttonmid"))
                                        {

                                        }
                                        GUI.contentColor = Color.white;
                                    }
                                    else
                                    {
                                        GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f); ;
                                        GUI.contentColor = Color.grey;
                                        if (GUILayout.Button(db.Turrets[i].Name, "minibuttonmid"))
                                        {
                                            selectedTurret = i;
                                            db.SelectedTurret = i;
                                            UpdateAllVariables();
                                            CheckForMounts();
                                            CheckForBarrels();
                                        }
                                       
                                    }
                                    // Button, that DELETE our turret 
                                    if (GUILayout.Button("X", "minibuttonmid", GUILayout.Width(20)))
                                    {
                                        DeleteTurret(i);
                                    }

                                    GUI.backgroundColor = baseCol;
                                    GUI.contentColor = Color.white;

                                EditorGUILayout.EndHorizontal();
                           }
                        }
                        else
                        {
                            EditorGUILayout.BeginVertical();                                   
                                                        
                                    EditorGUILayout.BeginHorizontal(myBoxEmpty);
                                        
                                        GUILayout.Label("No items to display", warningLabel);
                                      
                                    EditorGUILayout.EndHorizontal();

                            EditorGUILayout.BeginHorizontal(myHelpBox);
                           
                            GUILayout.Label("Click'Add' to create first turret template", myInfoBox, GUILayout.MinWidth(150));
                         
                            EditorGUILayout.EndHorizontal();

                            EditorGUILayout.EndVertical();
                        }                               
                    }
                    EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
                // Templates end
                    
                }
                EditorGUILayout.EndVertical();
                // First column end
                //////////////////////
              
                GUI.backgroundColor = baseCol;
                
                // Second column start
                ///////////////////////
                EditorGUILayout.BeginVertical(myBoxEmpty, GUILayout.MinWidth(250), GUILayout.MaxWidth(350)); 
                {
                    if (turrets.Count > 0)
                    {
                        // Box layout start
                        EditorGUILayout.BeginVertical("Box");
                        {
                            EditorGUILayout.BeginHorizontal();
                                GUILayout.Label("Turret setup:", "flow target in");
                            EditorGUILayout.EndHorizontal();

                            if (db.Turrets.Count > 0)
                            {
                                // Enable lodgroup start
                                EditorGUILayout.BeginVertical(lodGrpLayout);
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    {
                                        GUILayout.Label("Enable LODGroup:", miniLabel, GUILayout.MaxWidth(130));                                  
                                        GUILayout.FlexibleSpace();
                                        if (selectedTurret >= db.Turrets.Count)
                                        {
                                            selectedTurret = db.Turrets.Count - 1;
                                        }

                                        bool newValue = EditorGUILayout.Toggle(db.Turrets[selectedTurret].NeedLOD);
                                        GUILayout.FlexibleSpace();
                                        if (newValue != db.Turrets[selectedTurret].NeedLOD)
                                        {
                                            db.Turrets[selectedTurret].NeedLOD = newValue;
                                            changed = true;
                                        }
                                    }
                                    EditorGUILayout.EndHorizontal();

                                    EditorGUILayout.BeginHorizontal();
                                    {
                                        GUILayout.Label("Enable turret controller:", miniLabel, GUILayout.MaxWidth(130));
                                        GUILayout.FlexibleSpace();
                                        if (selectedTurret >= db.Turrets.Count)
                                        {
                                            selectedTurret = db.Turrets.Count - 1;
                                        }

                                        bool newValue = EditorGUILayout.Toggle(db.Turrets[selectedTurret].HasTurretScript);
                                        GUILayout.FlexibleSpace();
                                        if (newValue != db.Turrets[selectedTurret].HasTurretScript)
                                        {
                                            db.Turrets[selectedTurret].HasTurretScript = newValue;
                                            changed = true;
                                        }
                                    }
                                    EditorGUILayout.EndHorizontal();
                                }                                
                                EditorGUILayout.EndVertical();
                                // Enable lodgroup end
                                 
                                // Base prefab name start
                                EditorGUILayout.BeginHorizontal("HelpBox");
                                {
                                    if (baseNames.Length > 0)
                                    { 
                                        GUILayout.Label("Base prefab:", miniLabel);
                                        GUILayout.FlexibleSpace();
                                        int oldIndex = -1;
                                        if (selectedTurret >= db.Turrets.Count)
                                        {
                                            selectedTurret = db.Turrets.Count - 1;
                                        }
                                        if (db.Turrets[selectedTurret].Base != null)
                                        {
                                            oldIndex = FindIndex(baseNames, db.Turrets[selectedTurret].Base.name);
                                            if (baseNames.Length > oldIndex)
                                            {
                                                if (baseNames[oldIndex] != db.Turrets[selectedTurret].Base.name)
                                                {
                                                    db.Turrets[selectedTurret].Base = null;
                                                    changed = true;
                                                    UpdateSwivelPrefixNames();
                                                    UpdateHeadPrefixNames();
                                                    UpdateMountPrefixNames();
                                                    CheckForMounts();
                                                    CheckForBarrels();
                                                }
                                            }
                                            else
                                            {
                                                db.Turrets[selectedTurret].Base = null;
                                                changed = true;
                                                UpdateSwivelPrefixNames();
                                                UpdateHeadPrefixNames();
                                                UpdateMountPrefixNames();
                                                CheckForMounts();
                                                CheckForBarrels();
                                            }
                                        }
                                        else
                                        {
                                            oldIndex = FindIndex(baseNames, "None");
                                        }

                                        int newIndex = EditorGUILayout.Popup(oldIndex, baseNames, "ToolbarDropDown");

                                        if (newIndex != oldIndex)
                                        {
                                            if (baseNames.Length > newIndex)
                                            {
                                                if (newIndex == -1)
                                                {
                                                    db.Turrets[selectedTurret].Base = null;
                                                    changed = true;
                                                    UpdateSwivelPrefixNames();
                                                    UpdateHeadPrefixNames();
                                                    UpdateMountPrefixNames();
                                                    CheckForMounts();
                                                    CheckForBarrels();

                                                }
                                                else
                                                {
                                                    if (baseNames[newIndex] == "None")
                                                    {
                                                        db.Turrets[selectedTurret].Base = null;
                                                        changed = true;
                                                        UpdateSwivelPrefixNames();
                                                        UpdateHeadPrefixNames();
                                                        UpdateMountPrefixNames();
                                                        CheckForMounts();
                                                        CheckForBarrels();
                                                    }
                                                    if (db.Turrets[selectedTurret].Base == null)
                                                    {
                                                        db.Turrets[selectedTurret].Base = GetBaseByIndex(newIndex);
                                                        changed = true;
                                                        UpdateSwivelPrefixNames();
                                                        UpdateHeadPrefixNames();
                                                        UpdateMountPrefixNames();
                                                        CheckForMounts();
                                                        CheckForBarrels();
                                                    }
                                                    else if (db.Turrets[selectedTurret].Base.name != baseNames[newIndex])
                                                    {
                                                        db.Turrets[selectedTurret].Base = GetBaseByIndex(newIndex);
                                                        changed = true;
                                                        UpdateSwivelPrefixNames();
                                                        UpdateHeadPrefixNames();
                                                        UpdateMountPrefixNames();
                                                        CheckForMounts();
                                                        CheckForBarrels();
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                db.Turrets[selectedTurret].Base = null;
                                                changed = true;
                                                UpdateSwivelPrefixNames();
                                                UpdateHeadPrefixNames();
                                                UpdateMountPrefixNames();
                                                CheckForMounts();
                                                CheckForBarrels();

                                            }
                                        }
                                    }
                                    else
                                    {
                                        

                                        GUILayout.Label("Bases templates count 0!", GUILayout.MinWidth(100));
                                    }
                                }
                                EditorGUILayout.EndHorizontal();
                                // Base prefab name end

                                // Swivel prefab start
                                EditorGUILayout.BeginVertical("HelpBox");
                                {
                                    if (swivelNames.Length > 0)
                                    {
                                        // Swivel prefab popup start
                                        EditorGUILayout.BeginHorizontal(myPrefabLayout);
                                        {                                            
                                            GUILayout.Label("Swivel prefab:", miniLabel);
                                            GUILayout.FlexibleSpace();
                                            int oldIndex = -1;
                                            if (selectedTurret >= db.Turrets.Count)
                                            {
                                                selectedTurret = db.Turrets.Count - 1;
                                            }
                                            if (db.Turrets[selectedTurret].Swivel != null)
                                            {
                                                oldIndex = FindIndex(swivelNames, db.Turrets[selectedTurret].Swivel.name);
                                                if (swivelNames.Length > oldIndex)
                                                {
                                                    if (swivelNames[oldIndex] != db.Turrets[selectedTurret].Swivel.name)
                                                    {
                                                        db.Turrets[selectedTurret].Swivel = null;
                                                        changed = true;
                                                        UpdateHeadPrefixNames();
                                                        UpdateMountPrefixNames();
                                                        CheckForMounts();
                                                        CheckForBarrels();
                                                    }
                                                }
                                                else
                                                {
                                                    db.Turrets[selectedTurret].Swivel = null;
                                                    changed = true;
                                                    UpdateHeadPrefixNames();
                                                    UpdateMountPrefixNames();
                                                    CheckForMounts();
                                                    CheckForBarrels();
                                                }
                                            }
                                            else
                                            {
                                                oldIndex = FindIndex(swivelNames, "None");
                                            }

                                            int newIndex = EditorGUILayout.Popup(oldIndex, swivelNames, "ToolbarDropDown");

                                            if (newIndex != oldIndex)
                                            {
                                                if (swivelNames.Length > newIndex)
                                                {
                                                    if (newIndex == -1)
                                                    {
                                                        db.Turrets[selectedTurret].Swivel = null;
                                                        changed = true;
                                                        UpdateHeadPrefixNames();
                                                        UpdateMountPrefixNames();
                                                        CheckForMounts();
                                                        CheckForBarrels();
                                                    }
                                                    else
                                                    {
                                                        if (swivelNames[newIndex] == "None")
                                                        {
                                                            db.Turrets[selectedTurret].Swivel = null;
                                                            changed = true;
                                                            UpdateHeadPrefixNames();
                                                            UpdateMountPrefixNames();
                                                            CheckForMounts();
                                                            CheckForBarrels();
                                                        }
                                                        else if (db.Turrets[selectedTurret].Swivel == null)
                                                        {
                                                            db.Turrets[selectedTurret].Swivel = GetSwivelByIndex(newIndex);
                                                            changed = true;
                                                            UpdateHeadPrefixNames();
                                                            UpdateMountPrefixNames();
                                                            CheckForMounts();
                                                            CheckForBarrels();
                                                        }
                                                        else if (db.Turrets[selectedTurret].Swivel.name != swivelNames[newIndex])
                                                        {
                                                            turrets[selectedTurret].Swivel = GetSwivelByIndex(newIndex);
                                                            changed = true;
                                                            UpdateHeadPrefixNames();
                                                            UpdateMountPrefixNames();
                                                            CheckForMounts();
                                                            CheckForBarrels();
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        EditorGUILayout.EndHorizontal();
                                        // Swivel prefab popup end

                                    }
                                    else
                                    {                                        
                                        GUILayout.Label("Swivel templates count 0!", GUILayout.MinWidth(100));
                                    }

                                    if (db.Turrets[selectedTurret].Swivel != null)
                                    {
                                        
                                        //Swivel socket prefix start
                                        EditorGUILayout.BeginHorizontal(myPrefabLayout);
                                        {
                                            
                                            if (swivelPrefixes.Length > 0)
                                            {
                                                GUILayout.Label("Socket:", miniLabel );
                                                GUILayout.FlexibleSpace();
                                                int oldIndex = FindIndex(swivelPrefixes, db.Turrets[selectedTurret].SwivelPrefix);
                                                
                                                int newIndex = EditorGUILayout.Popup(oldIndex, swivelPrefixes, "ToolbarDropDown");

                                                if (db.Turrets[selectedTurret].SwivelPrefix != swivelPrefixes[newIndex])
                                                {
                                                    turrets[selectedTurret].SwivelPrefix = swivelPrefixes[newIndex];
                                                    changed = true;
                                                }
                                            }
                                            else
                                            {                                               
                                                EditorGUILayout.HelpBox("Swivel socket prefixes count 0!", MessageType.Warning);
                                            }
                                        }
                                        EditorGUILayout.EndHorizontal();
                                        // Swivel socket prefix end
                                    }
                                }
                                 EditorGUILayout.EndHorizontal();
                                // Swivel prefab end

                                // Head prefab start
                                EditorGUILayout.BeginVertical("HelpBox");
                                {
                                    if (headNames.Length > 0)
                                    {
                                        // Prefab popup layout start
                                        EditorGUILayout.BeginHorizontal(myPrefabLayout);
                                        {                                            
                                            GUILayout.Label("Head prefab:", miniLabel);
                                            GUILayout.FlexibleSpace();
                                            int oldIndex = -1;
                                            if (selectedTurret >= db.Turrets.Count)
                                            {
                                                selectedTurret = db.Turrets.Count - 1;
                                            }
                                            if (db.Turrets[selectedTurret].Head != null)
                                            {
                                                oldIndex = FindIndex(headNames, db.Turrets[selectedTurret].Head.name);
                                                if (headNames.Length > oldIndex)
                                                {
                                                    if (headNames[oldIndex] != db.Turrets[selectedTurret].Head.name)
                                                    {
                                                        db.Turrets[selectedTurret].Head = null;
                                                        changed = true;
                                                    }
                                                }
                                                else
                                                {
                                                    db.Turrets[selectedTurret].Head = null;
                                                    changed = true;
                                                }
                                            }
                                            else
                                            {
                                                oldIndex = FindIndex(headNames, "None");
                                            }

                                            int newIndex = EditorGUILayout.Popup(oldIndex, headNames, "ToolbarDropDown");

                                            if (newIndex != oldIndex)
                                            {
                                                if (headNames.Length > newIndex)
                                                {
                                                    if (newIndex == -1)
                                                    {
                                                        turrets[selectedTurret].Head = null;
                                                        changed = true;
                                                        CheckForMounts();
                                                        CheckForBarrels();
                                                    }
                                                    else
                                                    {
                                                        if (headNames[newIndex] == "None")
                                                        {
                                                            turrets[selectedTurret].Head = null;
                                                            changed = true;
                                                            CheckForMounts();
                                                            CheckForBarrels();
                                                        }
                                                        if (turrets[selectedTurret].Head == null)
                                                        {
                                                            turrets[selectedTurret].Head = GetHeadByIndex(newIndex);
                                                            changed = true;
                                                            CheckForMounts();
                                                            CheckForBarrels();
                                                        }
                                                        else if (turrets[selectedTurret].Head.name != headNames[newIndex])
                                                        {
                                                            turrets[selectedTurret].Head = GetHeadByIndex(newIndex);
                                                            changed = true;
                                                            CheckForMounts();
                                                            CheckForBarrels();
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        EditorGUILayout.EndHorizontal();
                                        // Prefab popup layout end
                                    }
                                    else
                                    {                                     
                                        GUILayout.Label("Head templates count 0!", GUILayout.MinWidth(100));
                                    }

                                    if (turrets[selectedTurret].Head != null)
                                    {
                                        // Head socket popup start
                                        EditorGUILayout.BeginHorizontal(myPrefabLayout);
                                        {                                            
                                            if (headPrefixes.Length > 0)
                                            {
                                                GUILayout.Label("Socket:", miniLabel);
                                                GUILayout.FlexibleSpace();
                                                int oldIndex = FindIndex(headPrefixes, turrets[selectedTurret].HeadPrefix);

                                                int newIndex = EditorGUILayout.Popup(oldIndex, headPrefixes, "ToolbarDropDown");

                                                if (newIndex >= headPrefixes.Length)
                                                {
                                                    turrets[selectedTurret].HeadPrefix = headPrefixes[0];
                                                }
                                                else if (turrets[selectedTurret].HeadPrefix != headPrefixes[newIndex])
                                                {
                                                    turrets[selectedTurret].HeadPrefix = headPrefixes[newIndex];
                                                    changed = true;
                                                    CheckForMounts();
                                                    CheckForBarrels();
                                                }
                                            }
                                            else
                                            {                                            
                                                EditorGUILayout.HelpBox("A swivel prefab has to be selected first.", MessageType.Warning);
                                            }
                                        }
                                        EditorGUILayout.EndHorizontal();
                                        // Head socket popup end
                                    }
                                }
                                EditorGUILayout.EndVertical();
                                // Head prefab end
                                
                                // Mount prefab start
                                EditorGUILayout.BeginVertical("HelpBox");
                                {
                                    if (mountPrefabNames.Length > 0)
                                    {            
                                        // Mount prefab layout start                         
                                        EditorGUILayout.BeginHorizontal(myPrefabLayout);
                                        {                                                                                                                              
                                            GUILayout.Label("Mount prefab:", miniLabel);
                                            GUILayout.FlexibleSpace();
                                            int oldIndex = -1;
                                            if (selectedTurret >= db.Turrets.Count)
                                            {
                                                selectedTurret = db.Turrets.Count - 1;
                                            }
                                            if (db.Turrets[selectedTurret].Mount != null)
                                            {
                                                oldIndex = FindIndex(mountPrefabNames, db.Turrets[selectedTurret].Mount.name);
                                                if (mountPrefabNames.Length > oldIndex)
                                                {
                                                    if (mountPrefabNames[oldIndex] != db.Turrets[selectedTurret].Mount.name)
                                                    {
                                                        db.Turrets[selectedTurret].Mount = null;
                                                        changed = true;
                                                        CheckForMounts();
                                                        CheckForBarrels();
                                                        UpdateMountNames();
                                                    }
                                                }
                                                else
                                                {
                                                    db.Turrets[selectedTurret].Mount = null;
                                                    changed = true;
                                                    CheckForMounts();
                                                    CheckForBarrels();
                                                    UpdateMountNames();
                                                }
                                            }
                                            else
                                            {
                                                oldIndex = FindIndex(mountPrefabNames, "None");
                                            }
                                                                                        
                                            int newIndex = EditorGUILayout.Popup(oldIndex, mountPrefabNames, "ToolbarDropDown");                                            

                                            if (newIndex != oldIndex)
                                            {
                                                if (mountPrefabNames.Length > newIndex)
                                                {
                                                    if (newIndex == -1)
                                                    {
                                                        turrets[selectedTurret].Mount = null;
                                                        changed = true;
                                                        CheckForMounts();
                                                        CheckForBarrels();
                                                        UpdateMountNames();
                                                    }
                                                    else
                                                    {
                                                        if (mountPrefabNames[newIndex] == "None")
                                                        {
                                                            turrets[selectedTurret].Mount = null;
                                                            changed = true;
                                                            CheckForMounts();
                                                            CheckForBarrels();
                                                            UpdateMountNames();
                                                        }
                                                        if (turrets[selectedTurret].Mount == null)
                                                        {
                                                            turrets[selectedTurret].Mount = GetMountByIndex(newIndex);
                                                            changed = true;
                                                            CheckForMounts();
                                                            CheckForBarrels();
                                                            UpdateMountNames();
                                                        }
                                                        else if (turrets[selectedTurret].Mount.name != mountPrefabNames[newIndex])
                                                        {
                                                            turrets[selectedTurret].Mount = GetMountByIndex(newIndex);
                                                            changed = true;
                                                            CheckForMounts();
                                                            CheckForBarrels();
                                                            UpdateMountNames();
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        EditorGUILayout.EndHorizontal();
                                        // Mount prefab layout end
                                    }
                                    else
                                    {                                        
                                        EditorGUILayout.HelpBox("Mount templates count 0!", MessageType.Warning);
                                    }
                                   
                                    if (turrets[selectedTurret].Mount != null)
                                    {
                                        // Mount prefab socket start
                                        EditorGUILayout.BeginHorizontal(myPrefabLayout);
                                        {                                           
                                            if (mountPrefixes.Length > 0)
                                            {                                                
                                                GUILayout.Label("Socket:", miniLabel);
                                                GUILayout.FlexibleSpace();
                                                int oldIndex = FindIndex(mountPrefixes, turrets[selectedTurret].MountPrefix);   

                                                int newIndex = EditorGUILayout.Popup(oldIndex, mountPrefixes, "ToolbarDropDown");

                                                if (turrets[selectedTurret].MountPrefix != mountPrefixes[newIndex])
                                                {
                                                    turrets[selectedTurret].MountPrefix = mountPrefixes[newIndex];
                                                    changed = true;
                                                    CheckForMounts();
                                                    CheckForBarrels();
                                                }
                                            }
                                            else
                                            {                                                
                                                EditorGUILayout.HelpBox("A swivel prefab has to be selected first.", MessageType.Warning);
                                            }
                                        }
                                        EditorGUILayout.EndHorizontal();
                                        // Mount prefab socket end
                                    }
                                }
                                EditorGUILayout.EndVertical();
                                // Mount prefab end
                            }

                            // Weapon sockets section
                            //////////////////////////
                            if (turrets.Count > 0 && mountNames.Length > 0)
                            {
                                // Weapon sockets foldout start
                                EditorGUILayout.BeginVertical(myPrefabLayout, GUILayout.MinHeight(20));
                                    GUILayout.Label("Weapon sockets:", "flow target in");
                                EditorGUILayout.EndVertical();
                                // Weapon sockets foldout end

                                EditorGUILayout.BeginHorizontal();
                                {
                                    if (GUILayout.Button("Select All", "toolbarbutton"))
                                    {
                                        AddAllWeaponsSelection();
                                        changed = true;
                                    }
                                    if (GUILayout.Button("Clear All", "toolbarbutton"))
                                    {
                                        ClearAllWeaponsSelection();
                                        changed = true;
                                    }
                                }
                                EditorGUILayout.EndHorizontal(); 

                                int horizontalMaxCount = 3;
                                int n = mountNames.Length;
                                int lineCount = n / horizontalMaxCount;
                                if (n % horizontalMaxCount > 0)
                                {
                                    lineCount++;
                                }
                                int curInd = 0; 
                                // Weapon sockets tag buttons start
                                EditorGUILayout.BeginVertical("Box"); 
                                {
                                    for (int j = 0; j < lineCount; j++)
                                    {
                                        // Generated row start 
                                        int curLineCount = 0;
                                        if (n - curInd > horizontalMaxCount)
                                        {
                                            curLineCount = horizontalMaxCount;
                                        }
                                        else
                                        {
                                            curLineCount = n - curInd;
                                        } 
                                        // Row start
                                        EditorGUILayout.BeginHorizontal();
                                        GUILayout.FlexibleSpace(); 
                                        for (i = 0; i < curLineCount; i++)
                                        {
                                            if (curInd >= n)
                                            {
                                                break;
                                            } 
                                            string cutedName = mountNames[curInd].Substring(db.WeaponSocket.Length, mountNames[curInd].Length - db.WeaponSocket.Length);
                                            if (db.Turrets[selectedTurret].WeaponSlotsNames.Contains(mountNames[curInd]))
                                            { 
                                                if (GUILayout.Button(cutedName, weaponTagButton, GUILayout.MaxWidth(550), GUILayout.MinWidth(50)))
                                                {
                                                    DeselectCurrentMount(mountNames[curInd]);
                                                    changed = true;
                                                }                                                        
                                            }
                                            else
                                            {
                                                GUI.contentColor = new Color(0.45f, 0.45f, 0.45f);
                                                GUI.backgroundColor = new Color(0.1f, 0.1f, 0.1f); 
                                                if (GUILayout.Button(cutedName, weaponTagButton, GUILayout.MaxWidth(550), GUILayout.MinWidth(50)))
                                                {
                                                    AddCurrentMount(mountNames[curInd]);
                                                    changed = true;
                                                }
                                                GUI.backgroundColor = baseCol;
                                                GUI.contentColor = Color.white; 
                                            }
                                            curInd++; 
                                        }
                                        GUILayout.FlexibleSpace();
                                        EditorGUILayout.EndHorizontal();
                                        // Row end 
                                    }// loop end

                                    GUI.backgroundColor = baseCol;
                                }
                                EditorGUILayout.EndVertical(); 
                                for (i = 0; i < turrets[selectedTurret].WeaponSlotsNames.Count; i++)
                                {                          
                                    // Weapon socket prefab layout start          
                                    EditorGUILayout.BeginVertical(myWeapBox, GUILayout.MaxHeight(1));
                                    {
                                        // Weapon socket name
                                        EditorGUILayout.BeginHorizontal();                                                                         
                                            GUILayout.Label(turrets[selectedTurret].WeaponSlotsNames[i], "ObjectFieldThumb");                                  
                                        EditorGUILayout.EndHorizontal(); 
                                        // Breech prefab start
                                        EditorGUILayout.BeginHorizontal(myPrefabLayout); 
                                        if (db.Breeches.Count > 0)
                                        {                                           
                                            GUILayout.Label("Breech prefab:", miniLabel);
                                            GUILayout.FlexibleSpace(); 
                                            int oldIndex = 0;
                                            if (db.Turrets[selectedTurret].WeaponBreeches == null)
                                            {
                                                if (db.Turrets[selectedTurret].WeaponSlotsNames != null)
                                                {
                                                    List<GameObject> tempBreeches = new List<GameObject>(db.Turrets[selectedTurret].WeaponSlotsNames.Count);
                                                    db.Turrets[selectedTurret].WeaponBreeches = tempBreeches;
                                                }
                                                else
                                                {
                                                    break;
                                                }
                                                }
                                                if (db.Turrets[selectedTurret].WeaponBreeches[i] != null)
                                                {
                                                    oldIndex = FindIndex(breechNames, db.Turrets[selectedTurret].WeaponBreeches[i].name);
                                                }
                                                else
                                                {
                                                    oldIndex = FindIndex(breechNames, "None");
                                                }                                                  
                                                // Breech prefab Popup
                                                int newIndex = EditorGUILayout.Popup(oldIndex, breechNames, EditorStyles.toolbarDropDown);                                                 
                                                if (newIndex != oldIndex)
                                                {
                                                    if (breechNames[newIndex] == "None")
                                                    {
                                                        turrets[selectedTurret].WeaponBreeches[i] = null;
                                                        changed = true;
                                                        CheckForMounts();
                                                        CheckForBarrels();
                                                    }
                                                    else
                                                    {
                                                        GameObject tempGO = GetBreechByIndex(newIndex);
                                                        if (turrets[selectedTurret].WeaponBreeches[i] != tempGO)
                                                        {
                                                            turrets[selectedTurret].WeaponBreeches[i] = tempGO;
                                                            changed = true;
                                                            CheckForMounts();
                                                            CheckForBarrels();
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                GUILayout.Label("Breech prefab count 0!", GUILayout.MinWidth(100));
                                            } 
                                            EditorGUILayout.EndHorizontal();
                                            // Breech prefab end
                                            
                                            // Barrel prefab start
                                            EditorGUILayout.BeginHorizontal(myPrefabLayout);
                                            {                                            
                                                if (db.Barrels.Count > 0)
                                                {
                                                    GUILayout.Label("Barrel prefab:", miniLabel);
                                                    GUILayout.FlexibleSpace();    
                                                    int oldIndex = 0;
                                                    if (db.Turrets[selectedTurret].WeaponBarrels == null)
                                                    {
                                                        if (db.Turrets[selectedTurret].WeaponSlotsNames != null)
                                                        {
                                                            List<GameObject> tempBarrels = new List<GameObject>(db.Turrets[selectedTurret].WeaponSlotsNames.Count);
                                                            db.Turrets[selectedTurret].WeaponBarrels = tempBarrels;
                                                        }
                                                        else
                                                        {
                                                            break;
                                                        }
                                                    }
                                                    if (db.Turrets[selectedTurret].WeaponBarrels[i] != null)
                                                    {
                                                        oldIndex = GetGameObjectIndex(db.Barrels, db.Turrets[selectedTurret].WeaponBarrels[i].name);
                                                        if (oldIndex < db.Barrels.Count)
                                                        {
                                                            if (db.Barrels[oldIndex] != null)
                                                            {
                                                                oldIndex = FindIndex(barrelNames, db.Barrels[oldIndex].name);
                                                            }
                                                            else
                                                            { 
                                                                oldIndex = FindIndex(barrelNames, "None");
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        oldIndex = FindIndex(barrelNames, "None");
                                                    } 
                                                    int newIndex = EditorGUILayout.Popup(oldIndex, barrelNames, EditorStyles.toolbarDropDown);
                                                    if (oldIndex != newIndex)
                                                    {
                                                        if (barrelNames[newIndex] == "None")
                                                        {
                                                            turrets[selectedTurret].WeaponBarrels[i] = null;
                                                            changed = true;
                                                            CheckForBarrels();
                                                        }
                                                        else
                                                        {
                                                            GameObject tempGO = GetBarrelByIndex(newIndex);
                                                            if (turrets[selectedTurret].WeaponBarrels[i] != tempGO)
                                                            {
                                                                turrets[selectedTurret].WeaponBarrels[i] = tempGO;
                                                                changed = true;
                                                                CheckForBarrels();
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    EditorGUILayout.HelpBox("Barrel prefab count 0!", MessageType.Warning);
                                                }
                                            }
                                            EditorGUILayout.EndHorizontal();
                                            // Barrel prefab end                                        

                                            // Barrel socket start
                                            EditorGUILayout.BeginHorizontal(myPrefabLayout);
                                            {                                            
                                                string[] namesBarrelCur = new string[0];
                                                if (turrets[selectedTurret].WeaponBreeches[i] == null)
                                                {
                                                    namesBarrelCur = FindAvailableSockets(turrets[selectedTurret].WeaponSlots[i], db.BarrelPrefix);
                                                }
                                                else
                                                {
                                                    namesBarrelCur = FindAvailableSockets(turrets[selectedTurret].WeaponBreeches[i], db.BarrelPrefix);
                                                }
                                                if (namesBarrelCur.Length > 0)
                                                {
                                                    // Barrel socket Label
                                                    GUILayout.Label("Barrel socket:", miniLabel);
                                                    GUILayout.FlexibleSpace(); 
                                                    int curIndex = FindIndex(namesBarrelCur, turrets[selectedTurret].WeaponBarrelSockets[i]);
                                                    // Barrel socket Popup
                                                    int newBarrelSocketIndex = EditorGUILayout.Popup(curIndex, namesBarrelCur, EditorStyles.toolbarDropDown);                                               
                                                    if (namesBarrelCur[newBarrelSocketIndex] != turrets[selectedTurret].WeaponBarrelSockets[i])
                                                    {
                                                        turrets[selectedTurret].WeaponBarrelSockets[i] = namesBarrelCur[newBarrelSocketIndex];
                                                        changed = true;
                                                    }
                                                }
                                            }
                                            EditorGUILayout.EndHorizontal();
                                            // Barrel socet end                                        
                                        }
                                        EditorGUILayout.EndVertical();
                                        // Weapon socket prefab layout end                                    
                                    }
                                }
                            }
                            EditorGUILayout.EndVertical();
                            // Box layout end
                        }

                        //////////////////////
                        // No items to display 
                        else
                        {
                            EditorGUILayout.BeginVertical("Box");
                            {
                                EditorGUILayout.BeginHorizontal();
                                {
                                    GUILayout.Label("Turret setup:", "flow target in");
                                }
                                EditorGUILayout.EndHorizontal();

                                EditorGUILayout.BeginHorizontal("HelpBox");
                                {
                                    EditorGUILayout.BeginVertical();
                                    {

                                        EditorGUILayout.BeginHorizontal(myBoxEmpty);
                                        {
                                            GUILayout.Label("No templates", warningLabel);
                                        }
                                        EditorGUILayout.EndHorizontal();
                                        EditorGUILayout.BeginHorizontal(myHelpBox);
                                        {
                                            GUILayout.FlexibleSpace();
                                            GUILayout.Label("Use this option box to modify any existing template", myInfoBox, GUILayout.MinWidth(150));
                                            GUILayout.FlexibleSpace();   
                                        }
                                        EditorGUILayout.EndHorizontal();
                                    } 
                                    EditorGUILayout.EndVertical();
                                }
                                EditorGUILayout.EndHorizontal();
                            }   
                            EditorGUILayout.EndVertical();
                        } // End turret count check
                    }
                    EditorGUILayout.EndVertical();
                    // Second column end
                    ////////////////////
                 
                // Thit column start
                /////////////////////
                EditorGUILayout.BeginVertical(myBoxEmpty, GUILayout.MinWidth(150), GUILayout.MaxWidth(300));
                {
                    EditorGUILayout.BeginVertical("Box");
                    {
                        EditorGUILayout.BeginVertical(myPrefabLayout);
                        {
                            GUILayout.Label("Turret prefabs:", "flow target in");   
                        }
                        EditorGUILayout.EndVertical();

                        //Bases options window
                        EditorGUILayout.BeginVertical("HelpBox");
                        {
                            //Bases showing parameter
                            EditorGUILayout.BeginHorizontal();
                            {
                                GUI.SetNextControlName("bases");
                                showBases = EditorGUILayout.Foldout(showBases, "Bases");

                                if (showBases)
                                {
                                    GUILayout.FlexibleSpace();
                                    string newCount = EditorGUILayout.TextField(currentBasesCountField, GUILayout.MaxWidth(25));
                                    int newCountVal = -1;
                                    if (newCount != currentBasesCountField && int.TryParse(newCount, out newCountVal) &&
                                        newCountVal >= 0)
                                    {
                                        lastClickChanged = true;
                                        currentBasesCountField = newCount;
                                    }
                                }
                            }
                            EditorGUILayout.EndHorizontal();

                            baseRect = GUILayoutUtility.GetLastRect();
                            if (GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                            {
                                unClickedBase = true;
                                unClickedSwivel = false;
                                unClickedHead = false;
                                unClickedMount = false;
                                unClickedBreech = false;
                                unClickedBarrel = false;
                            }

                            //Checking for base position
                            //Bases fields
                            if (showBases && db.Bases.Count > 0)
                            {
                                EditorGUILayout.BeginVertical();
                                {
                                    int n = 0;

                                    foreach (GameObject baseField in db.Bases)
                                    {
                                        GameObject oldTemp = baseField;
                                        db.Bases[n] =
                                            (GameObject)
                                                EditorGUILayout.ObjectField("", db.Bases[n], typeof (GameObject), true);
                                        if (oldTemp != db.Bases[n])
                                        {
                                            UpdateBaseCount(db.Bases.Count);
                                            changed = true;
                                        }
                                        n++;
                                    }
                                }
                                EditorGUILayout.EndVertical();
                            }
                        }
                        EditorGUILayout.EndVertical();

                        //Swivel options window
                        EditorGUILayout.BeginVertical("HelpBox");
                        {
                            //Bases showing parameter
                            EditorGUILayout.BeginHorizontal();
                            { 
                                showSwivels = EditorGUILayout.Foldout(showSwivels, "Swivels"); 
                                if (showSwivels)
                                {
                                    GUILayout.FlexibleSpace();
                                    string newCount = EditorGUILayout.TextField(currentSwivelCountField, GUILayout.MaxWidth(25));
                                    int newCountVal = -1;
                                    if (newCount != currentSwivelCountField && int.TryParse(newCount, out newCountVal) &&
                                        newCountVal >= 0)
                                    {
                                        lastClickChanged = true;
                                        currentSwivelCountField = newCount;
                                    }
                                }
                            }
                            EditorGUILayout.EndHorizontal(); 
                            swivelRect = GUILayoutUtility.GetLastRect();
                            if (GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                            {
                                unClickedBase = false;
                                unClickedSwivel = true;
                                unClickedHead = false;
                                unClickedMount = false;
                                unClickedBreech = false;
                                unClickedBarrel = false;
                            } 
                            //Checking for base position
                            //Bases fields
                            if (showSwivels && db.Swivels.Count > 0)
                            {
                                EditorGUILayout.BeginVertical();
                                {
                                    int n = 0;

                                    foreach (GameObject swivelField in db.Swivels)
                                    {
                                        GameObject oldTemp = swivelField;
                                        db.Swivels[n] =
                                            (GameObject)
                                                EditorGUILayout.ObjectField("", db.Swivels[n], typeof (GameObject), true);
                                        if (oldTemp != db.Swivels[n])
                                        {
                                            UpdateSwivelCount(db.Swivels.Count);
                                            changed = true;
                                        }
                                        n++;
                                    }
                                }
                                EditorGUILayout.EndVertical();
                            }
                        }
                        EditorGUILayout.EndVertical();

                        //Head options window
                        EditorGUILayout.BeginVertical("HelpBox");
                        {
                            //Bases showing parameter
                            EditorGUILayout.BeginHorizontal();
                            { 
                                showHeads = EditorGUILayout.Foldout(showHeads, "Heads"); 
                                if (showHeads)
                                {
                                    GUILayout.FlexibleSpace();
                                    string newCount = EditorGUILayout.TextField(currentHeadCountField, GUILayout.MaxWidth(25));
                                    int newCountVal;
                                    if (newCount != currentHeadCountField && int.TryParse(newCount, out newCountVal) &&
                                        newCountVal >= 0)
                                    {
                                        lastClickChanged = true;
                                        currentHeadCountField = newCount;
                                    }
                                }
                            }
                            EditorGUILayout.EndHorizontal(); 
                            headRect = GUILayoutUtility.GetLastRect();
                            if (GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                            {
                                unClickedBase = false;
                                unClickedSwivel = false;
                                unClickedHead = true;
                                unClickedMount = false;
                                unClickedBreech = false;
                                unClickedBarrel = false;
                            } 
                            //Checking for base position
                            //Bases fields
                            if (showHeads && db.Heads.Count > 0)
                            {
                                EditorGUILayout.BeginVertical();
                                {
                                    int n = 0;

                                    foreach (GameObject headField in db.Heads)
                                    {
                                        GameObject oldTemp = headField;
                                        db.Heads[n] =
                                            (GameObject)
                                                EditorGUILayout.ObjectField("", db.Heads[n], typeof (GameObject), true);
                                        if (oldTemp != db.Heads[n])
                                        {
                                            UpdateHeadCount(db.Heads.Count);
                                            changed = true;
                                        }
                                        n++;
                                    }
                                }
                                EditorGUILayout.EndVertical();
                            }
                        }
                        EditorGUILayout.EndVertical();

                        //Mount options window
                        EditorGUILayout.BeginVertical("HelpBox");
                        {
                            //Mount showing parameter
                            EditorGUILayout.BeginHorizontal();
                            {
                                //GUI.SetNextControlName("bases");
                                showMounts = EditorGUILayout.Foldout(showMounts, "Mounts");

                                if (showMounts)
                                {
                                    GUILayout.FlexibleSpace();
                                    string newCount = EditorGUILayout.TextField(currentMountCountField, GUILayout.MaxWidth(25));
                                    int newCountVal = -1;
                                    if (newCount != currentMountCountField && int.TryParse(newCount, out newCountVal) &&
                                        newCountVal >= 0)
                                    {
                                        lastClickChanged = true;
                                        currentMountCountField = newCount;
                                    }
                                }
                            }
                            EditorGUILayout.EndHorizontal();

                            mountRect = GUILayoutUtility.GetLastRect();
                            if (GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                            {
                                unClickedBase = false;
                                unClickedSwivel = false;
                                unClickedHead = false;
                                unClickedMount = true;
                                unClickedBreech = false;
                                unClickedBarrel = false;
                            }

                            //Checking for base position
                            //Bases fields
                            if (showMounts && db.Mounts.Count > 0)
                            {
                                EditorGUILayout.BeginVertical();
                                {
                                    int n = 0;

                                    foreach (GameObject mountField in db.Mounts)
                                    {
                                        GameObject oldTemp = mountField;
                                        db.Mounts[n] =
                                            (GameObject)
                                                EditorGUILayout.ObjectField("", db.Mounts[n], typeof (GameObject), true);
                                        if (oldTemp != db.Mounts[n])
                                        {
                                            UpdateMountCount(db.Mounts.Count);
                                            changed = true;
                                        }
                                        n++;
                                    }
                                }
                                EditorGUILayout.EndVertical();
                            }
                        }
                        EditorGUILayout.EndVertical();

                        //Breeches parameters window
                        EditorGUILayout.BeginVertical("HelpBox");
                        {
                            //Bases showing parameter
                            EditorGUILayout.BeginHorizontal();
                            {
                                GUI.SetNextControlName("breeches");
                                showBreeches = EditorGUILayout.Foldout(showBreeches, "Breeches");

                                if (showBreeches)
                                {
                                    GUILayout.FlexibleSpace();
                                    string newCount = EditorGUILayout.TextField(currentBreechesCountField, GUILayout.MaxWidth(25));
                                    int newCountVal = -1;
                                    if (newCount != currentBreechesCountField && int.TryParse(newCount, out newCountVal) &&
                                        newCountVal >= 0)
                                    {
                                        lastClickChanged = true;
                                        currentBreechesCountField = newCount;
                                    }
                                }
                            }
                            EditorGUILayout.EndHorizontal();

                            //Checking for breech position
                            brechRect = GUILayoutUtility.GetLastRect();
                            if (GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                            {
                                unClickedBase = false;
                                unClickedSwivel = false;
                                unClickedHead = false;
                                unClickedMount = false;
                                unClickedBreech = true;
                                unClickedBarrel = false;
                            }

                            //Bases fields
                            if (showBreeches && db.Breeches.Count > 0)
                            {
                                EditorGUILayout.BeginVertical();
                                {
                                    int n = 0;

                                    foreach (GameObject breechField in db.Breeches)
                                    {
                                        GameObject oldTemp = breechField;
                                        db.Breeches[n] =
                                            (GameObject)
                                                EditorGUILayout.ObjectField("", db.Breeches[n], typeof (GameObject),
                                                    true);
                                        if (oldTemp != db.Breeches[n])
                                        {
                                            UpdateBreechCount(db.Breeches.Count);
                                            changed = true;
                                        }
                                        n++;
                                    }
                                }
                                EditorGUILayout.EndVertical();
                            }
                        }
                        EditorGUILayout.EndVertical();

                        //Barrels parameters window
                        EditorGUILayout.BeginVertical("HelpBox");
                        {
                            //Bases showing parameter
                            EditorGUILayout.BeginHorizontal();
                            {
                                showBarrels = EditorGUILayout.Foldout(showBarrels, "Barrels");

                                if (showBarrels)
                                {
                                    GUILayout.FlexibleSpace();
                                    string newCount = EditorGUILayout.TextField(currentBarrelCountField, GUILayout.MaxWidth(25));
                                    int newCountVal = -1;
                                    if (newCount != currentBarrelCountField && int.TryParse(newCount, out newCountVal) &&
                                        newCountVal >= 0)
                                    {
                                        lastClickChanged = true;
                                        currentBarrelCountField = newCount;
                                    }
                                }
                            }
                            EditorGUILayout.EndHorizontal();

                            barrelRect = GUILayoutUtility.GetLastRect();
                            if (GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                            {
                                unClickedBase = false;
                                unClickedSwivel = false;
                                unClickedHead = false;
                                unClickedMount = false;
                                unClickedBreech = false;
                                unClickedBarrel = true;
                            }

                            //Bases fields
                            if (showBarrels && db.Barrels.Count > 0)
                            {
                                EditorGUILayout.BeginVertical();
                                int n = 0;

                                foreach (GameObject barrelField in db.Barrels)
                                {
                                    GameObject oldTemp = barrelField;
                                    db.Barrels[n] =
                                        (GameObject)
                                            EditorGUILayout.ObjectField("", db.Barrels[n], typeof(GameObject), true);
                                    if (oldTemp != db.Barrels[n])
                                    {
                                        UpdateBarrelCount(db.Barrels.Count);
                                        changed = true;
                                    }
                                    n++;
                                }
                                EditorGUILayout.EndVertical();
                            }
                        }
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.EndVertical();

                        EditorGUILayout.BeginVertical("Box"); 
                        // Socket options
                        EditorGUILayout.BeginVertical(myPrefabLayout);
                        {
                            GUILayout.Label("Socket search options:", "flow target in");   
                        }
                        EditorGUILayout.EndVertical();

                        // Socket options:names foldout
                        EditorGUILayout.BeginVertical("HelpBox");
                        {
                            showSwivelPrefixes = EditorGUILayout.Foldout(showSwivelPrefixes, "Prefix name");

                            //Bases fields
                            if (showSwivelPrefixes)
                            {
                                //Swivel
                                EditorGUILayout.BeginHorizontal(mySocketNamesLayout);
                                {
                                    GUILayout.Label("Swivel:", miniLabel);
                                    GUILayout.FlexibleSpace();

                                    string newMountPrefix =  EditorGUILayout.TextField(currentSwivelPrefixField);
                                    if (newMountPrefix != currentSwivelPrefixField)
                                    {
                                        lastClickChanged = true;
                                        currentSwivelPrefixField = newMountPrefix;
                                    }
                                }
                                EditorGUILayout.EndHorizontal();

                                //head
                                EditorGUILayout.BeginHorizontal(mySocketNamesLayout);
                                {
                                    GUILayout.Label("Head:", miniLabel);
                                    GUILayout.FlexibleSpace();

                                    string newMountPrefix = EditorGUILayout.TextField(currentHeadPrefixField);
                                    if (newMountPrefix != currentHeadPrefixField)
                                    {
                                        lastClickChanged = true;
                                        currentHeadPrefixField = newMountPrefix;
                                    }
                                }
                                EditorGUILayout.EndHorizontal();

                                //Mount prefix
                                EditorGUILayout.BeginHorizontal(mySocketNamesLayout);
                                {
                                    GUILayout.Label("Mount:", miniLabel);
                                    GUILayout.FlexibleSpace();

                                    string newMountPrefix = EditorGUILayout.TextField(currentMountPrefixField);
                                    if (newMountPrefix != currentMountPrefixField)
                                    {
                                        lastClickChanged = true;
                                        currentMountPrefixField = newMountPrefix;
                                    }
                                }
                                EditorGUILayout.EndHorizontal();

                                //Barrel prefix 
                                EditorGUILayout.BeginHorizontal(mySocketNamesLayout);
                                {
                                    GUILayout.Label("Barrel:", miniLabel);
                                    GUILayout.FlexibleSpace();

                                    string newBarrelPrefix = EditorGUILayout.TextField(currentBarrelPrefixField);
                                    if (newBarrelPrefix != currentBarrelPrefixField)
                                    {
                                        lastClickChanged = true;
                                        currentBarrelPrefixField = newBarrelPrefix;
                                    }
                                }
                                EditorGUILayout.EndHorizontal();

                                //WeaponSocketField
                                EditorGUILayout.BeginHorizontal(mySocketNamesLayout);
                                {
                                    GUILayout.Label("Weapon:", miniLabel);
                                    GUILayout.FlexibleSpace();

                                    string newBreechGrouper = EditorGUILayout.TextField(currentWeaponSocketField);
                                    if (newBreechGrouper != currentWeaponSocketField)
                                    {
                                        lastClickChanged = true;
                                        currentWeaponSocketField = newBreechGrouper;
                                    }
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                        EditorGUILayout.EndVertical();
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical(); // Third columt end 
            }

            // Space hack between scrollbar and last column
            EditorGUILayout.BeginVertical(GUILayout.MinWidth(15));
            {
                GUILayout.FlexibleSpace();   
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal(); // Main bg end
            EditorGUILayout.EndScrollView();

            // Starting function of applying gameobjects to array
            if (somethingDropped)
            {
                if (unClickedBase)
                {
                    SetNewTemplateBases();
                    UpdateAllVariables();
                }
                if (unClickedBreech)
                {
                    SetNewTemplateBreeches();
                    UpdateAllVariables();
                }
                if (unClickedBarrel)
                {
                    SetNewTemplateBarrels();
                    UpdateAllVariables();
                }
                if (unClickedSwivel)
                {
                    SetNewTemplateSwivels();
                    UpdateAllVariables();
                }
                if (unClickedHead)
                {
                    SetNewTemplateHeads();
                    UpdateAllVariables();
                }
                if (unClickedMount)
                {
                    SetNewTemplateMounts();
                    UpdateAllVariables();
                }
            }
            else
            {
                if (entered && lastClickChanged)
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
                    SaveDatabase();
                    LoadDatabase();
                    UpdateAllCurrentTurrels();
                }

                Rect nullRect = new Rect(0, 0, 1, 1);
               
                if (baseRect != nullRect && brechRect != nullRect && barrelRect != nullRect && swivelRect != nullRect && mountRect != nullRect && headRect != nullRect)
                {
                    if (!baseRect.Contains(Event.current.mousePosition + scrollPos) && !brechRect.Contains(Event.current.mousePosition + scrollPos) && !barrelRect.Contains(Event.current.mousePosition + scrollPos) &&
                        !swivelRect.Contains(Event.current.mousePosition + scrollPos) && !mountRect.Contains(Event.current.mousePosition + scrollPos) && !headRect.Contains(Event.current.mousePosition + scrollPos))
                    {
                        unClickedBase = false;
                        unClickedSwivel = false;
                        unClickedHead = false;
                        unClickedMount = false;
                        unClickedBreech = false;
                        unClickedBarrel = false;
                    }
                }
            }
        }

        /// <summary>
        /// Accepting selected barrels to templates
        /// </summary>
        void SetNewTemplateBarrels()
        {
            List<GameObject> tempObjects = new List<GameObject>();
            Object[] objects = DragAndDrop.objectReferences;
            if (DragAndDrop.objectReferences != null)
            {
                for (int i = 0; i < objects.Length; i++)
                {
                    GameObject addingGo = objects[i] as GameObject;
                    if (addingGo != null)
                    {
                        tempObjects.Add(addingGo);
                    }
                }
            }
            db.Barrels = tempObjects;
            SaveDatabase();
            LoadDatabase();
            UpdateAllCurrentTurrels();
            UpdateAllVariables();
        }

        /// <summary>
        /// Accepting selected bases to templates
        /// </summary>
        void SetNewTemplateBases()
        {
            List<GameObject> tempObjects = new List<GameObject>();
            Object[] objects = DragAndDrop.objectReferences;
            if (DragAndDrop.objectReferences != null)
            {
                for (int i = 0; i < objects.Length; i++)
                {
                    GameObject addingGo = objects[i] as GameObject;
                    if (addingGo != null)
                    {
                        tempObjects.Add(addingGo);
                    }
                }
            }
            db.Bases = tempObjects;
            SaveDatabase();
            LoadDatabase();
            UpdateAllCurrentTurrels();
            UpdateAllVariables();
        }

        /// <summary>
        /// Accepting selected swivels to templates
        /// </summary>
        void SetNewTemplateSwivels()
        {
            List<GameObject> tempObjects = new List<GameObject>();
            Object[] objects = DragAndDrop.objectReferences;
            if (DragAndDrop.objectReferences != null)
            {
                for (int i = 0; i < objects.Length; i++)
                {
                    GameObject addingGo = objects[i] as GameObject;
                    if (addingGo != null)
                    {
                        tempObjects.Add(addingGo);
                    }
                }
            }
            db.Swivels = tempObjects;
            SaveDatabase();
            LoadDatabase();
            UpdateAllCurrentTurrels();
            UpdateAllVariables();
        }

        /// <summary>
        /// Accepting selected heads to templates
        /// </summary>
        void SetNewTemplateHeads()
        {
            List<GameObject> tempObjects = new List<GameObject>();
            Object[] objects = DragAndDrop.objectReferences;
            if (DragAndDrop.objectReferences != null)
            {
                for (int i = 0; i < objects.Length; i++)
                {
                    GameObject addingGo = objects[i] as GameObject;
                    if (addingGo != null)
                    {
                        tempObjects.Add(addingGo);
                    }
                }
            }
            db.Heads = tempObjects;
            SaveDatabase();
            LoadDatabase();
            UpdateAllCurrentTurrels();
            UpdateAllVariables();
        }

        /// <summary>
        /// Accepting selected mount to templates
        /// </summary>
        void SetNewTemplateMounts()
        {
            List<GameObject> tempObjects = new List<GameObject>();
            Object[] objects = DragAndDrop.objectReferences;
            if (DragAndDrop.objectReferences != null)
            {
                for (int i = 0; i < objects.Length; i++)
                {
                    GameObject addingGo = objects[i] as GameObject;
                    if (addingGo != null)
                    {
                        tempObjects.Add(addingGo);
                    }
                }
            }
            db.Mounts = tempObjects;
            SaveDatabase();
            LoadDatabase();
            UpdateAllCurrentTurrels();
            UpdateAllVariables();
        }

        /// <summary>
        /// Accepting selected breeches to templates
        /// </summary>
        void SetNewTemplateBreeches()
        {
            List<GameObject> tempObjects = new List<GameObject>();
            Object[] objects = DragAndDrop.objectReferences;
            if (DragAndDrop.objectReferences != null)
            {
                for (int i = 0; i < objects.Length; i++)
                {
                    GameObject addingGo = objects[i] as GameObject;
                    if (addingGo != null)
                    {
                        tempObjects.Add(addingGo);
                    }
                }
            }
            db.Breeches = tempObjects;
            SaveDatabase();
            LoadDatabase();
            UpdateAllCurrentTurrels();
            UpdateAllVariables();
        }

        /// <summary>
        /// This function used for updating array length of BASE prefabs
        /// </summary>
        /// <param name="val">New size of array</param>
        void UpdateBaseCount(int val)
        {
            if (db.Bases.Count > val)
            {
                List<GameObject> tempList = new List<GameObject>();
                for (int i = 0; i < val; i++)
                {
                    tempList.Add(db.Bases[i]);
                }
                db.Bases = tempList;
            }
            else
            {
                List<GameObject> tempList = new List<GameObject>();
                for (int i = 0; i < val; i++)
                {
                    if (i < db.Bases.Count)
                    {
                        tempList.Add(db.Bases[i]);
                    }
                    else
                    {
                        tempList.Add(null);
                    }
                }
                db.Bases = tempList;
            }
            UpdateBaseNames();
        }

        /// <summary>
        /// This function used for updating array length of SWIVEL prefabs
        /// </summary>
        /// <param name="val">New size of array</param>
        void UpdateSwivelCount(int val)
        {
            if (db.Swivels.Count > val)
            {
                List<GameObject> tempList = new List<GameObject>();
                for (int i = 0; i < val; i++)
                {
                    tempList.Add(db.Swivels[i]);
                }
                db.Swivels = tempList;
            }
            else
            {
                List<GameObject> tempList = new List<GameObject>();
                for (int i = 0; i < val; i++)
                {
                    if (i < db.Swivels.Count)
                    {
                        tempList.Add(db.Swivels[i]);
                    }
                    else
                    {
                        tempList.Add(null);
                    }
                }
                db.Swivels = tempList;
            }
            UpdateSwivelNames();
        }

        /// <summary>
        /// This function used for updating array length of HEAD prefabs
        /// </summary>
        /// <param name="val">New size of array</param>
        void UpdateHeadCount(int val)
        {
            //Heads
            if (db.Heads.Count > val)
            {
                List<GameObject> tempList = new List<GameObject>();
                for (int i = 0; i < val; i++)
                {
                    tempList.Add(db.Heads[i]);
                }
                db.Heads = tempList;
            }
            else
            {
                List<GameObject> tempList = new List<GameObject>();
                for (int i = 0; i < val; i++)
                {
                    if (i < db.Heads.Count)
                    {
                        tempList.Add(db.Heads[i]);
                    }
                    else
                    {
                        tempList.Add(null);
                    }
                }
                db.Heads = tempList;
            }
            UpdateHeadNames();
        }

        /// <summary>
        /// This function used for updating array length of mount prefabs
        /// </summary>
        /// <param name="val">New size of array</param>
        void UpdateMountCount(int val)
        {
            //Heads
            if (db.Mounts.Count > val)
            {
                List<GameObject> tempList = new List<GameObject>();
                for (int i = 0; i < val; i++)
                {
                    tempList.Add(db.Mounts[i]);
                }
                db.Mounts = tempList;
            }
            else
            {
                List<GameObject> tempList = new List<GameObject>();
                for (int i = 0; i < val; i++)
                {
                    if (i < db.Mounts.Count)
                    {
                        tempList.Add(db.Mounts[i]);
                    }
                    else
                    {
                        tempList.Add(null);
                    }
                }
                db.Mounts = tempList;
            }
            UpdateMountNames();
        }

        /// <summary>
        /// This function updates base names of templates
        /// </summary>
        void UpdateBaseNames()
        {
            List<string> tempNames = new List<string>();
            for (int i = 0; i < db.Bases.Count; i++)
            {
                if (db.Bases[i] != null)
                {
                    tempNames.Add(db.Bases[i].name);
                }
            }
            tempNames.Sort();
            tempNames.Add("None");
            baseNames = tempNames.ToArray();

        }

        /// <summary>
        /// This function updated names of swivels
        /// </summary>
        void UpdateSwivelNames()
        {
            List<string> tempNames = new List<string>();
            for (int i = 0; i < db.Swivels.Count; i++)
            {
                if (db.Swivels[i] != null)
                {
                    tempNames.Add(db.Swivels[i].name);
                }
            }
            tempNames.Sort();
            tempNames.Add("None");
            swivelNames = tempNames.ToArray();
        }

        /// <summary>
        /// This function updated names of swivels
        /// </summary>
        void UpdateHeadNames()
        {

            List<string> tempNames = new List<string>();
            for (int i = 0; i < db.Heads.Count; i++)
            {
                if (db.Heads[i] != null)
                {
                    tempNames.Add(db.Heads[i].name);
                }
            }
            tempNames.Sort();
            tempNames.Add("None");
            headNames = tempNames.ToArray();
        }

        /// <summary>
        /// This function updated names of mount
        /// </summary>
        void UpdateMountNames()
        {
            List<string> tempNames = new List<string>();
            for (int i = 0; i < db.Mounts.Count; i++)
            {
                if (db.Mounts[i] != null)
                {
                    tempNames.Add(db.Mounts[i].name);
                }
            }
            tempNames.Sort();
            tempNames.Add("None");
            mountPrefabNames = tempNames.ToArray();
        }

        /// <summary>
        /// This function used for updating array length of breech prefabs
        /// </summary>
        /// <param name="val">New size of array</param>
        void UpdateBreechCount(int val)
        {
            if (db.Breeches.Count > val)
            {
                List<GameObject> tempList = new List<GameObject>();
                for (int i = 0; i < val; i++)
                {
                    tempList.Add(db.Breeches[i]);
                }
                db.Breeches = tempList;
            }
            else
            {
                List<GameObject> tempList = new List<GameObject>();
                for (int i = 0; i < val; i++)
                {
                    if (i < db.Breeches.Count)
                    {
                        tempList.Add(db.Breeches[i]);
                    }
                    else
                    {
                        tempList.Add(null);
                    }
                }
                db.Breeches = tempList;
            }
            UpdateBreechNames();
        }

        // Update strings
        void ApplyChanges()
        {
            // Base count
            if (turrets.Count > 0)
            { 
                turrets[selectedTurret].Name = currentNameField;
            }
            int newCountNumer = 0;
            if (int.TryParse(currentBasesCountField, out newCountNumer))
            {
                UpdateBaseCount(newCountNumer);
                UpdateBaseNames();
            }
            else
            {
                currentBasesCountField = db.Bases.Count.ToString();
            }
            //Swivel count
            newCountNumer = 0;
            if (int.TryParse(currentSwivelCountField, out newCountNumer))
            {
                UpdateSwivelCount(newCountNumer);
                UpdateSwivelNames();
            }
            else
            {
                currentSwivelCountField = db.Swivels.Count.ToString();
            }
            //Head count
            newCountNumer = 0;
            if (int.TryParse(currentHeadCountField, out newCountNumer))
            {
                UpdateHeadCount(newCountNumer);
                UpdateHeadNames();
            }
            else
            {
                currentHeadCountField = db.Heads.Count.ToString();
            }
            //Mount count
            newCountNumer = 0;
            if (int.TryParse(currentMountCountField, out newCountNumer))
            {
                UpdateMountCount(newCountNumer);
                UpdateMountNames();
            }
            else
            {
                currentMountCountField = db.Mounts.Count.ToString();
            }
            // Breeches count
            newCountNumer = 0;
            if (int.TryParse(currentBreechesCountField, out newCountNumer))
            {
                UpdateBreechCount(newCountNumer);
                UpdateBreechNames();
            }
            else
            {
                currentBreechesCountField = db.Breeches.Count.ToString();
            }
            // Barrels count
            newCountNumer = 0;
            if (int.TryParse(currentBarrelCountField, out newCountNumer))
            {
                UpdateBarrelCount(newCountNumer);
                UpdateBarrelNames();
            }
            else
            {
                currentBarrelCountField = db.Barrels.Count.ToString();
            }
            db.SwivelPrefix = currentSwivelPrefixField;
            UpdateSwivelNames();
            db.HeadPrefix = currentHeadPrefixField;
            UpdateHeadNames();
            db.MountPrefix = currentMountPrefixField;
            UpdateHeadPrefixNames();
            UpdateSwivelPrefixNames();
            UpdateMountPrefixNames();
            db.BarrelPrefix = currentBarrelPrefixField;
            UpdateBarrelPrefixNames();
            db.WeaponSocket = currentWeaponSocketField;
            UpdateWeaponPrefixNames();

            SaveDatabase();
            LoadDatabase();
            InstallAll();
            CheckForMounts();
            CheckForBarrels();
            UpdateAllVariables();
        }

        /// <summary>
        /// This function updates breech names from templates
        /// </summary>
        void UpdateBreechNames()
        {
            List<string> tempNames = new List<string>();
            for (int i = 0; i < db.Breeches.Count; i++)
            {
                if (db.Breeches[i] != null)
                {
                    tempNames.Add(db.Breeches[i].name);
                }
            }
            tempNames.Sort();
            tempNames.Add("None");
            breechNames = tempNames.ToArray();
        }

        /// <summary>
        /// This function used for updating array length of BARREL prefabs
        /// </summary>
        /// <param name="val">New size of array</param>
        void UpdateBarrelCount(int val)
        {
            if (db.Barrels.Count > val)
            {
                List<GameObject> tempList = new List<GameObject>();
                for (int i = 0; i < val; i++)
                {
                    tempList.Add(db.Barrels[i]);
                }
                db.Barrels = tempList;
            }
            else
            {
                List<GameObject> tempList = new List<GameObject>();
                for (int i = 0; i < val; i++)
                {
                    if (i < db.Barrels.Count)
                    {
                        tempList.Add(db.Barrels[i]);
                    }
                    else
                    {
                        tempList.Add(null);
                    }
                }
                db.Barrels = tempList;
            }
            UpdateBarrelNames();
        }

        /// <summary>
        /// This function updates barrel names from templates
        /// </summary>
        void UpdateBarrelNames()
        {

            List<string> tempNames = new List<string>();
            for (int i = 0; i < db.Barrels.Count; i++)
            {
                if (db.Barrels[i] != null)
                {
                    tempNames.Add(db.Barrels[i].name);
                }
            }
            tempNames.Sort();
            tempNames.Add("None");
            barrelNames = tempNames.ToArray();
        }

        /// <summary>
        /// This function updates swivel prefix names from templates
        /// </summary>
        void UpdateSwivelPrefixNames()
        {
            List<string> tempNames = new List<string>();
            if (db.Turrets.Count == 0)
                return;
            if (selectedTurret >= turrets.Count)
            {
                selectedTurret = turrets.Count - 1;
            }
            if (turrets[selectedTurret].Base != null)
            {
                Transform[] childrens = db.Turrets[selectedTurret].Base.GetComponentsInChildren<Transform>();
                string prefix = db.SwivelPrefix;
                foreach (Transform child in childrens)
                {
                    if (child.name.Length >= prefix.Length)
                    {
                        string checkingPart = child.name.Substring(0, prefix.Length);
                        if (checkingPart == prefix)
                        {
                            tempNames.Add(child.name);
                        }
                    }
                }
                tempNames.Sort();
                swivelPrefixes = tempNames.ToArray();
            }
            else
            {
                tempNames.Add("Parent position");
                swivelPrefixes = tempNames.ToArray();
            }
        }

        /// <summary>
        /// This function updates head prefix names from templates
        /// </summary>
        void UpdateHeadPrefixNames()
        {
            List<string> tempNames = new List<string>();
            if (db.Turrets.Count == 0)
                return;
            if (db.Turrets[selectedTurret].Base == null)
            {
                if (db.Turrets[selectedTurret].Swivel != null)
                {
                    Transform[] childrens = db.Turrets[selectedTurret].Swivel.GetComponentsInChildren<Transform>();
                    string prefix = db.HeadPrefix;
                    foreach (Transform child in childrens)
                    {
                        if (child.name.Length >= prefix.Length)
                        {
                            string checkingPart = child.name.Substring(0, prefix.Length);
                            if (checkingPart == prefix)
                            {
                                tempNames.Add(child.name);
                            }
                        }
                    }
                    //tempNames.Add(db.HeadPrefix);
                    tempNames.Sort();
                    headPrefixes = tempNames.ToArray();
                }
                else
                {
                    tempNames.Sort();
                    headPrefixes = tempNames.ToArray();
                }
            }
            else
            {
                Transform[] childrens = db.Turrets[selectedTurret].Base.GetComponentsInChildren<Transform>();
                string prefix = db.HeadPrefix;
                foreach (Transform child in childrens)
                {
                    if (child.name.Length >= prefix.Length)
                    {
                        string checkingPart = child.name.Substring(0, prefix.Length);
                        if (checkingPart == prefix)
                        {
                            tempNames.Add(child.name);
                        }
                    }
                }
                if (db.Turrets[selectedTurret].Swivel != null)
                {
                    childrens = db.Turrets[selectedTurret].Swivel.GetComponentsInChildren<Transform>();
                    foreach (Transform child in childrens)
                    {
                        if (child.name.Length >= prefix.Length)
                        {
                            string checkingPart = child.name.Substring(0, prefix.Length);
                            if (checkingPart == prefix)
                            {
                                tempNames.Add(child.name);
                            }
                        }
                    }
                } 
                tempNames.Sort();
                headPrefixes = tempNames.ToArray();
            }
        }

        /// <summary>
        /// This function updates mount prefix names from templates
        /// </summary>
        void UpdateMountPrefixNames()
        {
            List<string> tempNames = new List<string>();
            if (db.Turrets.Count == 0)
                return;
            if (db.Turrets[selectedTurret].Base == null)
            {
                if (db.Turrets[selectedTurret].Swivel != null)
                {
                    Transform[] childrens = db.Turrets[selectedTurret].Swivel.GetComponentsInChildren<Transform>();
                    string prefix = db.MountPrefix;
                    foreach (Transform child in childrens)
                    {
                        if (child.name.Length >= prefix.Length)
                        {
                            string checkingPart = child.name.Substring(0, prefix.Length);
                            if (checkingPart == prefix)
                            {
                                tempNames.Add(child.name);
                            }
                        }
                    } 
                    tempNames.Sort();
                    mountPrefixes = tempNames.ToArray();
                }
                else
                {
                    tempNames.Sort();
                    tempNames.Add("Parent position");
                    mountPrefixes = tempNames.ToArray();
                }
            }
            else
            {
                Transform[] childrens = db.Turrets[selectedTurret].Base.GetComponentsInChildren<Transform>();
                string prefix = db.MountPrefix;
                foreach (Transform child in childrens)
                {
                    if (child.name.Length >= prefix.Length)
                    {
                        string checkingPart = child.name.Substring(0, prefix.Length);
                        if (checkingPart == prefix)
                        {
                            tempNames.Add(child.name);
                        }
                    }
                }

                if (db.Turrets[selectedTurret].Swivel != null)
                {
                    childrens = db.Turrets[selectedTurret].Swivel.GetComponentsInChildren<Transform>();
                    foreach (Transform child in childrens)
                    {
                        if (child.name.Length >= prefix.Length)
                        {
                            string checkingPart = child.name.Substring(0, prefix.Length);
                            if (checkingPart == prefix)
                            {
                                tempNames.Add(child.name);
                            }
                        }
                    }
                } 
                tempNames.Sort();
                mountPrefixes = tempNames.ToArray();
            }
        }

        /// <summary>
        /// This function updates barrel prefix names from templates
        /// </summary>
        void UpdateBarrelPrefixNames()
        {
            List<string> tempNames = new List<string>();
            tempNames.Add(db.BarrelPrefix);
        }

        /// <summary>
        /// This function updates breech socket prefixes from templates(group prefixes)
        /// </summary>
        void UpdateWeaponPrefixNames()
        {
            List<string> tempNames = new List<string>();
            tempNames.Add(db.WeaponSocket);
        }

        /// <summary>
        /// This function used to add new turret with default name
        /// </summary>
        void AddNewTurret()
        {
            F3DTurretScriptable tempDB = db;
            TurretStructure newTurret = new TurretStructure();
            if (tempDB.Turrets == null)
            {
                tempDB.Turrets = new List<TurretStructure>();
            }
            selectedTurret = turrets.Count - 1;
            if (currentNameField != "")
                newTurret.Name = currentNameField;

            string newName = newTurret.Name;

            newName = GetAvailableName(newName, tempDB.Turrets.ToArray());
            newTurret.Name = newName;
            currentNameField = newName;
            tempDB.Turrets.Add(newTurret);
            if (turrets.Count > 0)
                selectedTurret = turrets.Count - 1;
            tempDB.SelectedTurret = selectedTurret;
            turrets = tempDB.Turrets;
            GUI.FocusControl("");
            SaveDatabase();
            LoadDatabase();
            UpdateAllTemplatesOnEditors();

            UpdateAllVariables();
            CheckForMounts();
            CheckForBarrels();
        }

        void ClearMountNames()
        {
            mountNames = new string[0];
        }

        /// <summary>
        /// This function used for getting available name from existing
        /// </summary>
        /// <param name="namer">Name  that have to be checked</param>
        /// <param name="structures">Turret structures with names</param>
        /// <returns>Returns not occupied name</returns>
        string GetAvailableName(string namer, TurretStructure[] structures)
        {
            string newName = namer; 
            int toCut = 0;
            //This turret have numerator, so we have to delete it
            if (newName[newName.Length - 1] == ')')
            {
                toCut++;
                while (newName[newName.Length - 1 - toCut] != '(' && toCut < newName.Length)
                {
                    toCut++;
                }
                toCut++;
                if (toCut > 0)
                {
                    newName = newName.Substring(0, newName.Length - 1 - toCut);
                }
            }

            string[] names = new string[structures.Length];

            int i;
            for (i = 0; i < names.Length; i++)
            {
                names[i] = structures[i].Name;
            }

            if (CheckForName(names, namer))
            {
                int numerator = 0;
                while (CheckForName(names, newName + " (" + numerator + ")"))
                {
                    numerator++;
                }
                newName = newName + " (" + numerator + ")";
            }
            else
            { 
                return namer;
            } 
            return newName;
        }

        /// <summary>
        /// Used for adding new turret with current turret's options
        /// </summary>
        void DuplicateTurret()
        {
            F3DTurretScriptable tempDB = db;
            TurretStructure newTurret = new TurretStructure();
            if (tempDB.Turrets == null)
            {
                tempDB.Turrets = new List<TurretStructure>();
            }
            if (tempDB.Turrets.Count == 0 || tempDB.Turrets.Count < selectedTurret)
                return;
            TurretStructure selectedTurretStructure = tempDB.Turrets[selectedTurret];

            newTurret.Name = GetAvailableName(selectedTurretStructure.Name, tempDB.Turrets.ToArray());
            newTurret.Base = selectedTurretStructure.Base;
            newTurret.Swivel = selectedTurretStructure.Swivel;
            newTurret.SwivelPrefix = selectedTurretStructure.SwivelPrefix;
            newTurret.Head = selectedTurretStructure.Head;
            newTurret.HeadPrefix = selectedTurretStructure.HeadPrefix;
            newTurret.Mount = selectedTurretStructure.Mount;
            newTurret.MountPrefix = selectedTurretStructure.MountPrefix;

            newTurret.HasTurretScript = selectedTurretStructure.HasTurretScript;
            newTurret.NeedLOD = selectedTurretStructure.NeedLOD;

            newTurret.WeaponBarrels = new List<GameObject>(); 
            newTurret.WeaponBarrelSockets = new List<string>(); 
            newTurret.WeaponBreeches = new List<GameObject>(); 
            newTurret.WeaponSlots = new List<GameObject>();  
            newTurret.WeaponSlotsNames = new List<string>(); 
            for (int i = 0; i < selectedTurretStructure.WeaponSlotsNames.Count; i++)
            {
                newTurret.WeaponSlotsNames.Add(selectedTurretStructure.WeaponSlotsNames[i]);
            }
            for (int i = 0; i < selectedTurretStructure.WeaponSlots.Count; i++)
            {
                newTurret.WeaponSlots.Add(selectedTurretStructure.WeaponSlots[i]);
            }
            for (int i = 0; i < selectedTurretStructure.WeaponBreeches.Count; i++)
            {
                newTurret.WeaponBreeches.Add(selectedTurretStructure.WeaponBreeches[i]);
            }
            for (int i = 0; i < selectedTurretStructure.WeaponBarrelSockets.Count; i++)
            {
                newTurret.WeaponBarrelSockets.Add(selectedTurretStructure.WeaponBarrelSockets[i]);
            }
            for (int i = 0; i < selectedTurretStructure.WeaponBarrels.Count; i++)
            {
                newTurret.WeaponBarrels.Add(selectedTurretStructure.WeaponBarrels[i]);
            }
            tempDB.Turrets.Add(newTurret);
            currentNameField = newTurret.Name;
            turrets = tempDB.Turrets; 
            tempDB.SelectedTurret = selectedTurret;
            if (turrets.Count > 0)
                selectedTurret = turrets.Count - 1;
            tempDB.SelectedTurret = selectedTurret;
            SaveDatabase();
            LoadDatabase();
            UpdateAllTemplatesOnEditors();


            UpdateAllVariables();
            CheckForMounts();
            CheckForBarrels();
        }

        /// <summary>
        /// This function returns index of gameobject in list by it's name
        /// </summary>
        /// <param name="array">Array of gameobjects</param>
        /// <param name="selectedGO">Name of looking gameobject</param>
        /// <returns>Index of gameobject in array</returns>
        int GetGameObjectIndex(List<GameObject> array, string selectedGO)
        {
            int i;
            for (i = 0; i < array.Count; i++)
            {
                if (array[i] != null)
                {
                    if (array[i].name == selectedGO)
                    {
                        return i;
                    }
                }
            }
            return 0;
        }

        /// <summary>
        /// Returns base by index from popup-style array. It considers same names and work correctly
        /// </summary>
        /// <param name="index">Index from popup menu</param>
        /// <returns>Selected prefab</returns>
        GameObject GetBaseByIndex(int index)
        {
            if (baseNames.Length < index)
            {
                return null;
            }
            if (baseNames[index] == "None")
            {
                return null;
            }
            if (index < 0 || index > baseNames.Length)
            {
                return null;
            }
            string needName = baseNames[index];
            for (int i = 0; i < db.Bases.Count; i++)
            {
                if (db.Bases[i] != null)
                {
                    if (db.Bases[i].name == needName)
                    {
                        return db.Bases[i];
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Returns swivel by index from popup-style array. It considers same names and work correctly
        /// </summary>
        /// <param name="index">Index from popup menu</param>
        /// <returns>Selected prefab</returns>
        GameObject GetSwivelByIndex(int index)
        {
            if (swivelNames.Length < index)
            {
                return null;
            }
            if (swivelNames[index] == "None")
            {
                return null;
            }
            if (index < 0 || index > swivelNames.Length)
            {
                return null;
            }
            string needName = swivelNames[index];
            for (int i = 0; i < db.Swivels.Count; i++)
            {
                if (db.Swivels[i] != null)
                {
                    if (db.Swivels[i].name == needName)
                    {
                        return db.Swivels[i];
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Returns head by index from popup-style array. It considers same names and work correctly
        /// </summary>
        /// <param name="index">Index from popup menu</param>
        /// <returns>Selected prefab</returns>
        GameObject GetHeadByIndex(int index)
        {
            if (headNames.Length < index)
            {
                return null;
            }
            if (headNames[index] == "None")
            {
                return null;
            }
            if (index < 0 || index > headNames.Length)
            {
                return null;
            }
            string needName = headNames[index];
            for (int i = 0; i < db.Heads.Count; i++)
            {
                if (db.Heads[i] != null)
                {
                    if (db.Heads[i].name == needName)
                    {
                        return db.Heads[i];
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Returns mount by index from popup-style array. It considers same names and work correctly
        /// </summary>
        /// <param name="index">Index from popup menu</param>
        /// <returns>Selected prefab</returns>
        GameObject GetMountByIndex(int index)
        {
            if (mountPrefabNames.Length < index)
            {
                return null;
            }
            if (mountPrefabNames[index] == "None")
            {
                return null;
            } 
            if (index < 0 || index > mountPrefabNames.Length)
            {
                return null;
            }
            string needName = mountPrefabNames[index];
            for (int i = 0; i < db.Mounts.Count; i++)
            {
                if (db.Mounts[i] != null)
                {
                    if (db.Mounts[i].name == needName)
                    {
                        return db.Mounts[i];
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Returns breech by index from popup-style array. It considers same names and work correctly
        /// </summary>
        /// <param name="index">Index from popup menu</param>
        /// <returns>Selected preafab</returns>
        GameObject GetBreechByIndex(int index)
        { 
            if (breechNames.Length < index)
            {
                return null;
            }
            if (breechNames.Length <= index)
                return null;
            string needName = breechNames[index];
            for (int i = 0; i < db.Breeches.Count; i++)
            {
                if (db.Breeches[i] != null)
                {
                    if (db.Breeches[i].name == needName)
                    {
                        return db.Breeches[i];
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Returns barrelby index from popup-style array. It considers same names and work correctly
        /// </summary>
        /// <param name="index">Index from popup menu</param>
        /// <returns>Selected prefab</returns>
        GameObject GetBarrelByIndex(int index)
        {
            if (barrelNames.Length <= index)
            {
                return null;
            }
            if (barrelNames.Length > index)
            {
                string needName = barrelNames[index];
                for (int i = 0; i < db.Barrels.Count; i++)
                {
                    if (db.Barrels[i] != null)
                    {
                        if (db.Barrels[i].name == needName)
                        {
                            return db.Barrels[i];
                        }
                    }
                }
            }
            else
            {
                return null;
            }
            return null;
        }

        /// <summary>
        /// This function used for finding name position in array
        /// </summary>
        /// <param name="names">Array of names</param>
        /// <param name="name">Name that looking for</param>
        /// <returns>Name position</returns>
        int FindIndex(string[] names, string name)
        {
            int i;
            for (i = 0; i < names.Length; i++)
            {
                if (name == names[i])
                {
                    return i;
                }
            }
            return 0;
        }

        /// <summary>
        /// This function used for deleting existing turret 
        /// </summary>
        void DeleteCurrentTurret()
        {
            F3DTurretScriptable tempDB = db; 
            F3DTurretConstructor[] editors = GameObject.FindObjectsOfType<F3DTurretConstructor>();
            int i;
            for (i = 0; i < editors.Length; i++)
            {
                if (editors[i].GetSelectedType() == selectedTurret)
                {
                    editors[i].UnlinkGameObject();
                }
            }

            if (tempDB.Turrets.Count > 0 && tempDB.Turrets.Count > selectedTurret)
            {
                tempDB.Turrets.RemoveAt(selectedTurret);
                turrets = tempDB.Turrets;
                selectedTurret--;
                if (selectedTurret < 0)
                {
                    selectedTurret = 0;
                }
                tempDB.SelectedTurret = selectedTurret;
            }
            else
            {
                selectedTurret = 0;
                tempDB.SelectedTurret = 0; 
            }
            if (selectedTurret >= tempDB.Turrets.Count)
            {
                selectedTurret = tempDB.Turrets.Count - 1;
            } 
            if (turrets.Count > selectedTurret && turrets.Count > 0)
            {
                currentNameField = turrets[selectedTurret].Name;
            }
            else
            {
                currentNameField = "";
            }
            SaveDatabase();
            LoadDatabase();
            UpdateAllTemplatesOnEditors();
            UpdateAllVariables();
            CheckForMounts();
            CheckForBarrels();
        }

        void DeleteTurret(int val)
        {
            F3DTurretScriptable tempDB = db; 
            F3DTurretConstructor[] editors = GameObject.FindObjectsOfType<F3DTurretConstructor>();
            int i;
            for (i = 0; i < editors.Length; i++)
            {
                if (editors[i].GetSelectedType() == val)
                {
                    editors[i].UnlinkGameObject();
                }
            } 
            if (tempDB.Turrets.Count > 0 && tempDB.Turrets.Count > val)
            {
                tempDB.Turrets.RemoveAt(val);
                turrets = tempDB.Turrets;
                if (selectedTurret < 0)
                {
                    val = 0;
                }
                tempDB.SelectedTurret = val;
            }
            else
            {
                selectedTurret = 0;
                tempDB.SelectedTurret = 0;
            }
            if (turrets.Count >= selectedTurret && turrets.Count > 0)
            {
                selectedTurret = tempDB.Turrets.Count - 1;
            }
            if (turrets.Count > selectedTurret)
            {
                currentNameField = turrets[selectedTurret].Name;
            }
            else
            {
                currentNameField = "";
            }
            SaveDatabase();
            LoadDatabase();
            UpdateAllTemplatesOnEditors();
            UpdateAllVariables();
            CheckForMounts();
            CheckForBarrels();
        }

        /// <summary>
        /// Function adds selected turret to scene in (0,0,0) or in the same position, as selected gameobject
        /// </summary>
        void AddToScene()
        {
            if (turrets.Count > 0)
            {
                if (turrets[selectedTurret] != null)
                {
                    if (AssetDatabase.LoadAssetAtPath("Assets/FORGE3D/Sci-Fi Effects/Turrets/Prefabs/ConstructorBase.prefab", typeof(GameObject)))
                    { 
                        GameObject loading = AssetDatabase.LoadAssetAtPath("Assets/FORGE3D/Sci-Fi Effects/Turrets/Prefabs/ConstructorBase.prefab", typeof(GameObject)) as GameObject; 
                        GameObject newGO = Instantiate(loading, Vector3.zero, Quaternion.identity) as GameObject;
                        newGO.GetComponent<F3DTurretConstructor>().turretIndex = selectedTurret;
                        newGO.name = turrets[selectedTurret].Name;
                        if (Selection.activeGameObject != null)
                        {
                            newGO.transform.position = Selection.activeGameObject.transform.position;
                            newGO.transform.rotation = Selection.activeGameObject.transform.rotation;
                            Selection.activeGameObject = newGO;
                        }
                        else
                        {
                            Selection.activeGameObject = newGO;
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Not found turret template! It have to be in:Assets/FORGE3D/Sci-Fi Effects/Turrets/Prefabs/ConstructorBase.prefab");
                    } 
                }
            }
        }

        /// <summary>
        /// This function updates available mounts by prefix(parsing current structure)
        /// </summary>
        void CheckForMounts()
        {
            List<string> foundNames = new List<string>();
            List<GameObject> foundGO = new List<GameObject>();

            if (db.Turrets.Count == 0)
                return;
            if (selectedTurret >= turrets.Count)
            {
                selectedTurret = turrets.Count - 1;
            }
            if (turrets[selectedTurret].Base != null)
            {
                Transform[] childrens = turrets[selectedTurret].Base.GetComponentsInChildren<Transform>();
                string loockingPrefix = db.WeaponSocket;
                foreach (Transform child in childrens)
                {
                    if (child.name.Length >= loockingPrefix.Length)
                    {
                        string cuted = child.name.Substring(0, loockingPrefix.Length);
                        if (cuted == loockingPrefix)
                        {
                            foundNames.Add(child.name);
                            foundGO.Add(child.gameObject);
                        }
                    }
                }
            }

            if (turrets[selectedTurret].Swivel != null)
            {
                Transform[] childrens = turrets[selectedTurret].Swivel.GetComponentsInChildren<Transform>();
                string loockingPrefix = db.WeaponSocket;
                foreach (Transform child in childrens)
                {
                    if (child.name.Length >= loockingPrefix.Length)
                    {
                        string cuted = child.name.Substring(0, loockingPrefix.Length);
                        if (cuted == loockingPrefix)
                        {
                            foundNames.Add(child.name);
                            foundGO.Add(child.gameObject);
                        }
                    }
                }
            }

            if (turrets[selectedTurret].Head != null)
            {
                Transform[] childrens = turrets[selectedTurret].Head.GetComponentsInChildren<Transform>();
                string loockingPrefix = db.WeaponSocket;
                foreach (Transform child in childrens)
                {
                    if (child.name.Length >= loockingPrefix.Length)
                    {
                        string cuted = child.name.Substring(0, loockingPrefix.Length);
                        if (cuted == loockingPrefix)
                        {
                            foundNames.Add(child.name);
                            foundGO.Add(child.gameObject);
                        }
                    }
                }
            }

            if (turrets[selectedTurret].Mount != null)
            {
                Transform[] childrens = turrets[selectedTurret].Mount.GetComponentsInChildren<Transform>();
                string loockingPrefix = db.WeaponSocket;
                foreach (Transform child in childrens)
                {
                    if (child.name.Length >= loockingPrefix.Length)
                    {
                        string cuted = child.name.Substring(0, loockingPrefix.Length);
                        if (cuted == loockingPrefix)
                        {
                            foundNames.Add(child.name);
                            foundGO.Add(child.gameObject);
                        }
                    }
                }
            }
            mountNames = foundNames.ToArray();
            CheckForSavedMounts();
        }

        /// <summary>
        /// This cheking saved mount and updates it's state
        /// </summary>
        void CheckForSavedMounts()
        {
            int i, j;
            List<string> haveToDeleteIndex = new List<string>();

            for (i = 0; i < db.Turrets[selectedTurret].WeaponSlotsNames.Count; i++)
            {
                bool found = false;
                for (j = 0; j < mountNames.Length; j++)
                {
                    if (mountNames[j] == db.Turrets[selectedTurret].WeaponSlotsNames[i])
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    haveToDeleteIndex.Add(db.Turrets[selectedTurret].WeaponSlotsNames[i]);
                }
            }

            for (i = 0; i < haveToDeleteIndex.Count; i++)
            {
                db.Turrets[selectedTurret].WeaponSlotsNames.Remove(haveToDeleteIndex[i]);
            }

            if (haveToDeleteIndex.Count > 0)
            {
                LoadDatabase();
                UpdateAllVariables();
                if (installedStart)
                {
                    installedStart = true;
                }
            }
        }

        /// <summary>
        /// Use this function for deleting current mount from selected
        /// </summary>
        /// <param name="value"></param>
        void DeselectCurrentMount(string value)
        {
            int deletingPosition = -1;
            if (db.Turrets[selectedTurret].WeaponSlotsNames.Contains(value))
            {
                deletingPosition = db.Turrets[selectedTurret].WeaponSlotsNames.IndexOf(value);
                db.Turrets[selectedTurret].WeaponSlotsNames.RemoveAt(deletingPosition);
                db.Turrets[selectedTurret].WeaponSlots.RemoveAt(deletingPosition);
                db.Turrets[selectedTurret].WeaponBreeches.RemoveAt(deletingPosition);
                db.Turrets[selectedTurret].WeaponBarrelSockets.RemoveAt(deletingPosition);
                db.Turrets[selectedTurret].WeaponBarrels.RemoveAt(deletingPosition);
            }
        }

        /// <summary>
        /// Use this function for find gameobject in a lot of prefabs 
        /// </summary>
        /// <param name="name">Name of finding object</param>
        /// <param name="prefab1">prefab1</param>
        /// <param name="prefab2">prefab2</param>
        /// <param name="prefab3">prefab3</param>
        /// <param name="prefab4">prefab4</param>
        /// <param name="prefab5">prefab5</param>
        /// <param name="prefab6">prefab6</param>
        /// <returns></returns>
        GameObject FindGameObjectInPrefab(string name, GameObject prefab1, GameObject prefab2, GameObject prefab3, GameObject prefab4, GameObject prefab5, GameObject prefab6)
        {
            Transform[] transforms;
            if (prefab1 != null)
            {
                transforms = prefab1.GetComponentsInChildren<Transform>();
                foreach (Transform child in transforms)
                {
                    if (child.name == name)
                    {
                        return child.gameObject;
                    }
                }
            }

            if (prefab2 != null)
            {
                transforms = prefab2.GetComponentsInChildren<Transform>();
                foreach (Transform child in transforms)
                {
                    if (child.name == name)
                    {
                        return child.gameObject;
                    }
                }
            }

            if (prefab3 != null)
            {
                transforms = prefab3.GetComponentsInChildren<Transform>();
                foreach (Transform child in transforms)
                {
                    if (child.name == name)
                    {
                        return child.gameObject;
                    }
                }
            }

            if (prefab4 != null)
            {
                transforms = prefab4.GetComponentsInChildren<Transform>();
                foreach (Transform child in transforms)
                {
                    if (child.name == name)
                    {
                        return child.gameObject;
                    }
                }
            }

            if (prefab5 != null)
            {
                transforms = prefab5.GetComponentsInChildren<Transform>();
                foreach (Transform child in transforms)
                {
                    if (child.name == name)
                    {
                        return child.gameObject;
                    }
                }
            }

            if (prefab6 != null)
            {
                transforms = prefab6.GetComponentsInChildren<Transform>();
                foreach (Transform child in transforms)
                {
                    if (child.name == name)
                    {
                        return child.gameObject;
                    }
                }
            } 
            return null;
        }

        /// <summary>
        /// Use this function for adding weapon to current weapons
        /// </summary>
        /// <param name="value">Name of adding weapon</param>
        void AddCurrentMount(string value)
        {
            if (!db.Turrets[selectedTurret].WeaponSlotsNames.Contains(value))
            {
                db.Turrets[selectedTurret].WeaponSlotsNames.Add(value);
                db.Turrets[selectedTurret].WeaponSlots.Add(FindGameObjectInPrefab(value, db.Turrets[selectedTurret].Base, db.Turrets[selectedTurret].Swivel, db.Turrets[selectedTurret].Head, db.Turrets[selectedTurret].Mount, null, null));
                db.Turrets[selectedTurret].WeaponBreeches.Add(null);
                db.Turrets[selectedTurret].WeaponBarrelSockets.Add("");
                db.Turrets[selectedTurret].WeaponBarrels.Add(null);
            }
            SaveDatabase();
        }


        /// <summary>
        /// Use this function for selection all weapons
        /// </summary>
        void AddAllWeaponsSelection()
        {
            List<string> foundNames = new List<string>();
            List<GameObject> foundGO = new List<GameObject>();

            if (turrets[selectedTurret].Base != null)
            {
                Transform[] childrens = turrets[selectedTurret].Base.GetComponentsInChildren<Transform>();
                string loockingPrefix = db.WeaponSocket;
                foreach (Transform child in childrens)
                {
                    if (child.name.Length >= loockingPrefix.Length)
                    {
                        string cuted = child.name.Substring(0, loockingPrefix.Length);
                        if (cuted == loockingPrefix)
                        {
                            foundNames.Add(child.name);
                            foundGO.Add(child.gameObject);
                        }
                    }
                }
            }

            if (turrets[selectedTurret].Swivel != null)
            {
                Transform[] childrens = turrets[selectedTurret].Swivel.GetComponentsInChildren<Transform>();
                string loockingPrefix = db.WeaponSocket;
                foreach (Transform child in childrens)
                {
                    if (child.name.Length >= loockingPrefix.Length)
                    {
                        string cuted = child.name.Substring(0, loockingPrefix.Length);
                        if (cuted == loockingPrefix)
                        {
                            foundNames.Add(child.name);
                            foundGO.Add(child.gameObject);
                        }
                    }
                }
            }

            if (turrets[selectedTurret].Head != null)
            {
                Transform[] childrens = turrets[selectedTurret].Head.GetComponentsInChildren<Transform>();
                string loockingPrefix = db.WeaponSocket;
                foreach (Transform child in childrens)
                {
                    if (child.name.Length >= loockingPrefix.Length)
                    {
                        string cuted = child.name.Substring(0, loockingPrefix.Length);
                        if (cuted == loockingPrefix)
                        {
                            foundNames.Add(child.name);
                            foundGO.Add(child.gameObject);
                        }
                    }
                }
            }

            if (turrets[selectedTurret].Mount != null)
            {
                Transform[] childrens = turrets[selectedTurret].Mount.GetComponentsInChildren<Transform>();
                string loockingPrefix = db.WeaponSocket;
                foreach (Transform child in childrens)
                {
                    if (child.name.Length >= loockingPrefix.Length)
                    {
                        string cuted = child.name.Substring(0, loockingPrefix.Length);
                        if (cuted == loockingPrefix)
                        {
                            foundNames.Add(child.name);
                            foundGO.Add(child.gameObject);
                        }
                    }
                }
            }

            List<GameObject> tempBreeches = new List<GameObject>(new GameObject[foundNames.Count]);
            List<string> tempBarrelSockets = new List<string>(new string[foundNames.Count]);
            List<GameObject> tempBarrels = new List<GameObject>(new GameObject[foundNames.Count]);

            for (int i = 0; i < foundNames.Count; i++)
            {
                if (turrets[selectedTurret].WeaponSlotsNames.Contains(foundNames[i]))
                {
                    int currentPosition = -1;
                    currentPosition = FindIndex(turrets[selectedTurret].WeaponSlotsNames.ToArray(), foundNames[i]);
                    tempBreeches[i] = turrets[selectedTurret].WeaponBreeches[currentPosition];
                    tempBarrelSockets[i] = turrets[selectedTurret].WeaponBarrelSockets[currentPosition];
                    tempBarrels[i] = turrets[selectedTurret].WeaponBarrels[currentPosition];
                }
            }

            turrets[selectedTurret].WeaponSlotsNames = foundNames;
            turrets[selectedTurret].WeaponSlots = foundGO;
            turrets[selectedTurret].WeaponBreeches = tempBreeches;
            turrets[selectedTurret].WeaponBarrels = tempBarrels;
            turrets[selectedTurret].WeaponBarrelSockets = tempBarrelSockets;

            mountNames = foundNames.ToArray();

            CheckForSavedMounts();
        }

        /// <summary>
        /// Use this function of clearing all selected weapons
        /// </summary>
        void ClearAllWeaponsSelection()
        { 
            turrets[selectedTurret].WeaponSlotsNames = new List<string>();
            turrets[selectedTurret].WeaponSlots = new List<GameObject>();
            turrets[selectedTurret].WeaponBreeches = new List<GameObject>();
            turrets[selectedTurret].WeaponBarrels = new List<GameObject>();
            turrets[selectedTurret].WeaponBarrelSockets = new List<string>();
        }

        /// <summary>
        /// This function updates available barrels by prefix(parsing current structure)
        /// </summary>
        void CheckForBarrels()
        {
            int count = 0;
            List<string> barrelSocketsList = new List<string>();
            if (turrets.Count <= 0)
            {
                return;
            }

            TurretStructure struc = turrets[selectedTurret];
            if (struc.Breech == null)
            {
                count = 0;
                return;
            }

            Transform[] childrens = struc.Breech.GetComponentsInChildren<Transform>();
            foreach (Transform child in childrens)
            {
                if (child.gameObject.name.Length < struc.BarrelPrefix.Length)
                {

                }
                else
                {
                    string curNamePart = child.gameObject.name.Substring(0, struc.BarrelPrefix.Length);
                    if (curNamePart == struc.BarrelPrefix)
                    {
                        barrelSocketsList.Add(child.name);
                        count++;
                    }
                }
            }
        }

        /// <summary>
        /// This function loading database or create new
        /// </summary>
        void LoadDatabase()
        {
            F3DTurretScriptable newManager = AssetDatabase.LoadAssetAtPath("Assets/FORGE3D/Sci-Fi Effects/Turrets/Database/database.asset", typeof(ScriptableObject)) as F3DTurretScriptable;
            if (newManager != null)
            {
                db = newManager;
                InstallAll();
            }
            else
            {
                CreateDatabase();
                InstallAll();
            }
        }

        /// <summary>
        /// This function installs turrets, selected turret and so on
        /// </summary>
        void InstallAll()
        {
            turrets = db.Turrets;
            selectedTurret = db.SelectedTurret;
            if (selectedTurret > turrets.Count)
            {
                selectedTurret = turrets.Count - 1;
            }
        }

        /// <summary>
        /// This function create new database
        /// </summary>
        void CreateDatabase()
        {
            if (Application.isPlaying)
                return;

            F3DTurretScriptable newManager = ScriptableObject.CreateInstance<F3DTurretScriptable>();
            newManager.Turrets = turrets;
            db = newManager;
            AssetDatabase.CreateAsset(newManager, "Assets/FORGE3D/Sci-Fi Effects/Turrets/Database/database.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// This function saving current database state
        /// </summary>
        void SaveDatabase()
        {
            F3DTurretScriptable newDB = ScriptableObject.CreateInstance<F3DTurretScriptable>();
            newDB.Turrets = db.Turrets;
            newDB.Swivels = db.Swivels;
            newDB.Heads = db.Heads;
            newDB.Mounts = db.Mounts;
            newDB.Barrels = db.Barrels;
            newDB.Breeches = db.Breeches;
            newDB.Bases = db.Bases;
            newDB.SwivelPrefix = db.SwivelPrefix;
            newDB.HeadPrefix = db.HeadPrefix;
            newDB.MountPrefix = db.MountPrefix;
            newDB.BarrelPrefix = db.BarrelPrefix;
            newDB.WeaponSocket = db.WeaponSocket;// = db.BreechSocketGroupers;

            newDB.SelectedTurret = db.SelectedTurret;
            AssetDatabase.CreateAsset(newDB, "Assets/FORGE3D/Sci-Fi Effects/Turrets/Database/database.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        } 

        /// <summary>
        /// Use this function to get know are names have name in themselves
        /// </summary>
        /// <param name="names"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        bool CheckForName(string[] names, string name)
        {
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i] == name)
                { 
                    return true;
                }
            }
            return false;
        }

        string[] FindAvailableSockets(GameObject parent, string socketPrefix)
        {
            if (parent != null)
            { 
                List<string> tempSockets = new List<string>();
                Transform[] childrens = parent.GetComponentsInChildren<Transform>();
                foreach (Transform child in childrens)
                {
                    if (child.name.Length >= socketPrefix.Length)
                    {
                        string cuted = child.name.Substring(0, socketPrefix.Length);
                        if (cuted == socketPrefix)
                        {
                            tempSockets.Add(child.name);
                        }
                    }
                } 
                return tempSockets.ToArray();
            }
            else
            { 
                return new string[0];
            }
        } 

        void UpdateAllTemplatesOnEditors()
        { 
            F3DTurretConstructor[] editors = GameObject.FindObjectsOfType<F3DTurretConstructor>();
            int i;
            for (i = 0; i < editors.Length; i++)
            {
                editors[i].needUpdateListOfTemplates = true; 
            }
        }
    }
}