using System.Linq;
using System.Numerics;
using System.Text;
using Content.Shared._Stalker.StalkerRepository;
using Content.Shared._Stalker.Storage;
using Content.Shared.VendingMachines;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client._Stalker.StalkerRepository;
[GenerateTypedNameReferences]
// TODO: REFACTOR ME PLEASE
public sealed partial class StalkerRepositoryMenu : DefaultWindow
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public event Action<RepositoryItemInfo, int>? RepositoryButtonPutPressed;
    public event Action<RepositoryItemInfo, int>? RepositoryButtonGetPressed;

    private (string, int) _currentCategory;
    private List<RepositoryItemInfo>? _curItems;
    private (string, int) _userCategory; // TODO: Better, faster, stronger
    private (string, int) _allCategory;
    private readonly List<(string, int)> _categories;
    private float _curWeight = 0f;
    private RepositorySlider.RepositorySlider? _slider;
    private StalkerRepositoryItemControl? _selectedControl;
    private float _maxWeight = 150f;
    public StalkerRepositoryMenu()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
        CategorySelector.OnItemSelected += OnItemSelected;
        SearchClearButton.OnPressed += _ =>
        {
            SearchLine.SetText(string.Empty, true);
        };
        SearchLine.OnTextChanged += OnTextChanged;
        _categories = new List<(string, int)>();

        AddUserCategory();
        AddAllCategory();

        _curItems = null;
    }

    private void OnTextChanged(LineEdit.LineEditEventArgs args)
    {
        if (_curItems == null)
            return;
        Clear();
        PopulateCurrentCategory(_curItems, SearchLine.Text);
    }
    public void UpdateAll(List<RepositoryItemInfo> items, List<RepositoryItemInfo> userItems, float maxWeight)
    {
        Clear();
        PopulateCategorySelector(items);
        SetupWeight(items, maxWeight);
        items.AddRange(userItems);
        _curItems = items;
        PopulateCurrentCategory(items, SearchLine.Text);
    }

    private void SetupWeight(List<RepositoryItemInfo> items, float maxWeight)
    {
        var weight = 0f;
        foreach (var el in items)
        {
            weight += el.Weight * el.Count;
        }

        _curWeight = weight;
        _maxWeight = maxWeight;
    }

    private void AddUserCategory()
    {
        var id = CategorySelector.ItemCount + 1;
        _userCategory = (Loc.GetString("repository-user-category"), id);
        CategorySelector.AddItem(_userCategory.Item1, _userCategory.Item2);
        _categories.Add(_userCategory);
        CategorySelector.SelectId(id);
        _currentCategory = _userCategory;
    }

    private void AddAllCategory()
    {
        var id = CategorySelector.ItemCount + 1;
        _allCategory = (Loc.GetString("repository-all-category"), id);
        CategorySelector.AddItem(_allCategory.Item1, _allCategory.Item2);
        _categories.Add(_allCategory);
    }
    private void PopulateCategorySelector(List<RepositoryItemInfo> items)
    {
        foreach (var item in items)
        {
            if (_categories.Any(p => p.Item1 == item.Category))
                continue;
            var id = CategorySelector.ItemCount + 1;
            CategorySelector.AddItem(item.Category, id);
            _categories.Add((item.Category, id));
        }
    }

    private void OnItemSelected(OptionButton.ItemSelectedEventArgs args)
    {
        if (args.Button.Name == null)
            return;
        CategorySelector.SelectId(args.Id);
        var selectedCategory = _categories
            .FirstOrDefault(tuple => tuple.Item2 == args.Id); // TODO: Remove LinQ
        _currentCategory = selectedCategory;

        if (_curItems == null)
            return;

        Clear();
        PopulateCurrentCategory(_curItems, SearchLine.Text);
    }
    private void PopulateCurrentCategory(List<RepositoryItemInfo> items, string? filter = null)
    {
        var spriteSys = _entityManager.EntitySysManager.GetEntitySystem<SpriteSystem>();
        ItemName.Text = Loc.GetString("repository-item-not-chosen");
        ItemDesc.Visible = false;
        ItemCategory.Visible = false;
        PutInsideButton.Visible = false;
        RepositoryWeight.Text = Loc.GetString("repository-weight-inside", ("weight", Math.Round(_curWeight, 2)), ("maxWeight", _maxWeight));
        foreach (var item in items)
        {
            if (!string.IsNullOrEmpty(filter) &&
                !item.Name.ToLowerInvariant().Contains(filter.Trim().ToLowerInvariant()))
            {
                continue;
            }
            var texture = item.Icon == null ? spriteSys.GetPrototypeIcon(item.ProductEntity).Default : null;
            var control = new StalkerRepositoryItemControl(item, texture);
            control.SelectButton.OnPressed += _ =>
            {
                _selectedControl = control;
                SetupLabels(control, item, texture);
                PutInsideButton.Text = item.UserItem
                    ? Loc.GetString("repository-insert-item")
                    : Loc.GetString("repository-eject-item");
                PutInsideButton.OnPressed -= PutInsideHandler;
                PutInsideButton.OnPressed += PutInsideHandler;
            };

            if (_currentCategory == _userCategory)
            {
                if (item.UserItem)
                {
                    ContainedItems.AddChild(control);
                    continue;
                }
            }

            if (_currentCategory == _allCategory && !item.UserItem)
            {
                ContainedItems.AddChild(control);
                continue;
            }

            if (item.Category != _currentCategory.Item1 || item.UserItem)
                continue;

            ContainedItems.AddChild(control);
        }
    }

    private void Clear()
    {
        ContainedItems.RemoveAllChildren();
    }
    private string FormatDescription(string description)
    {
        const int maxCharactersPerLine = 38;
        var formattedDescription = new StringBuilder();
        int currentIndex = 0;

        while (currentIndex + maxCharactersPerLine < description.Length)
        {
            formattedDescription.Append(description.Substring(currentIndex, maxCharactersPerLine));
            formattedDescription.Append(Environment.NewLine);
            currentIndex += maxCharactersPerLine;
        }

        formattedDescription.Append(description.Substring(currentIndex));

        return formattedDescription.ToString();
    }

    private void PutInsideHandler(BaseButton.ButtonEventArgs args)
    {
        if (_selectedControl == null)
            return;

        var control = _selectedControl;
        switch (control.ItemInfo.UserItem)
        {
            case true when control.ItemInfo.Count == 1:
                RepositoryButtonPutPressed?.Invoke(control.ItemInfo, 1);
                break;

            case true:
                {
                    if (_slider?.IsOpen == true)
                    {
                        _slider.MoveToFront();
                    }
                    else
                    {
                        _slider = new RepositorySlider.RepositorySlider(control.ItemInfo.Count);
                        _slider.ConfirmButtonPressed += () =>
                        {
                            RepositoryButtonPutPressed?.Invoke(control.ItemInfo, _slider.GetSliderValue());
                        };
                        _slider.OpenCentered();
                    }
                    break;
                }

            case false when control.ItemInfo.Count == 1:
                RepositoryButtonGetPressed?.Invoke(control.ItemInfo, 1);
                return;

            case false:
                {
                    if (_slider?.IsOpen == true)
                    {
                        _slider.MoveToFront();
                    }
                    else
                    {
                        _slider = new RepositorySlider.RepositorySlider(control.ItemInfo.Count);
                        _slider.ConfirmButtonPressed += () =>
                        {
                            RepositoryButtonGetPressed?.Invoke(control.ItemInfo, _slider.GetSliderValue());
                        };
                        _slider.OpenCentered();
                    }
                    break;
                }
        }
    }


    private void SetupLabels(StalkerRepositoryItemControl control, RepositoryItemInfo item, Texture? texture)
    {
        ItemName.Visible = true;
        ItemDesc.Visible = true;
        ItemCategory.Visible = true;
        PutInsideButton.Visible = true;
        foreach (var child in ContainedItems.Children)
        {
            if (child is not StalkerRepositoryItemControl stalkerRepositoryItemControl)
                continue;

            if (child == control)
                continue;

            stalkerRepositoryItemControl.SelectButton.Pressed = false;
        }
        PutInsideButton.Text = item.UserItem
            ? Loc.GetString("repository-insert-item")
            : Loc.GetString("repository-eject-item");
        ItemName.Text = Loc.GetString("repository-item-name-display", ("name", control.ItemInfo.Name));
        ItemDesc.Text = FormatDescription(Loc.GetString("repository-item-desc-display", ("description", control.ItemInfo.Desc)));
        ItemTexture.Texture = texture;
        ItemWeight.Text = Loc.GetString("repository-item-weight-display", ("weight", Math.Round(control.ItemInfo.Weight, 2)));
        ItemWeightSum.Text = Loc.GetString("repository-item-sum-weight-display",
            ("weight", Math.Round(control.ItemInfo.Weight * control.ItemInfo.Count, 2)));

        // If its a user category we will display summary of all weights inside this item
        if (_currentCategory == _userCategory)
        {
            ItemWeight.Text = Loc.GetString("repository-item-weight-display",
                ("weight", Math.Round(control.ItemInfo.SumWeight, 2)));
        }
        ItemCategory.Text = Loc.GetString("repository-item-category-display", ("category", control.ItemInfo.Category));

        // Setup additional info specific for this item
        AdditionalInfo.Text = control.ItemInfo.SStorageData switch
        {
            BatteryItemStalker simple => Loc.GetString("repository-battery-item-current-charge-display", ("charge", simple.CurrentCharge / 10)),
            AmmoContainerStalker ammo => Loc.GetString("repository-ammo-item-ammo-amount-display", ("amount", ammo.AmmoCount)),
            StackItemStalker stack => Loc.GetString("repository-stack-item-stack-amount-display", ("stackCount", stack.StackCount)),
            SolutionItemStalker solution => Loc.GetString("repository-solution-item-volume-display", ("volume", solution.Volume.Int())),
            AmmoItemStalker cartridge => Loc.GetString("repository-cartridge-item-exhausted-display", ("exhausted", cartridge.Exhausted ? "Spent" : "Charged")),
            _ => string.Empty
        };
    }

    private string GetName(string id)
    {
        return _proto.Index<EntityPrototype>(id).Name;
    }
}
