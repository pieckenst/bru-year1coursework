# COSMIC App Bar Component

## Overview

The `CosmicAppBar` component provides a clean, modern application bar with title display and action buttons, following COSMIC Desktop design principles.

## Requirements Implemented

- **16.1**: Surface background with minimal elevation
- **16.2**: Title display (18-20px, semibold)
- **16.3**: Action buttons (40x40px, 8px radius)
- **16.4**: Minimal elevation shadow
- **16.5**: Padding (24px horizontal, 16-20px vertical)
- **16.6**: Hover effect (surface-variant background)
- **16.7**: Menu toggle icon support

## Features

### Title Display
- 18-20px font size with semibold (600) weight
- Uses Inter font family
- Primary text color that adapts to light/dark mode

### Action Buttons
- 40x40px size with 8px border radius
- 20px icon size
- Hover effect with surface-variant background
- Support for enabled/disabled states
- Tooltip support via action structure

### Menu Toggle
- Optional menu toggle button
- Configurable icon
- Same styling as action buttons
- Separate callback for menu interactions

### Elevation
- Minimal shadow (1-2px) for subtle depth
- Adapts to light/dark mode

## Usage

### Basic App Bar

```slint
import { CosmicAppBar } from "../ui/components/app-bar.slint";

CosmicAppBar {
    title: "My Application";
}
```

### App Bar with Actions

```slint
import { CosmicAppBar, AppBarAction } from "../ui/components/app-bar.slint";

CosmicAppBar {
    title: "My Application";
    actions: [
        {
            icon: @image-url("search.svg"),
            tooltip: "Search",
            enabled: true,
        },
        {
            icon: @image-url("settings.svg"),
            tooltip: "Settings",
            enabled: true,
        },
    ];
    
    action-clicked(index) => {
        // Handle action click
        if index == 0 {
            // Search clicked
        } else if index == 1 {
            // Settings clicked
        }
    }
}
```

### App Bar with Menu Toggle

```slint
CosmicAppBar {
    title: "My Application";
    show-menu-toggle: true;
    menu-icon: @image-url("menu.svg");
    
    menu-toggle-clicked => {
        // Toggle navigation drawer
    }
}
```

### Disabled Actions

```slint
CosmicAppBar {
    title: "My Application";
    actions: [
        {
            icon: @image-url("save.svg"),
            tooltip: "Save",
            enabled: true,
        },
        {
            icon: @image-url("undo.svg"),
            tooltip: "Undo",
            enabled: false,  // Disabled state
        },
    ];
}
```

## Properties

### Input Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `title` | `string` | `""` | Title text displayed in the app bar |
| `actions` | `[AppBarAction]` | `[]` | Array of action buttons |
| `menu-icon` | `image` | - | Icon for menu toggle button |
| `show-menu-toggle` | `bool` | `false` | Whether to show menu toggle button |
| `disabled` | `bool` | `false` | Disables all interactions |

### AppBarAction Structure

| Field | Type | Description |
|-------|------|-------------|
| `icon` | `image` | Icon image for the action |
| `tooltip` | `string` | Tooltip text (for future implementation) |
| `enabled` | `bool` | Whether the action is enabled |

## Callbacks

| Callback | Parameters | Description |
|----------|------------|-------------|
| `action-clicked` | `int` (index) | Fired when an action button is clicked |
| `menu-toggle-clicked` | - | Fired when menu toggle button is clicked |

## Styling

The component automatically adapts to:
- Light/dark mode via `CosmicPalette`
- Current theme accent color
- Consistent spacing and typography

### Colors
- Background: `background-surface`
- Text: `text-primary`
- Hover: `surface-variant`

### Spacing
- Horizontal padding: 24px
- Vertical padding: 16px
- Button spacing: 8px

### Typography
- Font: Inter
- Size: 18-20px
- Weight: Semibold (600)

## Accessibility

- Clear hover states for all interactive elements
- Disabled state with reduced opacity (40%)
- Proper cursor feedback (pointer on hover)
- Keyboard navigation support (via TouchArea)

## Examples

See `app-bar-example.slint` for complete working examples demonstrating:
- Basic app bar with title only
- App bar with multiple action buttons
- App bar with menu toggle
- App bar with disabled actions

## Integration

The app bar is designed to be placed at the top of your application window:

```slint
Window {
    VerticalLayout {
        CosmicAppBar {
            title: "My App";
            // ... configuration
        }
        
        // Your main content here
        Rectangle {
            vertical-stretch: 1;
            // Content
        }
    }
}
```

## Best Practices

1. **Limit Actions**: Keep action buttons to 3-5 for optimal usability
2. **Icon Clarity**: Use clear, recognizable icons at 20px size
3. **Tooltips**: Provide descriptive tooltips for all actions
4. **Disabled States**: Use sparingly and provide visual feedback
5. **Menu Toggle**: Place on the left side for consistency with COSMIC Desktop
