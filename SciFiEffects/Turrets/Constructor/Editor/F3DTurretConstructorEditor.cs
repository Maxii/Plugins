using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Forge3D
{
    [CustomEditor(typeof(F3DTurretConstructor))]
    [CanEditMultipleObjects()]
    public class F3DTurretConstructorEditor : Editor
    {
        F3DTurretConstructor constructor; 

        F3DTurretScriptable db;
        private bool isInScene = false;
        List<TurretStructure> turrets = new List<TurretStructure>();
        string[] turretNames = new string[0];
        int turretIndex = 0;

        void OnEnable()
        {
            if (constructor == null)
            {
                constructor = (F3DTurretConstructor)target; 
                if (IsInScene())
                    isInScene = true;
                else
                {
                    isInScene = false;
                    return;
                } 
                LoadDatabase();
                LoadTurretNames();
                turretIndex = constructor.turretIndex;
                UpdateFullTurret();
            }
        }

        public int GetSelectedType()
        { 
            return turretIndex;
        }

        public void UpdateFullTurret()
        {
            constructor = (F3DTurretConstructor)target; 
            if (IsInScene())
                isInScene = true;
            else
            {
                isInScene = false;
                return;
            } 
            LoadDatabase();
            LoadTurretNames();
            turretIndex = constructor.turretIndex;

            if (turrets.Count > 0)
                if (turrets.Count > turretIndex)
                    constructor.UpdateFullTurret(turrets[turretIndex]);
        }

        /// <summary>
        /// This function loades names of templates
        /// </summary>
        public void LoadTurretNames()
        {
            List<string> names = new List<string>();
            int i; 
            for (i = 0; i < turrets.Count; i++)
            {
                names.Add(turrets[i].Name);
            }
            turretNames = names.ToArray();
        }

        /// <summary>
        /// We need this function to get know is object in scene, or just selected in project view
        /// </summary>
        /// <returns>Ruturns is this object in scene</returns>
        bool IsInScene()
        {
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            int i;
            GameObject thisGO = constructor.gameObject;
            for (i = 0; i < allObjects.Length; i++)
            {
                if (thisGO == allObjects[i])
                {
                    return true;
                }

            }
            return false;
        } 

        public override void OnInspectorGUI()
        {
            if (!isInScene)
                return;

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

            if (constructor.needUpdateListOfTemplates)
            {
                constructor.needUpdateListOfTemplates = false;
                LoadTurretNames();
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical("Box");
                {
                    int newTurretIndex = EditorGUILayout.Popup(turretIndex, turretNames);
                    if (newTurretIndex != constructor.turretIndex)
                    {
                        constructor.turretIndex = newTurretIndex; 
                        turretIndex = newTurretIndex;
                        db.SelectedTurret = turretIndex;
                        UpdateFullTurret();
                    }
                    if (GUILayout.Button("Open in editor"))
                    {
                        EditorWindow.GetWindow(typeof(F3DTurretEditorWindow));
                    }
                    if (GUILayout.Button("Unlink from turret templates"))
                    {
                        UnlinkGameObject();
                    }
                }
                EditorGUILayout.EndVertical();
            }
        }

        public void UnlinkGameObject()
        {
            if (Selection.objects.Length > 0)
            {
                foreach(GameObject obj in Selection.objects)
                {  
                    F3DTurretConstructor cons = obj.GetComponent<F3DTurretConstructor>();
                    if (cons != null)
                    {
                        DestroyImmediate(cons);
                    }
                }
            } 
        }

        /// <summary>
        /// Loading of preferences from containter to editor
        /// </summary>
        void LoadPreferences()
        {
            turretIndex = constructor.turretIndex;
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

        /// <summary>
        /// Loading database from resources
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
            }
        }

        /// <summary>
        /// Loading local turrets
        /// </summary>
        void InstallAll()
        {
            turrets = db.Turrets;
        }

        /// <summary>
        /// Creation of new database
        /// </summary>
        void CreateDatabase()
        {
            if (Application.isPlaying)
                return;
            F3DTurretScriptable newManager = ScriptableObject.CreateInstance<F3DTurretScriptable>();
            newManager.Turrets = turrets; 
            AssetDatabase.CreateAsset(newManager, "Assets/FORGE3D/Sci-Fi Effects/Turrets/Database/database.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}