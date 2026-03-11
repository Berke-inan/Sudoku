using System;
using UnityEngine;
using UnityEngine.UIElements;

public class BoardUI : MonoBehaviour
{
    private VisualElement _gridContainer;
    private readonly VisualElement[] _cells = new VisualElement[81];
    private readonly Label[] _cellLabels = new Label[81];

    // Çift tıklama hatasını önlemek için kullanılan zaman tutucu
    private float _lastClickTime = 0f;

    public int SelectedIndex { get; private set; } = -1;
    public event Action<int> OnCellClickedEvent;

    public void Initialize(VisualElement root)
    {
        _gridContainer = root.Q("GridContainer");
        BuildGrid();
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

            // Yazının (Label) tıklamayı engellememesi için
            label.pickingMode = PickingMode.Ignore;
            cell.Add(label);

            int index = i;

            // Dokunma başladığı an (PointerDown) tepki vermesi için ayarlandı
            cell.RegisterCallback<PointerDownEvent>(evt =>
            {
                evt.StopPropagation(); // Tıklamanın alt katmanlara sızmasını engelle
                HandleCellClick(index);
            });

            _cells[i] = cell;
            _cellLabels[i] = label;
            _gridContainer.Add(cell);
        }
    }

    private void HandleCellClick(int index)
    {
        // Android'deki çift tetiklenme (double-fire) bug'ını engellemek için 0.1 saniyelik koruma
        if (Time.unscaledTime - _lastClickTime < 0.1f) return;
        _lastClickTime = Time.unscaledTime;

        OnCellClickedEvent?.Invoke(index);
    }

    public void SelectCell(int index)
    {
        if (SelectedIndex == index)
        {
            ClearSelection();
            return;
        }

        ClearHighlights();
        SelectedIndex = index;
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
        {
            HighlightSameNumbers(num);
        }
    }

    public void HighlightSameNumbers(int number)
    {
        string s = number.ToString();
        for (int i = 0; i < 81; i++)
            if (i != SelectedIndex && _cellLabels[i].text == s)
                _cells[i].AddToClassList("cell--same-number");
    }

    public void SetGivenCell(int index, int number)
    {
        ResetCellStyles(index);
        _cellLabels[index].text = number.ToString();
        _cells[index].AddToClassList("cell--given");
        _cellLabels[index].AddToClassList("cell__number--given");
    }

    public void SetPlayerCell(int index, int number, bool isError)
    {
        ResetCellStyles(index);
        _cellLabels[index].text = number.ToString();

        if (isError)
        {
            _cells[index].AddToClassList("cell--error");
            _cellLabels[index].AddToClassList("cell__number--error");
        }
        else
        {
            _cellLabels[index].AddToClassList("cell__number--player");
            HighlightSameNumbers(number);
        }
    }

    public void SetHintCell(int index, int number)
    {
        ResetCellStyles(index);
        _cellLabels[index].text = number.ToString();
        _cellLabels[index].AddToClassList("cell__number--hint");

        if (!_cells[index].ClassListContains("cell--given"))
            _cells[index].AddToClassList("cell--given");

        HighlightSameNumbers(number);
    }

    public void ClearCell(int index)
    {
        _cellLabels[index].text = "";
        ResetCellStyles(index);
    }

    public void ClearAllCells()
    {
        ClearSelection();
        for (int i = 0; i < 81; i++)
        {
            _cellLabels[i].text = "";
            ResetCellStyles(i);
        }
    }

    private void ResetCellStyles(int index)
    {
        _cells[index].RemoveFromClassList("cell--given");
        _cells[index].RemoveFromClassList("cell--error");
        _cellLabels[index].RemoveFromClassList("cell__number--given");
        _cellLabels[index].RemoveFromClassList("cell__number--player");
        _cellLabels[index].RemoveFromClassList("cell__number--error");
        _cellLabels[index].RemoveFromClassList("cell__number--hint");
    }

    public void ClearSelection()
    {
        ClearHighlights();
        SelectedIndex = -1;
    }

    private void ClearHighlights()
    {
        for (int i = 0; i < 81; i++)
        {
            if (_cells[i] == null) continue;
            _cells[i].RemoveFromClassList("cell--selected");
            _cells[i].RemoveFromClassList("cell--related");
            _cells[i].RemoveFromClassList("cell--same-number");
        }
    }

    public bool IsCellGiven(int index) => _cells[index].ClassListContains("cell--given");
    public bool IsCellError(int index) => _cells[index].ClassListContains("cell--error");
}