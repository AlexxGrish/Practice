using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Pracrice.Services;

namespace Pracrice.Windows;

public partial class ReferenceEditWindow : Window
{
    private readonly ReferenceForm _form;
    private readonly Dictionary<string, Control> _controls = new(StringComparer.OrdinalIgnoreCase);

    public ReferenceEditWindow(ReferenceForm form)
    {
        InitializeComponent();

        _form = form;
        Title = form.Title;
        HeaderTitle.Text = form.Title;
        HeaderSubtitle.Text = form.Subtitle;

        BuildForm();
        Loaded += (_, _) => _controls.Values.FirstOrDefault()?.Focus();
    }

    private void BuildForm()
    {
        foreach (FormField field in _form.Fields)
        {
            var label = new TextBlock
            {
                Text = field.Label,
                Style = (Style)FindResource("FieldLabel")
            };
            FieldsPanel.Children.Add(label);

            Control control;
            object? value = _form.Values.GetValueOrDefault(field.Key);

            if (field.Kind == FieldKind.Lookup)
            {
                var comboBox = new ComboBox
                {
                    Height = 40,
                    Margin = new Thickness(0, 0, 0, 14),
                    ItemsSource = field.Options,
                    DisplayMemberPath = nameof(LookupItem.Name),
                    SelectedValuePath = nameof(LookupItem.Id),
                    ToolTip = field.ToolTip,
                    IsEnabled = field.IsEnabled
                };

                if (value is not null && int.TryParse(Convert.ToString(value), out int selectedId))
                {
                    comboBox.SelectedValue = selectedId;
                }

                control = comboBox;

                if (field.Options.Count == 0)
                {
                    FieldsPanel.Children.Add(new TextBlock
                    {
                        Text = "В связанном справочнике пока нет записей. Сначала добавьте их.",
                        Foreground = (System.Windows.Media.Brush)FindResource("DangerLight"),
                        FontSize = 11,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, -8, 0, 12)
                    });
                }
            }
            else
            {
                var textBox = new TextBox
                {
                    Height = 40,
                    Margin = new Thickness(0, 0, 0, 14),
                    ToolTip = field.ToolTip,
                    Text = FormatValue(value, field.Kind)
                };
                control = textBox;
            }

            _controls[field.Key] = control;
            FieldsPanel.Children.Add(control);

            if (!string.IsNullOrWhiteSpace(field.Hint))
            {
                FieldsPanel.Children.Add(new TextBlock
                {
                    Text = field.Hint,
                    Style = (Style)FindResource("MutedText"),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 8, 0, 0)
                });
            }
        }
    }

    private static string FormatValue(object? value, FieldKind kind)
    {
        if (value is null)
        {
            return string.Empty;
        }

        return kind switch
        {
            FieldKind.Decimal => Convert.ToDecimal(value, CultureInfo.CurrentCulture)
                .ToString("0.####", CultureInfo.CurrentCulture),
            FieldKind.Integer => Convert.ToInt32(value, CultureInfo.CurrentCulture)
                .ToString(CultureInfo.CurrentCulture),
            _ => Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty
        };
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (FormField field in _form.Fields)
        {
            Control control = _controls[field.Key];
            values[field.Key] = control switch
            {
                TextBox textBox => textBox.Text,
                ComboBox comboBox => comboBox.SelectedValue,
                _ => null
            };
        }

        BtnSave.IsEnabled = false;
        try
        {
            await ReferenceService.SaveAsync(_form, values);
            DialogResult = true;
        }
        catch (ValidationException exception)
        {
            DialogService.ShowWarning(exception.Message, "Проверьте данные");
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError("Не удалось сохранить запись.\n\n" + exception.Message);
        }
        finally
        {
            BtnSave.IsEnabled = true;
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
