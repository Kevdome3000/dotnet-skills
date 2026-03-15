---
name: dotnet-winui
version: "1.0.0"
category: "Desktop"
description: "Build or review WinUI 3 applications with the Windows App SDK, modern Windows desktop patterns, packaging decisions, and interop boundaries with other .NET stacks."
compatibility: "Requires a WinUI 3, Windows App SDK, or MAUI-on-Windows integration scenario."
---

# WinUI 3 and Windows App SDK

## Trigger On

- building native modern Windows desktop UI on WinUI 3
- integrating Windows App SDK features into a .NET app
- deciding between WinUI, WPF, WinForms, and MAUI for Windows work
- implementing MVVM patterns in Windows App SDK applications

## Documentation

- [WinUI 3 Overview](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/)
- [Create Your First WinUI 3 App](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/create-your-first-winui3-app)
- [Windows App SDK Overview](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/)
- [MVVM Toolkit with WinUI](https://learn.microsoft.com/en-us/windows/apps/tutorials/winui-mvvm-toolkit/intro)
- [Controls Reference](https://learn.microsoft.com/en-us/windows/apps/design/controls/)

### References

- [patterns.md](references/patterns.md) - WinUI 3 patterns including MVVM, navigation, services, and Windows App SDK integration
- [anti-patterns.md](references/anti-patterns.md) - Common WinUI mistakes and how to avoid them

## Workflow

1. **Confirm WinUI is the right choice** — use when modern Windows-native UI and Windows App SDK capabilities are needed
2. **Choose packaging model** — packaged (MSIX) vs unpackaged differ materially
3. **Apply MVVM pattern** — keep views dumb, logic in ViewModels
4. **Use Fluent Design** — leverage modern Windows 11 styling
5. **Handle Windows App SDK features** — windowing, app lifecycle, notifications
6. **Validate on Windows targets** — behavior depends on runtime environment

## Project Structure

```
MyWinUIApp/
├── MyWinUIApp/
│   ├── App.xaml                # Application entry
│   ├── MainWindow.xaml         # Main window
│   ├── Views/                  # XAML pages
│   ├── ViewModels/             # MVVM ViewModels
│   ├── Models/                 # Domain models
│   ├── Services/               # Business logic
│   ├── Helpers/                # Utility classes
│   └── Assets/                 # Images, fonts
├── MyWinUIApp (Package)/       # MSIX packaging project (if packaged)
└── MyWinUIApp.Tests/
```

## MVVM Pattern

### ViewModel with MVVM Toolkit
```csharp
public partial class ProductsViewModel : ObservableObject
{
    private readonly IProductService _productService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private ObservableCollection<Product> _products = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private Product? _selectedProduct;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadProductsCommand))]
    private bool _isLoading;

    public ProductsViewModel(IProductService productService, INavigationService navigationService)
    {
        _productService = productService;
        _navigationService = navigationService;
    }

    [RelayCommand(CanExecute = nameof(CanLoadProducts))]
    private async Task LoadProductsAsync()
    {
        IsLoading = true;
        try
        {
            var items = await _productService.GetAllAsync();
            Products = new ObservableCollection<Product>(items);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanLoadProducts() => !IsLoading;

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAsync()
    {
        if (SelectedProduct is null) return;
        await _productService.DeleteAsync(SelectedProduct.Id);
        Products.Remove(SelectedProduct);
        SelectedProduct = null;
    }

    private bool CanDelete() => SelectedProduct is not null;

    [RelayCommand]
    private void NavigateToDetail(Product product)
    {
        _navigationService.NavigateTo<ProductDetailViewModel>(product);
    }
}
```

### View Binding
```xml
<Page x:Class="MyWinUIApp.Views.ProductsPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:vm="using:MyWinUIApp.ViewModels"
      xmlns:models="using:MyWinUIApp.Models">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <CommandBar Grid.Row="0" DefaultLabelPosition="Right">
            <AppBarButton Icon="Refresh" Label="Refresh"
                          Command="{x:Bind ViewModel.LoadProductsCommand}"/>
            <AppBarButton Icon="Delete" Label="Delete"
                          Command="{x:Bind ViewModel.DeleteCommand}"/>
        </CommandBar>

        <ListView Grid.Row="1"
                  ItemsSource="{x:Bind ViewModel.Products, Mode=OneWay}"
                  SelectedItem="{x:Bind ViewModel.SelectedProduct, Mode=TwoWay}"
                  SelectionMode="Single">
            <ListView.ItemTemplate>
                <DataTemplate x:DataType="models:Product">
                    <Grid Padding="12" ColumnSpacing="12">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>
                        <TextBlock Text="{x:Bind Name}" Style="{StaticResource SubtitleTextBlockStyle}"/>
                        <TextBlock Grid.Column="1" Text="{x:Bind Price}"
                                   Style="{StaticResource BodyTextBlockStyle}"/>
                    </Grid>
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>

        <ProgressRing Grid.Row="1"
                      IsActive="{x:Bind ViewModel.IsLoading, Mode=OneWay}"
                      Visibility="{x:Bind ViewModel.IsLoading, Mode=OneWay}"/>
    </Grid>
</Page>
```

### Code-Behind with x:Bind
```csharp
public sealed partial class ProductsPage : Page
{
    public ProductsViewModel ViewModel { get; }

    public ProductsPage()
    {
        ViewModel = App.GetService<ProductsViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadProductsCommand.ExecuteAsync(null);
    }
}
```

## Dependency Injection

```csharp
public partial class App : Application
{
    private static IHost? _host;

    public App()
    {
        InitializeComponent();

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Services
                services.AddSingleton<IProductService, ProductService>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IDialogService, DialogService>();

                // ViewModels
                services.AddTransient<ProductsViewModel>();
                services.AddTransient<ProductDetailViewModel>();
                services.AddTransient<SettingsViewModel>();

                // Views
                services.AddTransient<MainWindow>();
                services.AddTransient<ProductsPage>();
                services.AddTransient<ProductDetailPage>();
            })
            .Build();
    }

    public static T GetService<T>() where T : class
        => _host!.Services.GetRequiredService<T>();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        m_window = GetService<MainWindow>();
        m_window.Activate();
    }

    private Window? m_window;
}
```

## Navigation Service

```csharp
public interface INavigationService
{
    bool CanGoBack { get; }
    void NavigateTo<TViewModel>(object? parameter = null) where TViewModel : class;
    void GoBack();
}

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private Frame? _frame;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Initialize(Frame frame) => _frame = frame;

    public bool CanGoBack => _frame?.CanGoBack ?? false;

    public void NavigateTo<TViewModel>(object? parameter = null) where TViewModel : class
    {
        var pageType = GetPageType<TViewModel>();
        _frame?.Navigate(pageType, parameter);
    }

    public void GoBack()
    {
        if (_frame?.CanGoBack == true)
        {
            _frame.GoBack();
        }
    }

    private static Type GetPageType<TViewModel>()
    {
        var viewModelName = typeof(TViewModel).Name;
        var pageName = viewModelName.Replace("ViewModel", "Page");
        var pageType = Type.GetType($"MyWinUIApp.Views.{pageName}");
        return pageType ?? throw new ArgumentException($"Page not found for {viewModelName}");
    }
}
```

## Windowing

```csharp
public sealed partial class MainWindow : Window
{
    private AppWindow _appWindow;

    public MainWindow()
    {
        InitializeComponent();

        // Get AppWindow for advanced windowing
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        // Customize title bar
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            var titleBar = _appWindow.TitleBar;
            titleBar.ExtendsContentIntoTitleBar = true;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        }

        // Set window size and position
        _appWindow.Resize(new SizeInt32(1200, 800));
        _appWindow.Move(new PointInt32(100, 100));
    }

    // Center window on screen
    private void CenterOnScreen()
    {
        var displayArea = DisplayArea.GetFromWindowId(_appWindow.Id, DisplayAreaFallback.Primary);
        var centerX = (displayArea.WorkArea.Width - _appWindow.Size.Width) / 2;
        var centerY = (displayArea.WorkArea.Height - _appWindow.Size.Height) / 2;
        _appWindow.Move(new PointInt32(centerX, centerY));
    }
}
```

## Theming

```csharp
public class ThemeService
{
    public void SetTheme(ElementTheme theme)
    {
        if (App.MainWindow.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme;
        }
    }

    public ElementTheme GetCurrentTheme()
    {
        if (App.MainWindow.Content is FrameworkElement rootElement)
        {
            return rootElement.RequestedTheme;
        }
        return ElementTheme.Default;
    }
}
```

```xml
<!-- App.xaml - Custom theme colors -->
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls"/>
        </ResourceDictionary.MergedDictionaries>

        <!-- Custom accent colors -->
        <SolidColorBrush x:Key="SystemAccentColor" Color="#0078D4"/>

        <!-- Custom styles -->
        <Style x:Key="PrimaryButtonStyle" TargetType="Button">
            <Setter Property="Background" Value="{ThemeResource SystemAccentColor}"/>
            <Setter Property="Foreground" Value="White"/>
            <Setter Property="CornerRadius" Value="4"/>
        </Style>
    </ResourceDictionary>
</Application.Resources>
```

## Dialogs

```csharp
public class DialogService : IDialogService
{
    public async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = "Yes",
            CloseButtonText = "No",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = App.MainWindow.Content.XamlRoot
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    public async Task ShowErrorAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = App.MainWindow.Content.XamlRoot
        };

        await dialog.ShowAsync();
    }
}
```

## Packaging Options

### Packaged (MSIX)
```xml
<!-- Package.appxmanifest -->
<Package>
  <Identity Name="MyCompany.MyWinUIApp" Publisher="CN=MyCompany" Version="1.0.0.0"/>
  <Properties>
    <DisplayName>My WinUI App</DisplayName>
    <PublisherDisplayName>My Company</PublisherDisplayName>
  </Properties>
  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.17763.0" MaxVersionTested="10.0.22621.0"/>
  </Dependencies>
  <Applications>
    <Application Id="App" Executable="MyWinUIApp.exe" EntryPoint="MyWinUIApp.App">
      <uap:VisualElements DisplayName="My WinUI App" BackgroundColor="transparent">
        <uap:DefaultTile Wide310x150Logo="Assets\Wide310x150Logo.png"/>
      </uap:VisualElements>
    </Application>
  </Applications>
  <Capabilities>
    <rescap:Capability Name="runFullTrust"/>
  </Capabilities>
</Package>
```

### Unpackaged
```xml
<!-- .csproj for unpackaged -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
    <UseWinUI>true</UseWinUI>
    <WindowsPackageType>None</WindowsPackageType>
  </PropertyGroup>
</Project>
```

## Anti-Patterns to Avoid

| Anti-Pattern | Why It's Bad | Better Approach |
|--------------|--------------|-----------------|
| Logic in code-behind | Hard to test | Use MVVM with ViewModels |
| Ignoring x:Bind | Poor performance | Use compiled bindings |
| Blocking UI thread | Frozen UI | Use async/await |
| Hardcoded styles | Inconsistent theming | Use resource dictionaries |
| Ignoring packaging choice | Deployment issues | Choose packaged vs unpackaged early |
| Direct service access in views | Tight coupling | Use dependency injection |
| Ignoring XamlRoot | Dialog failures | Always set XamlRoot for dialogs |
| Manual property notifications | Boilerplate, errors | Use MVVM Toolkit attributes |

## Best Practices

1. **Use x:Bind for compiled bindings:**
   ```xml
   <TextBlock Text="{x:Bind ViewModel.Title, Mode=OneWay}"/>
   ```

2. **Implement proper navigation:**
   ```csharp
   protected override void OnNavigatedTo(NavigationEventArgs e)
   {
       base.OnNavigatedTo(e);
       if (e.Parameter is Product product)
       {
           ViewModel.Initialize(product);
       }
   }
   ```

3. **Use InfoBar for notifications:**
   ```xml
   <InfoBar x:Name="SuccessInfoBar"
            Title="Success"
            Message="Changes saved"
            Severity="Success"
            IsOpen="{x:Bind ViewModel.ShowSuccess, Mode=OneWay}"/>
   ```

4. **Handle app lifecycle:**
   ```csharp
   public App()
   {
       InitializeComponent();

       // Handle suspension
       Suspending += (s, e) =>
       {
           var deferral = e.SuspendingOperation.GetDeferral();
           // Save state
           deferral.Complete();
       };
   }
   ```

5. **Virtualize large lists:**
   ```xml
   <ListView ItemsSource="{x:Bind ViewModel.Items}"
             VirtualizingStackPanel.VirtualizationMode="Recycling">
   ```

6. **Use semantic zoom for large datasets:**
   ```xml
   <SemanticZoom>
       <SemanticZoom.ZoomedInView>
           <ListView ItemsSource="{x:Bind ViewModel.GroupedItems}"/>
       </SemanticZoom.ZoomedInView>
       <SemanticZoom.ZoomedOutView>
           <GridView ItemsSource="{x:Bind ViewModel.GroupHeaders}"/>
       </SemanticZoom.ZoomedOutView>
   </SemanticZoom>
   ```

## Testing

```csharp
[Fact]
public async Task LoadProducts_UpdatesCollection()
{
    var mockService = new Mock<IProductService>();
    var mockNavigation = new Mock<INavigationService>();
    mockService.Setup(s => s.GetAllAsync())
        .ReturnsAsync(new[] { new Product { Name = "Test" } });

    var viewModel = new ProductsViewModel(mockService.Object, mockNavigation.Object);

    await viewModel.LoadProductsCommand.ExecuteAsync(null);

    Assert.Single(viewModel.Products);
    Assert.Equal("Test", viewModel.Products[0].Name);
}

[Fact]
public void DeleteCommand_CannotExecute_WhenNoSelection()
{
    var mockService = new Mock<IProductService>();
    var mockNavigation = new Mock<INavigationService>();
    var viewModel = new ProductsViewModel(mockService.Object, mockNavigation.Object);

    viewModel.SelectedProduct = null;

    Assert.False(viewModel.DeleteCommand.CanExecute(null));
}
```

## Deliver

- modern Windows UI code with clear platform boundaries
- explicit deployment and packaging assumptions
- cleaner interop between shared and Windows-specific layers
- MVVM pattern with testable ViewModels

## Validate

- WinUI is chosen for a real product reason
- Windows App SDK dependencies are explicit
- packaging and runtime assumptions are tested
- x:Bind is used for compiled bindings
- navigation and dialogs work correctly
