# COSMIC Alert Component

## Overview

The `CosmicAlert` component provides notification banners with semantic colors, icons, and optional close buttons. It follows COSMIC Desktop design principles with clear visual hierarchy and sufficient contrast for accessibility.

## Requirements Implemented

- **47.1**: Warning color background (#F39C12)
- **47.2**: Close button on right side
- **47.3**: Sufficient text contrast for readability
- **47.4**: Icon support (16px with spacing)
- **47.5**: Dismissible variant with close button
- **47.6**: Persistent variant without close button
- **47.7**: Stack multiple alerts with spacing

## Features

### Alert Severities

- **Warning** (default): Orange background (#F39C12) for warnings and cautions
- **Error**: Red background for errors and critical issues
- **Success**: Green background for successful operations
- **Info**: Blue background for informational messages

### Dismissible Behavior

- **Dismissible** (default): Shows close button (×) on the right side
- **Persistent**: No close button, alert remains visible

### Icon Support

- Optional icon display at 16px size
- Icon automatically colorized to match text color
- 12px spacing between icon and message

### Text Contrast

- White text (#FFFFFF) on colored backgrounds
- Ensures WCAG AA compliance for readability
- Text wraps for long messages

## Usage

### Basic Alert

```slint
import { CosmicAlert, AlertSeverity } from "../ui/components/alert.slint";

CosmicAlert {
    message: "This is a warning message";
    severity: AlertSeverity.warning;
}
```

### Alert with Icon

```slint
CosmicAlert {
    message: "Operation completed successfully";
    severity: AlertSeverity.success;
    icon: @image-url("../ui/icons/check-circle.svg");
}
```

### Persistent Alert (No Close Button)

```slint
CosmicAlert {
    message: "This alert cannot be dismissed";
    severity: AlertSeverity.error;
    dismissible: false;  // No close button
}
```

### Handling Dismissal

```slint
alert := CosmicAlert {
    message: "Click × to dismiss";
    severity: AlertSeverity.info;
    
    dismissed => {
        // Handle dismissal event
        debug("Alert was dismissed");
    }
}
```

### Controlling Visibility

```slint
alert := CosmicAlert {
    message: "Controlled alert";
    show: show-alert;  // Bind to a boolean property
}
```

### Stacking Multiple Alerts

```slint
VerticalLayout {
    spacing: 12px;  // Space between alerts (Requirement 47.7)
    
    CosmicAlert {
        message: "First alert";
        severity: AlertSeverity.warning;
    }
    
    CosmicAlert {
        message: "Second alert";
        severity: AlertSeverity.info;
    }
    
    CosmicAlert {
        message: "Third alert";
        severity: AlertSeverity.success;
    }
}
```

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `message` | `string` | `""` | Alert message text |
| `severity` | `AlertSeverity` | `warning` | Alert type (warning, error, success, info) |
| `icon` | `image` | - | Optional icon (16px) |
| `show-icon` | `bool` | computed | Whether to show icon (true if icon provided) |
| `dismissible` | `bool` | `true` | Show close button (Requirement 47.5/47.6) |
| `show` | `bool` | `true` | Control alert visibility |

## Callbacks

| Callback | Description |
|----------|-------------|
| `dismissed()` | Fired when close button is clicked |

## Styling

### Colors

- **Warning**: `#F39C12` (Requirement 47.1)
- **Error**: `#E74C3C`
- **Success**: `#27AE60`
- **Info**: `#3498DB`
- **Text**: `#FFFFFF` (white for contrast)

### Dimensions

- **Border Radius**: 8px
- **Padding**: 16px horizontal, 12px vertical
- **Icon Size**: 16px (Requirement 47.4)
- **Close Button**: 24x24px
- **Spacing**: 12px between elements

## Accessibility

- **Contrast Ratio**: White text on colored backgrounds ensures WCAG AA compliance
- **Keyboard Navigation**: Close button is keyboard accessible
- **Screen Readers**: Message text is readable by screen readers
- **Visual Feedback**: Hover state on close button provides clear interaction feedback

## Example

Run the example to see all alert variants:

```bash
slint-viewer examples/alert-example.slint
```

## Design Notes

1. **Semantic Colors**: Each severity uses a distinct color that conveys meaning
2. **Sufficient Contrast**: White text ensures readability on all background colors
3. **Clear Dismissal**: Close button (×) is clearly visible and interactive
4. **Flexible Layout**: Text wraps for long messages, maintaining readability
5. **Consistent Spacing**: 12px spacing between icon, text, and close button
6. **Hover Feedback**: Close button shows subtle background on hover

## Related Components

- **CosmicToast**: Temporary notifications with auto-dismiss
- **CosmicSnackbar**: Bottom notifications with action buttons
- **CosmicDialog**: Modal dialogs for important messages
