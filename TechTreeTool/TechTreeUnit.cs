using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TechTree;
using System.Linq;

/// <summary>
/// The TechTreeUnit class contains all information unique to an instance of 
/// something built using the BlueprintController.
/// </summary>
public class TechTreeUnit : MonoBehaviour {

    public BlueprintController bpc;
    public BlueprintModelController model;

    Dictionary<string, float> stats = new Dictionary<string, float> ();

    /// <summary>
    /// If this unit consumes resources after being built, does it have enough resources?
    /// </summary>
    public bool hasEnoughResources = true;

    /// <summary>
    /// //This is the upgrade level of the unit. Unit levels always start at the level of the factory that built them.
    /// </summary>
    public int Level {
        get;
        set;
    }

    public bool IsUpgrading { get; private set; }
    public float UpgradeProgress { get; private set; }

    public IEnumerable<string> Stats {
        get {
            foreach(var i in stats) yield return i.Key;
        }
        
    }

    public void Init(BlueprintModelController model, BlueprintController bpc) {
        this.model = model;
        this.bpc = bpc;
        this.Level = 0;
        this.IsUpgrading = false;
        this.UpgradeProgress = 0;
        if(this.bpc.blueprint.isFactory) {
            this.gameObject.AddComponent<TechTreeFactory>();
        }
        this.bpc.buildCount++;

        if(this.bpc.blueprint.productionRates.Count > 0 || this.bpc.blueprint.consumptionRates.Count > 0) {
            StartCoroutine(ProduceAndConsumeResources());
        }
        if ((from i in bpc.blueprint.statValues where i.regen || i.notifyIfZero select i).Count () > 0) {
            StartCoroutine(WatchStats());
        }
        InitStats ();
    }

    public int GetStat(string name) {
        float value;
        if (stats.TryGetValue (name, out value)) {
            return Mathf.FloorToInt(value);
        }
        return 0;
    }

    public void SetStat(string name, int value) {
        stats [name] = value;
    }

    void InitStats() {
        foreach (var i in bpc.blueprint.statValues) {
            if(i.level == Level) {
                stats[i.stat.ID] = i.startValue;
            }
        }
    }

    IEnumerator WatchStats() {
        var delay = new WaitForSeconds (1);
        while (true) {
            yield return delay;
            foreach (var i in bpc.blueprint.statValues) {
                if(i.level == Level) {
                    var sv = stats[i.stat.ID];
                    if(i.notifyIfZero && sv <= 0) {
                        gameObject.SendMessage("OnStatisticValueIsZero", i.stat.ID);
                    }
                    if(i.regen) {
                        sv += i.regenRate;
                    }
                    if(sv > i.maxValue) sv = i.maxValue;
                    stats[i.stat.ID] = sv;
                }
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether this unit can upgrade.
    /// </summary>
    public bool CanUpgrade {
        get {
            return Level < bpc.blueprint.upgradeLevels.Count;
        }
    }

    /// <summary>
    /// Upgrades this unit to the next level.
    /// </summary>
    public void PerformUpgrade() {
        StartCoroutine(_PerformUpgrade());
    }

    IEnumerator ProduceAndConsumeResources() {
        var delay = new WaitForSeconds(1);
        while(true) {
            yield return delay;
            var productionRates = this.bpc.blueprint.productionRates;
            var consumptionRates = this.bpc.blueprint.consumptionRates;
            if(Level > 0) {
                productionRates = this.bpc.blueprint.upgradeLevels[Level-1].productionRates;
                consumptionRates = this.bpc.blueprint.upgradeLevels[Level-1].consumptionRates;
            }
            foreach (var i in productionRates) {
                this.model.resources [i.resource.ID].Give (i.qtyPerSecond);
            }
            hasEnoughResources = true;

            foreach (var i in consumptionRates) {
                hasEnoughResources = hasEnoughResources && this.model.resources [i.resource.ID].Take (i.qtyPerSecond);
            }
        }
    }

    IEnumerator _PerformUpgrade() {
        if(!bpc.blueprint.isUpgradeable) yield break;
        if(Level >= bpc.blueprint.upgradeLevels.Count) yield break;
        if(IsUpgrading) yield break;
        IsUpgrading = true;
        var upgradeLevel = bpc.blueprint.upgradeLevels[Level];
        var totalResources = 0f;
        var costs = new Dictionary<string, float[]> ();
        foreach (var c in upgradeLevel.costs) {
            costs [c.resource.ID] = new float[] { c.qty, c.qty };
            totalResources += c.qty;
        }
        UpgradeProgress = 0f;
        var completed = false;
        while (!completed) {
            yield return null;

            foreach (var c in costs) {
                if (c.Value [0] <= 0)
                    continue;
                var amount = Mathf.Min ((c.Value [1] / upgradeLevel.constructTime) * Time.deltaTime, c.Value [0]);
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
            UpgradeProgress = totalConsumed / totalResources;
        }
        Level += 1;
        UpgradeProgress = 1;
        IsUpgrading = false;
        InitStats ();
        gameObject.SendMessage("SetRepr", upgradeLevel.gameObject, SendMessageOptions.DontRequireReceiver);
        gameObject.SendMessage("OnLevelUp", Level, SendMessageOptions.DontRequireReceiver);
    }

    void OnDisable() {
        this.bpc.buildCount--;
    }
	
}
