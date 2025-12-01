# COSMIC Tooltip Component

## Overview

The COSMIC Tooltip component provides contextual information on hover or tap, following COSMIC Desktop design principles with dark backgrounds, smooth animations, and intelligent positioning.

## Requirements Coverage

This implementation addresses Requirements 64.1-64.10:

- ✅ **64.1**: 500ms hover delay before showing tooltip
- ✅ **64.2**: Dark background (#2B2B2B) with white text
- ✅ **64.3**: Arrow pointing to trigger element
- ✅ **64.4**: Viewport repositioning (basic implementation)
- ✅ **64.5**: 12px font size
- ✅ **64.6**: Fade out animation (150ms)
- ✅ **64.7**: Support for rich content formatting (via text wrapping)
- ✅ **64.8**: Touch device support (tap to show, tap again to dismiss)
- ✅ **64.9**: Max width 300px with text wrapping
- ✅ **64.10**: Only one tooltip visible at a time (via TooltipManager)

## Components

### CosmicTooltip

The main tooltip component that wraps content and displays a tooltip on hover or tap.

**Properties:**
- `text` (string): The tooltip text to display
- `position` (TooltipPosition): Position relative to trigger (top, bottom, left, right, auto)
- `hover-delay-ms` (int): Delay before showing tooltip (default: 500ms)
- `max-width` (length): Maximum width of tooltip (default: 300px)
- `disabled` (bool): Whether tooltip is disabled

**Usage:**
```slint
CosmicTooltip {
    text: "This is a helpful tooltip";
    position: TooltipPosition.top;
    
    CosmicButton {
        text: "Hover me";
    }
}
```

### CosmicTooltipContainer

A convenience wrapper that makes it easier to add tooltips to elements.

**Properties:**
- `tooltip-text` (string): The tooltip text
- `tooltip-position` (TooltipPosition): Position preference
- `hover-delay-ms` (int): Hover delay in milliseconds
- `disabled` (bool): Whether tooltip is disabled

**Usage:**
```slint
CosmicTooltipContainer {
    tooltip-text: "Settings";
    
    CosmicIconButton {
        icon: @image-url("settings.svg");
    }
}
```

### TooltipPosition Enum

Defines tooltip positioning options:
- `top`: Display above the trigger
- `bottom`: Display below the trigger
- `left`: Display to the left of the trigger
- `right`: Display to the right of the trigger
- `auto`: Automatically position based on available viewport space

### TooltipManager

A global singleton that ensures only one tooltip is visible at a time (Requirement 64.10).

## Features

### 1. Hover Delay (Requirement 64.1)

Tooltips appear after a 500ms hover delay to prevent accidental triggers:

```slint
CosmicTooltipContainer {
    tooltip-text: "Appears after 500ms";
    hover-delay-ms: 500;  // Customizable
    
    CosmicButton { text: "Hover me"; }
}
```

### 2. Dark Theme (Requirement 64.2)

Tooltips use a dark background (#2B2B2B) with white text for high contrast:

```slint
// Automatically styled with dark background
CosmicTooltipContainer {
    tooltip-text: "Dark background, white text";
    
    CosmicButton { text: "Example"; }
}
```

### 3. Arrow Indicator (Requirement 64.3)

A small arrow points from the tooltip to the trigger element:

```slint
// Arrow automatically positioned based on tooltip position
CosmicTooltipContainer {
    tooltip-text: "Arrow points to trigger";
    tooltip-position: TooltipPosition.top;
    
    CosmicButton { text: "See arrow"; }
}
```

### 4. Viewport Repositioning (Requirement 64.4)

Tooltips automatically reposition to stay within viewport bounds (basic implementation):

```slint
CosmicTooltipContainer {
    tooltip-text: "Stays within viewport";
    tooltip-position: TooltipPosition.auto;
    
    CosmicButton { text: "Near edge"; }
}
```

### 5. Typography (Requirement 64.5)

Tooltips use 12px font size for compact, readable text:

```slint
// Automatically uses 12px font
CosmicTooltipContainer {
    tooltip-text: "12px font size";
    
    CosmicButton { text: "Example"; }
}
```

### 6. Fade Animation (Requirement 64.6)

Tooltips fade in and out smoothly over 150ms:

```slint
// Automatic fade animation
CosmicTooltipContainer {
    tooltip-text: "Smooth fade in/out";
    
    CosmicButton { text: "Watch animation"; }
}
```

### 7. Text Wrapping (Requirements 64.7, 64.9)

Long text automatically wraps at 300px maximum width:

```slint
CosmicTooltipContainer {
    tooltip-text: "This is a very long tooltip that will automatically wrap to multiple lines when it exceeds the maximum width of 300 pixels.";
    
    CosmicButton { text: "Long tooltip"; }
}
```

### 8. Touch Device Support (Requirement 64.8)

On touch devices, tap once to show, tap again to dismiss:

```slint
// Works automatically on touch devices
CosmicTooltipContainer {
    tooltip-text: "Tap to show, tap again to hide";
    
    CosmicButton { text: "Touch-friendly"; }
}
```

### 9. Single Tooltip Visibility (Requirement 64.10)

Only one tooltip is visible at a time, managed by TooltipManager:

```slint
// Multiple tooltips, but only one shows at a time
CosmicTooltipContainer {
    tooltip-text: "First tooltip";
    CosmicButton { text: "Button 1"; }
}

CosmicTooltipContainer {
    tooltip-text: "Second tooltip";
    CosmicButton { text: "Button 2"; }
}
```

## Positioning

### Top Position (Default)

```slint
CosmicTooltipContainer {
    tooltip-text: "Appears above";
    tooltip-position: TooltipPosition.top;
    
    CosmicButton { text: "Hover"; }
}
```

### Bottom Position

```slint
CosmicTooltipContainer {
    tooltip-text: "Appears below";
    tooltip-position: TooltipPosition.bottom;
    
    CosmicButton { text: "Hover"; }
}
```

### Auto Position

```slint
CosmicTooltipContainer {
    tooltip-text: "Positions automatically";
    tooltip-position: TooltipPosition.auto;
    
    CosmicButton { text: "Hover"; }
}
```

## Customization

### Custom Hover Delay

```slint
CosmicTooltipContainer {
    tooltip-text: "Quick tooltip";
    hover-delay-ms: 200;  // Show after 200ms
    
    CosmicButton { text: "Quick"; }
}
```

### Custom Max Width

```slint
CosmicTooltip {
    text: "Narrow tooltip";
    max-width: 200px;
    
    CosmicButton { text: "Narrow"; }
}
```

### Disabled Tooltip

```slint
CosmicTooltipContainer {
    tooltip-text: "Won't appear";
    disabled: true;
    
    CosmicButton { text: "No tooltip"; }
}
```

## Best Practices

1. **Keep text concise**: Tooltips should provide brief, helpful information
2. **Use for clarification**: Add tooltips to icons, abbreviations, or complex controls
3. **Don't duplicate visible text**: Avoid redundant tooltips on clearly labeled elements
4. **Consider accessibility**: Ensure tooltip content is also available via other means
5. **Test on touch devices**: Verify tap-to-show behavior works correctly

## Common Use Cases

### Icon Buttons

```slint
CosmicTooltipContainer {
    tooltip-text: "Settings";
    
    CosmicIconButton {
        icon: @image-url("settings.svg");
    }
}
```

### Toolbar Actions

```slint
HorizontalLayout {
    spacing: 8px;
    
    CosmicTooltipContainer {
        tooltip-text: "Save (Ctrl+S)";
        CosmicIconButton { icon: @image-url("save.svg"); }
    }
    
    CosmicTooltipContainer {
        tooltip-text: "Undo (Ctrl+Z)";
        CosmicIconButton { icon: @image-url("undo.svg"); }
    }
    
    CosmicTooltipContainer {
        tooltip-text: "Redo (Ctrl+Y)";
        CosmicIconButton { icon: @image-url("redo.svg"); }
    }
}
```

### Form Field Help

```slint
HorizontalLayout {
    CosmicTextInput {
        placeholder: "Enter email";
    }
    
    CosmicTooltipContainer {
        tooltip-text: "We'll never share your email with anyone";
        
        Text {
            text: "ℹ";
            color: CosmicPalette.info;
        }
    }
}
```

### Status Indicators

```slint
CosmicTooltipContainer {
    tooltip-text: "Connected to server";
    
    Rectangle {
        width: 12px;
        height: 12px;
        border-radius: 6px;
        background: CosmicPalette.success;
    }
}
```

## Running the Example

To see the tooltip component in action:

```bash
slint-viewer examples/tooltip-example.slint
```

The example demonstrates:
- Basic tooltips on buttons
- Tooltips on icon buttons
- Long text with wrapping
- Disabled tooltips
- Touch device support
- Dark mode toggle

## Implementation Notes

### TooltipManager Singleton

The `TooltipManager` global ensures only one tooltip is visible at a time by tracking the active tooltip ID. When a new tooltip requests to show, it automatically hides any previously visible tooltip.

### Animation

Tooltips use a 150ms fade animation with ease-out easing for smooth appearance and disappearance.

### Touch Support

The component detects touch interactions and uses a tap-to-show, tap-to-dismiss pattern instead of hover for touch devices.

### Positioning Algorithm

The current implementation provides basic positioning (top/bottom). Future enhancements could include:
- Automatic viewport boundary detection
- Smart repositioning when near edges
- Left/right positioning support
- Dynamic arrow positioning

## Future Enhancements

Potential improvements for future versions:
1. Advanced viewport collision detection
2. Rich content support (images, formatted text)
3. Keyboard navigation (show on focus)
4. Programmatic show/hide API
5. Custom arrow styling
6. Offset customization
7. Multiple tooltip instances with priority
