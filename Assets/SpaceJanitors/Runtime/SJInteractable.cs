using UnityEngine;

public class SJInteractable : MonoBehaviour
{
    public enum Kind { Waste, Cargo, Repair, Tether, Computer }
    public Kind kind;
    public string displayName;
    public float repairProgress;
    public bool completed;
    public Rigidbody Body => GetComponent<Rigidbody>();
    public bool CanGrab => (kind == Kind.Waste || kind == Kind.Cargo) && !completed;

    public void RepairFor(float seconds)
    {
        if (kind != Kind.Repair || completed) return;
        repairProgress = Mathf.Min(2f, repairProgress + seconds);
        if (repairProgress < 2f) return;
        completed = true;
        SJGame.Instance.repairs++;
        SJGame.Instance.Notify("수리 완료! / REPAIR COMPLETE");
        foreach (var r in GetComponentsInChildren<Renderer>())
            if (r.gameObject.name == "Inset") r.material.color = new Color(.2f,.8f,.55f);
    }
}
