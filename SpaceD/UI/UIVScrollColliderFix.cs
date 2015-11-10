using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UIVScrollColliderFix : MonoBehaviour {

    [SerializeField]
    private Vector2 offset = Vector2.zero;
    [SerializeField]
    private UIScrollBar scrollBar;
    private UIWidget widget;
    private BoxCollider col;

    void Start() {
        this.widget = this.GetComponent<UIWidget>();
        this.col = GetComponent<Collider>() as BoxCollider; // this.col = collider as BoxCollider; via Unity5.AutoAPIUpdater

        if (this.scrollBar != null && this.widget != null && this.col != null)
            this.scrollBar.onChange.Add(new EventDelegate(OnChange));
    }

    void OnChange() {
        this.StartCoroutine("Delay");
    }

    IEnumerator Delay() {
        yield return new WaitForSeconds(0.05f);

        if (this.widget != null && this.scrollBar != null && this.scrollBar.enabled && this.col != null) {
            Vector4 region = this.widget.drawingDimensions;
            this.col.center = new Vector3((offset.x + region.x + region.z) * 0.5f, (offset.y + region.y + region.w) * 0.5f);
            this.col.size = new Vector3(region.z - region.x, region.w - region.y);
        }

        yield break;
    }
}
