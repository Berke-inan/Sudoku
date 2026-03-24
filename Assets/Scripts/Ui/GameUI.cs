using UnityEngine;
using UnityEngine.UIElements;
using gameManagement;



public enum Difficulty { Easy, Medium, Hard }


namespace Ui
{
  
    [RequireComponent(typeof(UIDocument))]
    [RequireComponent(typeof(BoardUI))]
    [RequireComponent(typeof(DialogUI))]


    public class GameUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameManager gameManager;

        private UIDocument _uiDocument;
        private BoardUI _boardUI;
        private DialogUI _dialogUI;

        private readonly Label[] _remainingNumberLabels = new Label[10];
        private readonly Button[] _numpadButtons = new Button[10];

        private VisualElement _root;
        private Label _timerLabel;
        private Label _mistakesLabel;
        private Button _hintButton;
        private VisualElement _difficultyMenu;
        private Button _smartClearButton;



        private Difficulty _currentDifficulty = Difficulty.Medium;
        private int _remainingHints = 3;
        private float _elapsedSeconds = 0f;
        private bool _timerRunning = false;
        private bool _isPaused = false;

        public event System.Action<int, int> OnNumberEntered;
        public event System.Action<Difficulty> OnDifficultyChangedEvent;

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
            _boardUI = GetComponent<BoardUI>();
            _dialogUI = GetComponent<DialogUI>();
        }

        private void OnEnable()
        {
            _root = _uiDocument.rootVisualElement.Q("Root") ?? _uiDocument.rootVisualElement;

            _boardUI.Initialize(_root);
            _dialogUI.Initialize(_root);
            _boardUI.OnCellClickedEvent += HandleCellClicked;

            _timerLabel = _root.Q<Label>("TimerLabel");
            _mistakesLabel = _root.Q<Label>("MistakesLabel");

            BuildNumpad();
            BindControlButtons();
            BuildDifficultyMenu();

            _timerRunning = true;
        }

        private void OnDisable()
        {
            _boardUI.OnCellClickedEvent -= HandleCellClicked;
        }

        private void Update()
        {
            if (!_timerRunning || _timerLabel == null) return;
            _elapsedSeconds += Time.deltaTime;
            _timerLabel.text = FormatTime(_elapsedSeconds);
        }

        private string FormatTime(float seconds)
        {
            return $"{(int)seconds / 60:00}:{(int)seconds % 60:00}";
        }

        private void BuildNumpad()
        {
            var numpadContainer = _root.Q("NumpadContainer");
            if (numpadContainer == null) return;
            numpadContainer.Clear();

            for (int n = 1; n <= 9; n++)
            {
                int cn = n;
                var btn = new Button(() => OnNumpadPressed(cn)) { text = cn.ToString() };
                btn.AddToClassList("numpad__btn");

                // Yeni Eklenen Kısım: Rozet (Badge) Etiketi Oluşturma
                var badgeLabel = new Label { text = "9" }; // Başlangıçta 9 yazsın
                badgeLabel.AddToClassList("numpad__badge");
                btn.Add(badgeLabel); // Etiketi butonun içine ekliyoruz

                // Referansları saklıyoruz
                _numpadButtons[cn] = btn;
                _remainingNumberLabels[cn] = badgeLabel;

                numpadContainer.Add(btn);
            }

            var erase = new Button(() => OnNumpadPressed(0)) { text = "⌫" };
            erase.AddToClassList("numpad__btn");
            erase.AddToClassList("numpad__btn--erase");
            numpadContainer.Add(erase);
        }

        private void BindControlButtons()
        {
            _root.Q<Button>("BtnNewGame")?.RegisterCallback<ClickEvent>(_ => OnNewGameClicked());
            _root.Q<Button>("BtnPause")?.RegisterCallback<ClickEvent>(_ => TogglePause());

            _hintButton = _root.Q<Button>("BtnHint");
            _hintButton?.RegisterCallback<ClickEvent>(_ => OnHintOrSolveClicked());

            _root.Q<Button>("BtnUndo")?.RegisterCallback<ClickEvent>(_ => OnUndoClicked());
        }

        public void UpdateSmartClearUI(int clearsLeft)
        {
            if (_smartClearButton != null)
            {
                _smartClearButton.text = $"SMART CLEAR ({clearsLeft})";

                // Hak bittiyse butonu silikleştirip tıklanmaz yapabiliriz
                _smartClearButton.SetEnabled(clearsLeft > 0);
            }
        }

        private void OnUndoClicked()
        {
            // GameManager içindeki Undo fonksiyonunu çağıracağız
            gameManager.Undo();
        }



        private void OnNewGameClicked()
        {
            _dialogUI.ShowConfirmation("Are you sure you want to start a new game? Current progress will be lost.", () =>
            {
                StartNewGame();
            });
        }

        private void BuildDifficultyMenu()
        {
            _difficultyMenu = _root.Q<VisualElement>("DifficultyMenu");
            if (_difficultyMenu == null) return;

            string[] names = { "DiffBtn_Easy", "DiffBtn_Medium", "DiffBtn_Hard" };
            Difficulty[] diffs = { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard };

            for (int i = 0; i < 3; i++)
            {
                int ci = i;
                var item = _root.Q<Label>(names[i]);
                if (item != null)
                    item.RegisterCallback<ClickEvent>(_ => SelectDifficulty(diffs[ci], item));
            }
        }

        private void HandleCellClicked(int index)
        {
            if (_isPaused) return;
            _boardUI.SelectCell(index);
        }

        private void OnNumpadPressed(int number)
        {
            if (_isPaused || _boardUI.SelectedIndex < 0) return;

            int selectedIndex = _boardUI.SelectedIndex;

            if (_boardUI.IsCellGiven(selectedIndex)) return;

            if (number == 0)
            {
                if (_boardUI.IsCellError(selectedIndex))
                    _boardUI.ClearCell(selectedIndex);
                return;
            }

            OnNumberEntered?.Invoke(selectedIndex, number);
        }

        private void SelectDifficulty(Difficulty diff, Label selectedItem)
        {
            if (_currentDifficulty == diff) return;

            _dialogUI.ShowConfirmation("Are you sure? Current progress will be lost.", () =>
            {
                _currentDifficulty = diff;
                foreach (var child in _difficultyMenu.Children())
                    child.RemoveFromClassList("difficulty-menu__item--active");

                selectedItem?.AddToClassList("difficulty-menu__item--active");
                OnDifficultyChangedEvent?.Invoke(diff);
            });
        }

        private void OnHintOrSolveClicked()
        {
            if (_remainingHints > 0)
            {
                gameManager.GiveHint();
            }
            else
            {
                _dialogUI.ShowConfirmation("Are you sure you want to auto-solve the puzzle?", () =>
                {
                    gameManager.SolvePuzzle();
                });
            }
        }

        private void TogglePause()
        {
            if (_isPaused) ResumeGame();
            else PauseGame();
        }

        private void PauseGame()
        {
            _isPaused = true;
            _timerRunning = false;
            _dialogUI.ShowPause(ResumeGame, StartNewGame);
        }

        private void ResumeGame()
        {
            _isPaused = false;
            _timerRunning = true;
            _dialogUI.CloseDialog();
        }

        private void StartNewGame()
        {
            ClearGameState();
            gameManager.StartNewGame(_currentDifficulty);
        }

        public void LoadMatrixToUI(int[,] matrix)
        {
            ClearGameState();
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                    if (matrix[r, c] != 0)
                        _boardUI.SetGivenCell((r * 9) + c, matrix[r, c]);
        }

        private void ClearGameState()
        {
            _isPaused = false;

            _boardUI.ClearAllCells();
            _elapsedSeconds = 0f;
            _timerRunning = true;
            UpdateMistakeCountUI(0);
        }

        // --- Public API for GameManager ---

        public void UpdateMistakeCountUI(int count)
        {
            if (_mistakesLabel != null)
                _mistakesLabel.text = $"✕ {count} / 3  MISTAKES";
        }

        public void UpdateHintUI(int hintsLeft)
        {
            _remainingHints = hintsLeft;
            if (_hintButton != null)
                _hintButton.text = hintsLeft > 0 ? $"HINT ({hintsLeft})" : "SOLVE";
        }

        public void SetPlayerCell(int index, int number, bool isError) => _boardUI.SetPlayerCell(index, number, isError);
        public void SetHintCell(int row, int col, int number) => _boardUI.SetHintCell((row * 9) + col, number);

        public void ShowGameOverDialog() => _dialogUI.ShowGameOver(StartNewGame);
        public void ShowWinDialog()
        {
            _timerRunning = false;
            _dialogUI.ShowWin(FormatTime(_elapsedSeconds), StartNewGame);
        }

        /// <summary>
        /// GameManager, elindeki 1'den 9'a kadar olan sayıların kalan miktarlarını bir dizi halinde buraya yollayacak.
        /// remainingCounts dizisinin 0. indeksi önemsizdir, 1 ile 9 arasındaki indeksler kullanılır.
        /// </summary>
        public void UpdateRemainingNumbersUI(int[] remainingCounts)
        {
            for (int i = 1; i <= 9; i++)
            {
                if (_remainingNumberLabels[i] == null || _numpadButtons[i] == null) continue;

                int count = remainingCounts[i];

                // Eğer sayı bittiyse (0 veya daha az kaldıysa) gizleyebilir veya 0 yazdırabilirsin.
                // Ben şimdilik ekranda 0 yazmasını uygun gördüm.
                _remainingNumberLabels[i].text = count > 0 ? count.ToString() : "0";

                // Opsiyonel Görsel Geribildirim: Sayı bittiyse butonu görsel olarak silikleştirelim
                if (count <= 0)
                {
                    _numpadButtons[i].AddToClassList("numpad__btn--empty");
                }
                else
                {
                    _numpadButtons[i].RemoveFromClassList("numpad__btn--empty");
                }
            }
        }
    }

}