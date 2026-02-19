using UnityEngine;

public class BaseHUD : MonoBehaviour
{
    [SerializeField] private GameObject root;

    protected virtual void Awake()
    {

    }

    protected virtual void Start()
    {

    }

    protected virtual void Update()
    {

    }

    protected virtual void OnDestroy()
    {

    }

    public virtual void ShowHUD(bool isShow)
    {
        root.SetActive(isShow);
    }
}