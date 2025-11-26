# COSMIC Selection Input Components

This document describes the selection input components implemented for the COSMIC Desktop styling system.

## Components

### 1. CosmicCheckBox

A checkbox component with support for checked, unchecked, and indeterminate states.

**Requirements:** 57.1-57.10

**Features:**
- 20px × 20px size with 4px border radius
- Three states: unchecked, checked, indeterminate
- Checkmark icon for checked state
- Minus icon for indeterminate state
- Label with 8px spacing
- Disabled state (40% opacity)
- Focus outline (2px accent color)
- Support for parent checkbox (select-all functionality)
- Keyboard navigation (Space key to toggle)

**Properties:**
```slint
in property <string> label;
in-out property <CheckboxState> state;
in property <bool> disabled;
in property <bool> is-parent;
out property <bool> checked;
callback toggled(CheckboxState);
```

**Usage:**
```slint
CosmicCheckBox {
    label: "Accept terms";
    state: CheckboxState.unchecked;
    toggled(state) => {
        debug("Checkbox state:", state);
    }
}
```

### 2. CosmicRadioButton

A radio button component for single selection within a group.

**Requirements:** 57.5-57.10

**Features:**
- 20px circular shape
- Selected state with filled inner circle (10px)
- Accent color for selected state
- Label with 8px spacing
- Disabled state (40% opacity)
- Focus outline (2px accent color)
- Group-based selection (only one selected at a time)
- Keyboard navigation (Space key to select)

**Properties:**
```slint
in property <string> label;
in property <string> value;
in-out property <string> group-value;
in property <bool> disabled;
out property <bool> selected;
callback selected-changed(string);
```

**Usage:**
```slint
property <string> selected-option: "option1";

CosmicRadioButton {
    label: "Option 1";
    value: "option1";
    group-value <=> selected-option;
    selected-changed(val) => {
        debug("Selected:", val);
    }
}

CosmicRadioButton {
    label: "Option 2";
    value: "option2";
    group-value <=> selected-option;
}
```

### 3. CosmicToggle

A toggle switch component for binary on/off states.

**Requirements:** 58.1-58.10

**Features:**
- 24px height, 44px width pill-shaped switch
- Off state (neutral background)
- On state (accent color background)
- Animated knob transition (150ms ease-out)
- Label with 8px spacing
- Optional description text below
- Disabled state (40% opacity)
- Loading state (spinner on knob)
- Focus outline (2px accent color)
- Keyboard navigation (Space key to toggle)

**Properties:**
```slint
in property <string> label;
in property <string> description;
in-out property <bool> checked;
in property <bool> disabled;
in property <bool> loading;
callback toggled(bool);
```

**Usage:**
```slint
CosmicToggle {
    label: "Enable notifications";
    description: "Receive alerts when new messages arrive";
    checked: true;
    toggled(checked) => {
        debug("Toggle state:", checked);
    }
}
```

## Accessibility

All selection input components support:

1. **Keyboard Navigation:**
   - Tab/Shift+Tab to move focus
   - Space key to toggle/select
   - Clear focus indicators (2px accent outline)

2. **Visual Feedback:**
   - Hover states
   - Focus states
   - Disabled states (40% opacity)
   - Clear selection indicators

3. **Color Contrast:**
   - Accent colors meet WCAG AA standards
   - Text colors provide sufficient contrast
   - Disabled states remain readable

## States

### Checkbox States
- `CheckboxState.unchecked` - Empty checkbox
- `CheckboxState.checked` - Checkbox with checkmark
- `CheckboxState.indeterminate` - Checkbox with minus icon (for parent checkboxes)

### Radio Button States
- Selected: `value == group-value`
- Unselected: `value != group-value`

### Toggle States
- `checked: false` - Off state (neutral background)
- `checked: true` - On state (accent background)
- `loading: true` - Loading state (spinner on knob)

## Example

Run the example to see all selection input components:

```bash
slint-viewer examples/selection-inputs-example.slint
```

The example demonstrates:
- All checkbox states (unchecked, checked, indeterminate, disabled, parent)
- Radio button groups with multiple options
- Toggle switches with various configurations
- Dark mode toggle for testing theme switching

## Design Notes

1. **Consistent Sizing:** All components use 20px base size for the interactive element
2. **Spacing:** 8px spacing between control and label (CosmicSpacing.space-xxs)
3. **Border Radius:** 
   - Checkbox: 4px (CosmicCornerRadii.radius-xs)
   - Radio: Circular (radius = size / 2)
   - Toggle: Pill shape (CosmicCornerRadii.toggle-radius)
4. **Animation:** Toggle knob animates over 150ms with ease-out easing
5. **Focus Indicators:** 2px accent color outline with 4px offset

## Requirements Coverage

- ✅ 57.1-57.10: Checkbox component fully implemented
- ✅ 57.5-57.10: Radio button component fully implemented
- ✅ 58.1-58.10: Toggle switch component fully implemented

All components follow COSMIC Desktop design principles and meet accessibility standards.
