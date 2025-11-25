# CosmicMenuButton Component

## Overview

The CosmicMenuButton component provides a comprehensive menu system following COSMIC Desktop design principles.

## Components

### CosmicMenuButton
A button that opens a dropdown menu when clicked.

Properties:
- text (string): Button label text
- icon (image): Optional icon
- menu-items ([MenuItem]): Array of menu items
- disabled (bool): Whether disabled
- menu-open (bool, in-out): Controls menu visibility

Callbacks:
- menu-item-clicked(int): Fired when item clicked

### CosmicMenuBarItem
A menu bar root item for horizontal menu bars.

### CosmicMenu
The dropdown menu container.

### CosmicMenuItem
Individual menu item component.

## MenuItem Structure

```
struct MenuItem {
    label: string,
    icon: image,
    shortcut: string,
    item-type: MenuItemType,
    checked: bool,
    disabled: bool,
    children: [MenuItem],
}
```

## MenuItemType Enum

- standard: Regular menu item
- checkbox: Checkbox menu item
- divider: Menu divider
- folder: Submenu

## Features Implemented

1. Button with dropdown arrow
2. Menu item component (36px height)
3. Keyboard shortcut display
4. Submenu support with arrow indicator
5. Checkbox menu items with checkmark
6. Menu dividers

## Requirements Validated

Requirement 21.1-21.7: Menu System
Requirement 42.1-42.8: Menu Bar and Dropdown Menus
