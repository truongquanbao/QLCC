using System;
using System.Globalization;
using System.Reflection;
using System.Windows.Forms;

namespace ApartmentManager.GUI.Forms
{
    internal sealed class UiComboItem
    {
        public UiComboItem(string text, object? value)
        {
            Text = text ?? string.Empty;
            Value = value;
        }

        public string Text { get; }

        public object? Value { get; }

        public override string ToString() => Text;
    }

    internal static class ComboBoxHelper
    {
        public static void AddOption(this ComboBox comboBox, string text, object? value)
        {
            comboBox.Items.Add(new UiComboItem(text, value));
        }

        public static string GetSelectedText(this ComboBox comboBox)
        {
            return GetItemText(comboBox.SelectedItem);
        }

        public static string GetSelectedValueString(this ComboBox comboBox)
        {
            var value = GetItemValue(comboBox.SelectedItem);
            return value?.ToString() ?? string.Empty;
        }

        public static int GetSelectedValueInt(this ComboBox comboBox, int defaultValue = 0)
        {
            var value = GetItemValue(comboBox.SelectedItem);
            if (value == null)
            {
                return defaultValue;
            }

            if (value is int intValue)
            {
                return intValue;
            }

            return int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : defaultValue;
        }

        public static void SelectValue(this ComboBox comboBox, object? targetValue)
        {
            foreach (var item in comboBox.Items)
            {
                if (Equals(GetItemValue(item), targetValue))
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }
        }

        public static object? GetItemValue(object? item)
        {
            if (item == null)
            {
                return null;
            }

            if (item is UiComboItem uiItem)
            {
                return uiItem.Value;
            }

            var type = item.GetType();
            var valueProperty = type.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
            if (valueProperty != null)
            {
                return valueProperty.GetValue(item);
            }

            foreach (var propertyName in new[]
            {
                "ApartmentID",
                "ResidentID",
                "UserID",
                "ContractID",
                "InvoiceID",
                "ComplaintID",
                "NotificationID",
                "VehicleID",
                "FeeTypeID",
                "ID"
            })
            {
                var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                if (property != null)
                {
                    return property.GetValue(item);
                }
            }

            return item;
        }

        public static string GetItemText(object? item)
        {
            if (item == null)
            {
                return string.Empty;
            }

            if (item is UiComboItem uiItem)
            {
                return uiItem.Text;
            }

            var type = item.GetType();
            foreach (var propertyName in new[] { "Text", "DisplayText", "FullName", "ApartmentCode", "BuildingName", "BlockName", "FloorNumber", "FeeTypeName", "Subject" })
            {
                var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                if (property != null)
                {
                    var value = property.GetValue(item);
                    if (value != null)
                    {
                        return value.ToString() ?? string.Empty;
                    }
                }
            }

            return item.ToString() ?? string.Empty;
        }
    }
}
