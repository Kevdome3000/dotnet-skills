---
name: dotnet-winforms
version: "1.0.0"
category: "Desktop"
description: "Build, maintain, or modernize Windows Forms applications with practical guidance on designer-driven UI, event handling, data binding, and migration to modern .NET."
compatibility: "Requires a Windows Forms project on .NET or .NET Framework."
---

# Windows Forms

## Trigger On

- working on Windows Forms UI, event-driven workflows, or classic LOB applications
- migrating WinForms from .NET Framework to modern .NET
- cleaning up oversized form code or designer coupling
- implementing data binding, validation, or control customization

## Documentation

- [Windows Forms Overview](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/overview/)
- [What's New in Windows Forms](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/whats-new/)
- [Data Binding Overview](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/how-to-bind-a-windows-forms-control-to-a-type)
- [Migration Guide](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/migration/)
- [Controls Reference](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/)

### References

- [patterns.md](references/patterns.md) - WinForms architectural patterns (MVP, MVVM, Passive View), data binding patterns, validation patterns, form communication, and threading patterns
- [migration.md](references/migration.md) - Step-by-step migration guide from .NET Framework to modern .NET, common issues, deployment options, and gradual migration strategies

## Workflow

1. **Respect designer boundaries** — avoid editing generated `.Designer.cs` code directly
2. **Separate business logic** — forms should orchestrate, not contain business rules
3. **Use consistent naming** — control naming and layout should be predictable
4. **Consider MVP/MVVM patterns** — even WinForms benefits from separation
5. **Validate at runtime** — designer success alone proves very little
6. **Modernize incrementally** — choose better structure before rewriting

## Project Structure

```
MyWinFormsApp/
├── MyWinFormsApp/
│   ├── Program.cs              # Application entry
│   ├── Forms/                  # Form classes
│   │   ├── MainForm.cs
│   │   └── MainForm.Designer.cs
│   ├── Presenters/             # MVP Presenters or ViewModels
│   ├── Models/                 # Domain models
│   ├── Services/               # Business logic
│   ├── Controls/               # Custom user controls
│   └── Resources/              # Images, strings, etc.
└── MyWinFormsApp.Tests/
```

## MVP Pattern (Model-View-Presenter)

### View Interface
```csharp
public interface ICustomerView
{
    string CustomerName { get; set; }
    string CustomerEmail { get; set; }
    BindingSource CustomersBindingSource { get; }

    event EventHandler LoadRequested;
    event EventHandler SaveRequested;
    event EventHandler<int> CustomerSelected;

    void ShowError(string message);
    void ShowSuccess(string message);
}
```

### Presenter
```csharp
public class CustomerPresenter
{
    private readonly ICustomerView _view;
    private readonly ICustomerService _service;

    public CustomerPresenter(ICustomerView view, ICustomerService service)
    {
        _view = view;
        _service = service;

        _view.LoadRequested += OnLoadRequested;
        _view.SaveRequested += OnSaveRequested;
        _view.CustomerSelected += OnCustomerSelected;
    }

    private async void OnLoadRequested(object? sender, EventArgs e)
    {
        try
        {
            var customers = await _service.GetAllAsync();
            _view.CustomersBindingSource.DataSource = customers;
        }
        catch (Exception ex)
        {
            _view.ShowError($"Failed to load: {ex.Message}");
        }
    }

    private async void OnSaveRequested(object? sender, EventArgs e)
    {
        try
        {
            var customer = new Customer
            {
                Name = _view.CustomerName,
                Email = _view.CustomerEmail
            };
            await _service.SaveAsync(customer);
            _view.ShowSuccess("Customer saved successfully");
        }
        catch (Exception ex)
        {
            _view.ShowError($"Failed to save: {ex.Message}");
        }
    }

    private async void OnCustomerSelected(object? sender, int customerId)
    {
        var customer = await _service.GetByIdAsync(customerId);
        if (customer != null)
        {
            _view.CustomerName = customer.Name;
            _view.CustomerEmail = customer.Email;
        }
    }
}
```

### Form Implementation
```csharp
public partial class CustomerForm : Form, ICustomerView
{
    private readonly CustomerPresenter _presenter;
    public BindingSource CustomersBindingSource { get; } = new();

    public string CustomerName
    {
        get => txtName.Text;
        set => txtName.Text = value;
    }

    public string CustomerEmail
    {
        get => txtEmail.Text;
        set => txtEmail.Text = value;
    }

    public event EventHandler? LoadRequested;
    public event EventHandler? SaveRequested;
    public event EventHandler<int>? CustomerSelected;

    public CustomerForm(ICustomerService service)
    {
        InitializeComponent();
        _presenter = new CustomerPresenter(this, service);

        dgvCustomers.DataSource = CustomersBindingSource;
        dgvCustomers.SelectionChanged += (s, e) =>
        {
            if (dgvCustomers.CurrentRow?.DataBoundItem is Customer c)
            {
                CustomerSelected?.Invoke(this, c.Id);
            }
        };
    }

    private void CustomerForm_Load(object sender, EventArgs e)
        => LoadRequested?.Invoke(this, EventArgs.Empty);

    private void btnSave_Click(object sender, EventArgs e)
        => SaveRequested?.Invoke(this, EventArgs.Empty);

    public void ShowError(string message)
        => MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

    public void ShowSuccess(string message)
        => MessageBox.Show(message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
}
```

## Dependency Injection

```csharp
internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var services = new ServiceCollection();
        ConfigureServices(services);

        using var serviceProvider = services.BuildServiceProvider();
        var mainForm = serviceProvider.GetRequiredService<MainForm>();
        Application.Run(mainForm);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Services
        services.AddSingleton<ICustomerService, CustomerService>();
        services.AddSingleton<IOrderService, OrderService>();

        // Forms
        services.AddTransient<MainForm>();
        services.AddTransient<CustomerForm>();
        services.AddTransient<OrderForm>();
    }
}
```

## Data Binding

### BindingSource Pattern
```csharp
public partial class ProductForm : Form
{
    private readonly BindingSource _bindingSource = new();
    private readonly List<Product> _products;

    public ProductForm()
    {
        InitializeComponent();
        SetupBindings();
    }

    private void SetupBindings()
    {
        // Bind list to grid
        dgvProducts.DataSource = _bindingSource;

        // Bind current item to detail controls
        txtName.DataBindings.Add("Text", _bindingSource, "Name",
            true, DataSourceUpdateMode.OnPropertyChanged);
        txtPrice.DataBindings.Add("Text", _bindingSource, "Price",
            true, DataSourceUpdateMode.OnPropertyChanged, "0.00");

        // Enable/disable based on selection
        _bindingSource.CurrentChanged += (s, e) =>
        {
            btnEdit.Enabled = _bindingSource.Current != null;
            btnDelete.Enabled = _bindingSource.Current != null;
        };
    }

    private async Task LoadDataAsync()
    {
        var products = await _productService.GetAllAsync();
        _bindingSource.DataSource = new BindingList<Product>(products.ToList());
    }
}
```

### INotifyPropertyChanged Support
```csharp
public class Product : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private decimal _price;

    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
            }
        }
    }

    public decimal Price
    {
        get => _price;
        set
        {
            if (_price != value)
            {
                _price = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
```

## Validation

```csharp
public partial class CustomerForm : Form
{
    private readonly ErrorProvider _errorProvider = new();

    private void txtEmail_Validating(object sender, CancelEventArgs e)
    {
        if (!IsValidEmail(txtEmail.Text))
        {
            _errorProvider.SetError(txtEmail, "Invalid email address");
            e.Cancel = true;
        }
        else
        {
            _errorProvider.SetError(txtEmail, string.Empty);
        }
    }

    private void txtName_Validating(object sender, CancelEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtName.Text))
        {
            _errorProvider.SetError(txtName, "Name is required");
            e.Cancel = true;
        }
        else
        {
            _errorProvider.SetError(txtName, string.Empty);
        }
    }

    private bool IsValidEmail(string email)
    {
        return !string.IsNullOrWhiteSpace(email) &&
               email.Contains('@') &&
               email.Contains('.');
    }

    private void btnSave_Click(object sender, EventArgs e)
    {
        if (ValidateChildren(ValidationConstraints.Enabled))
        {
            // All validations passed
            SaveCustomer();
        }
    }
}
```

## Async Operations

```csharp
public partial class DataForm : Form
{
    // Good: Use async/await properly
    private async void btnLoad_Click(object sender, EventArgs e)
    {
        btnLoad.Enabled = false;
        progressBar.Visible = true;

        try
        {
            var data = await LoadDataAsync();
            dgvData.DataSource = data;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}");
        }
        finally
        {
            btnLoad.Enabled = true;
            progressBar.Visible = false;
        }
    }

    // Progress reporting
    private async void btnProcess_Click(object sender, EventArgs e)
    {
        var progress = new Progress<int>(percent =>
        {
            progressBar.Value = percent;
            lblStatus.Text = $"Processing: {percent}%";
        });

        await ProcessDataAsync(progress);
    }

    private async Task ProcessDataAsync(IProgress<int> progress)
    {
        for (int i = 0; i <= 100; i += 10)
        {
            await Task.Delay(100);
            progress.Report(i);
        }
    }
}
```

## .NET 8+ Features

```csharp
// Button commands (.NET 8+)
public partial class ModernForm : Form
{
    private readonly ICommand _saveCommand;

    public ModernForm()
    {
        InitializeComponent();

        _saveCommand = new RelayCommand(
            execute: _ => Save(),
            canExecute: _ => CanSave());

        // Bind command to button
        btnSave.Command = _saveCommand;
    }

    private bool CanSave() => !string.IsNullOrEmpty(txtName.Text);
    private void Save() { /* save logic */ }
}

// Modern system icons (.NET 8+)
var infoIcon = SystemIcons.GetStockIcon(StockIconId.Info, StockIconOptions.Large);
pictureBox.Image = infoIcon.ToBitmap();
```

## Anti-Patterns to Avoid

| Anti-Pattern | Why It's Bad | Better Approach |
|--------------|--------------|-----------------|
| Business logic in forms | Hard to test, tight coupling | Use MVP/Presenter pattern |
| Editing Designer.cs | Changes lost on regeneration | Modify in Form.cs only |
| Synchronous I/O in events | UI freezes | Use async/await |
| Giant form classes | Unmaintainable | Split into user controls |
| Direct database calls in forms | Coupling, hard to test | Use service layer |
| Ignoring validation events | Silent failures | Use ErrorProvider, Validating |
| Manual control population | Error-prone | Use data binding |
| Nested event handler logic | Spaghetti code | Extract to methods/services |

## Best Practices

1. **Use User Controls for reusable UI:**
   ```csharp
   public partial class AddressControl : UserControl
   {
       public string Street { get; set; }
       public string City { get; set; }
       public string ZipCode { get; set; }
   }
   ```

2. **Implement proper disposal:**
   ```csharp
   protected override void Dispose(bool disposing)
   {
       if (disposing)
       {
           _bindingSource?.Dispose();
           _errorProvider?.Dispose();
           components?.Dispose();
       }
       base.Dispose(disposing);
   }
   ```

3. **Use BindingList for observable collections:**
   ```csharp
   var bindingList = new BindingList<Product>(products);
   bindingList.ListChanged += (s, e) => UpdateStatus();
   dgvProducts.DataSource = bindingList;
   ```

4. **Handle high-DPI properly:**
   ```xml
   <!-- app.manifest -->
   <application xmlns="urn:schemas-microsoft-com:asm.v3">
     <windowsSettings>
       <dpiAware xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">true/pm</dpiAware>
       <dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">PerMonitorV2</dpiAwareness>
     </windowsSettings>
   </application>
   ```

5. **Configure application settings properly:**
   ```csharp
   ApplicationConfiguration.Initialize(); // .NET 6+
   Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
   Application.EnableVisualStyles();
   Application.SetCompatibleTextRenderingDefault(false);
   ```

## Testing

```csharp
[Fact]
public async Task Presenter_LoadsCustomers_OnLoadRequested()
{
    var mockView = new Mock<ICustomerView>();
    var mockService = new Mock<ICustomerService>();
    var bindingSource = new BindingSource();

    mockView.Setup(v => v.CustomersBindingSource).Returns(bindingSource);
    mockService.Setup(s => s.GetAllAsync())
        .ReturnsAsync(new[] { new Customer { Name = "Test" } });

    var presenter = new CustomerPresenter(mockView.Object, mockService.Object);

    mockView.Raise(v => v.LoadRequested += null, EventArgs.Empty);

    await Task.Delay(100); // Allow async completion
    Assert.Single((IList<Customer>)bindingSource.DataSource);
}

[Fact]
public void Presenter_ShowsError_OnLoadFailure()
{
    var mockView = new Mock<ICustomerView>();
    var mockService = new Mock<ICustomerService>();
    var bindingSource = new BindingSource();

    mockView.Setup(v => v.CustomersBindingSource).Returns(bindingSource);
    mockService.Setup(s => s.GetAllAsync()).ThrowsAsync(new Exception("DB Error"));

    var presenter = new CustomerPresenter(mockView.Object, mockService.Object);
    mockView.Raise(v => v.LoadRequested += null, EventArgs.Empty);

    mockView.Verify(v => v.ShowError(It.IsAny<string>()), Times.Once);
}
```

## Deliver

- less brittle form code and event handling
- better separation between UI and business logic
- pragmatic modernization guidance for WinForms-heavy apps
- MVP pattern with testable presenters

## Validate

- designer files stay stable
- forms are not acting as the application service layer
- Windows-only runtime behavior is tested
- async operations do not block the UI
- validation is implemented consistently
