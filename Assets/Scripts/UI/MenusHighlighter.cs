using System;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalBlacksmithStory.UI
{
    public class MenusHighlighter : MonoBehaviour
    {
        [SerializeField] private Button[] buttons;
        private int _highlights = 0;
        public void Start()
        {
            ChangeHighLight(0);
        }
        public void ChangeHighLight(int menu)
        {
            if (buttons[_highlights].targetGraphic is Image previousImage)
            {
                previousImage.color = Color.white;
            }
            _highlights = menu;
            if (buttons[menu].targetGraphic is Image targetImage && ColorUtility.TryParseHtmlString("#FFCD75", out Color newColor))
            {
                targetImage.color = newColor;
            }
        }
    }
}
