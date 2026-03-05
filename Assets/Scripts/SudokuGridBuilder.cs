/*  ============================================================
    SudokuGridBuilder.cs  — Unity 6 / UI Toolkit
    ============================================================ */

using UnityEngine;
using UnityEngine.UIElements;

public enum Difficulty { Easy, Medium, Hard }

[RequireComponent(typeof(UIDocument))]
public class SudokuGridBuilder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument uiDocument;

    private VisualElement _root;
    private VisualElement _gridContainer;
    private VisualElement _numpadContainer;
    private Label _timerLabel;
    private Label _mistakesLabel;

    private readonly VisualElement[] _cells = new VisualElement[81];
    private readonly Label[] _cellLabels = new Label[81];

    private int _selectedIndex = -1;
    private bool _notesMode = false;
    private float _elapsedSeconds = 0f;
    private bool _timerRunning = false;
    private int _mistakeCount = 0;

    // Zorluk
    private Difficulty _currentDifficulty = Difficulty.Medium;
    private VisualElement _difficultyMenu;

    // ── Awake ───────────────────────────────────────────────
    private void Awake()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();
    }

    // ── Start ───────────────────────────────────────────────
    private void OnEnable()
    {
        var docRoot = uiDocument.rootVisualElement;
        _root = docRoot.Q("Root") ?? docRoot;
        _gridContainer = docRoot.Q("GridContainer");
        _numpadContainer = docRoot.Q("NumpadContainer");
        _timerLabel = docRoot.Q<Label>("TimerLabel");
        _mistakesLabel = docRoot.Q<Label>("MistakesLabel");

        if (_gridContainer == null)
        {
            Debug.LogError("[Sudoku] ❌ GridContainer NULL!");
            return;
        }

        BuildGrid();
        BuildNumpad();
        BindControlButtons();
        BuildDifficultyMenu();
        _timerRunning = true;
    }

    // ── Update — Timer ──────────────────────────────────────
    private void Update()
    {
        if (!_timerRunning || _timerLabel == null) return;
        _elapsedSeconds += Time.deltaTime;
        _timerLabel.text = $"{(int)_elapsedSeconds / 60:00}:{(int)_elapsedSeconds % 60:00}";
    }

    // ══════════════════════════════════════════════════════════
    //  GRID
    // ══════════════════════════════════════════════════════════
    private void BuildGrid()
    {
        _gridContainer.Clear();

        for (int i = 0; i < 81; i++)
        {
            int row = i / 9;
            int col = i % 9;

            var cell = new VisualElement();
            cell.AddToClassList("cell");
            cell.name = $"cell_{row}_{col}";

            if (col == 2 || col == 5) cell.AddToClassList("cell--box-right");
            if (row == 2 || row == 5) cell.AddToClassList("cell--box-bottom");

            var label = new Label { text = "" };
            label.AddToClassList("cell__number");
            label.pickingMode = PickingMode.Ignore;
            cell.Add(label);

            var pencilContainer = new VisualElement();
            pencilContainer.AddToClassList("cell__pencil-container");
            pencilContainer.name = $"pencil_{i}";
            pencilContainer.style.display = DisplayStyle.None;
            for (int p = 1; p <= 9; p++)
            {
                var pm = new Label { name = $"pm_{i}_{p}", text = "" };
                pm.AddToClassList("pencil-mark");
                pencilContainer.Add(pm);
            }
            cell.Add(pencilContainer);

            int ci = i;
            cell.RegisterCallback<ClickEvent>(_ => OnCellClicked(ci));

            _cells[i] = cell;
            _cellLabels[i] = label;
            _gridContainer.Add(cell);
        }
    }

    // ══════════════════════════════════════════════════════════
    //  NUMPAD
    // ══════════════════════════════════════════════════════════
    private void BuildNumpad()
    {
        if (_numpadContainer == null) return;
        _numpadContainer.Clear();

        for (int n = 1; n <= 9; n++)
        {
            int cn = n;
            var btn = new Button(() => OnNumpadPressed(cn));
            btn.text = n.ToString();
            btn.AddToClassList("numpad__btn");
            _numpadContainer.Add(btn);
        }

        var erase = new Button(() => OnNumpadPressed(0));
        erase.text = "⌫";
        erase.AddToClassList("numpad__btn");
        erase.AddToClassList("numpad__btn--erase");
        _numpadContainer.Add(erase);
    }

    // ══════════════════════════════════════════════════════════
    //  CONTROL BUTTONS
    // ══════════════════════════════════════════════════════════
    private void BindControlButtons()
    {
        _root.Q<Button>("BtnNewGame")?.RegisterCallback<ClickEvent>(_ => OnNewGame());
        _root.Q<Button>("BtnNotes")?.RegisterCallback<ClickEvent>(_ => OnToggleNotes());
        _root.Q<Button>("BtnUndo")?.RegisterCallback<ClickEvent>(_ => OnUndo());
        _root.Q<Button>("BtnHint")?.RegisterCallback<ClickEvent>(_ => OnHint());
    }

    // ══════════════════════════════════════════════════════════
    //  DIFFICULTY MENU
    // ══════════════════════════════════════════════════════════
    private void BuildDifficultyMenu()
    {
        // UXML'den elementleri bul
        _difficultyMenu = _root.Q<VisualElement>("DifficultyMenu");
        if (_difficultyMenu == null) return;

        string[] labels = { "Easy", "Medium", "Hard" };
        Difficulty[] diffs = { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard };
        string[] names = { "DiffBtn_Easy", "DiffBtn_Medium", "DiffBtn_Hard" };

        for (int i = 0; i < 3; i++)
        {
            int ci = i;
            var item = _root.Q<Label>(names[i]);
            if (item == null) continue;

            item.RegisterCallback<ClickEvent>(_ => SelectDifficulty(diffs[ci], item));
        }
    }

    private void SelectDifficulty(Difficulty d, Label selectedItem)
    {
        _currentDifficulty = d;

        // Active class'ı güncelle
        foreach (var child in _difficultyMenu.Children())
            child.RemoveFromClassList("difficulty-menu__item--active");
        selectedItem?.AddToClassList("difficulty-menu__item--active");

        OnDifficultyChanged(d);
    }

    // ══════════════════════════════════════════════════════════
    //  SEÇİM & HIGHLIGHT
    // ══════════════════════════════════════════════════════════
    private void OnCellClicked(int index)
    {
        if (_selectedIndex == index) { ClearSelection(); return; }

        ClearHighlights();
        _selectedIndex = index;

        int selRow = index / 9, selCol = index % 9;
        int selBox = (selRow / 3) * 3 + (selCol / 3);

        _cells[index].AddToClassList("cell--selected");

        // İlgili satır, sütun ve bloğu vurgula
        for (int i = 0; i < 81; i++)
        {
            if (i == index) continue;
            int r = i / 9, c = i % 9;
            int b = (r / 3) * 3 + (c / 3);
            if (r == selRow || c == selCol || b == selBox)
                _cells[i].AddToClassList("cell--related");
        }

        // YENİ EKLENEN KISIM: 
        // Eğer tıklanan hücrede halihazırda bir sayı varsa, o sayıları da vurgula.
        string clickedNumber = _cellLabels[index].text;
        if (!string.IsNullOrEmpty(clickedNumber))
        {
            // Parse etmeye gerek yok, zaten string olarak karşılaştırıyoruz
            if (int.TryParse(clickedNumber, out int num))
            {
                HighlightSameNumbers(num);
            }
        }
    }

    private void ClearSelection() { ClearHighlights(); _selectedIndex = -1; }

    private void ClearHighlights()
    {
        for (int i = 0; i < 81; i++)
        {
            _cells[i]?.RemoveFromClassList("cell--selected");
            _cells[i]?.RemoveFromClassList("cell--related");
            _cells[i]?.RemoveFromClassList("cell--same-number");
        }
    }

    // ══════════════════════════════════════════════════════════
    //  NUMPAD INPUT
    // ══════════════════════════════════════════════════════════
    private void OnNumpadPressed(int number)
    {
        if (_selectedIndex < 0) return;
        if (_cells[_selectedIndex].ClassListContains("cell--given")) return;

        if (number == 0)
        {
            _cellLabels[_selectedIndex].text = "";
            _cells[_selectedIndex].RemoveFromClassList("cell--error");
            _cellLabels[_selectedIndex].RemoveFromClassList("cell__number--player");
            _cellLabels[_selectedIndex].RemoveFromClassList("cell__number--error");
            return;
        }

        if (_notesMode) { TogglePencilMark(_selectedIndex, number); return; }

        _cellLabels[_selectedIndex].text = number.ToString();
        _cellLabels[_selectedIndex].AddToClassList("cell__number--player");
        HighlightSameNumbers(number);
    }

    private void TogglePencilMark(int ci, int number)
    {
        var pc = _cells[ci].Q($"pencil_{ci}");
        if (pc == null) return;
        pc.style.display = DisplayStyle.Flex;
        _cellLabels[ci].style.display = DisplayStyle.None;

        var pm = pc.Q<Label>($"pm_{ci}_{number}");
        if (pm != null) pm.text = pm.text == "" ? number.ToString() : "";

        bool any = false;
        for (int p = 1; p <= 9; p++)
            if (pc.Q<Label>($"pm_{ci}_{p}")?.text != "") { any = true; break; }

        if (!any)
        {
            pc.style.display = DisplayStyle.None;
            _cellLabels[ci].style.display = DisplayStyle.Flex;
        }
    }

    private void HighlightSameNumbers(int number)
    {
        string s = number.ToString();
        for (int i = 0; i < 81; i++)
            if (i != _selectedIndex && _cellLabels[i].text == s)
                _cells[i].AddToClassList("cell--same-number");
    }

    // ══════════════════════════════════════════════════════════
    //  PUBLIC API
    // ══════════════════════════════════════════════════════════

    public void SetGivenCell(int index, int number)
    {
        if (index < 0 || index >= 81) return;
        _cellLabels[index].text = number.ToString();
        _cells[index].AddToClassList("cell--given");
        _cellLabels[index].AddToClassList("cell__number--given");
        _cellLabels[index].RemoveFromClassList("cell__number--player");
    }

    public void LoadMatrixToUI(int[,] matrix)
    {
        ClearAllCells(); // Eski verileri temizleyip sıfır bir tahta açar

        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                int number = matrix[r, c];

                // Eğer hücrede 0 varsa (boşsa) atla, sayı varsa ekrana yaz
                if (number != 0)
                {
                    int flatIndex = (r * 9) + c;
                    SetGivenCell(flatIndex, number);
                }
            }
        }
    }

    public void SetPlayerCell(int index, int number, bool isError)
    {
        if (index < 0 || index >= 81) return;
        _cellLabels[index].text = number.ToString();
        if (isError)
        {
            _cells[index].AddToClassList("cell--error");
            _cellLabels[index].AddToClassList("cell__number--error");
            _cellLabels[index].RemoveFromClassList("cell__number--player");
            _mistakeCount++;
            if (_mistakesLabel != null)
                _mistakesLabel.text = $"✕ {_mistakeCount} / 3  MISTAKE";
        }
        else
        {
            _cells[index].RemoveFromClassList("cell--error");
            _cellLabels[index].RemoveFromClassList("cell__number--error");
            _cellLabels[index].AddToClassList("cell__number--player");
        }
    }

    public void ClearCell(int index)
    {
        if (index < 0 || index >= 81) return;
        _cellLabels[index].text = "";
        _cells[index].RemoveFromClassList("cell--error");
        _cellLabels[index].RemoveFromClassList("cell__number--player");
        _cellLabels[index].RemoveFromClassList("cell__number--error");
    }

    public void ClearAllCells()
    {
        ClearSelection();
        for (int i = 0; i < 81; i++)
        {
            _cellLabels[i].text = "";
            _cells[i].RemoveFromClassList("cell--given");
            _cells[i].RemoveFromClassList("cell--error");
            _cellLabels[i].RemoveFromClassList("cell__number--given");
            _cellLabels[i].RemoveFromClassList("cell__number--player");
            _cellLabels[i].RemoveFromClassList("cell__number--error");
            var pc = _cells[i].Q($"pencil_{i}");
            if (pc != null) pc.style.display = DisplayStyle.None;
            _cellLabels[i].style.display = DisplayStyle.Flex;
        }
        _mistakeCount = 0; _elapsedSeconds = 0f;
        if (_mistakesLabel != null) _mistakesLabel.text = "";
    }

    public Difficulty GetCurrentDifficulty() => _currentDifficulty;

    // ══════════════════════════════════════════════════════════
    //  BUTTON HANDLERS — senin SudokuBoard'un buraya bağlanır
    // ══════════════════════════════════════════════════════════
    private void OnNewGame() { ClearAllCells(); /* SudokuBoard.GenerateNewPuzzle() */ }
    private void OnToggleNotes()
    {
        _notesMode = !_notesMode;
        var btn = _root.Q<Button>("BtnNotes");
        if (_notesMode) btn?.AddToClassList("ctrl-btn--primary");
        else btn?.RemoveFromClassList("ctrl-btn--primary");
    }
    private void OnUndo() { /* SudokuBoard.Undo() */ }
    private void OnHint() { /* SudokuBoard.GiveHint() */ }
    private void OnDifficultyChanged(Difficulty d) { ClearAllCells(); /* SudokuBoard.SetDifficulty(d) */ }
}