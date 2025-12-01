# COSMIC Navigation Components

This document describes the COSMIC Desktop navigation components: **CosmicDrawer** and **CosmicNavigationBar**.

## Components Overview

### CosmicDrawer
A vertical navigation sidebar with icon + text layout, perfect for application-level navigation.

**Requirements:** 11.1-11.7, 17.1-17.5

### CosmicNavigationBar
A horizontal navigation bar with support for icon-only and icon+text modes, ideal for view-level navigation.

**Requirements:** 41.1-41.7

## CosmicDrawer

### Features

- **Surface-variant background** - Slightly different from main content (2-5% variation)
- **Icon + text layout** - 20-24px icons with 12px spacing
- **44px item height** - With 8px border radius
- **Three states:**
  - Normal: transparent background
  - Hover: surface 50% opacity
  - Active: accent 15% opacity, accent text, medium weight
- **Section headers and dividers** - For organizing navigation groups
- **Full height** - With smooth transitions
- **Disabled items** - With 40% opacity

### Usage

#### Flat List Mode

```slint
import { CosmicDrawer, DrawerItem } from "../ui/components/drawer.slint";

drawer := CosmicDrawer {
    header-text: "Navigation";
    
    items: [
        {
            icon: @image-url("home.svg"),
            label: "Home",
            enabled: true,
        },
        {
            icon: @image-url("search.svg"),
            label: "Search",
            enabled: true,
        },
    ];
    
    selected-index: 0;
    
    item-clicked(index) => {
        debug("Item clicked:", index);
    }
}
```

#### Sectioned Mode

```slint
import { CosmicDrawer, DrawerSection } from "../ui/components/drawer.slint";

drawer := CosmicDrawer {
    header-text: "App Menu";
    
    sections: [
        {
            header: "Main",
            items: [
                { icon: @image-url("dashboard.svg"), label: "Dashboard", enabled: true },
                { icon: @image-url("projects.svg"), label: "Projects", enabled: true },
            ],
        },
        {
            header: "Tools",
            items: [
                { icon: @image-url("calendar.svg"), label: "Calendar", enabled: true },
                { icon: @image-url("messages.svg"), label: "Messages", enabled: true },
            ],
        },
    ];
    
    selected-section: 0;
    selected-item: 0;
    
    section-item-clicked(section-index, item-index) => {
        debug("Section item clicked:", section-index, item-index);
    }
}
```

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `header-text` | `string` | `""` | Optional header text at top of drawer |
| `items` | `[DrawerItem]` | `[]` | Flat list of navigation items |
| `sections` | `[DrawerSection]` | `[]` | Grouped navigation items with headers |
| `selected-index` | `int` | `-1` | Selected item index (for flat list) |
| `selected-section` | `int` | `-1` | Selected section index (for sections) |
| `selected-item` | `int` | `-1` | Selected item index within section |
| `disabled` | `bool` | `false` | Disable all interactions |

### Callbacks

| Callback | Parameters | Description |
|----------|------------|-------------|
| `item-clicked` | `int` | Fired when flat list item is clicked |
| `section-item-clicked` | `int, int` | Fired when section item is clicked (section-index, item-index) |

### Structures

#### DrawerItem
```slint
struct DrawerItem {
    icon: image,
    label: string,
    enabled: bool,
}
```

#### DrawerSection
```slint
struct DrawerSection {
    header: string,
    items: [DrawerItem],
}
```

## CosmicNavigationBar

### Features

- **32px button height** - Compact horizontal navigation
- **Two display modes:**
  - Icon + text: Shows icons with labels
  - Icon-only: Collapsed mode showing only icons
- **Selected state highlighting** - With accent color
- **Scrolling support** - For many items
- **Context menu support** - Right-click on items
- **Drag-drop reordering** - Visual feedback during drag
- **Disabled items** - With 40% opacity

### Usage

#### Icon + Text Mode

```slint
import { CosmicNavigationBar, NavigationBarItem, NavigationDisplayMode } from "../ui/components/navigation-bar.slint";

nav := CosmicNavigationBar {
    items: [
        { icon: @image-url("home.svg"), label: "Home", enabled: true },
        { icon: @image-url("search.svg"), label: "Search", enabled: true },
        { icon: @image-url("settings.svg"), label: "Settings", enabled: true },
    ];
    
    selected-index: 0;
    display-mode: NavigationDisplayMode.icon-text;
    
    item-clicked(index) => {
        debug("Item clicked:", index);
    }
}
```

#### Icon-Only Mode (Collapsed)

```slint
nav := CosmicNavigationBar {
    items: [
        { icon: @image-url("home.svg"), label: "Home", enabled: true },
        { icon: @image-url("search.svg"), label: "Search", enabled: true },
    ];
    
    selected-index: 0;
    display-mode: NavigationDisplayMode.icon-only;  // Collapsed mode
}
```

#### With Scrolling

```slint
nav := CosmicNavigationBar {
    items: [/* many items */];
    enable-scrolling: true;  // Enable horizontal scrolling
}
```

#### With Context Menu

```slint
nav := CosmicNavigationBar {
    items: [/* items */];
    enable-context-menu: true;
    
    item-context-menu(index) => {
        debug("Context menu for item:", index);
        // Show context menu here
    }
}
```

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `items` | `[NavigationBarItem]` | `[]` | Navigation items |
| `selected-index` | `int` | `-1` | Selected item index |
| `display-mode` | `NavigationDisplayMode` | `icon-text` | Display mode (icon-text or icon-only) |
| `enable-scrolling` | `bool` | `true` | Enable horizontal scrolling for many items |
| `disabled` | `bool` | `false` | Disable all interactions |
| `enable-drag-drop` | `bool` | `false` | Enable drag-drop reordering |
| `enable-context-menu` | `bool` | `false` | Enable right-click context menu |

### Callbacks

| Callback | Parameters | Description |
|----------|------------|-------------|
| `item-clicked` | `int` | Fired when item is clicked |
| `item-context-menu` | `int` | Fired when item is right-clicked |
| `item-drag-start` | `int` | Fired when drag operation starts |
| `item-drag-drop` | `int, int` | Fired when item is dropped (from-index, to-index) |

### Structures

#### NavigationBarItem
```slint
struct NavigationBarItem {
    icon: image,
    label: string,
    enabled: bool,
}
```

### Enums

#### NavigationDisplayMode
```slint
enum NavigationDisplayMode {
    icon-only,      // Show only icons (collapsed)
    icon-text,      // Show icons with text labels
}
```

## Design Guidelines

### When to Use CosmicDrawer

- **Application-level navigation** - Main sections of your app
- **Persistent navigation** - Always visible sidebar
- **Hierarchical navigation** - Multiple sections with headers
- **Desktop applications** - Where screen space allows

### When to Use CosmicNavigationBar

- **View-level navigation** - Switching between views in a section
- **Compact navigation** - Limited vertical space
- **Responsive layouts** - Can collapse to icon-only mode
- **Horizontal layouts** - Top or bottom navigation bars

### Combining Both Components

You can use both components together:
- **CosmicDrawer** for main application sections
- **CosmicNavigationBar** for sub-navigation within each section

Example:
```
┌─────────────┬──────────────────────────────────┐
│             │ [Home] [Search] [Favorites]      │ ← NavigationBar
│  Dashboard  ├──────────────────────────────────┤
│  Projects   │                                  │
│  Tasks      │         Content Area             │
│             │                                  │
│  Calendar   │                                  │
│  Messages   │                                  │
│             │                                  │
└─────────────┴──────────────────────────────────┘
    ↑ Drawer
```

## Accessibility

Both components support:
- **Keyboard navigation** - Tab to focus, Enter to activate
- **Focus indicators** - 2px accent color outline
- **Disabled states** - Clear visual indication with reduced opacity
- **High contrast** - Works in both light and dark modes

## Examples

See the example files:
- `drawer-example.slint` - Demonstrates both flat and sectioned drawer modes
- `navigation-bar-example.slint` - Shows all navigation bar modes and features

## Related Components

- **CosmicAppBar** - Top application bar with title and actions
- **CosmicHeaderBar** - Window header with controls
- **CosmicTabBar** - Tab-based navigation (coming soon)
- **CosmicBreadcrumb** - Breadcrumb navigation (coming soon)
