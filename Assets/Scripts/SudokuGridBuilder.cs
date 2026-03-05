/* ============================================================
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
    [SerializeField] private GameManager gameManager;

    private VisualElement _root;
    private VisualElement _gridContainer;
    private VisualElement _numpadContainer;
    private Label _timerLabel;
    private Label _mistakesLabel;

    private readonly VisualElement[] _cells = new VisualElement[81];
    private readonly Label[] _cellLabels = new Label[81];

    private int _selectedIndex = -1;
    private float _elapsedSeconds = 0f;
    private bool _timerRunning = false;

    // Events
    public event System.Action<int, int> OnNumberEntered;
    public event System.Action<Difficulty> OnDifficultyChangedEvent;

    // Zorluk
    private Difficulty _currentDifficulty = Difficulty.Medium;
    private VisualElement _difficultyMenu;

    private void Awake()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();
    }

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

    private void Update()
    {
        if (!_timerRunning || _timerLabel == null) return;
        _elapsedSeconds += Time.deltaTime;
        _timerLabel.text = $"{(int)_elapsedSeconds / 60:00}:{(int)_elapsedSeconds % 60:00}";
    }

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

            int ci = i;
            cell.RegisterCallback<ClickEvent>(_ => OnCellClicked(ci));

            _cells[i] = cell;
            _cellLabels[i] = label;
            _gridContainer.Add(cell);
        }
    }

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

    private void BindControlButtons()
    {
        _root.Q<Button>("BtnNewGame")?.RegisterCallback<ClickEvent>(_ => OnNewGame());
        _root.Q<Button>("BtnHint")?.RegisterCallback<ClickEvent>(_ => OnHint());
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
            if (item == null) continue;
            item.RegisterCallback<ClickEvent>(_ => SelectDifficulty(diffs[ci], item));
        }
    }

    private void SelectDifficulty(Difficulty d, Label selectedItem)
    {
        if (_currentDifficulty == d) return;
        ShowConfirmationDialog("Are you sure? Current progress will be lost.", () =>
        {
            _currentDifficulty = d;
            foreach (var child in _difficultyMenu.Children())
                child.RemoveFromClassList("difficulty-menu__item--active");
            selectedItem?.AddToClassList("difficulty-menu__item--active");
            OnDifficultyChangedEvent?.Invoke(d);
        });
    }

    private void ShowConfirmationDialog(string message, System.Action onConfirm)
    {
        var overlay = CreateOverlay();
        var box = CreateDialogBox();

        var label = new Label(message);
        label.style.color = new StyleColor(Color.black);
        label.style.fontSize = 16;
        label.style.marginBottom = 20;
        label.style.whiteSpace = WhiteSpace.Normal;

        var btnRow = new VisualElement();
        btnRow.style.flexDirection = FlexDirection.Row;

        var btnYes = new Button(() => { onConfirm?.Invoke(); _root.Remove(overlay); }) { text = "YES" };
        btnYes.style.width = 100; btnYes.style.height = 40; btnYes.style.marginRight = 10;
        btnYes.style.backgroundColor = new StyleColor(new Color(0.8f, 0.2f, 0.2f, 1f));
        btnYes.style.color = new StyleColor(Color.white);

        var btnNo = new Button(() => { _root.Remove(overlay); }) { text = "NO" };
        btnNo.style.width = 100; btnNo.style.height = 40;

        btnRow.Add(btnYes); btnRow.Add(btnNo);
        box.Add(label); box.Add(btnRow);
        overlay.Add(box); _root.Add(overlay);
    }

    public void ShowGameOverDialog()
    {
        var overlay = CreateOverlay();
        overlay.style.backgroundColor = new StyleColor(new Color(0, 0, 0, 0.9f));

        var box = CreateDialogBox();

        var title = new Label("GAME OVER");
        title.style.color = new StyleColor(new Color(0.8f, 0.2f, 0.2f, 1f));
        title.style.fontSize = 24;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.marginBottom = 10;

        // İNGİLİZCE AÇIKLAMA: 3 hata hakkı doldu, 4. hata oyun bitirdi.
        var description = new Label("You've used all 3 lives. The 4th mistake ended the game. Try again!");
        description.style.color = new StyleColor(Color.black);
        description.style.fontSize = 14;
        description.style.marginBottom = 20;
        description.style.whiteSpace = WhiteSpace.Normal;
        description.style.unityTextAlign = TextAnchor.MiddleCenter;

        var btnNewGame = new Button(() => {
            gameManager.StartNewGame(_currentDifficulty);
            _root.Remove(overlay);
        })
        { text = "START NEW GAME" };

        btnNewGame.style.width = 200; btnNewGame.style.height = 45;
        btnNewGame.style.backgroundColor = new StyleColor(new Color(0.15f, 0.15f, 0.15f, 1f));
        btnNewGame.style.color = new StyleColor(Color.white);

        box.Add(title);
        box.Add(description);
        box.Add(btnNewGame);
        overlay.Add(box);
        _root.Add(overlay);
    }

    private void OnCellClicked(int index)
    {
        if (_selectedIndex == index) { ClearSelection(); return; }
        ClearHighlights();
        _selectedIndex = index;
        _cells[index].AddToClassList("cell--selected");

        int selRow = index / 9, selCol = index % 9;
        int selBox = (selRow / 3) * 3 + (selCol / 3);

        for (int i = 0; i < 81; i++)
        {
            if (i == index) continue;
            int r = i / 9, c = i % 9;
            int b = (r / 3) * 3 + (c / 3);
            if (r == selRow || c == selCol || b == selBox)
                _cells[i].AddToClassList("cell--related");
        }

        string clickedNumber = _cellLabels[index].text;
        if (!string.IsNullOrEmpty(clickedNumber) && int.TryParse(clickedNumber, out int num))
            HighlightSameNumbers(num);
    }

    private void OnNumpadPressed(int number)
    {
        if (_selectedIndex < 0) return;

        // Başlangıç sayıları silinemez/değiştirilemez
        if (_cells[_selectedIndex].ClassListContains("cell--given")) return;

        // SİLME İŞLEMİ (⌫)
        if (number == 0)
        {
            // ÖNEMLİ: Sadece hata (error) olan hücreleri silmeye izin veriyoruz.
            // Eğer hücrede sayı varsa ama hata sınıfı yoksa, bu doğru bir sayıdır ve silinmez.
            if (!_cells[_selectedIndex].ClassListContains("cell--error")) return;

            _cellLabels[_selectedIndex].text = "";
            _cells[_selectedIndex].RemoveFromClassList("cell--error");
            _cellLabels[_selectedIndex].RemoveFromClassList("cell__number--player");
            _cellLabels[_selectedIndex].RemoveFromClassList("cell__number--error");
            return;
        }

        OnNumberEntered?.Invoke(_selectedIndex, number);
    }

    public void UpdateMistakeCountUI(int count)
    {
        if (_mistakesLabel == null) _mistakesLabel = _root?.Q<Label>("MistakesLabel");
        if (_mistakesLabel != null) _mistakesLabel.text = $"✕ {count} / 3  MISTAKES";
    }

    private void HighlightSameNumbers(int number)
    {
        string s = number.ToString();
        for (int i = 0; i < 81; i++)
            if (i != _selectedIndex && _cellLabels[i].text == s)
                _cells[i].AddToClassList("cell--same-number");
    }

    public void SetGivenCell(int index, int number)
    {
        if (index < 0 || index >= 81) return;
        _cellLabels[index].text = number.ToString();
        _cellLabels[index].RemoveFromClassList("cell__number--hint");
        _cellLabels[index].RemoveFromClassList("cell__number--player");
        _cells[index].AddToClassList("cell--given");
        _cellLabels[index].AddToClassList("cell__number--given");
    }

    public void LoadMatrixToUI(int[,] matrix)
    {
        ClearAllCells();
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                if (matrix[r, c] != 0) SetGivenCell((r * 9) + c, matrix[r, c]);
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
        }
        else
        {
            _cells[index].RemoveFromClassList("cell--error");
            _cellLabels[index].RemoveFromClassList("cell__number--error");
            _cellLabels[index].AddToClassList("cell__number--player");
            HighlightSameNumbers(number);
        }
    }

    public void ShowWinDialog()
    {
        // 1. Oyun kazandığında zamanlayıcıyı durdur
        _timerRunning = false;

        // Süreyi dakika ve saniye olarak formatla
        string finalTime = $"{(int)_elapsedSeconds / 60:00}:{(int)_elapsedSeconds % 60:00}";

        var overlay = CreateOverlay();
        var box = CreateDialogBox();

        // Tebrik Başlığı
        var title = new Label("CONGRATULATIONS!");
        title.style.color = new StyleColor(new Color(0.1f, 0.6f, 0.1f, 1f));
        title.style.fontSize = 24;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.marginBottom = 10;

        // Çözüldü Mesajı
        var message = new Label("You have solved the entire puzzle!");
        message.style.color = new StyleColor(Color.black);
        message.style.fontSize = 16;
        message.style.marginBottom = 5;

        // YENİ: Bitirme Süresi Etiketi
        var timeLabel = new Label($"Completion Time: {finalTime}");
        timeLabel.style.color = new StyleColor(new Color(0.2f, 0.2f, 0.2f, 1f));
        timeLabel.style.fontSize = 18;
        timeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        timeLabel.style.marginBottom = 30;

        var btnPlayAgain = new Button(() => {
            gameManager.StartNewGame(_currentDifficulty);
            _root.Remove(overlay);
        })
        { text = "PLAY AGAIN" };

        btnPlayAgain.style.width = 180; btnPlayAgain.style.height = 50;
        btnPlayAgain.style.backgroundColor = new StyleColor(new Color(0.15f, 0.15f, 0.15f, 1f));
        btnPlayAgain.style.color = new StyleColor(Color.white);

        box.Add(title);
        box.Add(message);
        box.Add(timeLabel); // Süreyi kutuya ekle
        box.Add(btnPlayAgain);

        overlay.Add(box);
        _root.Add(overlay);
    }

    public void SetHintCell(int row, int col, int number)
    {
        int index = (row * 9) + col;
        if (index < 0 || index >= 81) return;
        _cellLabels[index].text = number.ToString();
        _cellLabels[index].RemoveFromClassList("cell__number--given");
        _cellLabels[index].RemoveFromClassList("cell__number--player");
        _cellLabels[index].RemoveFromClassList("cell__number--error");
        _cellLabels[index].AddToClassList("cell__number--hint");
        if (!_cells[index].ClassListContains("cell--given")) _cells[index].AddToClassList("cell--given");
        HighlightSameNumbers(number);
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
            _cellLabels[i].RemoveFromClassList("cell__number--hint");
        }
        _elapsedSeconds = 0f;
        _timerRunning = true;
        UpdateMistakeCountUI(0);
    }

    private void ClearSelection() { ClearHighlights(); _selectedIndex = -1; }
    private void ClearHighlights() { for (int i = 0; i < 81; i++) { _cells[i]?.RemoveFromClassList("cell--selected"); _cells[i]?.RemoveFromClassList("cell--related"); _cells[i]?.RemoveFromClassList("cell--same-number"); } }
    private void OnNewGame() { ClearAllCells(); gameManager.StartNewGame(_currentDifficulty); }
    private void OnHint() { gameManager.GiveHint(); }

    // Yardımcı UI oluşturucular
    private VisualElement CreateOverlay()
    {
        var overlay = new VisualElement();
        overlay.style.position = Position.Absolute;
        overlay.style.left = 0; overlay.style.right = 0; overlay.style.top = 0; overlay.style.bottom = 0;
        overlay.style.backgroundColor = new StyleColor(new Color(0, 0, 0, 0.7f));
        overlay.style.alignItems = Align.Center; overlay.style.justifyContent = Justify.Center;
        return overlay;
    }

    private VisualElement CreateDialogBox()
    {
        var box = new VisualElement();
        box.style.backgroundColor = new StyleColor(new Color(0.95f, 0.95f, 0.95f, 1f));
        box.style.paddingTop = 30; box.style.paddingBottom = 30;
        box.style.paddingLeft = 40; box.style.paddingRight = 40;
        box.style.borderTopLeftRadius = 12; box.style.borderTopRightRadius = 12;
        box.style.borderBottomLeftRadius = 12; box.style.borderBottomRightRadius = 12;
        box.style.alignItems = Align.Center;
        return box;
    }
}