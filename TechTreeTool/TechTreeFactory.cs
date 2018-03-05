using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TechTree;
using TechTree.Model;

public class TechTreeFactory : MonoBehaviour
{

#region API
    [HideInInspector]
    public TechTreeUnit
        unit;

    /// <summary>
    /// The blueprints that are built by this factory.
    /// </summary>
    public List<BlueprintController> blueprints = new List<BlueprintController> ();

    /// <summary>
    /// Gets the build queue.
    /// </summary>
    public BlueprintBuildRequest[] BuildQueue {
        get {
            return buildQueue.ToArray ();
        }
    }

    /// <summary>
    /// Remove completed items from the BuildQueue.
    /// </summary>
    public void CleanBuildQueue ()
    {
        buildQueue = (from i in buildQueue where !i.Complete select i).ToList ();
    }

    
    /// <summary>
    /// Can this factory build this blueprint?
    /// </summary>
    public bool CanBuild (BlueprintController b)
    {
        return b.CanBeBuilt && b.blueprint.requiredFactoryLevel <= unit.Level;
    }
    
    /// <summary>
    /// Build the unit specified by blueprint ID. Only works if this unit is a factory.
    /// </summary>
    /// <param name="ID">I.</param>
    public BlueprintBuildRequest Build (string ID)
    {
        var bpr = unit.bpc.CreateBuildRequest (this.unit, ID);
        if (bpr.status == BuildStatus.Success) {
            var bpc = unit.model.blueprints [bpr.blueprint.ID];
            bpc.IsBuilding = true;
            if (unit.bpc.blueprint.factory.type == FactoryQueueType.ParallelQueue) {
                StartCoroutine (ProcessBuildRequest (bpr));
            }
            if (unit.bpc.blueprint.factory.type == FactoryQueueType.SingleQueue) {
                buildQueue.Add (bpr);
            }
        }
        return bpr;
    }
#endregion
    void Awake ()
    {
        this.unit = GetComponent<TechTreeUnit> ();
    }

    void Start ()
    {
        blueprints.AddRange (from i in unit.bpc.blueprint.factory.blueprints select unit.model.blueprints [i.ID]);
        if (unit.bpc.blueprint.factory.type == FactoryQueueType.SingleQueue) {
            StartCoroutine (SingleQueueFactory ());
        }
    }

    IEnumerator SingleQueueFactory ()
    {
        while (true) {
            yield return null;
            if (buildQueue.Count > 0) {
                yield return StartCoroutine (ProcessBuildRequest (buildQueue [0]));
                buildQueue.RemoveAt (0);
            }
        }
    }

    IEnumerator ProcessBuildRequest (BlueprintBuildRequest req)
    {
        var bpc = unit.model.blueprints [req.blueprint.ID];
        var model = unit.model;
        var costs = new Dictionary<string, float[]> ();
        var totalResources = 0f;
        foreach (var c in bpc.blueprint.costs) {
            costs [c.resource.ID] = new float[] { c.qty, c.qty };
            totalResources += c.qty;
        }
        var completed = false;
        while (!completed) {
            yield return null;
            if (req.cancel) {
                bpc.IsBuilding = false;
                foreach (var c in costs) {
                    model.resources [c.Key].Give (c.Value [1] - c.Value [0]);
                }
                req.status = BuildStatus.Cancelled;
                yield break;
            }
            foreach (var c in costs) {
                if (c.Value [0] <= 0)
                    continue;
                var amount = Mathf.Min ((c.Value [1] / bpc.blueprint.constructTime) * Time.deltaTime, c.Value [0]);
                if (model.resources [c.Key].Take (amount)) {
                    c.Value [0] -= amount;
                } 
            }
            completed = true;
            var totalConsumed = 0f;
            foreach (var c in costs) {
                if (c.Value [0] > 0)
                    completed = false;
                totalConsumed += c.Value [1] - c.Value [0];
            }
            req.percentComplete = totalConsumed / totalResources;
        }
        req.percentComplete = 1;
        bpc.IsBuilding = false;
        foreach (var p in bpc.blueprint.prerequisites) {
            model.blueprints [p.blueprint.ID].OnDependentHasBeenBuilt (bpc);
        }
        model.SpawnUnit (this, bpc);
    }

    List<BlueprintBuildRequest> buildQueue = new List<BlueprintBuildRequest> ();
}
