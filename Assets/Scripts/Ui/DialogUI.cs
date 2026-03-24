using System;
using UnityEngine;
using UnityEngine.UIElements;


namespace Ui
{
    public class DialogUI : MonoBehaviour
    {
        private VisualElement _root;
        private VisualElement _activeOverlay;
        //TODO:Namespace,Interface skinnydev 
        public void Initialize(VisualElement root)
        {
            _root = root;
        }

        public void ShowConfirmation(string message, Action onConfirm)
        {
            var overlay = CreateOverlay();
            var box = CreateDialogBox();

            var label = new Label(message);
            label.AddToClassList("dialog-message");

            var btnRow = new VisualElement();
            btnRow.AddToClassList("dialog-button-row");

            var btnYes = new Button(() => { onConfirm?.Invoke(); CloseDialog(); }) { text = "YES" };
            btnYes.AddToClassList("dialog-btn");
            btnYes.AddToClassList("dialog-btn--yes");

            var btnNo = new Button(CloseDialog) { text = "NO" };
            btnNo.AddToClassList("dialog-btn");
            btnNo.AddToClassList("dialog-btn--no");

            btnRow.Add(btnYes);
            btnRow.Add(btnNo);
            box.Add(label);
            box.Add(btnRow);

            Show(overlay, box);
        }

        public void ShowGameOver(Action onRestart)
        {
            var overlay = CreateOverlay();
            var box = CreateDialogBox();

            var title = new Label("GAME OVER");
            title.AddToClassList("dialog-title");
            title.AddToClassList("dialog-title--danger");

            var description = new Label("You've used all 3 lives. Try again!");
            description.AddToClassList("dialog-message");

            var btnNewGame = new Button(() => { onRestart?.Invoke(); CloseDialog(); }) { text = "START NEW GAME" };
            btnNewGame.AddToClassList("dialog-btn");
            btnNewGame.AddToClassList("dialog-btn--primary");

            box.Add(title);
            box.Add(description);
            box.Add(btnNewGame);

            Show(overlay, box);
        }

        public void ShowPause(Action onResume, Action onRestart)
        {
            var overlay = CreateOverlay();
            var box = CreateDialogBox();

            var title = new Label("PAUSED");
            title.AddToClassList("dialog-title");

            var btnResume = new Button(() => { onResume?.Invoke(); CloseDialog(); }) { text = "RESUME" };
            btnResume.AddToClassList("dialog-btn");
            btnResume.AddToClassList("dialog-btn--primary");
            btnResume.AddToClassList("dialog-btn--stacked");

            var btnRestart = new Button(() => { onRestart?.Invoke(); CloseDialog(); }) { text = "RESTART" };
            btnRestart.AddToClassList("dialog-btn");
            btnRestart.AddToClassList("dialog-btn--yes");
            btnRestart.style.width = 450; // Special case override for consistency

            box.Add(title);
            box.Add(btnResume);
            box.Add(btnRestart);

            Show(overlay, box);
        }

        public void ShowWin(string finalTime, Action onPlayAgain)
        {
            var overlay = CreateOverlay();
            var box = CreateDialogBox();

            var title = new Label("CONGRATULATIONS!");
            title.AddToClassList("dialog-title");
            title.AddToClassList("dialog-title--success");

            var message = new Label("You have solved the entire puzzle!");
            message.AddToClassList("dialog-message");
            message.style.marginBottom = 10;

            var timeLabel = new Label($"Completion Time: {finalTime}");
            timeLabel.AddToClassList("dialog-message");

            var btnPlayAgain = new Button(() => { onPlayAgain?.Invoke(); CloseDialog(); }) { text = "PLAY AGAIN" };
            btnPlayAgain.AddToClassList("dialog-btn");
            btnPlayAgain.AddToClassList("dialog-btn--primary");

            box.Add(title);
            box.Add(message);
            box.Add(timeLabel);
            box.Add(btnPlayAgain);

            Show(overlay, box);
        }

        private void Show(VisualElement overlay, VisualElement box)
        {
            CloseDialog(); // Ensure no stacking
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

        private VisualElement CreateOverlay()
        {
            var overlay = new VisualElement();
            overlay.AddToClassList("dialog-overlay");
            return overlay;
        }

        private VisualElement CreateDialogBox()
        {
            var box = new VisualElement();
            box.AddToClassList("dialog-box");
            return box;
        }
    }

}