using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ui
{
    public class DialogUI : MonoBehaviour
    {
        private VisualElement _root;
        private VisualElement _activeOverlay;

        public void Initialize(VisualElement root)
        {
            _root = root;
        }

      
        public void ShowMessage(string title, string message, string primaryBtnText, Action onPrimaryClick, string secondaryBtnText = null, Action onSecondaryClick = null)
        {
            CloseDialog(); 

            var overlay = new VisualElement();
            overlay.AddToClassList("dialog-overlay");

            var box = new VisualElement();
            box.AddToClassList("dialog-box");

           
            if (!string.IsNullOrEmpty(title))
            {
                var titleLabel = new Label(title);
                titleLabel.AddToClassList("dialog-title");
                box.Add(titleLabel);
            }

          
            if (!string.IsNullOrEmpty(message))
            {
                var msgLabel = new Label(message);
                msgLabel.AddToClassList("dialog-message");
                box.Add(msgLabel);
            }

        
            var btnRow = new VisualElement();
            btnRow.AddToClassList("dialog-button-row");

          
            var btnPrimary = new Button(() => { onPrimaryClick?.Invoke(); CloseDialog(); }) { text = primaryBtnText };
            btnPrimary.AddToClassList("dialog-btn");
            btnPrimary.AddToClassList("dialog-btn--primary");
            btnRow.Add(btnPrimary);

            // İkinci Buton (varsa)
            if (!string.IsNullOrEmpty(secondaryBtnText))
            {
                var btnSecondary = new Button(() => { onSecondaryClick?.Invoke(); CloseDialog(); }) { text = secondaryBtnText };
                btnSecondary.AddToClassList("dialog-btn");
                btnSecondary.AddToClassList("dialog-btn--no"); 
                btnRow.Add(btnSecondary);
            }

            box.Add(btnRow);
            overlay.Add(box);

            _root.Add(overlay);
            _activeOverlay = overlay;
        }

        public void CloseDialog()
        {
            if (_activeOverlay != null && _root.Contains(_activeOverlay))
            {
                _root.Remove(_activeOverlay);
                _activeOverlay = null;
            }
        }
    }
}