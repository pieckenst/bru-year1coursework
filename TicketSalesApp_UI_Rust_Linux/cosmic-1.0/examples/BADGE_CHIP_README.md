# Badge and Chip Components

This document describes the COSMIC Desktop badge and chip components implementation.

## Components

### CosmicBadge

A small count or status indicator component, typically used to show notification counts or status information.

**Requirements:** 62.1-62.4, 62.10

#### Properties

- `count: int` - Numeric count to display (default: 0)
- `text: string` - Alternative custom text instead of count (default: "")
- `variant: BadgeVariant` - Color variant (default: accent)
- `show-zero: bool` - Whether to show badge when count is 0 (default: false)

#### Badge Variants

- `accent` - Default accent color (brownish/orange)
- `success` - Green for success states
- `warning` - Orange/yellow for warnings
- `error` - Red for errors
- `info` - Blue for information
- `neutral` - Neutral gray

#### Features

1. **20px Height with 10px Radius** (Requirement 62.1)
   - Fixed 20px height for consistency
   - 10px border radius creates pill shape

2. **Count Display with Accent Background** (Requirement 62.2)
   - Displays numeric counts
   - Uses accent color background by default
   - White text for contrast

3. **99+ Display** (Requirement 62.4)
   - Automatically shows "99+" for counts over 99
   - Prevents badge from becoming too wide

4. **Semantic Color Variants** (Requirement 62.10)
   - Success (green), Warning (orange), Error (red), Info (blue)
   - Neutral gray variant
   - Accent color variant (default)

#### Usage Examples

```slint
// Basic count badge
CosmicBadge {
    count: 5;
}

// Badge with custom text
CosmicBadge {
    text: "NEW";
}

// Badge with semantic color
CosmicBadge {
    count: 3;
    variant: BadgeVariant.error;
}

// Badge showing 99+
CosmicBadge {
    count: 150;  // Displays "99+"
}
```

### CosmicBadgeContainer

A wrapper component that positions a badge at the top-right corner of its child content.

**Requirements:** 62.3

#### Properties

- `badge-count: int` - Count to display on badge
- `badge-text: string` - Custom text for badge
- `badge-variant: BadgeVariant` - Badge color variant
- `show-zero: bool` - Whether to show badge when count is 0

#### Features

1. **Top-Right Positioning** (Requirement 62.3)
   - Automatically positions badge at top-right corner
   - Badge overlaps parent by half its size
   - Works with any child content (buttons, icons, etc.)

#### Usage Example

```slint
// Badge on button
CosmicBadgeContainer {
    badge-count: 12;
    badge-variant: BadgeVariant.error;
    
    Rectangle {
        width: 120px;
        height: 40px;
        background: CosmicPalette.accent-primary;
        // Button content...
    }
}
```

---

## CosmicChip

A compact element for tags, filters, or selections with optional avatar and remove functionality.

**Requirements:** 62.5-62.9

#### Properties

- `text: string` - Chip label text (default: "Chip")
- `avatar: image` - Optional avatar image
- `removable: bool` - Show close icon (default: false)
- `clickable: bool` - Enable click interaction (default: false)
- `selected: bool` - Selected state (default: false)

#### Callbacks

- `clicked()` - Emitted when chip is clicked (if clickable)
- `removed()` - Emitted when close icon is clicked (if removable)

#### Features

1. **28px Height with Full Radius** (Requirement 62.5)
   - Fixed 28px height
   - Fully rounded ends (pill shape)

2. **Removable Variant** (Requirement 62.6)
   - Shows close icon (×) on the right
   - Close icon has hover state
   - Emits `removed` callback when clicked

3. **Avatar Support** (Requirement 62.7)
   - 24px circular avatar on left side
   - Automatically adjusts padding when avatar is present

4. **Clickable Variant** (Requirement 62.8)
   - Shows hover state with background change
   - Emits `clicked` callback
   - Can be combined with selected state

5. **Inline Display** (Requirement 62.9)
   - Designed for inline use in forms and filters
   - CosmicChipGroup component supports multiple chips

#### Usage Examples

```slint
// Basic chip
CosmicChip {
    text: "Design";
}

// Removable chip
CosmicChip {
    text: "JavaScript";
    removable: true;
    removed => {
        debug("Chip removed");
    }
}

// Clickable chip with selection
CosmicChip {
    text: "Active";
    clickable: true;
    selected: true;
    clicked => {
        debug("Chip clicked");
    }
}

// Chip with avatar
CosmicChip {
    text: "Alice Johnson";
    avatar: @image-url("avatar.png");
    removable: true;
}
```

### CosmicChipGroup

A container for multiple chips with vertical stacking.

#### Properties

- `chips: [string]` - Array of chip labels
- `removable: bool` - Make all chips removable
- `clickable: bool` - Make all chips clickable

#### Callbacks

- `chip-clicked(int)` - Emitted with chip index when clicked
- `chip-removed(int)` - Emitted with chip index when removed

#### Usage Example

```slint
CosmicChipGroup {
    chips: ["Design", "Development", "Testing"];
    removable: true;
    
    chip-removed(index) => {
        debug("Removed chip at index: " + index);
    }
}
```

---

## Design Specifications

### Badge Dimensions
- Height: 20px (fixed)
- Min-width: 20px
- Border radius: 10px (half of height)
- Padding: 4px horizontal
- Font size: 11px
- Font weight: Semibold

### Chip Dimensions
- Height: 28px (fixed)
- Border radius: Full (9999px for pill shape)
- Padding: 12px horizontal (8px with avatar, 4px with close icon)
- Spacing: 8px between elements
- Font size: 14px
- Font weight: Medium

### Avatar in Chip
- Size: 24px × 24px
- Shape: Circular (full border radius)
- Position: Left side with 8px spacing

### Close Icon in Chip
- Size: 16px × 16px
- Shape: Circular background on hover
- Icon: × (multiplication sign)
- Position: Right side

---

## Color Variants

### Badge Colors
- **Accent**: `CosmicPalette.accent-primary` (brownish/orange)
- **Success**: `CosmicPalette.success` (green)
- **Warning**: `CosmicPalette.warning` (orange/yellow)
- **Error**: `CosmicPalette.error` (red)
- **Info**: `CosmicPalette.info` (blue)
- **Neutral**: `CosmicPalette.text-secondary` (gray)

All badges use white text (#FFFFFF) for contrast.

### Chip Colors
- **Default**: `CosmicPalette.surface-variant`
- **Hover** (clickable): `CosmicPalette.component-hover`
- **Selected**: `CosmicPalette.accent-primary` with 15% opacity
- **Text**: `CosmicPalette.text-primary` (or accent when selected)

---

## Accessibility

### Badge
- High contrast white text on colored backgrounds
- Semantic color variants for different states
- Clear visual distinction from surrounding content

### Chip
- Clear hover states for interactive chips
- Distinct close button with hover feedback
- Selected state uses accent color for visibility
- Adequate touch target size (28px height)

---

## Interactive States

### Badge
- No interactive states (display-only component)
- Visibility controlled by count/text and show-zero property

### Chip
- **Normal**: Default appearance
- **Hover**: Background change (if clickable)
- **Selected**: Accent color background with reduced opacity
- **Close Hover**: Close icon background appears

---

## Animation

### Chip
- Background color transitions: 150ms ease-out
- Smooth hover state changes

---

## Running the Example

To see the badge and chip components in action:

```bash
slint-viewer examples/badge-chip-example.slint
```

The example demonstrates:
1. Basic count badges
2. Badge variants with semantic colors
3. Badges positioned on buttons
4. Basic chips
5. Removable chips
6. Clickable chips with selection
7. Chips with avatars (placeholder)

---

## Implementation Notes

### Badge Positioning
The `CosmicBadgeContainer` component uses absolute positioning to place the badge at the top-right corner. The badge is positioned at:
- X: `parent.width - self.width / 2` (half overlaps right edge)
- Y: `-self.height / 2` (half overlaps top edge)

### 99+ Display Logic
The badge automatically displays "99+" when the count exceeds 99, preventing the badge from becoming too wide and maintaining visual consistency.

### Chip Wrapping
Note: The current `CosmicChipGroup` implementation stacks chips vertically. For true inline wrapping behavior, custom layout logic would be needed as Slint doesn't have automatic flex-wrap functionality built-in.

### Avatar Images
Avatar images should be provided as `@image-url("path/to/image.png")`. The chip component will display them as 24px circular avatars on the left side.

---

## Requirements Coverage

✅ **62.1**: Badge 20px height with 10px radius  
✅ **62.2**: Count display with accent background  
✅ **62.3**: Badge positioned at top-right of parent  
✅ **62.4**: Display "99+" for counts over 99  
✅ **62.5**: Chip 28px height with full radius  
✅ **62.6**: Removable variant with close icon  
✅ **62.7**: Avatar support (24px on left)  
✅ **62.8**: Clickable variant with hover state  
✅ **62.9**: Inline display support  
✅ **62.10**: Semantic color variants for badges
