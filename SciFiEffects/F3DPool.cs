using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class F3DPool : MonoBehaviour {

    public static F3DPool instance;         // Singleton instance

    [Header("VFX Pool")]
    public Transform[] poolItems;           // Effect pool prefabs
    public int[] poolLength;                // Effect pool items count

    [Header("Audio Pool")]
    public Transform audioSourcePrefab;     // Audio source prefab

    public AudioClip[] audioPoolItems;      // Audio pool prefabs
    public int[] audioPoolLength;           // Audio pool items count

    // Pooled items collections
    private Dictionary<Transform, Transform[]> pool;
    private Dictionary<AudioClip, AudioSource[]> audioPool;

    // Use this for initialization
    void Start ()
    {
        // Singleton instance
        instance = this;

        // Initialize effects pool
        if (poolItems.Length > 0)
        {
            pool = new Dictionary<Transform, Transform[]>();

            for (int i = 0; i < poolItems.Length; i++)
            {
                Transform[] itemArray = new Transform[poolLength[i]];

                for (int x = 0; x < poolLength[i]; x++)
                {
                    Transform newItem = (Transform)Instantiate(poolItems[i], Vector3.zero, Quaternion.identity);
                    newItem.gameObject.SetActive(false);
                    newItem.parent = transform;

                    itemArray[x] = newItem;
                }

                pool.Add(poolItems[i], itemArray);
            }
        }

        // Initialize audio pool
        if (audioPoolItems.Length > 0)
        {
            audioPool = new Dictionary<AudioClip, AudioSource[]>();

            for (int i = 0; i < audioPoolItems.Length; i++)
            {
                AudioSource[] audioArray = new AudioSource[audioPoolLength[i]];

                for (int x = 0; x < audioPoolLength[i]; x++)
                {
                    AudioSource newAudio = ((Transform)Instantiate(audioSourcePrefab, Vector3.zero, Quaternion.identity)).GetComponent<AudioSource>();
                    newAudio.clip = audioPoolItems[i];

                    newAudio.gameObject.SetActive(false);
                    newAudio.transform.parent = transform;

                    audioArray[x] = newAudio;
                }

                audioPool.Add(audioPoolItems[i], audioArray);
            }
        }
    }
    
    // Spawn effect prefab and send OnSpawned message
    public Transform Spawn(Transform obj, Vector3 pos, Quaternion rot, Transform parent)
    {
        for (int i = 0; i < pool[obj].Length; i++)
        {
            if(!pool[obj][i].gameObject.activeSelf)
            {
                Transform spawnItem = pool[obj][i];

                spawnItem.parent = parent;
                spawnItem.position = pos;
                spawnItem.rotation = rot;
                
                spawnItem.gameObject.SetActive(true);
                spawnItem.BroadcastMessage("OnSpawned", SendMessageOptions.DontRequireReceiver);

                return spawnItem;
            }
        }

        return null;
    }

    // Spawn audio prefab and send OnSpawned message
    public AudioSource SpawnAudio(AudioClip clip, Vector3 pos, Transform parent)
    {
        for (int i = 0; i < audioPool[clip].Length; i++)
        {
            if (!audioPool[clip][i].gameObject.activeSelf)
            {
                AudioSource spawnItem = audioPool[clip][i];

                spawnItem.transform.parent = parent;
                spawnItem.transform.position = pos;

                spawnItem.gameObject.SetActive(true);
                spawnItem.BroadcastMessage("OnSpawned", SendMessageOptions.DontRequireReceiver);

                return spawnItem;
            }
        }

        return null;
    }

    // Despawn effect or audio and send OnDespawned message
    public void Despawn(Transform obj)
    {
        obj.BroadcastMessage("OnDespawned", SendMessageOptions.DontRequireReceiver);
        obj.gameObject.SetActive(false);
    }
}
