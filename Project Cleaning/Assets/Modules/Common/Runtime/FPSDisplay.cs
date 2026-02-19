using UnityEngine;
using UnityEngine.Events;

namespace Modules
{
    public class FPSDisplay : MonoBehaviour
    {
        [SerializeField] private TextAnchor position = TextAnchor.LowerRight;
        [SerializeField] private float updateInterval = 0.5F;
        [Space]
        public FPSUpdateFloatEvent OnUpdateFPSFloat;
        public FPSUpdateStringEvent OnUpdateFPSString;

        private int frameCounter;
        private float timeCounter;
        private float fps;

        private int w;
        private int h;
        private GUIStyle style = new GUIStyle();
        private Rect rect;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            OnScreenSizeChanged();
        }

        private void OnGUI()
        {
            style.alignment = position;

            if (fps < 10)
                style.normal.textColor = Color.red;
            else if (fps < 30)
                style.normal.textColor = Color.yellow;
            else
                style.normal.textColor = Color.green;

            GUI.Label(rect, string.Format("{0:F2}", fps), style);
        }

        private void Update()
        {
            if (timeCounter < updateInterval)
            {
                timeCounter += Time.deltaTime;
                frameCounter++;
            }
            else
            {
                fps = frameCounter / timeCounter;
                OnUpdateFPSFloat?.Invoke(fps);
                OnUpdateFPSString?.Invoke(fps.ToString("0:F2"));

                frameCounter = 0;
                timeCounter = 0f;
            }

            if (w != Screen.width || h != Screen.height)
            {
                OnScreenSizeChanged();
            }
        }

        private void OnScreenSizeChanged()
        {
            w = Screen.width;
            h = Screen.height;
            rect = new Rect(0, 0, w, h);

            style.fontSize = h * 2 / 100;
        }
    }

    [System.Serializable]
    public class FPSUpdateFloatEvent : UnityEvent<float> { }

    [System.Serializable]
    public class FPSUpdateStringEvent : UnityEvent<string> { }
}