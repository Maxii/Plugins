using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;

namespace TechTree.Model {
    public class BlueprintModelAsset : ScriptableObject {
#if UNITY_EDITOR
        public static T Create<T>(BlueprintModelAsset nextTo = null) where T : BlueprintModelAsset {

            var instance = ScriptableObject.CreateInstance<T>();
            var root = "Assets";
            if (nextTo != null) {
                AssetDatabase.AddObjectToAsset(instance, nextTo);
            }
            else {
                var path = AssetDatabase.GenerateUniqueAssetPath(string.Format("{0}/{1}.asset", root, typeof(T).Name));
                AssetDatabase.CreateAsset(instance, path);
            }
            instance.OnCreate();
            return instance;
        }

        public static void Remove(BlueprintModelAsset instance) {
            var path = AssetDatabase.GetAssetPath(instance);
            DestroyImmediate(instance, true);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        public static void Rename(BlueprintModelAsset instance, string newName) {
            newName = instance.GetType().Name + "-" + newName;
            instance.name = newName;
        }

        public virtual void OnCreate() {
        }


        public new virtual void SetDirty() {
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif


    }

}
