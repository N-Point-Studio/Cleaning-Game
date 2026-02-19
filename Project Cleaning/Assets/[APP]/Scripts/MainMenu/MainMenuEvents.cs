using System;
using UnityEngine;

public static class MainMenuEvents
{
    public static Action<ArtefactData> OnRequestArtefactPlay;
    public static Action OnCloseArtefactDetail;
    public static Action OnNextPage;
    public static Action OnPreviousPage;
    public static Action<int> OnPageChanged;
    public static Action<int> OnGoToPage;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        OnRequestArtefactPlay = null;
        OnCloseArtefactDetail = null;
        OnNextPage = null;
        OnPreviousPage = null;
        OnPageChanged = null;
        OnGoToPage = null;
    }
}
