using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalBlacksmithStory
{
    [RequireComponent(typeof(TMP_Text), typeof(LayoutElement))]
    public class FiniteFlexibleText : MonoBehaviour
    {
        public float maxWidth = 400f;
        TMP_Text tmp;
        LayoutElement le;

        void Awake()
        {
            if (tmp == null) tmp = GetComponent<TMP_Text>();
            if (le == null) le = GetComponent<LayoutElement>();
        }
        void OnEnable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        }
        void OnDisable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        }
        private void OnTextChanged(UnityEngine.Object obj)
        {
            if (obj == tmp) Refresh();
        }

        void Refresh()
        {
            float w = tmp.GetPreferredValues(tmp.text).x;
            le.preferredWidth = Mathf.Min(w, maxWidth);
        }
    }
}
