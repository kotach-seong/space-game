using UnityEngine;

public class SJBin : MonoBehaviour
{
    void OnTriggerEnter(Collider other) => Collect(other.GetComponentInParent<SJInteractable>());
    public bool Collect(SJInteractable item)
    {
        if (item == null || item.kind != SJInteractable.Kind.Waste || item.completed) return false;
        if (SJGame.Instance.player.held == item) SJGame.Instance.player.Release();
        item.completed = true;
        SJGame.Instance.collected++;
        SJGame.Instance.Notify("폐기물 수거 완료 / WASTE RECYCLED");
        item.gameObject.SetActive(false);
        return true;
    }
}
