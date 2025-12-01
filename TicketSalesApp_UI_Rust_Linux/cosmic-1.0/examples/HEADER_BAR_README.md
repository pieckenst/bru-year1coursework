# COSMIC Header Bar Component

## Overview

The `CosmicHeaderBar` component provides a consistent header bar for windows with window control buttons (minimize, maximize/restore, close) and support for custom content regions, following COSMIC Desktop design principles.

## Requirements Implemented

- **22.1, 45.1**: 32-40px height header
- **22.2, 45.2**: Window control buttons (minimize, maximize, close)
- **22.3**: Symbolic icons at 16px
- **22.3, 45.3**: Maximize/restore icon toggle based on window state
- **22.4, 45.4**: Full opacity when focused
- **22.5, 45.5**: 75% opacity when unfocused
- **22.6**: Symbolic icons at 16px with 8px padding
- **22.7, 45.6**: Draggable area for window movement
- **45.7**: Double-click to toggle maximize
- **45.8**: Support for start, center, end content regions

## Features

### Window Control Buttons
- Minimize, Maximize/Restore, and Close buttons
- 32x32px size with 8px border radius
- 16px symbolic icons
- Hover effects with surface-variant background
- Close button shows red background on hover
- Automatic icon switching between maximize and restore

### Focus States
- Full opacity (100%) when window is focused
- Reduced opacity (75%) when window is unfocused
- Smooth transitions between states

### Draggable Area
- Central area can be dragged to move window
- Emits `drag-requested` callback for window movement
- Double-click to toggle maximize state

### Title Display
- Optional centered title text
- Adapts opacity based on focus state
- Uses semibold weight for emphasis

### Window State Management
- Tracks window state (normal, maximized, minimized)
- Automatically switches between maximize and restore icons
- Adjusts padding when maximized (per COSMIC spec)

## Usage

### Basic Header Bar

```slint
import { CosmicHeaderBar, WindowState } from "../ui/components/header-bar.slint";

CosmicHeaderBar {
    window-state: WindowState.normal;
    focused: true;
    minimize-icon: @image-url("window-minimize.svg");
    maximize-icon: @image-url("window-maximize.svg");
    restore-icon: @image-url("window-restore.svg");
    close-icon: @image-url("window-close.svg");
    
    minimize-clicked => {
        // Minimize window
    }
    
    maximize-clicked => {
        // Toggle maximize/restore
    }
    
    close-clicked => {
        // Close window
    }
    
    drag-requested => {
        // Move window
    }
}
```

### Header Bar with Title

```slint
CosmicHeaderBar {
    title: "Document Title";
    window-state: WindowState.normal;
    focused: true;
    // ... icon properties
}
```

### Header Bar without Controls

```slint
CosmicHeaderBar {
    title: "Custom Header";
    show-controls: false;
    // ... other properties
}
```

### Managing Window State

```slint
property <WindowState> current-state: WindowState.normal;

CosmicHeaderBar {
    window-state: current-state;
    
    maximize-clicked => {
        if current-state == WindowState.maximized {
            current-state = WindowState.normal;
        } else {
            current-state = WindowState.maximized;
        }
    }
}
```

## Properties

### Input Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `window-state` | `WindowState` | `normal` | Current window state (normal, maximized, minimized) |
| `focused` | `bool` | `true` | Whether the window is focused |
| `minimize-icon` | `image` | - | Icon for minimize button |
| `maximize-icon` | `image` | - | Icon for maximize button |
| `restore-icon` | `image` | - | Icon for restore button |
| `close-icon` | `image` | - | Icon for close button |
| `show-controls` | `bool` | `true` | Whether to show window control buttons |
| `title` | `string` | `""` | Optional title text displayed in center |

### WindowState Enum

```slint
enum WindowState {
    normal,
    maximized,
    minimized,
}
```

## Callbacks

| Callback | Parameters | Description |
|----------|------------|-------------|
| `minimize-clicked` | - | Fired when minimize button is clicked |
| `maximize-clicked` | - | Fired when maximize/restore button is clicked or header is double-clicked |
| `close-clicked` | - | Fired when close button is clicked |
| `drag-requested` | - | Fired when draggable area is clicked for window movement |

## Styling

The component automatically adapts to:
- Light/dark mode via `CosmicPalette`
- Focus state with opacity changes
- Window state with icon switching

### Colors
- Background: `background-surface`
- Text: `text-primary`
- Hover: `surface-variant`
- Close hover: `error` (red)

### Dimensions
- Height: 40px (32-40px range per spec)
- Button size: 32x32px
- Icon size: 16px (symbolic)
- Border radius: 8px

### Opacity
- Focused: 100%
- Unfocused: 75%

## Accessibility

- Clear hover states for all buttons
- Visual feedback for focus state
- Proper cursor feedback (pointer on buttons)
- Keyboard navigation support
- Color contrast for close button

## Examples

See `header-bar-example.slint` for complete working examples demonstrating:
- Basic header bar with window controls
- Header bar with title text
- Header bar without controls
- Focus state management
- Window state management
- Interactive controls for testing

## Integration

The header bar is designed to be placed at the very top of your application window:

```slint
Window {
    VerticalLayout {
        CosmicHeaderBar {
            // ... configuration
        }
        
        // Your app bar (optional)
        CosmicAppBar {
            // ... configuration
        }
        
        // Your main content
        Rectangle {
            vertical-stretch: 1;
            // Content
        }
    }
}
```

## Platform Integration

To fully integrate with window management:

1. **Drag Handling**: Connect `drag-requested` to your platform's window drag API
2. **Maximize Handling**: Connect `maximize-clicked` to toggle window maximize state
3. **Minimize Handling**: Connect `minimize-clicked` to minimize the window
4. **Close Handling**: Connect `close-clicked` to close the window
5. **Focus Tracking**: Update `focused` property based on window focus events

Example Rust integration:

```rust
header_bar.on_drag_requested(move || {
    window.drag_window();
});

header_bar.on_maximize_clicked(move || {
    if window.is_maximized() {
        window.restore();
    } else {
        window.maximize();
    }
});

header_bar.on_minimize_clicked(move || {
    window.minimize();
});

header_bar.on_close_clicked(move || {
    window.close();
});
```

## Best Practices

1. **Icon Consistency**: Use symbolic icons at 16px for all window controls
2. **Focus Tracking**: Keep the `focused` property synchronized with actual window focus
3. **State Management**: Update `window-state` when window state changes
4. **Title Text**: Keep title text short to avoid cluttering the header
5. **Draggable Area**: The entire central area is draggable for window movement
6. **Double-Click**: The double-click to maximize feature works on the draggable area
7. **Close Button**: The red hover effect on close button is intentional for warning

## Differences from App Bar

- **Header Bar**: Window-level controls, minimal height, focus on window management
- **App Bar**: Application-level actions, more prominent, focus on app functionality

Use both together for a complete COSMIC Desktop experience:
- Header Bar at the very top for window controls
- App Bar below for application title and actions
