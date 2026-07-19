using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;

namespace Frisson.Desktop.ViewModels;

/// <summary>
/// Observable wrapper around UiItem for XAML binding.
/// Fires SendValue when the user changes a value.
/// </summary>
public class RemoteUiItemViewModel : INotifyPropertyChanged
{
    public Core.Scheme.Remote.UiItem Source { get; }

    public string Type => Source.Type;
    public string Key => Source.Key;
    public string? Label => Source.Label;
    public string? Title => Source.Title;
    public double? Min => Source.Min;
    public double? Max => Source.Max;
    public double Step => Source.Step;
    public List<string>? Options => Source.Options;
    public bool IsGroup => Type == "group";
    public bool IsReadOnly => Type == "text";

    private bool _isEnabled;
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value) return;
            _isEnabled = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
        }
    }

    private double _numericValue;
    public double NumericValue
    {
        get => _numericValue;
        set
        {
            if (_numericValue == value) return;
            _numericValue = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NumericValue)));
            OnUserValueChanged();
        }
    }

    private bool _boolValue;
    public bool BoolValue
    {
        get => _boolValue;
        set
        {
            if (_boolValue == value) return;
            _boolValue = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BoolValue)));
            OnUserValueChanged();
        }
    }

    private string _stringValue = string.Empty;
    public string StringValue
    {
        get => _stringValue;
        set
        {
            if (_stringValue == value) return;
            _stringValue = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StringValue)));
            OnUserValueChanged();
        }
    }

    /// <summary>
    /// Called when the user changes a control value in the UI.
    /// Parameters: (key, rawValue)
    /// </summary>
    public Action<string, object?>? SendValue { get; set; }

    public RemoteUiItemViewModel(Core.Scheme.Remote.UiItem source)
    {
        Source = source;

        switch (source.Type)
        {
            case "number":
                _numericValue = source.Value.HasValue && source.Value.Value.ValueKind == JsonValueKind.Number
                    ? source.Value.Value.GetDouble()
                    : source.Min ?? 0;
                break;
            case "switch":
                _boolValue = source.Value.HasValue && source.Value.Value.ValueKind == JsonValueKind.True;
                break;
            case "select":
                _stringValue = source.Value.HasValue && source.Value.Value.ValueKind == JsonValueKind.String
                    ? source.Value.Value.GetString() ?? string.Empty
                    : source.Options?.FirstOrDefault() ?? string.Empty;
                break;
            case "text":
                _stringValue = source.Value.HasValue && source.Value.Value.ValueKind == JsonValueKind.String
                    ? source.Value.Value.GetString() ?? string.Empty
                    : string.Empty;
                break;
        }
    }

    /// <summary>
    /// Updates this ViewModel with a value received from the Remote.
    /// Does NOT fire SendValue (avoids echo loop).
    /// </summary>
    internal void UpdateFromRemote(object? rawValue)
    {
        switch (Type)
        {
            case "number":
                if (rawValue is double dv && _numericValue != dv)
                {
                    _numericValue = dv;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NumericValue)));
                }
                break;
            case "switch":
                if (rawValue is bool bv && _boolValue != bv)
                {
                    _boolValue = bv;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BoolValue)));
                }
                break;
            case "select":
                var sv = rawValue?.ToString() ?? string.Empty;
                if (_stringValue != sv)
                {
                    _stringValue = sv;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StringValue)));
                }
                break;
        }
    }

    private void OnUserValueChanged()
    {
        if (IsReadOnly) return;

        object? val = Type switch
        {
            "number" => _numericValue,
            "switch" => _boolValue,
            "select" => _stringValue,
            _ => null
        };

        if (val != null)
            SendValue?.Invoke(Key, val);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
